# CrowdMatch「失败判定漏判」排查文档

> 状态：**门禁 8 / 9 已按 §6 (b) 定案删除**（2026-09-23），其余结论仍为「仅分析」。本文记录
> 「失败条件已满足、却不触发失败（复活）」的排查结论：全部判定入口、全部「判定为非失败」的门禁、
> 每个门禁解除时是否有复查点，以及确认漏判的成因清单。
>
> 排查日期：2026-09-22（口径 (b) 定案并落地：2026-09-23）
> 范围：`GameController` / `ContainerGroup` / `ContainerItem` / `ConveyorBeltZone` / `ConveyorBelt` /
> `CrowdBufferZone` / `PixelGroup` / `BoxItem` / `ElevatorItem` / `GameManager` / `GameState`
>
> 历史存档见仓库根目录的 `失败判定逻辑分析.md`（描述的是已被取代的「每帧 `Update` 检测」旧管线，
> 其中的 §7.2 口径不一致已在事件驱动重构中消除）。

---

## 1. 失败只有一个入口

失败路径**唯一**：

```
TryCheckFail()                        GameController.cs:281
 ├─ _transitioning 为真        → return
 ├─ recordMode 为真            → return   （Record 模式不判失败，容器不参与吸收）
 └─ IsFail() 为真
      ├─ _transitioning = true
      ├─ GameState.GameFail()         （GameState.cs:26，纯状态位，State.Fail）
      └─ Invoke(nameof(DoRevive), 1.5f)
            └─ DoRevive()              GameController.cs:350
                 ├─ Revive()           （保留 reviveKeepBeltCount 个在带，其余溢出 + 缓冲区全部匹配后排车；
                 │                       无同色车的销毁并计入 ClearedPixelCount）
                 ├─ GameState.GameStart()
                 └─ _transitioning = false
```

**注意：`GameManager.GameFail()`（`GameManager.cs:127`，重置当前关 + 连败 +1）目前是死代码** ——
全工程没有任何调用方，失败的实际表现就是上面这条「原地复活、继续本关」。

胜负判定**都已事件驱动、不再每帧轮询**：失败见 `TryCheckFail`（本文主题），
胜利见 `ContainerGroup.TryCheckWin`——只在「某辆车完成匹配（最后一颗像素开始上车）」与「某辆车离开盘面（开始倒车）」
两个时点调用，口径已从**像素侧计数**（`ClearedPixelCount >= TotalPixelCount`）改为**载体侧**的
「板上不存在『未完成匹配』的车」（顺带修掉了倍乘门额外像素漏加总数导致的提前判胜）。

---

## 2. 四个检查点

`TryCheckFail` 全工程只有四处调用（外加 `Start` 里的订阅）：

| # | 触发时机 | 位置 |
|---|---|---|
| 1 | 像素上带（`TryEnter` 成功之后） | `ConveyorBeltZone.cs:305` `OnSlotPassedEntry` |
| 2 | 单个像素上车落定（jump 弹回完成 / 回退 lerp 完成） | `ContainerGroup.cs:364` `OnPixelConsumed` |
| 3 | 补位车抵达新位置（roll 或 lerp 完成） | `ContainerGroup.cs:378` `OnCarArrivedFront` |
| 4 | 网格内寻路的最后一颗像素走出网格（边沿检测） | `CrowdBufferZone.cs:559` → 事件 → `GameController.cs:120` 订阅 |

检查点 4 的边沿判定（`CrowdBufferZone.StepExtracting`）：

```csharp
if (_batches.Count == 0) return;                  // 早退：此时 hasGridPixels 必为 false
bool hadGridPixels = HasGridPathfindingPixels;    // 本帧开始
... （推进各批次）
if (hadGridPixels && !HasGridPathfindingPixels)   // 本帧结束
    OnGridPathfindingFinished?.Invoke();
```

即它只在「网格内寻路的像素**由有变无**」的那一帧发一次。`_batches` 里只剩 `exiting` 像素（已离开网格、
正在飞向入口）时不会再发 —— 这是刻意的，避免每帧重复通知。

---

## 3. `IsFail()` 的八道门禁

