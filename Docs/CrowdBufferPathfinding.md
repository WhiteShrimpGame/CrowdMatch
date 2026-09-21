# CrowdBufferZone 提取寻路逻辑（当前代码逐条梳理）

> 状态：**与 `Assets/Scripts/Gameplay/CrowdBufferZone.cs` 当前代码一致**。
> 代码现状 = **原始寻路 + 倍乘门最初那版的接入**（接入点见 §13.1）+ 两个排查/对比用具：
> **`exitOnlyFromRow0`**（离场时机开关，见 §7 说明 1）与 **`debugMoveLog`**（打印每次「空格挑中像素」的当前格 → 目标格）。
> 从「没有考虑目标格子的占用性」那条需求起做的后续改动（第二条 `CanExit` 守卫、逐帧重叠扫描、分身弹入、
> 那一轮的调试日志）**已全部撤销**；撤销清单见 §13.2，那期间实测到的事实见 §13.3。
>
> 范围：只讲**提取阶段** —— 匹配像素在网格内寻路离开、移向入口边、切入物理之前。
> 物理过闸、释放调度、传送带分别见 [`CrowdBufferDesign.md`](CrowdBufferDesign.md)、[`ConveyorBeltDesign.md`](ConveyorBeltDesign.md)。
>
> 代码位置：`Assets/Scripts/Gameplay/CrowdBufferZone.cs`。入口 `EnterBatch`，每帧驱动 `StepExtracting`。

---

## 1. 生命周期与范围

```
InGrid ──点击匹配──▶ Matched ──(GameController.ResolveMatch → EnterBatch)──▶ Extracting（本文）
                                                                                │
                                                            (EnterPhysical) ◀───┘
                                                                     │
                                                        Physical ──▶ Released ──▶ Arrived
```

提取阶段内部有两个子阶段，**同一批里可以同时存在**（前面的已出网格、后面的还在网格里走）：

| 子阶段 | 状态标志 | 驱动方式 |
|---|---|---|
| 网格内寻路 | `moving`（格子间动画中）/ 都不是（等待中） | **离散 tick**（每 tick 走一格）+ 连续插值 |
| 移向入口边 | `exiting = true` | 每帧连续匀速移动 + 同列排队 |

---

## 2. 数据结构

### 2.1 单个像素状态 `ExtractState`（私有内部类）

| 字段 | 类型 | 含义 |
|---|---|---|
| `item` | `PixelItem` | 像素本体 |
| `col, row` | `int` | 当前逻辑格（初始 = `item.gridX/gridZ`；`exiting` 后**冻结**、不再更新） |
| `fromCol, fromRow` | `int` | 最近一次移动的**离开格**（决定移动时记录，动画期间保留；停靠时无意义） |
| `waitCount` | `int` | 被挡住的次数（公平性：越大越优先） |
| `moving` | `bool` | 是否在播格子到格子的平滑动画 |
| `animFrom / animTo` | `Vector3` | 动画起点 / 终点（**世界**坐标） |
| `animT` | `float` | 动画进度 0..1 |
| `exiting` | `bool` | 已离开网格、正在移向入口边 |
| `pendingExit` | `bool` | 本 tick 决定退出网格（瞬态，每次 sweep 前重置） |
| `pendingNext` | `Vector2Int` | 本 tick 决定移入的格（`(-1,-1)` = 不动） |
| `resolved` | `bool` | 本 tick 是否已确定（移动或退出） |

### 2.2 一次匹配 = 一个批次 `Batch`（私有内部类）

| 字段 | 类型 | 含义 |
|---|---|---|
| `extracting` | `List<ExtractState>` | 本批全部像素（**含 `exiting`**，直到 `EnterPhysical` 前都在此） |
| `matchedOccupied` | `bool[columns, TotalRows]` | 本批「尚未离开网格」的匹配像素占用表，由 sweep 原子更新 |
| `tickTimer` | `float` | 本批自己的 tick 累加器 |

### 2.3 宿主字段

| 字段 | 含义 |
|---|---|
| `_batches: List<Batch>` | 提取中的批次。**每次点击匹配 = 一个独立批次**，各批独立寻路、独立占用表、独立 tick |
| `_extractGroup: PixelGroup` | 唯一、所有批次共享（进第一批时设，全部批次结束时置 `null`） |
| `_extractTickInterval: float` | `CellSizeZ / extractSpeed`，一个 tick 的时长；逻辑步进与格子动画共用，所有批次共享 |
| `_physical: List<PixelItem>` | 物理阶段（不在本文范围） |

