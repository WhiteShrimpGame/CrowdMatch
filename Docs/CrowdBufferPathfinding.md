# CrowdBufferZone 寻路逻辑详解（提取阶段）

> 状态：**与当前代码一致**。本文只聚焦「提取阶段」——匹配像素在网格内寻路离开、移向入口边、切入物理阶段之前
> 的全部逻辑。物理过闸、释放、传送带不在本文（见 [`CrowdBufferDesign.md`](CrowdBufferDesign.md) 与
> [`ConveyorBeltDesign.md`](ConveyorBeltDesign.md)）。
>
> 代码位置：`Assets/Scripts/Gameplay/CrowdBufferZone.cs`；入口 `EnterBatch`，驱动 `StepExtracting`（每帧 `Update`）。

---

## 1. 定位与范围

一个匹配像素的完整生命周期：

```
InGrid ──匹配──▶ Matched ──(EnterBatch)──▶ Extracting（本文范围）──▶ Physical ──▶ Released ──▶ Arrived
```

本文只覆盖 **Extracting** 段，即从 `GameController.ResolveMatch` 调 `EnterBatch` 起，到像素调用
`EnterPhysical` 附加刚体为止。这段再细分为两个子阶段（同一批像素可同时处于不同子阶段）：

| 子阶段 | 状态标志 | 含义 | 驱动方式 |
|---|---|---|---|
| 网格内寻路 | `moving`（格子间动画）或等待中 | 仍在 `_extractGroup` 网格里，逐格朝更小 `dist`（距离场）走向入口边 | 离散 tick + 连续插值 |
| 移向入口边 | `exiting = true` | 已离开网格，连续匀速移向入口边落位点 | 每帧连续移动 + 同列排队 |

---

## 2. 数据结构

### 2.1 单个像素状态 `ExtractState`（私有内部类）

| 字段 | 类型 | 含义 |
|---|---|---|
| `item` | `PixelItem` | 像素本体 |
| `col, row` | `int` | 当前逻辑格（本 tick 结束后的所在格，初始 = `item.gridX/gridZ`；退出后保持最后所在格不再更新） |
| `waitCount` | `int` | 被挡住的等待次数（公平性：等待越多，下次 sweep 越优先） |
| `moving` | `bool` | 是否在做格子到格子的平滑动画 |
| `animFrom/animTo/animT` | `Vector3/Vector3/float` | 格子间动画起点/终点/进度（0..1） |
| `exiting` | `bool` | 已离开网格、正在移向入口边 |
| `pendingExit` | `bool` | 本 tick 决定退出网格（瞬态，每次 sweep 前重置） |
| `pendingNext` | `Vector2Int` | 本 tick 决定移入的格（`(-1,-1)` = 不动） |
| `resolved` | `bool` | 本 tick 是否已确定（移动或退出） |

### 2.2 集合与占用表

| 字段 | 类型 | 含义 |
|---|---|---|
| `_extracting` | `List<ExtractState>` | 提取中的全部像素（含 `moving` 与 `exiting`），直到 `EnterPhysical` 前都在此 |
| `_extractGroup` | `PixelGroup` | 本批引用的像素群 |
| `_extractGroup.grid[col,row]` | `PixelItem[,]` | **实时网格**。`ResolveMatch` 已把匹配格置 null，故非 null 项 = 未匹配像素（障碍） |
| `_matchedOccupied[col,row]` | `bool[,]` | **本批匹配像素占用表**（尚未离开网格的匹配像素），每次 sweep 原子更新 |
| `_extractTickInterval` | `float` | 一个 tick 的时长 = `CellSizeZ / extractSpeed`（逻辑步进与格子动画共用） |
| `_extractTickTimer` | `float` | tick 累加器 |

> **两套占用表**：`grid` 记录「未匹配球」（静态障碍，本批提取期间不变）；`_matchedOccupied` 记录「本批尚未
> 离开的匹配球」（动态障碍，随 sweep 腾出）。两者叠加起来才是"当前哪些格子走不了"。

---

## 3. 坐标约定与「前方」

