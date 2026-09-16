using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace CrowdMatch
{
    /// <summary>
    /// 轻量级对象池：按 tag 索引，支持预加载、按需扩容、定时回收、批量清理。
    /// 非 MonoBehaviour，需由宿主（GameManager）持有并显式调用 GC。
    /// </summary>
    public class SpawnPool
    {
        private Dictionary<string, GameObject> poolTempDic;
        private Dictionary<string, Queue<GameObject>> poolDic;
        private Dictionary<GameObject, string> usingObjDic;
        private SpawnPoolConfig config;
        private Transform root;

        private bool _isGCInProgress = false;

        public void Init(SpawnPoolConfig config, Transform root)
        {
            this.config = config;
            this.root = root;

            poolTempDic = new Dictionary<string, GameObject>();
            poolDic = new Dictionary<string, Queue<GameObject>>();
            usingObjDic = new Dictionary<GameObject, string>();

#if UNITY_EDITOR
            // Validate prefab references at edit time
            foreach (var item in config.items)
            {
                if (item.item != null && item.item.scene.IsValid())
                {
                    Debug.LogError($"[SpawnPool] '{item.tag}' references a scene object instead of a prefab. Please fix the config.");
                }
            }
#endif

            for (int i = 0; i < config.items.Length; i++)
            {
                var item = config.items[i];
                poolTempDic.Add(item.tag, item.item);

                var queue = new Queue<GameObject>();

                for (int j = 0; j < item.preloadCount; j++)
                {
                    GameObject go = Object.Instantiate(item.item, root);
                    go.SetActive(false);
                    queue.Enqueue(go);
                }

                poolDic.Add(item.tag, queue);
            }
        }

        public GameObject Spawn(string tag, Transform parent = null)
        {
            if (!poolDic.ContainsKey(tag))
            {
                Debug.LogErrorFormat("Pool Dict not contains tag: {0}", tag);
                return null;
            }

            if (parent == null)
            {
                parent = root;
            }

            GameObject go;

            if (poolDic[tag].Count == 0)
            {
                go = Object.Instantiate(poolTempDic[tag], parent);
            }
            else
            {
                go = poolDic[tag].Dequeue();

                // Validate the pooled object hasn't been destroyed externally
                if (go == null)
                {
                    Debug.LogError($"[SpawnPool] Pooled object (tag: {tag}) was destroyed externally. Re-instantiating.");
                    go = Object.Instantiate(poolTempDic[tag], parent);
                }
                else
                {
                    go.transform.parent = parent;
                }
            }

            go.SetActive(true);

            usingObjDic.Add(go, tag);
            return go;
        }

        public void Despawn(GameObject go, bool isTry = false)
        {
            if (!usingObjDic.ContainsKey(go))
            {
                if (!isTry)
                {
                    Debug.LogErrorFormat("GameObject {0} not spawn from Pool", go.name);
                }
                return;
            }

            go.SetActive(false);
            go.transform.parent = root;

            poolDic[usingObjDic[go]].Enqueue(go);
            usingObjDic.Remove(go);
        }

        public GameObject SpawnDuration(string tag, float timeDuration, Transform parent = null)
        {
            var go = Spawn(tag, parent);
            DOVirtual.DelayedCall(timeDuration, () =>
            {
                Despawn(go, true);
            });
            return go;
        }

        public void GC(bool clear = false)
        {
            if (_isGCInProgress)
            {
                Debug.LogWarning("[SpawnPool] GC re-entry blocked. Check if OnDisable calls Despawn on pooled objects.");
                return;
            }
            _isGCInProgress = true;

            if (clear)
            {
                var iter = usingObjDic.GetEnumerator();

                while (iter.MoveNext())
                {
                    var go = iter.Current.Key;

                    if (go == null)
                    {
                        continue;
                    }

                    go.SetActive(false);
                    go.transform.parent = root;

                    poolDic[iter.Current.Value].Enqueue(go);
                }

                usingObjDic.Clear();
            }

            for (int i = 0; i < config.items.Length; i++)
            {
                var item = config.items[i];
                while (poolDic[item.tag].Count > item.preloadCount)
                {
                    var go = poolDic[item.tag].Dequeue();
                    Object.DestroyImmediate(go);
                }
            }

            _isGCInProgress = false;
        }
    }
}