### 2.4 两套占用表 + 两张瞬态表（关键）

| 表 | 内容 | 谁维护 |
|---|---|---|
| `_extractGroup.grid[c,r]` | **实时网格**。`ResolveMatch` 已把匹配格置 `null`，故非 `null` = 未匹配像素（静态障碍，本批期间不变） | PixelGroup / 管道 / 箱子 / 升降台 |
| `batch.matchedOccupied[c,r]` | 本批**尚未离开网格**的匹配像素（动态障碍，随 sweep 腾出/转移） | **只在 `EnterBatch` 与 `SweepOnce` 两处改** |
| `vacated[c,r]` | 本 tick 被腾出的格（瞬态） | `SweepOnce` 内，每 tick 新建 |
| `claimed[c,r]` | 本 tick 已被填入的格（瞬态；一格只填一次） | 同上 |

前两者叠加 = 「此刻哪些格走不了」；后两者只回答「本 tick 这一步谁先谁后」。
`matchedOccupied` 是**按批**的 ⇒ 批次之间不互斥（组间穿模，见 §10）。

---

## 3. 坐标约定与「前方」

- **`col` = `gridX`**：横向（X），`0` = 最左。
- **`row` = `gridZ`**：纵深（Z），`0` = 最前排（Z 最大），`row` 越大越靠后（越 -Z）。
- 本地坐标：`x = (col − (columns−1)/2) · CellSizeX`，`z = −row · CellSizeZ`（`PixelGroup.GetLocalPosition`）；
  世界坐标 = `transform.TransformPoint(local)`（`GetWorldPosition`）。
- **「前方」= row 更小**（朝入口边方向）。离开网格的路线 = 「向前 + 横向绕行」。
- 缓冲区几何（`RefreshGeometry`）：`entrance` = 组件自身位置；`gap` = `gapPoint.position`；
  `axis = normalize(gap − entrance)`（朝出口）；`perp` = `axis` 在 XZ 平面旋转 90°（沿入口边方向）。

---

## 4. 进入：`EnterBatch(matched, group)`

1. `_extractGroup = group`；`_extractTickInterval = group.CellSizeZ / max(0.0001, extractSpeed)`。
2. 新建 Batch：`matchedOccupied = new bool[group.columns, group.TotalRows]`，`tickTimer = 0`。
3. 逐个匹配像素：
   - 关掉它的 `SphereCollider`（停止点击检测；进入物理阶段时复用同一个碰撞体）。
   - 不在网格范围内 → 跳过。
   - 否则 `matchedOccupied[gridX, gridZ] = true`，并加入一个 `ExtractState{ col = gridX, row = gridZ }`。
4. 若一个都没进网格（整批越界）→ **整批丢弃**，不加入 `_batches`。

> 调用点：`GameController.ResolveMatch`（`GameController.cs:816`）。**不等上一批离场** —— 上一批还在网格里走，
> 新的点击就再开一个批次。
>
> 细节：`_extractGroup` 在第 1 步就已赋值，早于第 4 步的提前返回。此时 `_batches` 仍为空，
> 下一帧 `StepExtracting` 的收尾分支会把 `_extractGroup` 置 `null`，所以不会留下悬空引用。

---

## 5. 主循环：`StepExtracting`（每帧，`Update` 调用）