- **`col` = `gridX`**：横向（X），`0` = 最左（最小 X）。
- **`row` = `gridZ`**：纵深（Z），`0` = 最前排（Z 最大），`row` 越大越靠后（向 -Z）。
- 本地坐标：`x = (col - (columns-1)/2) · CellSizeX`，`z = -row · CellSizeZ`（`PixelGroup.GetLocalPosition`）；
  世界坐标 = `transform.TransformPoint(local)`（`GetWorldPosition`）。
- **「前方」= row 更小**（朝入口边方向）。像素离开网格的路径是"向前（row 递减）+ 横向绕行"。
- 缓冲区几何：`entrance` = 组件自身位置（入口边中心），`gap` = 缺口位置，`axis = normalize(gap - entrance)`
  （朝出口），`perp` = `axis` 在 XZ 平面旋转 90°（入口边方向）。

---

## 4. 阶段流转

### 4.1 进入 `EnterBatch(matched, group)`

1. 存 `_extractGroup`，建 `_matchedOccupied`，`_extractTickInterval = CellSizeZ / extractSpeed`。
2. 对每个匹配像素：关闭 `SphereCollider`（停止点击检测，进入物理阶段时复用同一碰撞体）。
3. 若 `IsInRange(gridX, gridZ)`：`_matchedOccupied[gridX, gridZ] = true`，加入 `_extracting`
   （`col/gridX`、`row/gridZ`）。
4. 空批（全越界/null）直接返回（像素离开后不再补位，无需通知）。

### 4.2 主循环 `StepExtracting`（每帧，`Update` 调用）

见第 5 节，四步：exiting 像素推进 → grid 内动画推进 → tick 累积 + sweep → 清空回调。

### 4.3 切物理 `EnterPhysical(item)`

见第 9 节末尾，附加球碰撞体 + 刚体，从 `_extracting` 移除、加入 `_physical`。

---

## 5. 主循环 `StepExtracting`

```
if _extracting 空: return

RefreshGeometry(entrance, gap, axis, perp, length)

# 步骤 1：exiting 像素 —— 移向入口边 + 同列排队 + 到点切物理
收集所有 st.exiting（剔除 item==null）
for st in exiting:
    target   = ComputeEntryTarget(pos, entrance, perp)   # 横向 clamp 到入口边
    moveTarget = ApplyEntryQueue(st, target, ...)        # 同列前不追尾
    MoveToward(st, moveTarget)                           # 匀速移动 + 旋转朝向
    if 到达 target 或 进入 physicalEntryDepth 范围:
        _extracting.Remove(st); EnterPhysical(st.item)

# 步骤 2：grid 内 moving 像素 —— 推进格子动画
for st in _extracting:
    if st.exiting or !st.moving: continue
    st.animT += dt / _extractTickInterval
    if animT >= 1: animT=1; moving=false
    pos = Lerp(animFrom, animTo, animT)
    RotateToward(item, animTo - animFrom)

# 步骤 3：整体步进（仅当上一波动画全部走完才触发，保证状态原子）
_extractTickTimer += dt
if _extractTickTimer >= _extractTickInterval:
    if HasMovingPixel():
        暂不 sweep（等动画走完，timer 继续累加）
    else:
        _extractTickTimer = 0   # 动画与计时器同步归零（动画才是真正的节拍器）
        SweepOnce()

# 步骤 4：全部离开 → 清理
```

**时序要点**：逻辑步进（sweep）与格子动画共用同一个 `_extractTickInterval`，但 sweep 额外受 `HasMovingPixel()`
门槛约束——只有上一波所有网格内移动动画都走完（无 `moving` 像素）才触发下一次 sweep，保证"完成上一步所有移动
→ 才检查/执行下一步"，画面严格追平逻辑、不再出现视觉重合。sweep 触发时 `_extractTickTimer` 归零而非减
`interval`：动画是真正的节拍器（每段恰 `interval` 秒），若用减法会把门槛阻塞期间累积的超时带进下一周期，
导致余量逐 tick 翻倍、整体推进变慢。

---

