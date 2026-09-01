using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 多段圆弧轨迹控制器（首尾相接）。
    /// 将多个 ArcPath 分段串联成一条连续轨迹，支持按全局长度采样位置与欧拉角。
    /// Scene 视图预览与 Inspector 测试控制由 ArcPathEditor（Editor/ 目录）提供。
    ///
    /// Multi-segment arc path controller (segments joined end-to-end).
    /// Chains multiple ArcPath segments into one continuous path, sampled by
    /// global length for position and euler angles. Scene-view preview and Inspector
    /// test controls are provided by ArcPathEditor (Editor/ folder).
    /// </summary>
    public class ArcPathController : MonoBehaviour
    {
        [Header("轨迹配置 / Path config")]
        [Tooltip("分段圆弧轨迹列表 / List of arc path segments")]
        public List<ArcPath> arcPaths = new List<ArcPath>();

        [Header("预览配置 / Preview config")]
        [Tooltip("预览线段精度（越高越平滑）/ Preview segment resolution (higher = smoother)")]
        public int previewSegments = 30;
        [Tooltip("预览线颜色 / Preview line color")]
        public Color previewColor = Color.cyan;

        [Header("测试运动 / Test motion")]
        public Transform testObject;
        [Range(0, 1)] public float previewPosition = 0;

        /// <summary>
        /// 初始化所有轨迹（自动衔接首尾）。
        /// 必须在采样前调用。
        /// Initializes all segments, auto-linking each one to the previous end.
        /// Must be called before sampling.
        /// </summary>
        [ContextMenu("初始化轨迹 / Initialize paths")]
        public void InitializePaths()
        {
            if (arcPaths.Count == 0) return;

            // 初始化第一段 / initialize first segment
            arcPaths[0].Initialize();

            // 后续段自动衔接前一段的终点 / later segments continue from previous end
            for (int i = 1; i < arcPaths.Count; i++)
            {
                ArcPath prevPath = arcPaths[i - 1];
                ArcPath currPath = arcPaths[i];

                // 自动设置当前段的起始参数 = 前一段的终点参数
                // Auto-set current segment start = previous segment end
                currPath.startPosition = prevPath.GetEndPosition();
                currPath.startEulerAngles = prevPath.GetEndEulerAngles();
                currPath.startTangent = prevPath.GetEndForward();
                currPath.startNormal = prevPath.startNormal; // 法向保持一致 / normal stays consistent

                currPath.Initialize();
            }
        }

        /// <summary>
        /// 获取整个轨迹总长度。
        /// Returns the total length of all segments combined.
        /// </summary>
        public float GetTotalPathLength()
        {
            float total = 0;
            foreach (var path in arcPaths) total += path.GetTotalLength();
            return total;
        }

        /// <summary>
        /// 全局采样：根据总长度获取位置。
        /// Returns the position at the given global length.
        /// </summary>
        public Vector3 GetGlobalPosition(float totalLength)
        {
            if (arcPaths.Count == 0) return Vector3.zero;

            totalLength = Mathf.Clamp(totalLength, 0, GetTotalPathLength());
            float currentLength = 0;

            foreach (var path in arcPaths)
            {
                float pathLength = path.GetTotalLength();
                if (totalLength <= currentLength + pathLength)
                {
                    return path.GetPositionByLength(totalLength - currentLength);
                }
                currentLength += pathLength;
            }

            return arcPaths[arcPaths.Count - 1].GetEndPosition();
        }

        /// <summary>
        /// 全局采样：根据总长度获取欧拉角。
        /// Returns the euler angles at the given global length.
        /// </summary>
        public Vector3 GetGlobalEulerAngles(float totalLength)
        {
            if (arcPaths.Count == 0) return Vector3.zero;

            totalLength = Mathf.Clamp(totalLength, 0, GetTotalPathLength());
            float currentLength = 0;

            foreach (var path in arcPaths)
            {
                float pathLength = path.GetTotalLength();
                if (totalLength <= currentLength + pathLength)
                {
                    return path.GetEulerAnglesByLength(totalLength - currentLength);
                }
                currentLength += pathLength;
            }

            return arcPaths[arcPaths.Count - 1].GetEndEulerAngles();
        }
    }
}