```
if _batches.Count == 0: return
dt = Time.deltaTime
RefreshGeometry(entrance, gap, axis, perp, length)

# 注意：批次用【倒序】遍历（批内会 Remove，倒着走不会踩下标）
for b = _batches.Count-1 downto 0:
    batch = _batches[b]

    # 步骤 1：已出网格的像素（exiting）——移向入口边 + 同列排队 + 到点切物理
    收集本批 st.exiting（顺带剔除 item == null 的）
    for st in 收集到的:
        target     = ComputeEntryTarget(st.pos, entrance, perp)      # 横向 clamp 到入口边
        moveTarget = ApplyEntryQueue(st, target, entrance, axis, perp, exiting)  # 同列前不追尾
        MoveToward(st, moveTarget)                                    # 匀速移动 + 朝向移动方向
        if XZDistance(st.pos, target) <= ArriveEpsilon                # 注意：判的是【原始 target】
           or (physicalEntryDepth > 0 and Dot(st.pos-entrance, axis) >= -physicalEntryDepth):
             batch.extracting.Remove(st); EnterPhysical(st.item)

    # 步骤 2：网格内 moving 像素 —— 推进格子动画
    for st in batch.extracting:
        if st.exiting or !st.moving: continue
        st.animT += dt / _extractTickInterval
        if st.animT >= 1: st.animT = 1; st.moving = false
        st.pos = Lerp(st.animFrom, st.animTo, st.animT)
        RotateToward(st.item, st.animTo - st.animFrom)

    # 批内已空 → 移除该批
    if batch.extracting.Count == 0: _batches.RemoveAt(b); continue

    # 步骤 3：整体步进 —— 【只有本批没有任何 moving 像素时】才 sweep
    batch.tickTimer += dt
    if batch.tickTimer >= _extractTickInterval and !HasMovingPixel(batch):
        batch.tickTimer = 0            # 归零，不是减 interval
        SweepOnce(batch)

# 步骤 4：全部批次都结束 → 清管道蛇形「可通行」标记 + 释放 PixelGroup 引用
if _batches.Count == 0:
    ClearExtractionWalkableFlags()
    _extractGroup = null

# 步骤 5：网格内寻路的最后一颗像素本帧走出网格 → 通知一次（边沿检测：帧初有、帧末没有；见 §10）
if 帧初有网格内像素 and 帧末没有网格内像素:
    OnGridPathfindingFinished?.Invoke()
```

**时序要点（本文最重要的一条前提）**

逻辑步进（`SweepOnce`）与格子动画共用同一个 `_extractTickInterval`，但 sweep **额外受 `HasMovingPixel` 门槛约束**：
只有上一波所有网格内移动动画都走完，才触发下一次 sweep。因此：

> **`SweepOnce` 被调用的那一刻，网格内每一颗像素都精确停在格心上**（动画 `animT == 1` 时位置正好是 `animTo`）。
> 这是读/写提取状态的唯一安全时刻，也是 §11-I1。

`tickTimer` 触发时**归零而不是减 `interval`**：动画才是真正的节拍器（每段恰 `interval` 秒）；用减法会把
「门槛阻塞期间累积的超时」带进下一周期，导致余量逐 tick 翻倍、整体推进越来越慢。

---

## 6. `SweepOnce(batch)`：一次并行 sweep

核心语义：**基于本 tick 开始前的占用快照，一次性算出所有可移动像素的下一格**（每个像素本 tick 最多动一格），
于是整批像波前一样连续推进，而不是逐个串行。

严格按代码顺序：

```
# 0. 重置本 tick 决策
for st in batch.extracting: st.pendingExit = false; st.pendingNext = (-1,-1); st.resolved = false

# 1. 静态距离场（每 sweep 算一次；读的是实时 grid，见 §8）
dist = ComputeExitDistance(cols, rows)

# 2. 在格像素快照 + 参与决策列表
stateAt[cols, rows] = null
seeds = [st for st in batch.extracting if st.item != null and !st.exiting]
for st in seeds: stateAt[st.col, st.row] = st

# 3. 本 tick 瞬态表
vacated / claimed = false[cols, rows]
exits = []; movers = []

# 4. 步骤 0：退出者（CanExit 命中）无条件离开，腾出各自格子，不参与 wait 竞争
seeds.Sort(row 升序, col 升序)          # 前到后：同列前方先退出，后方能在同一 pass 里连锁退出
for st in seeds:
    if CanExit(st.col, st.row, vacated, claimed, batch.matchedOccupied):
        exits.Add(st); st.resolved = true; st.pendingExit = true; vacated[st.col, st.row] = true

# 5. 蛇格 rank：正在释放的管道的 snakeCells（已按蛇头→蛇尾排好）给递增 rank
snakeOrder = { cell → rank }

# 6. 种子：所有「当前可被填」的格（起始空位 + 步骤 0 刚腾出的格）= IsObstacle 取反
frontier = [ (c,r) for 所有格 if !IsObstacle(c, r, vacated, claimed, batch.matchedOccupied) ]

# 7. 逐层传播（空格找像素）
while frontier 非空:
    frontier.Sort( 非蛇格优先 → 蛇格按 snakeOrder(蛇头→蛇尾) → dist 升序 → row 升序 → col 升序 )
    next = []
    for cell in frontier:
        if claimed[cell]: continue
        winner = PickBestPixel(cell.x, cell.y, dist, stateAt)   # 4 邻接里 dist 更大、公平性最高者
        if winner == null: continue        # 没人想进 → 格保持空
        movers.Add(winner); winner.resolved = true; winner.pendingNext = cell
        claimed[cell] = true
        vacated[winner.col, winner.row] = true
        next.Add(winner.col, winner.row)   # 腾出的旧格进入下一层 → 形成连续波前
    frontier = next

# 8. 未解决的球等待计数 +1（公平性：被挡越久，下次越优先）
for st in batch.extracting:
    if st.item != null and !st.exiting and !st.resolved: st.waitCount++

# 9. 原子更新占用表 + 触发动画
for st in exits:
    batch.matchedOccupied[st.col, st.row] = false
    st.waitCount = 0; st.moving = false; st.exiting = true
for st in movers:
    batch.matchedOccupied[st.col, st.row] = false
    batch.matchedOccupied[st.pendingNext.x, st.pendingNext.y] = true
    StartCellMove(st, st.pendingNext)     # 记 fromCol/fromRow、animFrom=当前位置、animTo=目标格世界坐标、moving=true
    st.col = st.pendingNext.x; st.row = st.pendingNext.y
    st.waitCount = 0
```