## 6. 并行 sweep：`SweepOnce`（空格驱动）

核心语义：**基于本 tick 开始前的占用快照，一次性算出所有可移动像素的下一格**，实现"整列/整批一波一波
连续推进"，而不是逐个串行。方向是**空格找像素（cell-first）**——不再让像素逐个扫邻格抢空位，而是让每个
"当前可被填"的空格，从相邻像素里挑 wait 最高者填入。

```
# 0. 重置本 tick 决策
for st in _extracting: pendingExit=false; pendingNext=(-1,-1); resolved=false

# 1. 网格内像素快照 + 位置查找表
stateAt[cols,rows] = null
seeds = [st for st in _extracting if st.item != null && !st.exiting]
for st in seeds: stateAt[st.col, st.row] = st

# 2. vacated / claimed（本 tick 瞬态表）
vacated[cols,rows] = false   # 本 tick 被腾出的格
claimed[cols,rows] = false   # 本 tick 已被填的格（一格只填一次）

# 3. 静态距离场（每个 sweep 算一次，见第 8 节）
dist[cols,rows] = ComputeExitDistance()

# 4. 步骤 0：退出者（CanExit）无条件离开，腾出格子（不参与 wait 竞争）
seeds.Sort(row 升序, col 升序)               # 保证同列前方退出后，后方在同一次 pass 连锁退出
for st in seeds:
    if CanExit(st.col, st.row, vacated, claimed):
        st.resolved=true; st.pendingExit=true
        vacated[st.col, st.row] = true

# 5. 种子：所有"当前可被填"的格（起始空位 + 退出者刚腾出的格）
frontier = [(c,r) for 所有格 if !IsObstacle(c, r, vacated, claimed)]

# 6. 逐层传播（空格找像素）—— 每个可用格从相邻像素挑 wait 最高者填入
while frontier 非空:
    frontier.Sort(dist 升序, row 升序, col 升序)   # 离出口近者先，保证优先朝前
    next = []
    for cell in frontier:
        if claimed[cell]: continue
        winner = PickBestPixel(cell, dist, stateAt)   # 4 邻格里 dist 更大、公平性最高者
        if winner == null: continue                    # 没人想进，格保持空
        winner.resolved=true; winner.pendingNext=cell
        claimed[cell]=true
        vacated[winner.old]=true; next.add(winner.old)  # 腾出的旧格进入下一层
    frontier = next

# 7. 未解决的球等待计数 +1
for st in _extracting:
    if !st.resolved && !st.exiting && st.item != null: st.waitCount++

# 8. 原子更新占用表 + 触发动画
for st in exits:                                   # pendingExit 的球
    _matchedOccupied[st.col, st.row] = false
    st.moving=false; st.exiting=true; st.waitCount=0
for st in movers:                                  # pendingNext 的球
    _matchedOccupied[st.col, st.row] = false
    _matchedOccupied[st.pendingNext.x, st.pendingNext.y] = true
    StartCellMove(st, st.pendingNext)
    st.col = st.pendingNext.x; st.row = st.pendingNext.y
    st.waitCount = 0
```

**为什么空格驱动能根治"饿死"与"重合"**：旧"像素驱动"里，像素按固定顺序（wait 降序）逐个扫自己的邻格，
但格子被腾出的时机晚于高 wait 像素被扫到——高 wait 像素早早就被跳过，格子腾出后反而被排在后面的低 wait
像素抢走，每 tick 都如此，高 wait 者永久饿死。空格驱动反转方向：**格子一变得可用，就把它的所有相邻像素
同时拿出来比 wait，高 wait 者必胜**，不再受"谁先被扫到"影响。一格只被填一次（`claimed`）、一像素只动一次
（`resolved`），结构性保证"一个空格只被一个占用"，与排序无关。移动方向由 `dist` 决定（朝更小 `dist` 走
一步），不依赖"整条走廊是否已清空"，因此拐角处像素会紧跟前面刚腾出的格子，而非干等。

---

## 7. 可达性判定：`CanExit` / `IsObstacle`

