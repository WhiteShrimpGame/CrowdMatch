using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace CrowdMatch
{
    /// <summary>
    /// Project 窗口右键 Prefab → Add to SpawnPoolConfig（需 Inspector 锁定一个 SpawnPoolConfig）
    /// </summary>
    public static class SpawnPoolConfigContextMenu
    {
        private const int MenuPriority = 30;

        [MenuItem("Assets/Add to SpawnPoolConfig", false, MenuPriority)]
        private static void AddToSpawnPoolConfig()
        {
            SpawnPoolConfig config = FindLockedSpawnPoolConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("SpawnPool",
                    "未找到锁定的 SpawnPoolConfig。\n请先在 Inspector 中选中一个 SpawnPoolConfig 资产并将其锁定（点击右上角小锁图标）。",
                    "OK");
                return;
            }

            List<GameObject> prefabs = GetSelectedPrefabs();
            if (prefabs.Count == 0)
            {
                Debug.LogWarning("[SpawnPool] No valid prefabs selected.");
                return;
            }

            Undo.RecordObject(config, "Add prefabs to SpawnPoolConfig");

            List<SpawnPoolItem> items = config.items != null
                ? new List<SpawnPoolItem>(config.items)
                : new List<SpawnPoolItem>();

            int added = 0;
            foreach (GameObject prefab in prefabs)
            {
                // 去重：同 prefab 不重复添加
                if (items.Exists(it => it.item == prefab))
                {
                    Debug.Log($"[SpawnPool] \"{prefab.name}\" already in config, skipped.");
                    continue;
                }

                items.Add(new SpawnPoolItem
                {
                    tag = prefab.name,
                    item = prefab,
                    preloadCount = 0
                });
                added++;
            }

            config.items = items.ToArray();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SpawnPool] Added {added} prefab(s) to {config.name}.");
        }

        [MenuItem("Assets/Add to SpawnPoolConfig", true)]
        private static bool ValidateAddToSpawnPoolConfig()
        {
            // 必须有锁定的 SpawnPoolConfig 才显示菜单
            if (FindLockedSpawnPoolConfig() == null)
                return false;

            // 至少选中一个 Prefab
            return GetSelectedPrefabs().Count > 0;
        }

        // ---- helpers ----

        /// <summary>找到当前锁定 Inspector 中正在检视的 SpawnPoolConfig。</summary>
        private static SpawnPoolConfig FindLockedSpawnPoolConfig()
        {
            var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return null;

            var allInspectors = Resources.FindObjectsOfTypeAll(inspectorType);
            var isLockedProp = inspectorType.GetProperty("isLocked",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var trackerField = inspectorType.GetField("m_Tracker",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (isLockedProp == null || trackerField == null) return null;

            foreach (var inspector in allInspectors)
            {
                bool locked = (bool)isLockedProp.GetValue(inspector);
                if (!locked) continue;

                var tracker = trackerField.GetValue(inspector) as ActiveEditorTracker;
                if (tracker == null) continue;

                foreach (var editor in tracker.activeEditors)
                {
                    if (editor != null && editor.target is SpawnPoolConfig config)
                        return config;
                }
            }

            return null;
        }

        /// <summary>从当前选中项中筛选出 Prefab 的 GameObject 列表。</summary>
        private static List<GameObject> GetSelectedPrefabs()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Object obj in Selection.objects)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    result.Add(go);
                }
            }
            return result;
        }
    }
}