**为什么是「空格找像素」而不是「像素找空格」**：旧做法里像素按固定顺序（wait 降序）逐个扫自己的邻格，
但「格子被腾出的时机」晚于「高 wait 像素被扫到」——高 wait 像素早被跳过，格子腾出后反被排在后面的低 wait
像素抢走，于是高 wait 者永久饿死。空格驱动把方向反过来：**格子一变得可用，就把它的所有相邻像素同时拿出来
比 wait，高 wait 者必胜**，与「谁先被扫到」无关。再加上 `claimed`（一格只填一次）与 `resolved`（一像素只动
一次）两道栅栏，结构性保证「一个空格只被一个像素占用」。

`frontier` 排序里 `dist` 升序 = **离出口近的格先被填**，于是推进方向自然朝前。

---

## 7. 三条判定

```
CanExit(col, row, vacated, claimed, matchedOccupied):
    # ① 离场时机开关：勾上后只有最前排（row 0）能离场
    if exitOnlyFromRow0 and row != 0: return false
    # ② 倍乘门守卫（**仅「不勾 row0」模式需要**）：在某道门的区域内、却不在门格上 → 不许直接离场
    if not exitOnlyFromRow0 and _extractGroup.MustWalkToGate(col, row): return false
    # ③ 管道：必须走到「正在释放的管道」轨迹的最前排【之前】才算真正越过它，方可离场
    minTrackRow = _extractGroup.MinActivePipeTrackRow()      # 无正在释放的管道 → int.MaxValue
    if minTrackRow > 0 and row >= minTrackRow: return false   # minTrackRow == 0 时放行（否则整批死锁）
    # ④ 同列前方全空
    for r in [0, row):                                        # 只看【同列前方】
        if IsObstacle(col, r, vacated, claimed, matchedOccupied): return false
    return true

IsObstacle(col, row, vacated, claimed, matchedOccupied):
    if _extractGroup.IsBlocked(col, row):                                  return true  # 墙 / 管道自身格 / 未开箱箱子
    gridItem = _extractGroup.grid[col, row]
    if gridItem != null and !gridItem.walkableDuringExtraction:            return true  # 未匹配像素
    if claimed[col, row]:                                                  return true  # 本 tick 已被填
    if matchedOccupied[col, row] and !vacated[col, row]:                   return true  # 本批球，本 tick 未腾出
    return false

IsEmptyForExtraction(col, row):                               # 「可通行」，距离场与管道都用它
    if 越界 or _extractGroup.IsBlocked(col, row): return false
    item = _extractGroup.grid[col, row]
    return item == null or item.walkableDuringExtraction
```

五点说明：

1. **`exitOnlyFromRow0`（默认关）是「离场时机」的可选模式**：关上 = 现状（同列前方全空就能在**任意 row** 离场，
   深层像素会在原处直接起飞）；勾上 = 必须先被 `dist` 导到 **row 0** 才准离场（出网格更慢，但离场像素「在网格内
   飞行」的路程变得极短）。保留这个开关用于对比两种离场时机的重合表现（见 §12.1）。
2. **两种模式下的倍乘门处理**：守卫 ② **只在「不勾 row0」时生效**，因为勾上 row0 时它本来就永不触发 ——
   闭环区域里**不可能有 row 0 的格子**（`GateRegion.ComputeRegion` 把 row 0 上所有「非障碍、非门格」的格都当
   外部种子），所以区域内的像素根本走不到 row 0、只能经门走过去并占用门格，裂变照常触发。**两者行为等价**，
   不加这条只是把冗余规则去掉、把意图写明确。
