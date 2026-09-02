using System;

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

        /// <summary>PixelGroup 布局：尺寸 + 每格颜色（一维拍平，row-major，row 0 = 最前排）。</summary>
        [Serializable]
        public class PixelData
        {
            public int columns = 5;
            public int rows = 5;
            public int tailRows = 0;
            public float unitSize = 1f;
            public float spacingX = 0.1f;
            public float spacingZ = 0.1f;

            /// <summary>长度 = columns × (rows + tailRows)，index = row * columns + col。</summary>
            public int[] cells = new int[0];
        }

        /// <summary>ContainerGroup 布局：尺寸 + 稀疏容器列表（只存非空格）。</summary>
        [Serializable]
        public class ContainerData
        {
            public int columns = 5;
            public int rows = 3;
            public float xSpacing = 1.2f;
            public float zSpacing = 1.2f;

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
    }
}