```
CanExit(col, row, vacated, claimed):
    for r in [0, row):              # 前方所有格（更小 row）
        if IsObstacle(col, r, vacated, claimed): return false
    return true

IsObstacle(col, row, vacated, claimed):
    if _extractGroup.grid[col,row] != null:        return true   # 未匹配球（静态障碍）
    if claimed[col,row]:                           return true   # 本 tick 已被抢占
    if _matchedOccupied[col,row] && !vacated[col,row]: return true  # 本批球且本 tick 未腾出
    return false
```

> 判定只用**同列前方**（`CanExit`）——像素能"直着走"的前提是前方（row 0..row-1）无障。横向绕行由静态
> 距离场 `dist` 驱动（见第 8 节）。

---

## 8. 静态距离场与单步移动：`ComputeExitDistance` / `PickBestPixel`

移动方向由**预计算的静态距离场**决定：

```
ComputeExitDistance(cols, rows):
    dist[cols,rows] = INF
    BFS 多源起点 = 前排 row 0 上所有非静态格（dist=0）
    只把未匹配球（_extractGroup.grid != null）当墙；本批匹配球正在离开，不作为墙
    返回 dist[c,r] = 从 (c,r) 走到前排出口的最短步数（被静态障碍围死则保持 INF）
```

每个空格每层从 4 邻接里挑"该填谁"（**空格找像素**），只允许 `dist` **严格更大**（离出口更远）的相邻像素
朝本格前进一步：

```
PickBestPixel(col, row, dist, stateAt):
    myDist = dist[col, row]
    best = null
    for n in 4 邻接:
        越界跳过
        st = stateAt[n]                           # 该格起始的像素
        if st == null or st.resolved: continue    # 无像素，或本 tick 已动过
        if dist[n] <= myDist: continue            # 必须 dist 更大（更接近出口方向）才前进
        if best == null or ComparePriority(st, best) < 0: best = st
    return best   # null = 无人想进，格保持空

ComparePriority(a, b):   # 返回 <0 表示 a 优先
    waitCount 降序 → row 升序 → 距中心列近 → col 升序
```

**语义**：`dist` 是"方向"（该往哪走），`IsObstacle`/`claimed`/`resolved` 是"时机"（此刻谁走、格是否还空）。
拐角处，前面像素一腾出格子，该空格就把它的相邻像素拿出来比 wait——后面像素立刻跟进，因为 `dist` 只看静态
障碍，不受前面像素是否清空整条走廊影响；而 `claimed`（一格只填一次）+ `resolved`（一像素只动一次）保证
"一个空格只被一个占用"。`dist` 严格递减保证无环、无死锁（每步都朝出口逼近一格）。

**为什么旧的 `FindNextCell` 会"拐角不紧跟"**：旧 BFS 的目标是"找到一个 `CanExit` 为真的格子"（前方整列
清空的出口格），并只在**完整路径**存在时才返回第一步。拐角处的中间格子（如被前排静态障碍堵死的侧列）永远
不满足 `CanExit`，于是 BFS 一直返回"无路"，像素只能等整条走廊都被前面像素清空、出口格可达后才动，造成
拐角处大片停顿。

---

## 9. 移向入口边阶段（`exiting`）

### 9.1 落位点 `ComputeEntryTarget`

```
rel = pos - entrance; rel.y = 0
lateral = Dot(rel, perp)                          # 横向（入口边方向）偏移
clampHalf = entranceWidth/2 - radius
lateral = Clamp(lateral, -clampHalf, clampHalf)   # 横向 clamp 到入口边内
entry = entrance + perp * lateral; entry.y = pos.y
```

像素沿自身横向位置映射到入口边，并把横向 clamp 到 `±(entranceWidth/2 - radius)`，保证球落在封闭区间内、不穿墙。

### 9.2 同列排队 `ApplyEntryQueue`