3. **`CanExit` 只看同列前方**（`row 0..row-1`），横向绕行完全交给距离场（§8）。
4. `PixelItem.walkableDuringExtraction` 是「本批提取可以穿过这一格」的**临时**标记：
   - `PipeItem` 在蛇形生成期间给自己那批像素打上（`PipeItem.cs:400`）；
   - `BoxItem` 在「本次点击开箱」占格时打上（`BoxItem.cs:324`）；
   - 提取全部结束时由 `ClearExtractionWalkableFlags()` 统一清除（§5 步骤 4）。
5. `IsBlocked` = 墙 ∪ 管道自身格 ∪ **未开箱**的箱子（`PixelGroup.IsBlocked`）。

---

## 8. 距离场与单步选择

### 8.1 `ComputeExitDistance(cols, rows)`

```
dist[cols, rows] = INF
多源 BFS：所有满足 IsEmptyForExtraction(c, 0) 的前排格 → dist = 0、入队
扩展时穿行条件同样是 IsEmptyForExtraction（越界/障碍/不可穿行像素都不走）
返回 dist：从该格走到前排出口的最短步数；被静态障碍围死的格保持 INF
```

要点：`dist` **只描述方向**，且**每个 sweep 重算一次**；它不包含 `claimed`/`vacated` 这些本 tick 瞬态。
「本批正在离开的匹配球」不算墙（它们已不在 `grid` 里），所以距离场在整批提取期间基本稳定。

### 8.2 `PickBestPixel(col, row, dist, stateAt)`

```
myDist = dist[col, row]
for n in 4 邻接:
    if 越界: continue
    st = stateAt[n]                                   # 该格【本 tick 开始时】的像素
    if st == null or st.resolved: continue            # 无像素 / 本 tick 已动过
    if dist[n] <= myDist: continue                    # 必须 dist 严格更大（更远离出口）才朝本格前进一步
    if best == null or ComparePriority(st, best) < 0: best = st
return best                                           # null = 没人想进，格保持空

ComparePriority(a, b):     # < 0 表示 a 优先
    waitCount 降序 → row 升序 → 距中心列近 → col 升序
```

**语义分工**：`dist` 管「该往哪走」，`IsObstacle` / `claimed` / `resolved` 管「此刻谁走、格还空不空」。
`dist` 严格递减 ⇒ 每步都朝出口逼近 ⇒ 无环、无死锁；拐角处前面一腾出格，该空格立刻把相邻像素拿出来比 wait，
后面的像素马上跟进（不像早期 `FindNextCell` 要等整条走廊清空）。

---

## 9. 移向入口边阶段（`exiting = true`）

### 9.1 落位点 `ComputeEntryTarget`

```
rel = pos - entrance; rel.y = 0
lateral = Dot(rel, perp)                                # 沿入口边的横向偏移
clampHalf = entranceWidth/2 - radius
lateral = Clamp(lateral, -clampHalf, clampHalf)          # clamp 进入口边内（不穿墙）
entry = entrance + perp * lateral; entry.y = pos.y
```

### 9.2 同列排队 `ApplyEntryQueue`

若前方存在**同列**（横向差 ≤ `radius`）且前后间距 < `entryQueueSpacing` 的退出像素，则把移动目标退回到
「前方像素之后 `entryQueueSpacing`」处；`stopProg` 不小于自身进度（**不后退**，只停等）。无阻挡则用原目标。

### 9.3 移动与旋转

- `MoveToward`：沿 XZ 匀速（`extractSpeed`）移向目标；`RotateToward`：z 正向以 `extractRotateSpeed` 转朝移动方向。
- 切物理条件（二者之一）：① 到达落位点（`XZDistance(pos, target) ≤ ArriveEpsilon`，判的是**原始 target**）；
  ② 进入物理起始范围（`Dot(pos − entrance, axis) ≥ −physicalEntryDepth`，仅当 `physicalEntryDepth > 0`）。

### 9.4 `EnterPhysical(item)`

- 清掉除 `SphereCollider` 以外的碰撞体；`sphere.radius = radius / physicalTargetScale`（本地半径固定，
  之后随视觉 `localScale` 同步缩放）；`sphere.enabled = true`。
