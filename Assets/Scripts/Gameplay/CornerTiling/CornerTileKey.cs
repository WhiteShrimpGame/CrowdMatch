/// <summary>
/// Corner tiling key / 四角拼接的「键」
///
/// 把交点四周 4 个角的状态压缩成 8 位：
///   · 高 4 位 occ = 占据掩码（bit i = 角 i 激活）
///   · 低 4 位 bd  = 分界掩码（bit i = 角 i 与角 (i+1)%4 身份不同 → 该内侧半边要画分界）
///
/// 角序固定 TL=0, TR=1, BR=2, BL=3（顺时针环序）。
/// 角身份：-1 = 不激活（无格 / 未暴露 / 障碍）；>=0 = 变体号（颜色）。
///
/// 【为什么是这两组掩码】
/// 出图的形状只取决于「哪些角激活」+「哪些内侧半边有分界」，与具体变体号无关。
/// 枚举 625 种四角配置验证：(occ, bd) → (单元号, 旋转) 零冲突，共 43 个可达键。
/// 四种模式（描边/填充 × 单色/多色）共用这一个键，差别只在查哪张表。
/// </summary>
public static class CornerTileKey
{
    /// <summary>不激活。</summary>
    public const int None = -1;

    /// <summary>由四角身份算键。角序 TL, TR, BR, BL。</summary>
    public static int Make(int tl, int tr, int br, int bl, CornerTileStates states)
    {
        int occ = 0, bd = 0;

        // 单色：所有激活角归为同一组（变体号不参与判定，相邻激活必然相连）
        int i0 = tl, i1 = tr, i2 = br, i3 = bl;
        if (states == CornerTileStates.Single)
        {
            if (i0 >= 0) i0 = 0;
            if (i1 >= 0) i1 = 0;
            if (i2 >= 0) i2 = 0;
            if (i3 >= 0) i3 = 0;
        }

        if (i0 >= 0) occ |= 1;
        if (i1 >= 0) occ |= 2;
        if (i2 >= 0) occ |= 4;
        if (i3 >= 0) occ |= 8;

        if (i0 != i1) bd |= 1;   // 上   内半边（TL|TR）
        if (i1 != i2) bd |= 2;   // 右   内半边（TR|BR）
        if (i2 != i3) bd |= 4;   // 下   内半边（BR|BL）
        if (i3 != i0) bd |= 8;   // 左   内半边（BL|TL）

        return (occ << 4) | bd;
    }

    /// <summary>由四角身份数组（长度 >= 4，顺序 TL, TR, BR, BL）算键。</summary>
    public static int Make(int[] ids, CornerTileStates states)
    {
        if (ids == null || ids.Length < 4)
            return None;
        return Make(ids[0], ids[1], ids[2], ids[3], states);
    }

    /// <summary>取占据掩码。</summary>
    public static int Occupancy(int key) { return (key >> 4) & 0xF; }

    /// <summary>取分界掩码。</summary>
    public static int Boundary(int key) { return key & 0xF; }

    /// <summary>该键是否合法（越界键返回 false）。</summary>
    public static bool IsValid(int key) { return key >= 0 && key < 256; }
}
