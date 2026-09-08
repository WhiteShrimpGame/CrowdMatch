using System;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 单个关卡的序列化数据（JSON）。包含 PixelGroup 与 ContainerGroup 两部分的布局，
    /// 供编辑器导出与运行时加载共用。
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int version = 1;
        public PixelData pixel = new PixelData();
        public ContainerData container = new ContainerData();
        public WallData[] walls = new WallData[0];

        /// <summary>PixelGroup 布局：尺寸 + 每格颜色（一维拍平，row-major，row 0 = 最前排）。</summary>
        [Serializable]
        public class PixelData
        {
            public int columns = 5;
            public int rows = 5;
            public int tailRows = 0;
            public float unitSize = 1f;

            /// <summary>长度 = columns × (rows + tailRows)，index = row * columns + col。</summary>
            public int[] cells = new int[0];
        }

        /// <summary>ContainerGroup 布局：尺寸 + 稀疏容器列表（只存非空格）。</summary>
        [Serializable]
        public class ContainerData
        {
            public int columns = 5;
            public int rows = 3;

            /// <summary>是否锁定容器排列：true 时运行时跳过洗牌，保持 JSON 中记录的摆放位置。</summary>
            public bool lockContainer = false;

            public ContainerItemData[] items = new ContainerItemData[0];
        }

        [Serializable]
        public class ContainerItemData
        {
            public int x;
            public int y;
            public int colorId;
            public int capacity;
        }

        /// <summary>一段墙体：端点序列（网格坐标，x = 列 col，y = 行 row），相邻两点构成一段，每段平行于 X 或 Z 轴。</summary>
        [Serializable]
        public class WallData
        {
            public Vector2[] points = new Vector2[0];
        }
    }

    /// <summary>
    /// 运行（Play）模式关卡初始化状态缓存：由 GameController 在初始化关卡（洗牌后）写入，
    /// 供编辑器导出工具在 Play 模式下导出「锁定」的初始关卡布局。运行时安全，无 UnityEditor 依赖。
    /// </summary>
    public static class LevelDataCache
    {
        /// <summary>最近一次关卡初始化（洗牌后）的 LevelData 快照；仅在编辑器 Play 模式下有意义。</summary>
        public static LevelData LastInitData;
    }
}