- 刚体：`useGravity = false`、`mass = 1`、`drag = 0`、`angularDrag = 0.05`、
  `constraints = FreezePositionY | FreezeRotationX|Y|Z`、`interpolation = Interpolate`、
  `collisionDetectionMode = ContinuousDynamic`。
- 进入时随机一次 `bufferCrowdSpeed ∈ [crowdSpeed ± crowdSpeedRandomRange]` 与 `bufferAimOffset ∈ ±aimOffsetX`，
  给一个朝 `gap` 的初速度；之后每物理帧由 `FixedUpdate` 重写速度（够远时朝 `gap + perp*offset` 散开）。
- 尺寸偏差超过 `scaleTolerance` 时启动 `SmoothScaleToTarget`（延迟 `scaleDelay`、`scaleSmoothDuration` 内匀速到
  `physicalTargetScale`）。
- 记 `bufferedAt = Time.time`（供「排队太久」类判定），加入 `_physical`。

---

## 10. 批次之间、以及对外接口

- **批次完全独立**：各批有自己的 `extracting` / `matchedOccupied` / `tickTimer`；只有 `_extractGroup` 与
  `_extractTickInterval` 共享。**批间允许穿模** —— `HandleClick`（`GameController.cs:580`）与 `Batch` 的注释都写明：
  「提取进行中仍允许点击，每次匹配作为独立批次，各自独立寻路（组间可穿模），无需等待上一批离场」。
  ⇒ 后一批的像素可以走进前一批像素正在走的格子（前者看不到后者的占用表）。
- `IsExtractingOccupied(col, row)`：**给管道蛇头用**（`PipeItem.cs:253` `WaitUntilCellFree`）。它扫**所有批次**的
  `extracting`，跳过 `exiting`，因此是「所有批次都避让」的口径（蛇不是匹配组，不参与组间穿模）。
- `PendingCount` = 物理阶段 + 所有批次 `extracting`（**含 `exiting`**）＝「已点击但尚未进传送带」的总数。
  `GameController.CurrentInflight()` = 传送带占用槽位 + `PendingCount`，`PassOverflowClickGate()` 用它做
  **堆积节流**：总数低于传送带容量时放行；达到容量后累计 2 次放行；之后只有 `PendingCount ≤ overflowPendingLimit`
  才放行。
- `HasGridPathfindingPixels`：**是否仍有像素在网格内寻路**（扫所有批次，跳过 `exiting` 与已销毁的）。
  `GameController.IsFail` 用它做**静止门槛**：网格里还有像素在走就不判失败 —— 那些像素可能**还没穿过倍乘门**
  （分身尚未生成），此时复活会把它们直接收走，实际送出的像素数就与 `GameData.TotalPixelCount` 对不上。
- `OnGridPathfindingFinished` 事件（§5 步骤 5 的边沿检测：本帧开始有网格内像素、结束没有了 → 触发一次）：
  失败判定借此**延迟复查**，否则「带满且不匹配」的关卡会一直不判失败。`GameController` 在 `Start` 订阅、
  `OnDestroy` 退订。
- **复活**：`DrainAllPixels()` 取走全部提取中 + 物理阶段的像素（解除物理约束、清 `walkableDuringExtraction`
  标记、清批次），与 `ConveyorBeltZone.DrainBeltKeep` 一起交给 `GameController.Revive`（`GameController.cs:360-362`）。
- **重载**：`ResetAll()` 销毁提取中与物理阶段的所有像素并清空批次。
- **点击门**：`GameController.CanReachFront` 做连通性 BFS（把组内格视为即将腾空），决定一次点击是否有效。

---

## 11. 不变量清单（改动前先逐条确认）

| # | 不变量 | 由什么保证 |
|---|---|---|
| **I1** | **`SweepOnce` 被调用时，网格内每颗像素都精确停在格心上**（非 moving 状态位置 == `GetWorldPosition(col,row)`） | §5 步骤 3 的 `HasMovingPixel` 门槛 + 步骤 2 的插值终点 |
| **I2** | 同一批次内**一格最多一个 `ExtractState`** | `stateAt` 是单值表；`claimed` 保证本 tick 一格只被填一次；`matchedOccupied` 保证已占格不被再填 |
| **I3** | `ExtractState.col/row` 与 `batch.matchedOccupied` 同步 | 全项目只有两处改它们：`EnterBatch` 初始化、`SweepOnce` 的原子 apply（步骤 9） |
| **I4** | 移动方向严格沿 `dist` 递减 | `PickBestPixel` 要求 `dist[n] > myDist`；`dist` 由 BFS 得来 ⇒ 无环、无死锁 |
| **I5** | 只有 `CanExit` 命中才会 `exiting`；`exiting` 之后 `col/row` 冻结、占用已释放、不再参与 sweep（但仍留在 `batch.extracting` 直到 `EnterPhysical`） | `SweepOnce` 步骤 0 / 步骤 9；`seeds`/`stateAt` 过滤 `exiting` |
| **I6** | **`exiting` 像素的 `col/row` 不是占格信息** | 它们已不在 `grid` 也不在 `matchedOccupied`；任何「按 col/row 统计占用」的代码都不能把 `exiting` 算进去 |
| **I7** | 批次之间互相不可见（各占用表独立），批间穿模是**设计如此** | `Batch` 的数据划分 + `HandleClick` 注释 |