`GameController.cs:312`。门禁 1~4 是前置门槛，5~7 是「静止门槛」（还有进度就不判失败），最后逐槽遍历。
**原门禁 8 / 9（木箱释放中 / 升降台推进中）已于 2026-09-23 删除，原门禁 10（逐槽遍历）顺位改为门禁 8** ——
日志里的编号与本节一致。

| # | 门禁 | 「判定为非失败」的条件 | 代码 | 解除那一刻是否有检查点 |
|---|---|---|---|---|
| 1 | `conveyorZone == null \|\| belt == null` | 没有传送带 | :317 | 不适用（配置问题） |
| 2 | `TotalSlots <= 0` | 容量为 0 | :322 | 不适用（配置问题） |
| 3 | `OccupiedSlots < TotalSlots` | **传送带未满** | :327 | ✅ 只有「上带」能变满 → 检查点 1 |
| 4 | `containerGroup == null` | 没有容器组 | :332 | 不适用（配置问题） |
| 5 | `crowdBuffer.HasGridPathfindingPixels` | 网格里还有像素在寻路 | :342 | ✅ 检查点 4（边沿事件） |
| 6 | `containerGroup.HasPendingFrontTransition()` | 有车在补位 / 后排车已开盖且前方已放行 | :349 | ⚠️ 仅 `isRefilling` 路径 → 检查点 3；见 M2 / M5 |
| 7 | `containerGroup.consumingCount > 0` | 有像素正在上车 | :356 | ✅ 检查点 2 |
| 8 | 逐槽遍历：任一像素有同色可匹配容器 | 至少一个能出得去 | :362-373 | ✅ 容器被消耗 → 检查点 2 / 3 |

> **已删除（2026-09-23）**：原门禁 8「有木箱正在释放」（`releasingBoxesCount`）、
> 原门禁 9「有升降台正在推进」（`advancingElevatorsCount`）。删除理由见 §4 M1，口径见 §6 (b)。

### 3.1 门禁 6 的两个分支

`ContainerGroup.cs:830`：

```csharp
if (it.isRefilling)                                  // 分支 A：正在补位移动
    return true;
if (row >= 1 && it.lidOpened && IsFrontCleared(col, row))   // 分支 B：后排车已开盖且前方放行
    return true;
```

- 分支 A 的解除：`MoveContainer` 完成回调把 `isRefilling = false` 并调 `OnCarArrivedFront`
  → 检查点 3。**但**若该回调因故未触发，`isRefilling` 永久为真（见 M5）。
- 分支 B 的解除：那辆车被补位到 row 0（row 0 不参与本分支）。而**补位只从
  `StartContainerExit` 的完成回调进入**（`RefillColumn`，`ContainerGroup.cs:710`）。
- `lidOpened` 是**锁存**：只有置 `true` 的地方（`ContainerItem.cs:218` `HideLid`、
  `:229` `OpenLid`），全工程**没有任何 reset**。
- 分支 B 与暴露 / 开盖共用 `IsRowReleased`（`ContainerGroup.cs:170`），
  刻意排除「装满但同组还没齐、必须继续堵住整列的绳车」（`IsWaitingRopeCar`），
  否则被绳车堵死的列会让本方法恒为真。

### 3.2 已核对为正常的部分

| 项 | 结论 |
|---|---|
| `ShouldLeave` 与 `IsFail` 的口径 | ✅ **已一致**：`ShouldLeave` 用的就是 `ContainerGroup.FindMatchableInColumn`（`ConveyorBeltZone.cs:449`），`HasMatchableContainerOfColor` 是它的全列扫描。旧文档 §7.2 的「失败判定比真正能离开更宽松」已不存在 |
| 传送带容量 | ✅ `ConveyorBelt.slotCount` 是固定序列化值，不随速度 / 清带变化；带「变满」只能由 `TryEnter` 造成 |
| 槽位占用记账 | ✅ `TryEnter` 同步占槽（`ConveyorBelt.cs:405`），`VacateSlot` 在离开 / 清槽时同步清（`:624`）；`OccupiedCount` 是实时扫描 |
| 倍乘门分身 | ✅ 分身与本体同批次（`CrowdBufferZone.cs:756`），仍被门禁 5 覆盖 |
| Record 模式 | ✅ 已在 `TryCheckFail` 里旁路（旧文档 §7.3 的隐患已处理） |
| 上带路径 | ✅ `crowdBuffer.CollectNearest()` 是唯一入带来源，上带成功必然走到检查点 1 |