```
myProg = Dot(pos - entrance, axis)        # 前进方向进度（沿 axis）
latMe  = Dot(pos - entrance, perp)        # 横向位置
for other in exiting:
    if other == st or other.item == null: continue
    oProg = Dot(other.pos - entrance, axis)
    if oProg <= myProg: continue          # 不在前方
    latOther = Dot(other.pos - entrance, perp)
    if |latOther - latMe| > radius: continue  # 不同列，互不阻塞
    gap = oProg - myProg
    if gap < entryQueueSpacing:
        stopProg = max(oProg - entryQueueSpacing, myProg)   # 退到前方后 spacing 处，不后退
        return entrance + perp*latMe + axis*stopProg
return target                             # 无阻挡，走原目标
```

语义：若前方存在**同列**（横向差 ≤ `radius`）且**间距不足**的退出像素，则把移动目标退回到前方像素后
`entryQueueSpacing` 处，实现同列前不追尾。横向用 `radius` 判同列，避免不同列的像素被误排。

### 9.3 移动与旋转

- `MoveToward`：沿 XZ 匀速移动 `extractSpeed`，`RotateToward` 使 z 正向朝向移动方向（角速度 `extractRotateSpeed`）。
- 切物理条件（二选一）：
  1. `XZDistance(pos, target) ≤ ArriveEpsilon`（到达落位点）；
  2. `Dot(pos - entrance, axis) ≥ -physicalEntryDepth`（进入物理起始范围，提前赋予刚体朝缺口）。

### 9.4 切物理 `EnterPhysical`

附加球碰撞体（`SphereCollider.radius = radius / lossyScale`）+ 刚体（冻结 Y 与旋转、关重力、`Interpolate` +
`ContinuousDynamic`），给定朝 `gap` 的初速度，加入 `_physical`。之后由 `FixedUpdate` 每物理帧重写速度。

---

## 10. 关键不变量与集成

- **占用表一致性**：`_matchedOccupied` 只在 `EnterBatch`（置 true）与 `SweepOnce`（腾出/转移）两处修改，
  且每次 sweep 原子更新，保证逻辑格与实际占用同步。
- **点击门（GameController）**：点击一个像素后，`FloodFill` 得到同色组，`CanReachFront` 做 BFS 连通性检查——
  把组内格视为即将腾空，检查是否存在只经过「空 / 组内」格、从组连通到首排（row 0）的路径；有路径才进入提取，
  否则点击无效（组被其他像素完全包围）。像素离开后网格不再补位（保持空位）。
- **点击屏蔽**：提取进行中（`IsExtracting`）`GameController` 不响应点击，保证网格状态一致。
- **旋转只写 rotation**：`RotateToward` 只改朝向，不参与位置与碰撞，与重合问题无关。

---

## 11. 已知问题（待修复）

> 结论来自前一轮 `DebugOverlapCheck` 日志分析（该调试代码已移除）。

`exiting` 阶段仍可能出现像素重合，两类：

1. **跨列收敛到同一入口点**：`ComputeEntryTarget` 的横向 clamp 会把横向位置不同（或都超出 clamp 范围）的
   多个像素映射到**同一个入口点**；`ApplyEntryQueue` 只对「同列」（横向差 ≤ `radius`）排队，跨列像素互不
   阻塞，最终撞到同一点。
2. **收尾瞬间塌陷**：前端像素 `EnterPhysical` 从 `_exiting` 移除的当帧，后端失去阻挡对象，`MoveToward`
   直接冲到同一入口点（前后间距从 ~0.44 塌到 0）。

修复方向（未实施）：给每个 `exiting` 像素分配**互不相同的入口槽位**（沿入口边把 `entranceWidth` 分成 N 槽，
或按到达顺序分配横向偏移），从根上避免多像素映射同一点；并在前端进入物理时让后端保持 `entryQueueSpacing`
等待，而非立即追平。

---

## 12. 相关文档

- [`CrowdBufferDesign.md`](CrowdBufferDesign.md) — 缓冲区整体功能设计（几何、物理、释放调度、状态机）。
- [`CrowdBufferImplementations.md`](CrowdBufferImplementations.md) — 早期实现归档（软力、顺序投影硬约束）。
- [`ConveyorBeltDesign.md`](ConveyorBeltDesign.md) — 释放后闭环传送带（`conveyorZone` 分支）。