---

## 12. 已知问题：像素视觉重合

### 12.1 通则：同 tick「一进一出」且方向不共线 ⇒ 动画中必然互相插入（**与倍乘门无关**）

`SweepOnce` 的波前允许**同一 tick 内「一颗走出一格」与「另一颗走进这一格」同时发生**（腾出的格经 `vacated`
立刻可填、再由 `next` 带进下一层）。当这两步的方向**不共线**时，两颗像素在动画过程中会互相穿过：

```
格距 = 直径 = p；A 从角格 C 向上走（C → C_up），B 从左格进 C（C_left → C），同一 tick 内：
    t 时刻   A = C + (0, 0, p·t)        B = C + (−p(1−t), 0, 0)
    间距 = p·√((1−t)² + t²)，在 t = 0.5 取最小 = p/√2 ≈ 0.707p  <  p（= 直径）
⇒ 整个 tick（约 _extractTickInterval）间距都小于一个直径，最深处插入约 0.29p
```

- **任何 90° 拐弯处**都成立：波前链在拐角折向时，「一进一出」正好是垂直的。
- **`exiting` 的接棒处**是最显眼的一例：像素在 `(col,r)` 被判离场（+z 飞出网格）的同一 tick，后面的像素
  沿该行走进 `(col,r)`（±x）—— 依然垂直，于是同样插入。离场还会顺手把「这一格已空」提前一帧
  （§11-I5/I6：离场像素不再阻挡任何东西），窗口更大。
- `unitSize == CellSize`（像素直径 = 格距）的关卡最明显：**只要间距掉到 1 格距以下就有可见插入**。
- `HasMovingPixel` 只看 `!exiting && moving` ⇒ **离场像素连 sweep 的节拍都不挡**。
- **验证手段**：`exitOnlyFromRow0`（§7）把离场点从「任意 row」改成「只在最前排」，用来量化
  「离场像素在网格内长距离飞行」占多少；`debugMoveLog` 打印每次「空格挑中像素」的当前格 → 目标格。

### 12.2 `exiting` 阶段自身的两类重合（更早一轮 `DebugOverlapCheck` 的结论）

> 该调试代码已移除。

1. **跨列收敛到同一入口点**：`ComputeEntryTarget` 的横向 clamp 会把横向位置不同（或都超出 clamp 范围）的多个
   像素映射到**同一个入口点**；`ApplyEntryQueue` 只对「同列」（横向差 ≤ `radius`）排队，跨列像素互不阻塞，
   最终撞到同一点。
2. **收尾瞬间塌陷**：前端像素 `EnterPhysical` 从批次里移除的当帧，后端失去阻挡对象，`MoveToward` 直接冲到
   同一入口点（前后间距从 ~0.44 塌到 0）。

修复方向（未实施）：给每个 `exiting` 像素分配**互不相同的入口槽位**（沿入口边把 `entranceWidth` 切成 N 槽，
或按到达顺序分配横向偏移）；并在前端进入物理时让后端保持 `entryQueueSpacing` 等待，而不是立即追平。

---

## 13. 倍乘门当前的接入点（代码现状）与回退记录

### 13.1 现在代码里有哪些门的介入（**保留着**）

`CrowdBufferZone.cs` 里有且仅有下面这五处 —— 这就是「最初那版」：

