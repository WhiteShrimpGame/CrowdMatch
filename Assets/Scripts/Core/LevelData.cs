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
        public PipeData[] pipes = new PipeData[0];
        public BoxData[] boxes = new BoxData[0];
        public ElevatorData[] elevators = new ElevatorData[0];

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

            /// <summary>长度 = columns × (rows + tailRows)，index = row * columns + col；true = 该格为问号 Pixel（隐藏真实颜色）。</summary>
            public bool[] questionCells = new bool[0];
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

            /// <summary>true = 问号车（开盖揭晓前隐藏真实颜色）。旧 JSON 无此字段时为 false。</summary>
            public bool question;
        }

        /// <summary>一段墙体：端点序列（网格坐标，x = 列 col，y = 行 row），相邻两点构成一段，每段平行于 X 或 Z 轴。</summary>
        [Serializable]
        public class WallData
        {
            public Vector2[] points = new Vector2[0];
        }

        /// <summary>
        /// 一个管道：轨迹端点序列 points（网格坐标，points[0] = 管道格，points[1..] = 轨道格），
        /// 以及每波生成颜色 colors（第 i 波用 colors[i]）。
        /// </summary>
        [Serializable]
        public class PipeData
        {
            public Vector2[] points = new Vector2[0];
            public int[] colors = new int[0];
        }

        /// <summary>
        /// 一个箱子：矩形区域（左上 + 右下）+ 容量 + 隐藏 Pixel 颜色 + 行为开关。
        /// 箱子区域的格子在 pixel.cells 里写 -1（占位/空像素），开箱后的 Pixel 颜色由 colorIds 提供。
        /// </summary>
        [Serializable]
        public class BoxData
        {
            public int colMin, rowMin, colMax, rowMax;
            public int capacity;
            public int[] colorIds = new int[0];
            public float jumpStartInterval = 0.1f;
            public float jumpSpawnYOffset = 0.5f;
        }

        /// <summary>
        /// 一个地面升降台：矩形区域（左上 + 右下）+ 若干组稀疏像素（全部在地下竖井里等待升起）。
        /// 区域内的格子是普通地上像素（触发升起的「上方层」），在 pixel.cells 里正常记录；
        /// 升降台自身的像素全部在地下，由 groups 提供（第 0 组是第一个升起的组）。
        /// </summary>
        [Serializable]
        public class ElevatorData
        {
            public int colMin, rowMin, colMax, rowMax;
            public ElevatorGroupData[] groups = new ElevatorGroupData[0];

            /// <summary>地面高度（PixelGroup 本地 Y）；<=-500 表示自动（像素底部 -unitSize/2）。</summary>
            public float groundY = -999f;

            /// <summary>竖井深度（世界单位）；<=0 表示自动（unitSize × 1.5）。</summary>
            public float pitDepth = 0f;
        }

        /// <summary>升降台的一组像素：cells = 三元组拍平 [col,row,color, col,row,color, ...]。</summary>
        [Serializable]
        public class ElevatorGroupData
        {
            public int[] cells = new int[0];
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
