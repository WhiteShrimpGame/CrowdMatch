using System.Collections.Generic;

/// <summary>单元风格 / Tile style。</summary>
public enum CornerTileStyle
{
    /// <summary>描边：单元只画分界（线稿）。</summary>
    Outline = 0,

    /// <summary>填充：单元画实心块。</summary>
    Fill = 1,
}

/// <summary>状态模型 / State model。</summary>
public enum CornerTileStates
{
    /// <summary>单色：只有「激活 / 不激活」，相邻激活必然相连。</summary>
    Single = 0,

    /// <summary>多色：激活角带变体号，相邻异变体之间画分界。</summary>
    Multi = 1,
}

/// <summary>一个单元的取用结果：哪张图 / 预制体 + 旋转几个 90°。</summary>
public struct CornerTileRef
{
    /// <summary>单元号（= 贴图 / 预制体数组下标）。-1 = 该单元不生成。</summary>
    public int tileId;

    /// <summary>旋转序号 0..3，按 90° 为单位施加到模板 / 预制体上。</summary>
    public int rotation;

    /// <summary>是否要生成这个单元。</summary>
    public bool spawned;

    public CornerTileRef(int tileId, int rotation, bool spawned)
    {
        this.tileId = tileId;
        this.rotation = rotation;
        this.spawned = spawned;
    }
}

/// <summary>
/// 键 → 单元的查表 / Key → tile lookup table
///
/// 两张表（描边 / 填充）都是 256 项数组，按 <see cref="CornerTileKey"/> 的键直接索引。
/// 表是**数据**（由穷举 625 种四角配置生成并验证，不是手写 if-else），四种模式共用：
/// 单色模式只是可达键变少（16 个），用的是同一张表。
///
/// 旋转约定：rotation = r 表示把模板 / 预制体绕网格朝上轴转 r × 90°，
/// 具体为 localRotation = Quaternion.Euler(0, r * 90 + baseYaw, 0) * 模板原始旋转。
/// baseYaw 默认 180（与常见「贴图基准姿态」的约定一致），由 CornerTileFrame 暴露为可调字段。
///
/// 注：宿主组件若不暴露 baseYaw（例如让单元模板自己的 localRotation 承担基准姿态、代码里只乘
/// rotation × 90°），等价于 baseYaw = 0。注意基准角**只能在一处补**：模板与代码各补一个会互相抵消
/// （净 0，看着是对的），删掉其中一个就整体偏 180° —— 这是实际踩过的坑。
/// </summary>
public static class CornerTileTable
{
    // ===== 描边表：43 键（现存 A–I 那套单元；tileId = -1 表示不生成）=====
    // 布局：{ 键, tileId, rotation, ... }
    private static readonly int[] OutlineEntries =
    {
          0, -1, 0,   // 四角全不激活
         25,  0, 0,   // A：单角 / 三角（孤立角在 TL）
         35,  0, 1,
         58,  8, 0,   // I：相邻两角同组（上下排）
         59,  1, 0,   // B：相邻两角异组
         70,  0, 2,
         95,  2, 0,   // C：对角两角
        101,  8, 1,
        103,  1, 1,
        124,  0, 3,
        125,  7, 0,   // H
        126,  6, 2,   // G
        127,  3, 1,   // D
        140,  0, 3,
        149,  8, 1,
        157,  1, 3,
        175,  2, 1,
        182,  0, 2,
        183,  6, 1,
        190,  7, 3,
        191,  3, 0,
        202,  8, 0,
        206,  1, 2,
        211,  0, 1,
        215,  7, 2,
        219,  6, 0,
        223,  3, 3,
        233,  0, 0,
        235,  7, 1,
        237,  6, 3,
        239,  3, 2,
        240, -1, 0,   // 四角同组（区域内部）
        243,  0, 1,
        245,  8, 1,
        246,  0, 2,
        247,  5, 1,   // F
        249,  0, 0,
        250,  8, 0,
        251,  5, 0,
        252,  0, 3,
        253,  5, 3,
        254,  5, 2,
        255,  4, 0,   // E
    };