---

## 4. 确认的漏判成因

按可能性排序。M1（最像实际遇到的现场）已处理；其余仍未改。

### M1 · 木箱 / 升降台挡住判定 —— **已处理：整个门禁与计数管线删除**（2026-09-23）

原门禁 8 / 9 由 `PixelGroup.releasingBoxesCount` / `advancingElevatorsCount` 驱动，
而这两个计数归零时**没有任何人调用 `TryCheckFail`**（`OnBoxReleaseFinished` / `OnElevatorAdvanceFinished`
只做自减），于是「挡一下」变成「永久挡住」：只有玩家再点一颗能走动的像素（凑出检查点 4）才自愈。

定案时确认了更根本的一点，也是**删除而非补复查点**的理由：

- **判决只取决于两件事**——门禁 3（带满）与门禁 8（带上每个槽位像素在同色容器里找不到可匹配的）。
- 木箱释放 / 升降台推进**既不碰传送带、也不碰容器**：`HasMatchableContainerOfColor`
  （`ContainerGroup.cs:809`）只读容器；而带满了就没有像素能再上带，容器集合也不会再变。
- 所以那一刻判决**已经终局**：这两条门禁挡不住「误判失败」，只是把一个**正确**的判决往后推。

因此直接删除：门禁 8 / 9、两个计数、`OnBoxReleaseFinished` / `OnElevatorAdvanceFinished` /
`OnElevatorAdvanceStarted` 三个方法与各自调用点（`BoxItem.cs` / `ElevatorItem.cs`）。
原门禁 10（逐槽遍历）顺位为门禁 8。删掉后 M1 的僵死路径不复存在。

**残留因素（不阻塞判定，但需要实测观感）**：木箱 / 升降台在揭示动画期间仍会**当场把像素写进 `grid`**
（`BoxItem.cs:406`、`ElevatorItem.cs:551`），只是 `placing = true` + 不可点、`RefreshExposed()` 要等
动画收尾。若动画中途判失败弹复活面板，揭示动画会继续跑完并落在一个正在复活的盘面上。
`Revive()` 只重排「带上 + 缓冲区」的像素（动画中的像素既不在带上也不在缓冲区），所以**计数不会错**，
但「复活与揭示同时进行」的观感需要你实际看一眼是否可接受。

### M2 · `OnPixelConsumed` 里检查点早于状态变更（顺序问题）

`ContainerGroup.cs:359-371`：

```csharp
consumingCount = Mathf.Max(0, consumingCount - 1);
var gc = GameController.Instance;
if (gc != null)
    gc.TryCheckFail();          // ← 判定在这里
if (!isLast)
    return;
if (destroyInPlace)
    DestroyRopeGroupInPlace(container);   // ← 状态变更在这里（整组原地销毁）
else
    TryExitIfAtFront(container, col);
```

`DestroyRopeGroupInPlace` → `DestroyContainerInPlace`（`ContainerGroup.cs:556`）会
`grid[col,row] = null` 并让后车**瞬移补位**（直接写 `grid` + `transform.localPosition`，
**不经过 `OnCarArrivedFront`**）。也就是说这次销毁对「可匹配容器」与门禁 6 的全部影响都发生在
判定**之后**，且没有复查。若正是这次销毁移除了带上像素的唯一同色容器 → 该判失败时不判。

### M3 · 传送带未满但已死锁 → 恒不判失败（口径问题，待确认）

门禁 3 要求带**满**（`OccupiedSlots == TotalSlots`）。若剩余像素根本到不了传送带
（提取卡死 / 全部被冰冻住或被木箱盖住 / 寻路被围死），槽位永远填不满 → `IsFail` 恒 false
→ **既不判失败也不判胜利**，关卡静默卡死，复活永远不来。

这是「玩家已无解、却不触发失败」最直接的一种，但它属于**口径**问题不是 bug：
当前设计明确规定「带未满 = 还有空位 = 不算死锁」。是否要把这种死锁也算失败，需要拍板。

### M4 · 提取中的像素永久卡在网格里 → 门禁 5 恒为真

`CrowdBufferZone.PickBestPixel`（`:1035`）：

