using System;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 可序列化的圆弧/直线轨迹类。
    /// 支持直线与曲线（圆弧）两种模式，统一按「长度」采样位置与欧拉角。
    ///
    /// Serializable arc/straight path segment. Supports both straight-line and
    /// curve (arc) modes, sampled uniformly by length for position and euler angles.
    /// </summary>
    [Serializable]
    public class ArcPath
    {
        [Header("轨迹类型 / Path type")]
        [Tooltip("true=直线，false=曲线（圆弧）/ true=straight, false=curve (arc)")]
        public bool isStraightLine;

        [Header("曲线参数（仅曲线模式生效）/ Curve params (curve mode only)")]
        public float radius = 5f;
        public float totalAngle = 90f;

        [Header("直线参数（仅直线模式生效）/ Straight params (straight mode only)")]
        public float straightLength = 10f;

        [Header("起始姿态定义 / Start pose")]
        public Vector3 startPosition;
        public Vector3 startEulerAngles;
        public Vector3 startTangent = Vector3.forward;
        public Vector3 startNormal = Vector3.up;

        // 缓存标准化向量 / Cached normalized vectors
        private Vector3 _normalizedTangent;
        private Vector3 _normalizedNormal;
        private float _totalRadian;
        private float _totalLength; // 统一总长度 / unified total length

        /// <summary>
        /// 初始化（采样前必须调用）。
        /// Initializes the segment. Must be called before any sampling.
        /// </summary>
        public void Initialize()
        {
            _normalizedTangent = startTangent.normalized;
            _normalizedNormal = startNormal.normalized;

            if (isStraightLine)
            {
                // 直线：直接使用直线长度 / straight: use straight length directly
                _totalLength = Mathf.Max(0, straightLength);
            }
            else
            {
                // 曲线：使用圆弧公式 / curve: arc formula
                _totalRadian = totalAngle * Mathf.Deg2Rad;
                _totalLength = Mathf.Abs(radius * _totalRadian);
            }
        }

        /// <summary>
        /// 获取轨迹段总长度（统一接口）。
        /// Returns the segment's total length.
        /// </summary>
        public float GetTotalLength()
        {
            return _totalLength;
        }

        /// <summary>
        /// 根据长度获取位置（直线/曲线自动适配）。
        /// Returns the position at the given length (auto adapts straight/curve).
        /// </summary>
        public Vector3 GetPositionByLength(float length)
        {
            length = Mathf.Clamp(length, 0, _totalLength);

            if (isStraightLine)
            {
                // 直线：沿切向匀速延伸 / straight: extend along tangent
                return startPosition + _normalizedTangent * length;
            }

            // 曲线：圆弧计算 / curve: arc computation
            if (_totalLength <= 0) return startPosition;
            float currentRadian = (length / _totalLength) * _totalRadian;

            Vector3 currentDir = Quaternion.AngleAxis(currentRadian * Mathf.Rad2Deg, _normalizedNormal) * _normalizedTangent;
            Vector3 rightDir = Vector3.Cross(_normalizedTangent, _normalizedNormal) * Mathf.Sign(_totalRadian);
            Vector3 center = startPosition - rightDir * radius;
            return center + Vector3.Cross(currentDir, _normalizedNormal) * radius * Mathf.Sign(_totalRadian);
        }

        /// <summary>
        /// 根据长度获取欧拉角。
        /// Returns the euler angles at the given length.
        /// </summary>
        public Vector3 GetEulerAnglesByLength(float length)
        {
            length = Mathf.Clamp(length, 0, _totalLength);

            if (isStraightLine)
            {
                // 直线：角度保持起始角度不变 / straight: keep start euler angles
                return startEulerAngles;
            }

            // 曲线：绕法向旋转 / curve: rotate around normal
            if (_totalLength <= 0) return startEulerAngles;
            float currentRadian = (length / _totalLength) * _totalRadian;
            Quaternion rotation = Quaternion.AngleAxis(currentRadian * Mathf.Rad2Deg, _normalizedNormal);
            return (rotation * Quaternion.Euler(startEulerAngles)).eulerAngles;
        }

        /// <summary>
        /// 获取终点前方向量（切向）。
        /// Returns the end forward (tangent) direction.
        /// </summary>
        public Vector3 GetEndForward()
        {
            if (isStraightLine)
            {
                return _normalizedTangent; // 直线方向不变 / straight direction unchanged
            }

            Vector3 endDir = Quaternion.AngleAxis(totalAngle, _normalizedNormal) * _normalizedTangent;
            return endDir.normalized;
        }

        /// <summary>
        /// 获取终点欧拉角。
        /// Returns the end euler angles.
        /// </summary>
        public Vector3 GetEndEulerAngles()
        {
            return GetEulerAnglesByLength(_totalLength);
        }

        /// <summary>
        /// 获取终点位置。
        /// Returns the end position.
        /// </summary>
        public Vector3 GetEndPosition()
        {
            return GetPositionByLength(_totalLength);
        }
    }
}
