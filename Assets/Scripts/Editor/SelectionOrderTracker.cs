using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 按点选先后缓存 Scene 视图中选中的 GameObject 顺序（供墙体/管道创建工具读取端点顺序）。
    /// 每帧把缓存顺序列表与 Selection.gameObjects 做「集合」对账：
    /// 移除已不在选择中的对象，追加新出现的选择对象到尾部。
    /// 由于每次 Ctrl+点击通常只新增 1 个对象，追加顺序即点选顺序；单选/清空/整组替换都被集合差自动覆盖。
    /// 不依赖点击事件时序，因此不受「点击后选择更新晚于 delayCall」的竞态影响。
    /// </summary>
    [InitializeOnLoad]
    public static class SelectionOrderTracker
    {
        /// <summary>诊断日志开关（定位端点数量不符时保持开启）。</summary>
        public static bool LogEnabled = true;

        private static readonly List<GameObject> OrderedList = new List<GameObject>();

        static SelectionOrderTracker()
        {
            EditorApplication.update += OnUpdate;
        }

        /// <summary>缓存的点选顺序（GameObject 列表，头部 = 最先点选）。</summary>
        public static IReadOnlyList<GameObject> Ordered => OrderedList;

        private static void OnUpdate()
        {
            var sel = Selection.gameObjects;
            if (sel == null)
                return;

            // 1. 移除已不在选择中的对象（含已被销毁的假 null）
            bool removed = false;
            for (int i = OrderedList.Count - 1; i >= 0; i--)
            {
                var go = OrderedList[i];
                if (go == null || !Contains(sel, go))
                {
                    OrderedList.RemoveAt(i);
                    removed = true;
                }
            }

            // 2. 追加新出现的对象到尾部
            int added = 0;
            foreach (var go in sel)
            {
                if (go != null && !OrderedList.Contains(go))
                {
                    OrderedList.Add(go);
                    added++;
                }
            }

            //if (removed || added > 0)
            //{
            //    string reason = added > 0
            //        ? (removed ? "追加 " + added + " 个 / 移除若干" : "追加 " + added + " 个")
            //        : "移除若干";
            //    Log(reason);
            //}
        }

        private static bool Contains(GameObject[] arr, GameObject go)
        {
            int id = go.GetInstanceID();
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null && arr[i].GetInstanceID() == id)
                    return true;
            return false;
        }

        private static void Log(string reason)
        {
            if (!LogEnabled)
                return;
            var sb = new StringBuilder();
            sb.Append("[SelectionOrder] ").Append(reason).Append(" | 顺序列表 ").Append(OrderedList.Count).Append(" 个:");
            foreach (var go in OrderedList)
                sb.Append(" ").Append(go != null ? go.name : "null");
            Debug.Log(sb.ToString());
        }

        /// <summary>打印诊断：原始 Selection.gameObjects、缓存顺序列表、以及解析出的 PixelItem（标注 null / 重复）。</summary>
        public static void LogState()
        {
            if (!LogEnabled)
                return;

            var sel = Selection.gameObjects;
            var sb = new StringBuilder();
            sb.Append("[SelectionOrder] 诊断 | Selection.gameObjects=").Append(sel != null ? sel.Length : 0)
              .Append(" | Ordered=").Append(OrderedList.Count).Append("\n  Ordered:");
            foreach (var go in OrderedList)
                sb.Append(" ").Append(go != null ? go.name : "null");

            sb.Append("\n  Pixels:");
            var seen = new HashSet<PixelItem>();
            foreach (var go in OrderedList)
            {
                var p = go != null ? go.GetComponentInParent<PixelItem>() : null;
                if (p == null)
                {
                    sb.Append(" <无PixelItem:").Append(go != null ? go.name : "null").Append(">");
                    continue;
                }
                sb.Append(" ").Append(p.name);
                if (!seen.Add(p))
                    sb.Append("(重复)");
            }
            Debug.Log(sb.ToString());
        }
    }
}
