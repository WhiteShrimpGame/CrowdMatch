namespace CrowdMatch
{
    /// <summary>
    /// 编辑器菜单「CrowdMatch」的排序常量 —— **顺序只写在这一个文件里**，以后调整不必翻十几个脚本。
    ///
    /// Unity 的三条规则决定了这套数字：
    /// · 同一层内按 priority 升序排；**不写 priority 就退化成字母序**（原来 26 项看着乱就是这个原因）；
    /// · **相邻两项差 ≥ 11 会自动画一条分隔线**，所以段与段之间靠「空档」分开，不用写占位项；
    /// · **子菜单在父菜单里的位置由它的子项决定**（Unity 取子项 priority 的最小值 / 首个注册值），
    ///   所以每个分组必须占一整段互不重叠的区段 —— 否则某个分组的子项跑到别的分组区段里，分组顺序就会漂。
    ///
    /// ⚠️ 菜单名里**不能出现半角 `/`** —— Unity 按 `/` 切路径，`更多工具（低频 / 一次性）` 会当场多出一层子菜单。
    /// 括号里要分隔用「、」或全角「／」。
    ///
    /// 最终结构（`CrowdMatch/`，括号内是 priority）：
    /// <code>
    /// 像素颜色画布(1) · 容器拖移画布(2)                                  高频画布，最上
    /// ────────────────────────────
    /// 导出关卡 JSON(20) · 从 JSON 导入配置到当前场景(21)
    /// 清空当前场景两个 Group 的子物体(22) · 按 Record 重排容器(23)        高频关卡操作，不分组
    /// ────────────────────────────
    /// 创建（场景视图 · 选中 Pixel）(100)  → 墙体/管道/倍乘门/升降台 | 箱子/木箱 | 冰▸
    /// Pixel 工具(300)                    → 填充全部空格/填充矩形范围空格 | 清除选中矩形范围 Pixel
    /// 关卡工具(700)                      → 导出关卡 JSON（锁定） | 替换关卡颜色/批量图片转关卡
    /// ────────────────────────────
    /// 更多工具(900)                      → Create Color Config/从材质主色生成字色与描边色
    ///                                      | 生成升降台预制体/生成挖洞地面材质球/生成圆片 Mesh
    /// </code>
    /// </summary>
    internal static class MenuPriority
    {
        // ===== 顶层：直接挂在 CrowdMatch/ 下的项 =====

        /// <summary>高频画布：像素颜色画布、容器拖移画布（+0 / +1）。</summary>
        public const int Canvas = 1;

        /// <summary>高频关卡操作，直接放顶层不分组：导出、导入、清空、按 Record 重排（+0..+3）。</summary>
        public const int Level = 20;

        // ===== 分组：每个分组一个 100 宽的区段，子项用「区段基址 + 段内偏移」=====

        /// <summary>「创建（场景视图 · 选中 Pixel）」区段。</summary>
        public const int Create = 100;

        /// <summary>「Pixel 工具」区段。</summary>
        public const int Pixel = 300;

        /// <summary>「关卡工具」区段。</summary>
        public const int LevelTools = 700;

        /// <summary>「更多工具」区段 —— 低频 / 一次性，与上面隔出分隔线，压到最后。</summary>
        public const int More = 900;

        // ===== 区段内的三段（相邻两段之间自动出分隔线）=====

        /// <summary>段 1：主项（+0..+9）。</summary>
        public const int Seg1 = 10;

        /// <summary>段 2：次要 / 破坏性项（+0..+9）。</summary>
        public const int Seg2 = 30;

        /// <summary>段 3：再下一段（+0..+9）。</summary>
        public const int Seg3 = 50;
    }
}