    // ===== 填充表：42 键（只有「四角全不激活」不生成）=====
    // tileId = 填充图形类 0..12，含义见 FillClassNames。
    //
    // 旋转号的方向**有世界依据**，不能随便定：+col → +X、+row → −Z（见 PixelGroup.GetLocalPosition），
    // 而绕 Y 转 +90° 把 (x,z) 映到 (z,−x)，于是交点四角偏移
    //     TL=(−½,+½)、TR=(+½,+½)、BR=(+½,−½)、BL=(−½,−½)
    //   ⇒ 转 +90° = 沿环序 TL→TR→BR→BL 前进一步。
    // 所以每条轨道必须满足不变量：**环序前进一步 → 旋转号 +1 (mod 4)**，
    // 其中「前进一步」= occ / bd 两个 4 位掩码各自循环左移 1 位。
    // 每个图形类的旋转 0 给**该类的基准姿态** —— 也就是美术那张基图实际画的朝向。
    // 绝大多数类里它恰好是轨道中键最小的那个配置，沿环序依次 1 / 2 / 3
    // （180° 对称的轨道只有 2 个键 → 0 / 1；全对称的只有 1 个键 → 恒为 0）。
    //
    // ⚠ 唯一例外：**类 4**（3 格同组 + 1 凹舌）的基图是「凹舌在 LT」= 键 0xE9，而**不是**键序最小的
    //   0x7C（凹舌在 LB），所以类 4 那四条旋转整体比「从最小键出发」少 1（即 −90°）。
    //   基准姿态是「美术的图长什么样」这一事实，推导不出来 —— 换图就要跟着改这四条。
    //
    // ⚠ 曾经踩过的坑：这张表起初把其中 9 个类（0,1,2,4,5,6,7,9,11）的轨道按**反方向**枚举，
    //   症状是「TL 单格对、TR 单格差 180°」这种一半对一半错 —— 因为旋转号差 2 才看得出来，
    //   而差 2 只出现在奇数旋转号上。**不要用一个全局 180° 翻转去修**：类 3 / 10 的轨道长 2，
    //   本来就是对的，全局翻一下反而把这两个弄错。改这张表后请重跑 handedness 断言。
    private static readonly int[] FillEntries =
    {
         25,  0, 0,
         35,  0, 1,
         58,  1, 0,
         59,  2, 0,
         70,  0, 2,
         95,  3, 0,
        101,  1, 1,
        103,  2, 1,
        124,  4, 3,
        125,  5, 0,
        126,  6, 0,
        127,  7, 0,
        140,  0, 3,
        149,  1, 3,
        157,  2, 3,
        175,  3, 1,
        182,  4, 2,
        183,  6, 3,
        190,  5, 3,
        191,  7, 3,
        202,  1, 2,
        206,  2, 2,
        211,  4, 1,
        215,  5, 2,
        219,  6, 2,
        223,  7, 2,
        233,  4, 0,
        235,  5, 1,
        237,  6, 1,
        239,  7, 1,
        240,  8, 0,
        243,  9, 0,
        245, 10, 0,
        246,  9, 1,
        247, 11, 0,
        249,  9, 3,
        250, 10, 1,
        251, 11, 3,
        252,  9, 2,
        253, 11, 2,
        254, 11, 1,
        255, 12, 0,
    };

    /// <summary>
    /// 填充模式的 13 个图形类（美术要画的就是这 13 张 / 个；单色模式只会用到 0/1/3/4/8）。
    /// 每张只画基准姿态，其余朝向由脚本旋转得到。
    ///
    /// **基准姿态 = 该类的旋转 0**，也就是「旋转 0 的那个键」所画的四角配置。逐类写在这里，
    /// 美术照着核对：如果某张图的特征角不在括号里写的位置，把它整体转 180°（或改画）即可。
    /// 角名用 LT / RT / LB / RB（左上 / 右上 / 左下 / 右下），对应代码里的 TL / TR / BL / BR。
    /// </summary>
    public static readonly string[] FillClassNames =
    {
        "1 格激活：一个凸圆角象限块（基准：实心块在 LT）",              // 0  (键 0x19)
        "相邻 2 格同组：半块，直角连成一体（基准：LT+RT 上排）",         // 1  (键 0x3A)
        "相邻 2 格异组：2 个凸圆角块（基准：LT 与 RT 各一块，下排空）",  // 2  (键 0x3B)
        "对角 2 格：2 个凸圆角块，中心断开（基准：LT 与 RB）",           // 3  (键 0x5F)
        "3 格同组：3 直角 + 1 凹舌（基准：凹舌在 LT；对应键 0xE9，不是键序最小的 0x7C）", // 4 (键 0xE9)
        "3 格，孤立角在顺向 tip：1 凸 + 2 直角（基准：孤立格在 LT，直角格 RT+RB，空 LB）", // 5 (键 0x7D)
        "3 格，孤立角在逆向 tip：1 凸 + 2 直角（基准：孤立格在 RB，直角格 LT+RT，空 LB）", // 6 (键 0x7E)
        "3 格，肘部与两侧都不同组：3 个凸圆角块（基准：LT/RT/RB，空 LB）",  // 7  (键 0x7F)
        "满、全同组：整块（基准：无任何分界）",                          // 8  (键 0xF0)
        "满、3+1：3 直角 + 1 凸（基准：孤立格在 RT，其余 LT/RB/LB 同组）", // 9 (键 0xF3)
        "满、2+2 相邻：整块（基准：左列 LT+LB 与右列 RT+RB 异色，竖分界）", // 10 (键 0xF5)
        "满、2+1+1（同色对相邻）：2 直角 + 2 凸（基准：LT+LB 同组，RT 与 RB 各自一色）", // 11 (键 0xF7)
        "满、4 组全异：4 个凸圆角块（基准：LT / RT / LB / RB 四色）",    // 12 (键 0xFF)
    };

