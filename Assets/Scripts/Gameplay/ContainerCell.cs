namespace CrowdMatch
{
    /// <summary>
    /// 单个容器格的**数据层**状态。
    ///
    /// 运行模式下容器不再开局全部实例化（只实例化前 <c>ContainerGroup.WindowRows</c> 排），
    /// 于是「还没实例化的深排车」也必须有一份完整状态存在——复活路径（<c>ContainerGroup.MatchPixelsToCars</c>）
    /// 会直接写它，等这辆车补位滚进视窗被实例化时，再由 <see cref="ContainerItem.ApplyCell"/> 一次性水合到车上。
    ///
    /// **权威来源规则**（无镜像、无双重写入，所以两者不可能漂移）：
    /// · 该格**有实例**（视窗内 / 编辑模式全量）→ 一律读写 <see cref="ContainerItem"/>，本结构只是旁听；
    /// · 该格**无实例**（懒实例化下的深排）→ 本结构是唯一权威。
    /// <c>ContainerGroup</c> 的取值器（<c>CarAt</c> / <c>EmptyAt</c> / <c>ColorAt</c> / <c>LidOpenedAt</c>）
    /// 就是这条规则的唯一落点，读者一律走它们。
    ///
    /// 存进数组、按 <c>row * columns + col</c> 索引（与像素数据的 cells 同一约定），每关一次性分配、零 GC。
    /// </summary>
    public struct ContainerCell
    {
        /// <summary>网格列坐标（X 方向）</summary>
        public int col;

        /// <summary>网格行坐标（Z 方向），0 为最前排</summary>
        public int row;

        /// <summary>
        /// **原始格坐标** = 关卡数据里这一格的出生位置（<see cref="col"/> / <see cref="row"/> 会随补位前移而改，
        /// 这两个不会）。用途：车实例化时按它命名，于是名字是这辆车的稳定身份——
        /// 补位前移、原地销毁后的邻车前移都不影响，查 Hierarchy 时能直接对回关卡数据。
        /// </summary>
        public int originCol;

        /// <summary>原始格行坐标，见 <see cref="originCol"/>。</summary>
        public int originRow;

        /// <summary>该格是否还有车（false = 已出库 / 已原地销毁 / 从来是空格）</summary>
        public bool occupied;

        /// <summary>车接受的颜色 ID</summary>
        public int colorId;

        /// <summary>总容量</summary>
        public int capacity;

        /// <summary>
        /// 剩余容量。**只在没有实例时才有意义**（有实例时一律读实例）——
        /// 复活路径命中未实例化的深排车时会把账记在这里。
        /// </summary>
        public int remaining;

        /// <summary>绳子组 id（0 = 未连接）</summary>
        public int ropeGroupId;

        /// <summary>是否问号车</summary>
        public bool isQuestion;

        /// <summary>盖子是否已打开（深排车开盖时记在这里；有实例时读实例）</summary>
        public bool lidOpened;

        /// <summary>问号车是否已揭晓（含义与 <see cref="ContainerItem.revealed"/> 一致）</summary>
        public bool revealed;
    }
}