```csharp
if (dist[nx, nz] <= myDist)
    continue;   // 必须严格更接近出口（dist 更小）才前进，防原地打转
```

**成因链**：

1. 某颗已点击像素的出口距离一旦变为不可达（例如被随后释放的木箱 / 升降台像素、
   或管道新蛇围死。注意 `dist` 把「不在本批次的像素」当墙），它的邻居 `dist` 都不更小；
2. 它永远不动 → `HasGridPathfindingPixels` 恒为 true → 门禁 5 永久挡住判失败；
3. 而检查点 4 是**边沿事件**（只在「网格内像素由有变无」时发），这批卡住的像素
   **永远不会发事件** → 没有任何自愈机会；
4. 附带后果：倍乘门的分身依赖本体走出门格，本体卡住 → 分身永不生成。

`CanReachFront`（`GameController.cs:694`）只在**点击那一刻**校验一次，之后网格变化不重算。

### M5 · 门禁 6 的 `lidOpened` 锁存 + 解除依赖补位回调

两个独立的弱点：

- **分支 B 依赖「补位到 row 0」作为唯一解除途径**。若出现「前方已放行、但不会补位」的状态
  （目前只在出库动画窗口内短暂出现，因为 `RefillColumn` 紧随 `StartContainerExit` 的完成回调），
  该方法会恒为真。正常布局下不会长期停留在这个状态，但它是结构性脆弱点。
- **分支 A 依赖 `MoveContainer` 的完成回调**。`MoveContainer` 有两条路径都会调
  `OnCarArrivedFront`（`:735` roll 回调 / `:756` Lerp 分支），但若 roll 完成回调因
  `_rollPhase` 或对象被销毁而没触发，`isRefilling` 就永久为真 → 门禁 6 永久挡住。
  **本条未能排除干净，列为待确认。**

### M6 · 反向情形（仅记录，未修）

`TryCheckFail` 只看 `_transitioning` 与 `recordMode`，**不检查 `GameState`**。
若 `GameState` 因外部原因离开 `Start`（暂停 / Idle）而 `_transitioning` 仍为 false，
判定仍会触发 —— 这是「多判」方向，不属于本次的漏判，仅记录。

---

## 5. 验证与定位建议

我不能启动编辑器，**M4 / M5 是否真的可达需要实际复现**。建议的自助定位手段：

1. 打开 `GameController.debugClickLog`，观察 Console 里 `[复活] 溢出=…` 是否出现；
2. 用 `CrowdBufferZone.DescribeExtraction()`（`:239`，诊断用快照）确认是「提取卡死」
   还是「根本没在提取」—— 前者指向 M4，后者指向 M1；
3. ✅ **已实现的诊断日志**：`GameController.debugFailLog`（默认开）。每个检查点在**没判失败**时打印一行，
   一次复现即可在 M1~M5 之间定位，比盲改省事。行格式：

   ```
   [失败判定] 检查点=像素上带 ｜ 传送带=7/12 ｜ 未判失败：门禁3 传送带未满（还有空槽可进像素）
   ```

   | 字段 | 取值 |
   |---|---|
   | 检查点 | `FailCheckpoint` 常量：`像素上带` / `上车落定` / `补位车抵达前排` / `网格寻路排空` / `未标注` |
   | 传送带 | `OccupiedSlots / TotalSlots`（没有传送带时打 `无传送带`） |
   | 未判失败的原因 | `IsFail(out reason)` 输出的**第一条**被挡下的门禁（编号见 §3）+ 实测计数值 |

   被挡在判定之前的情形也会打印：`已锁定 _transitioning（胜负过渡中）`、`Record 模式不判失败`。
   与上一条**完全相同**的行不重复打印（复活期间会有几十次上车回调，行内容一模一样），
   真判失败时会清空这条去重记忆。

---

## 6. 处理方向