    /// <summary>描边模式的单元名（tileId 0..8 = A..I），供编辑器预览与文档引用。</summary>
    public static readonly string[] OutlineTileNames =
    {
        "A 单角/三角", "B 相邻异组", "C 对角", "D 三格异组", "E 四角全异",
        "F 满且一对同组", "G 三格·顺向同色对", "H 三格·逆向同色对", "I 相邻同组",
    };

    /// <summary>旋转序号 r → 说明（供文档 / 编辑器预览）。</summary>
    public static readonly string[] RotationNames = { "0°", "90°", "180°", "270°" };

    private static readonly CornerTileRef[] OutlineTable = Build(OutlineEntries);
    private static readonly CornerTileRef[] FillTable = Build(FillEntries);

    private static CornerTileRef[] Build(int[] entries)
    {
        // 默认：不存在该键 → 不生成
        var table = new CornerTileRef[256];
        for (int i = 0; i < table.Length; i++)
            table[i] = new CornerTileRef(-1, 0, false);

        for (int i = 0; i + 2 < entries.Length; i += 3)
        {
            int key = entries[i];
            if (key < 0 || key >= 256)
                continue;
            int tile = entries[i + 1];
            int rot = entries[i + 2] & 3;
            table[key] = new CornerTileRef(tile, rot, tile >= 0);
        }
        return table;
    }

    /// <summary>查表：键 → 单元。键不存在时返回「不生成」。</summary>
    public static CornerTileRef Resolve(int key, CornerTileStyle style)
    {
        if (key < 0 || key >= 256)
            return new CornerTileRef(-1, 0, false);
        return style == CornerTileStyle.Fill ? FillTable[key] : OutlineTable[key];
    }

    /// <summary>查表（由四角身份直接算键再查）。</summary>
    public static CornerTileRef Resolve(int tl, int tr, int br, int bl,
                                        CornerTileStyle style, CornerTileStates states)
    {
        return Resolve(CornerTileKey.Make(tl, tr, br, bl, states), style);
    }

    /// <summary>
    /// 该状态模型下会出现的全部键 / All keys reachable in this state model。
    /// 单色 = 16 个（4 位占据的全部组合）；多色 = 43 个（穷举得到，按变体数 >= 3 计）。
    /// 供编辑器校验「有没有配漏单元」用。
    /// </summary>
    public static List<int> ReachableKeys(CornerTileStates states)
    {
        var keys = new List<int>();

        if (states == CornerTileStates.Single)
        {
            for (int occ = 0; occ < 16; occ++)
            {
                int key = KeyFromOccupancy(occ);
                if (!keys.Contains(key))
                    keys.Add(key);
            }
            return keys;
        }

        // 多色：穷举四角身份（4 种变体足够覆盖全部等价类）
        for (int a = -1; a < 4; a++)
            for (int b = -1; b < 4; b++)
                for (int c = -1; c < 4; c++)
                    for (int d = -1; d < 4; d++)
                    {
                        int key = CornerTileKey.Make(a, b, c, d, CornerTileStates.Multi);
                        if (!keys.Contains(key))
                            keys.Add(key);
                    }
        keys.Sort();
        return keys;
    }

    /// <summary>该键在当前风格 + 状态下会用到的单元号（去重、升序），供编辑器列「要配哪几张」。</summary>
    public static List<int> RequiredTiles(CornerTileStyle style, CornerTileStates states)
    {
        var ids = new List<int>();
        foreach (var key in ReachableKeys(states))
        {
            var r = Resolve(key, style);
            if (r.spawned && !ids.Contains(r.tileId))
                ids.Add(r.tileId);
        }
        ids.Sort();
        return ids;
    }

    // 由占据掩码直接推键（单色：bd = 相邻两角占据是否不同）
    private static int KeyFromOccupancy(int occ)
    {
        int bd = 0;
        for (int i = 0; i < 4; i++)
        {
            int a = (occ >> i) & 1;
            int b = (occ >> ((i + 1) & 3)) & 1;
            if (a != b) bd |= 1 << i;
        }
        return (occ << 4) | bd;
    }
}