| 位置 | 做了什么 |
|---|---|
| `CanExit` 顶部 | **守卫**（**仅「不勾 row0」模式生效**，见 §7 说明 2）：`_extractGroup.MustWalkToGate(col,row)` 为真（在某道门的闭合区域内、但不在门格上）→ 不许直接离场，必须继续走到门格 |
| `SweepOnce` 步骤 0（退出者） | 站在门格上的像素若 `CanExit` 命中：`IsLeavingGate(st,-1,-1)` + `TakeGateBudget(st)` → **本体照常离场，但这一格不 `vacate`**（分身接管它），`multiplyPending = true` |
| `SweepOnce` 波前传播 | winner 被选中走进的 `cell` 已不属于同一道门 → `IsLeavingGate(winner, cell)` + `TakeGateBudget(winner)` → **本体照常前进，但旧门格不 `vacate`、也不进下一层 frontier**，`multiplyPending = true` |
| `SweepOnce` 原子 apply | 上述两处若 `multiplyPending`：生成分身 → `batch.extracting.Add(clone)` + `matchedOccupied[分身格] = true`（exits 分支「保持占用、不置 false」；movers 分支「先腾格、再让分身接管」） |
| 三个辅助方法 | `IsLeavingGate`（判「正要离开门格」；`(-1,-1)` = 直接从门格离场，换到别的门也算离开本门）、`TakeGateBudget`（首次进门格把预算初始化为 倍数−1，换门则重算）、`SpawnGateClone`（`SpawnPixel` + `SetClickable(false)` + `SetWalking`，**不写 `grid[,]`**，返回一个 `resolved=true` 的 `ExtractState`） |

`PixelGroup` 为它提供：`gates`、`gateGrid`（格 → 门）、`gateRegionMask`（该格是否在某门的区域内）、`gateMultiplier`，
以及查询 `GateAt` / `IsGateCell` / `IsInGateRegion` / `MustWalkToGate`；`GateItem` 提供 `cells` / `cellMask` / `regionMask`。
**门格不是障碍**（不进 `wallGrid`/`pipeGrid`/`boxGrid`，不改 `IsBlocked`）。

### 13.2 已撤销的改动（从「考虑目标格占用性」那条需求起）

- `CanExit` 的**第二条**守卫：站在门格上也不许直接离场（强迫先走一格）。
- 随之去掉 `SweepOnce` 步骤 0 的裂变分支（那条路变得不可达）、去掉 `IsLeavingGate` 的 `(-1,-1)` 分支。
- 整套调试日志：`debugGateLog`、裂变日志、`DebugCheckOccupancy`、`debugOccupancyDump`/`DebugDumpOccupancy`、
  `debugOverlapScan`/`ScanVisualOverlap`（逐帧边沿触发）、`NameOf`。
  （后来只补回了 `debugMoveLog` 一条，见文首。）
- 分身「弹入」：`gateClonePopInDelayFraction` + `ExtractState.popIn*` + `StepExtracting` 里的弹入 pass。

### 13.3 那期间日志实测到的事实（**仍然有效**，下一步分析可直接用）

- **裂变那一刻，本体与分身的 `localPos` 完全相同**：`StartCellMove` 只记 `animFrom`，本体的 transform 要到
  **下一帧**才真正移动；而分身是同一 tick 的 apply 阶段就被放到格心的。这直接来自 §11-I1
  （sweep 触发时本体精确停在格心上）。若「格距 == 像素直径」，要走满一整格才分开 ⇒ 整段移动都在互相穿插。
- 「按逻辑格查重」**必须排除 `exiting`**（§11-I6），否则会出现「同一格 4 颗」（3 颗 `exiting` + 1 颗 `moving`）的假象。
- 同列多颗 `exiting` 会同时飞向同一入口点 —— 与 §12.2 的既有现象是同一件事，**不是倍乘门引入的**。
- **已确认**：先前归因于「倍乘门」的重合，就是 §12.1 那条通则 —— **不开门也能复现**。
  但留心：倍乘门当年**还额外**带来一种重合 —— `SpawnGateClone` 把分身生成在本体**完全相同的坐标**上
  （实测两者 `localPos` 一模一样、距离 0），那是门独有的、与 §12.1 的「一进一出」不同源。
  将来重做倍乘门时，别指望两者表现一样。


---

## 14. 相关文档

- [`CrowdBufferDesign.md`](CrowdBufferDesign.md) — 缓冲区整体功能设计（几何、物理、释放调度、状态机）。
- [`CrowdBufferImplementations.md`](CrowdBufferImplementations.md) — 早期实现归档（软力、顺序投影硬约束）。
- [`ConveyorBeltDesign.md`](ConveyorBeltDesign.md) — 释放后闭环传送带（`conveyorZone` 分支）与 `DrainBeltKeep`。