| 编号 | 方向 | 状态 |
|---|---|---|
| M1 | ~~在 `OnBoxReleaseFinished` / `OnElevatorAdvanceFinished` 末尾各补一次 `TryCheckFail()`~~ | ✅ **口径 (b) 选了「不保留」：门禁 8 / 9 与两个计数整体删除**（2026-09-23） |
| M2 | 把 `TryCheckFail()` 移到两条状态变更分支**之后**，或分支结束后补一次 | 待处理 |
| M3 | 若「带未满的死锁」也要算失败：加「无可推进像素」判定或带未满的静止看门狗 | **待口径 (a)** |
| M4 | 把「已点击但无法推进的提取」与「正在推进的提取」区分开（前者不应挡住判失败）；或给提取加放弃 / 回滚路径 | 待处理 |
| M5 | `HasPendingFrontTransition` 只认 `isRefilling`；或补位落定时清 `lidOpened`；或给 `isRefilling` 加超时兜底 | 先确认分支 A 的漏洞是否可达 |

### 两个口径

- **(a) 传送带未满、但已彻底死锁（剩余像素都到不了带），算不算失败？** —— **仍未定**
  算 → 必须做 M3；不算 → M3 仅记录。
- **(b) 木箱 / 升降台释放动画期间的「不判失败」还要不要保留？** —— **已定：不保留**
  理由见 §4 M1 —— 这两条门禁改变不了判决本身（判决只看带满 + 带上像素有无同色可匹配容器，
  而木箱 / 升降台既不碰带也不碰容器），只是把正确的判决往后推；推完又没人复查 → 僵死。
  2026-09-23 已删除。

---

## 7. 相关代码索引

| 关注点 | 文件 | 方法 / 字段 |
|---|---|---|
| 失败入口 | `Assets/Scripts/Gameplay/GameController.cs` | `TryCheckFail` / `IsFail` / `DoRevive` / `Revive` |
| 检查点 1 | `Assets/Scripts/Gameplay/ConveyorBeltZone.cs` | `OnSlotPassedEntry`（:305） |
| 检查点 2 / 3 | `Assets/Scripts/Gameplay/ContainerGroup.cs` | `OnPixelConsumed`（:364）/ `OnCarArrivedFront`（:378） |
| 检查点 4 | `Assets/Scripts/Gameplay/CrowdBufferZone.cs` | `StepExtracting`（:558）/ 事件 `OnGridPathfindingFinished`（:215） |
| 门禁 6 判定 | `Assets/Scripts/Gameplay/ContainerGroup.cs` | `HasPendingFrontTransition`（:830）/ `IsFrontCleared`（:851）/ `IsRowReleased`（:170）/ `IsWaitingRopeCar`（:540） |
| 门禁 8 / 9 计数 | `Assets/Scripts/Gameplay/PixelGroup.cs` | 已删除（原 `OnBoxReleaseFinished` / `OnElevatorAdvanceFinished`，见 §4 M1） |
| 匹配口径（共用） | `Assets/Scripts/Gameplay/ContainerGroup.cs` | `FindMatchableInColumn`（:246）/ `HasMatchableContainerOfColor`（:809）/ `IsOpen`（:148） |
| 传送带离开判定 | `Assets/Scripts/Gameplay/ConveyorBeltZone.cs` | `ShouldLeave`（:412）/ `OnLeave`（:460） |
| 槽位记账 | `Assets/Scripts/Gameplay/Conveyor/ConveyorBelt.cs` | `TryEnter`（:390）/ `VacateSlot`（:624）/ `OccupiedCount`（:443） |
| 寻路卡死点 | `Assets/Scripts/Gameplay/CrowdBufferZone.cs` | `PickBestPixel`（:1035）/ `SweepOnce`（:581） |
| 原地销毁 / 瞬移补位 | `Assets/Scripts/Gameplay/ContainerGroup.cs` | `DestroyRopeGroupInPlace`（:628）/ `DestroyContainerInPlace`（:556） |
| 补位 | `Assets/Scripts/Gameplay/ContainerGroup.cs` | `RefillColumn`（:710）/ `MoveContainer`（:726） |
| 盖子锁存 | `Assets/Scripts/Gameplay/ContainerItem.cs` | `lidOpened`（:77）/ `HideLid`（:216）/ `OpenLid`（:225） |
| 点击可达性（只算一次） | `Assets/Scripts/Gameplay/GameController.cs` | `CanReachFront`（:694） |
| 状态机 / 数据 | `Assets/Scripts/Core/GameState.cs`、`Assets/Scripts/Core/GameManager.cs` | `GameFail`（GameState:26 / GameManager:127，后者为死代码） |
