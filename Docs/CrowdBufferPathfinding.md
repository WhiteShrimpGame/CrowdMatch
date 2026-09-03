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
| 网格内寻路 | `moving`（格子间动画）或等待中 | 仍在 `_extractGroup` 网格里，逐格 BFS 走向入口边 | 离散 tick + 连续插值 |
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

# 步骤 3：tick 累积 + 并行 sweep
_extractTickTimer += dt
if _extractTickTimer >= _extractTickInterval:
    _extractTickTimer -= _extractTickInterval
    SweepOnce()

# 步骤 4：全部离开 → 清理
```

**时序要点**：逻辑步进（sweep）与格子动画共用同一个 `_extractTickInterval`——像素恰好在一格动画结束时，
下一格决策（sweep）随之而来，逻辑与视觉同步。

---

## 6. 并行 sweep：`SweepOnce`

核心语义：**基于本 tick 开始前的占用快照，一次性算出所有可移动像素的下一格**，实现"整列/整批一波一波
连续推进"，而不是逐个串行。

```
# 0. 重置本 tick 决策
for st in _extracting: pendingExit=false; pendingNext=(-1,-1); resolved=false

# 1. 参与决策的球（网格内、未退出）
order = [st for st in _extracting if st.item != null && !st.exiting]

# 2. 公平性排序
order.Sort:
    waitCount 降序         # 被挡越久越优先
    → row 升序             # 前到后
    → 距中心列近者优先
    → col 升序

# 3. vacated / claimed（本 tick 瞬态表）
vacated[cols,rows] = false   # 本 tick 被腾出的格（"即将腾出"，后排可跟进）
claimed[cols,rows] = false   # 本 tick 已被移入的格（防两球同格）

# 4. 迭代到不动点（链式"即将腾出"需要多轮）
changed = true
while changed:
    changed = false
    for st in order:
        if st.resolved or st.exiting or st.item==null: continue

        if CanExit(st.col, st.row, vacated):
            st.resolved = true; st.pendingExit = true
            vacated[st.col, st.row] = true; changed = true
            continue

        next = FindNextCell(st.col, st.row, vacated)
        if next.x < 0: continue                    # 无路，等下一轮
        if claimed[next.x, next.y]: continue       # 目标已被抢占

        st.resolved = true; st.pendingNext = next
        claimed[next.x, next.y] = true
        vacated[st.col, st.row] = true
        changed = true

# 5. 未解决的球等待计数 +1
for st in _extracting:
    if !st.resolved && !st.exiting && st.item != null: st.waitCount++

# 6. 原子更新占用表 + 触发动画
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

**为什么"即将腾出"能让整列连续推进**：若前排球本 tick 判定要移动/退出（写入 `vacated`），后排球在同一轮
迭代中就能看到它已"腾出"，于是也跟着移动——形成一波。若窄缝处多球争用同一单格（`claimed` 冲突），则只有
排序靠前者抢到，其余 `waitCount++` 等待下一 tick，并按公平性排序下次优先。

---

## 7. 可达性判定：`CanExit` / `IsObstacle`

```
CanExit(col, row, vacated):
    for r in [0, row):              # 前方所有格（更小 row）
        if IsObstacle(col, r, vacated): return false
    return true

IsObstacle(col, row, vacated):
    if _extractGroup.grid[col,row] != null:        return true   # 未匹配球（静态障碍）
    if _matchedOccupied[col,row] && !vacated[col,row]: return true  # 本批球且本 tick 未腾出
    return false
```

> 判定只用**同列前方**（`CanExit`）——像素能"直着走"的前提是前方（row 0..row-1）无障。横向绕行交给
> `FindNextCell` 的 BFS 处理。

---

## 8. BFS 寻路：`FindNextCell`

目标：从 `(startCol, startRow)` 找一条可达"出口格"（`CanExit` 为 true 的格）的路径，返回**第一步**的格子
坐标；无路返回 `(-1,-1)`。

```
4 邻接（dx/dz: 上/下/左/右，即 row±1 与 col±1）
BFS 队列从起点出发，用 prev[cols,rows] 记录前驱
遍历邻格：
    - 越界跳过（IsInRange）
    - 已访问（prev ≥ 0）跳过
    - IsObstacle 跳过
    - 否则 prev[nx,nz]=cur；若 CanExit(nx,nz) → 找到 goal，终止 BFS
从 goal 沿 prev 回溯到 start，取 path 最后一项 = 第一步
```

**语义**：`FindNextCell` 返回的是"朝某个出口格迈出的第一步"，而非完整路径。每 tick 只走一格，下一 tick
重新基于最新占用快照再算，天然适应动态障碍（同批球也在动）。

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
