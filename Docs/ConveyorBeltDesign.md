# CrowdMatch「传送带」功能设计文档

> 状态：**已实现（槽位直接收集版）**。本文描述"像素离开缓冲区出口后，不再进入单点集结点，而是进入一个闭环传送带；
> 传送带带着像素循环，当像素到达传送带对侧、并进入某 Container 正前方的一定范围时，才与同色 Container 匹配并被吸收"的完整玩法效果，
> 以及它与现有 `CrowdBufferZone` / `ContainerGroup` / `GameController` 的集成方式。
>
> 底层复用 skill `unity-conveyorbelt`（`ArcPath` + `ArcPathController` + `ArcPathEditor` + `ConveyorBelt` + `IConveyorItem`），
> 并对 `ConveyorBelt` 做「槽位承载物（carrier）」改造以支持「reparent + localPosition 平滑」的无瞬移进入。

---

## 1. 概述

当前 Demo（物理过闸版）里，像素过闸释放后先到 `gapPoint` 再匀速到 `collectPoint`（= `gatherPoint`），
置 `arrivedAtGatherPoint` 加入 `gatheredItems`，随后 `ContainerGroup.ProcessConsumption` 从池里"拉"同色像素进容器。

本次把「释放后到集结点」这一步，替换为一段**传送带运送**体验：

1. 像素留在缓冲区出口（物理队列）排队；传送带每个**空槽位**越过**入口关口**时，直接从出口取最近的一颗像素上车。
2. 像素在入口处 `reparent` 到该槽位的**承载物（carrier）**上（保持世界位置不瞬移），`localPosition` 平滑到 0，成为该槽位的乘员。
3. 传送带带动 carrier（以及其上的像素）在**闭环轨迹**上循环（近侧直线 → 180°圆弧 → 远侧直线 → 180°圆弧）。
4. 当像素到达**对侧（远侧直线）**、并进入某个**同色非空前排 Container 的正前方范围**时，`ShouldLeave` 触发 → 像素离开传送带。
5. 离开的像素被同色 Container 吸收：`Consume()` 扣容量 → 像素 Lerp 进容器 → 销毁 → 容器耗尽则 `DisappearAndRefill`。

匹配方向由「容器从 gatheredItems 里拉」反转为「传送带把像素推到正前方的容器」。

---

## 2. 目标与非目标

### 目标
- 把"释放 → 单点集结"替换为"释放 → 传送带循环 → 远侧匹配进容器"。
- 闭环体育场传送带：近侧上车、远侧匹配；匹配仍要求 `colorId` 相同。
- 无瞬移进入：reparent 到 carrier + `localPosition → 0` 平滑。
- 背压：槽位过关口时出口无像素（或该槽被占）则自然跳过，像素留在缓冲区出口物理队列里排队。
- 与现有 `ContainerGroup` 消费契约（`Consume` / `DisappearAndRefill`）复用。

### 非目标（本期不做）
- 不做性能优化（闭环轨迹采样 + 槽位定位，Demo 规模无压力）。
- 不做像素在传送带上的碰撞/变道（槽位等距、天然排队）。
- 不改 `ContainerGroup` 的容器生成逻辑。
- 不做"匹配失败兜底"（像素到远侧但正前方无同色前排容器时，无限绕圈等待——见 §11）。

---

## 3. 概念与术语

| 术语 | 含义 |
|---|---|
| **闭环传送带（Conveyor Belt）** | 由 `ArcPathController` 定义的闭合轨迹（近侧直线 + 180°圆弧 + 远侧直线 + 180°圆弧），像素在槽位上等距循环 |
| **槽位（Slot）** | 传送带上的固定位置，`slotCount` 个等距分布；`slots[i]` 为 null 即空槽 |
| **承载物（Carrier）** | 每个槽位对应的 Transform（`ConveyorBelt` 的子物体，scale=1），传送带每帧移动 carrier 的世界坐标，像素作为 carrier 的子物体被带着走 |
| **近侧（Entry Side）** | 闭环的其中一条直线段，紧邻缓冲区缺口，像素在此上车 |
| **对侧 / 远侧（Match Side）** | 闭环的另一条直线段，正对 ContainerGroup 前排，像素在此匹配 |
| **正前方范围（Match Range）** | 以 Container 前排位置为中心的 2D 判定范围（`matchRangeX` 横向 + `matchRangeZ` 纵向），像素落入且同色即匹配 |
| **入口关口（Entry Gate）** | 一个由世界 X 坐标标定的位置；近侧槽位世界 X 从 > gateX 跨到 ≤ gateX 时触发一次收集 |
| **背压（Backpressure）** | 槽位过关口时出口无球（或该槽被占）则跳过收集，像素留在缓冲区出口物理队列里等待 |

---

## 4. 几何定义（2D XZ 平面）

传送带是一条**闭合的体育场（stadium）轨迹**，由 `ArcPathController` 的 4 个分段串成：

```
                    远侧直线段（正对 Container 前排）
        ┌─────────  ●────────────────────────●  ─────────┐
        │           │  (像素在此匹配)            │          │
        │180°圆弧   │                          │  180°圆弧 │
        │           │  近侧直线段（像素在此上车）  │          │
        └─────────  ●────────────────────────●  ─────────┘
                        ▲
                    入口关口（gateX，紧邻缓冲区缺口）
```

- 两条直线段沿 **X** 方向，长度 ≈ `ContainerGroup` 的横向跨度（`columns × xSpacing`）。
- 两条 **180° 圆弧** 把两条直线段首尾相连，圆弧半径 = 传送带"宽度"的一半（近侧/远侧在 Z 方向的间距）。
- 第一段（近侧直线）的 `startPosition / startEulerAngles / startTangent / startNormal` 手动指定，后续三段由 `InitializePaths()` 自动衔接。
- 像素所在平面 y=0，轨迹采样与定位都在 XZ 平面（carrier 的 y 恒 0）。
- 近侧沿 **-X** 方向运动（槽位从 x > gateX 跨到 x ≤ gateX），这是 `ConveyorBelt` 过关口检测的默认方向假设。

> 布局（屏幕下 → 上）：`PixelGroup` → `CrowdBufferZone`（入口 → 缺口）→ 传送带近侧 → 传送带远侧 → `ContainerGroup` 前排。
> 远侧直线段应落在 Container 前排正下方（Z 略小于 Container 前排 Z），使"正前方"在空间上自然成立。

---

## 5. 像素生命周期状态机

```
InGrid ──(点击/FloodFill)──▶ Matched
                                │ 提取(sweep) → 物理过闸
                                ▼
                            Physical（缓冲区，刚体挤向缺口）
                                │
                    ┌───────────┴────────────┐
                    │ 有传送带               │ 无传送带(fallback)
                    ▼                        ▼
              槽位过关口时 CollectNearest    TryRelease → MoveToCollect(gap→collectPoint)
                    │ 直接上车（reparent）            │
                    ▼                        ▼
               OnBelt（槽位循环）             Arrived(gatheredItems)
                    │
                    ▼
               远侧 + 同色前排容器正前方?
                    │ 否 → 继续循环（无限绕圈）
                    │ 是 → OnLeave（解绑 carrier）
                    ▼
               EnterContainer（Consume + Lerp进容器 + 销毁 + 容器耗尽补位）
```

> 有传送带时，像素**始终停留在缓冲区物理队列里**，直到某个空槽位过关口时被 `CollectNearest()` 直接取走。
> 「上车」不再是独立的释放步骤，而是每个槽位过关口时独立触发的一次收集行为。

---

## 6. 运动模型（carrier 模型）

传送带本身保持 skill 的"等距槽位 + 归一化 offset 循环"，但把**被定位对象从"像素"改为"承载物 carrier"**：

1. `ConveyorBelt.Initialize()` 创建 `slotCount` 个空子物体作为 `carriers[]`（scale=1）。
2. 每帧 `Update`：`offset += dt * speed / cycleTime`（`%1`），对每个槽位 `i` 计算
   `slotOffset = (offset + i/slotCount) % 1`，采样
   `carriers[i].position = path.GetGlobalPosition(slotOffset * totalLength)`、
   `carriers[i].rotation = Quaternion.Euler(path.GetGlobalEulerAngles(...))`。
3. 像素作为 `carriers[i]` 的子物体（`localPosition = 0` 稳态），跟随 carrier 循环。

### 6.1 无瞬移进入（关口驱动，槽位直接收集）

`ConveyorBelt` 每帧在 `ApplyPositions` 里定位每个 carrier 后，用 `_prevSlotX[]` 检测过关口：

```csharp
// ConveyorBelt.ApplyPositions（每槽独立判定）
float gateX = entryGate != null ? entryGate.position.x : transform.position.x;
float currX = carriers[i].position.x;
if (!firstTrack && _prevSlotX[i] > gateX && currX <= gateX)
    SlotPassedEntry?.Invoke(i);          // 该槽过关口 → 宿主收集
_prevSlotX[i] = currX;
```

`ConveyorBeltZone.OnSlotPassedEntry(slotIndex)` 响应：

```csharp
// 1. 该槽过关口且仍空 → 取出口最近像素
if (belt.GetItem(slotIndex) != null) return;
var pixel = crowdBuffer.CollectNearest();   // 取距缺口最近、releaseRadius 内的像素并解物理
if (pixel == null) return;

// 2. 上车：reparent 到该槽 carrier（保持世界位置 → 不瞬移）
belt.TryEnter(pixel, slotIndex);   // 内部：pixel.Transform.SetParent(carriers[slot], worldPositionStays:true)

// 3. SettleRoutine：localPosition 平滑到 0（每个像素一条协程，互不阻塞）
```

> 由于 reparent 时用 `worldPositionStays:true`，像素世界位置不变（零瞬移）；随后 `localPosition → 0` 让它平滑追赶上移动中的 carrier。
> 「进哪个槽」由「哪个槽过关口」天然决定；像素直接从缺口上车，无需先找最近空槽，local 偏移最大不过半个槽距，追赶动画很短。

### 6.2 离开（解绑 + 吸收）

`CheckLeave` 每帧对每个占用槽调 `ShouldLeave`；命中后**先把像素从 carrier 解绑**（`SetParent(null, true)`）再清槽、再回调 `OnLeave`，
宿主 `OnLeave` 里做吸收（见 §7）。

---

## 7. 匹配与离开（ShouldLeave / OnLeave）

匹配语义（R3 确认 + 颜色一致）：

```csharp
// ConveyorBeltZone.Start 注入
belt.ShouldLeave = item =>
{
    var pixel = (PixelItem)item;
    return containerGroup.FindFrontContainerInFrontOf(pixel, matchRangeX, matchRangeZ) != null;
};
belt.OnLeave = item =>
{
    var pixel = (PixelItem)item;
    var container = containerGroup.FindFrontContainerInFrontOf(pixel, matchRangeX, matchRangeZ);
    if (container != null) containerGroup.ConsumePixel(pixel, container);
};
```

`ContainerGroup.FindFrontContainerInFrontOf(pixel, matchRangeX, matchRangeZ)`：

```
遍历每列 col：
    front = GetItem(col, 0)
    if front == null || front.IsEmpty || front.colorId != pixel.colorId: continue
    dx = |front.transform.position.x - pixel.x|
    dz = |front.transform.position.z - pixel.z|
    if dx <= matchRangeX && dz <= matchRangeZ: 记为候选
返回候选里 dx 最小者（正前方最近），无则 null
```

`ContainerGroup.ConsumePixel(pixel, container)`：沿用现有 `MovePixelToContainer` 抽掉"找像素"后的逻辑——
`Consume()` 扣容量 → 协程 Lerp 像素到容器位置 → 销毁像素 → 若 `isLast` 则 `DisappearAndRefill`。

> 由于传送带按槽位等距错开、且 `CheckLeave` 一帧对同一槽位最多触发一次，
> 两像素同帧抢同一容器的竞态概率低；仍建议 `ConsumePixel` 开头加 `if (container.IsEmpty) return;` 兜底（见 review H1/M1）。

---

## 8. 背压调度（关口驱动的槽位直接收集）

R2 确认：**释放完全交给槽位过关口驱动**，`minReleaseInterval` 不再参与传送带模式；像素始终停留在缓冲区物理队列，直到被某个过关口的空槽取走。

`CrowdBufferZone` 侧：`TryRelease` 在传送带模式下直接返回，不再按帧放行：

```csharp
private void TryRelease()
{
    // 传送带模式：释放由「槽位过关口」驱动（ConveyorBeltZone 调 CollectNearest），这里不再按帧释放
    if (conveyorZone != null) return;

    // ... 以下仅 fallback 路径：节流 + 找 front + Release
}

public PixelItem CollectNearest()   // 由 ConveyorBeltZone.OnSlotPassedEntry 调用
{
    // 找距缺口最近、releaseRadius 内的像素 → _physical.Remove → DetachPhysics → return
}
```

> 背压效果：槽位过关口时若出口无球（`CollectNearest()` 返回 null）或该槽仍被占（`GetItem(slotIndex) != null`），
> 本次收集跳过，像素留在 `_physical`（仍是刚体）被后排挤在 gap 封口墙前排队，等下一个空槽过关口再被取走——正是"挤地铁"的延续。

---

## 9. 组件与 API 设计

### 9.1 `ConveyorBelt`（改造 skill，`Gameplay/Conveyor/ConveyorBelt.cs`，namespace `CrowdMatch`）

| 分类 | 成员 | 说明 |
|---|---|---|
| 轨迹 | `ArcPathController path` | 闭环轨迹引用 |
| 运动 | `float cycleTime = 6` | 循环一周秒数 |
| 运动 | `float speed = 1` | 速度倍率 |
| 槽位 | `int slotCount = 12` | 槽位总数（= 传送带总容量） |
| 入口 | `Transform entryGate` | 收集关口（只用其世界 X 坐标） |
| 入口 | `Action<int> SlotPassedEntry` | 槽位过关口时触发（参数 = 槽位索引），由宿主注入 |
| 钩子 | `Func<IConveyorItem,bool> ShouldLeave` | 离开判定（宿主注入） |
| 钩子 | `Action<IConveyorItem> OnLeave` | 离开回调（宿主注入） |
| 方法 | `Initialize()` | 建 `slots[]` + `carriers[]`，`path.InitializePaths()` |
| 方法 | `bool TryEnter(IConveyorItem, int)` | 置槽位 + `item.Transform.SetParent(carriers[i], true)` |
| 方法 | `IConveyorItem GetItem(int)` | 取槽位乘员 |
| 方法 | `void ClearSlot(int)` | 解绑 + 清空（不触发 OnLeave） |
| 属性 | `int OccupiedCount` | 已占用槽位数（供 UI） |
| 方法 | `Vector3 GetSlotWorldPosition(int)` | 某槽位当前世界坐标（供背压/调试） |
| — | `Update()` | `Advance → ApplyPositions(写 carrier + 过关口检测) → CheckLeave` |

### 9.2 `ConveyorBeltZone`（宿主，`Gameplay/ConveyorBeltZone.cs`）

| 分类 | 字段 | 默认 | 说明 |
|---|---|---|---|
| 引用 | `ConveyorBelt belt` | — | 传送带 |
| 引用 | `ContainerGroup containerGroup` | — | 容器组（匹配/吸收目标） |
| 引用 | `CrowdBufferZone crowdBuffer` | — | 缓冲区（出口像素来源） |
| 匹配 | `float matchRangeX` | 0.6 | 正前方横向判定（约半列间距） |
| 匹配 | `float matchRangeZ` | 0.8 | 正前方纵向判定（远侧到容器前排的间隙） |
| 方法 | `Start()` | 注入 `ShouldLeave` / `OnLeave`，订阅 `SlotPassedEntry` |
| 方法 | `OnSlotPassedEntry(int)` | 槽位过关口：取最近像素 `TryEnter` 上车 + 起 `SettleRoutine` |
| 方法 | `SettleRoutine(PixelItem)` | localPosition 平滑到 0（每像素一条协程） |
| 方法 | `ShouldLeave(IConveyorItem)` / `OnLeave(IConveyorItem)` | 远侧同色前排容器匹配判定 / 吸收 |
| 属性 | `int OccupiedSlots` / `int TotalSlots` | 供 UI |

### 9.3 其余改动

| 位置 | 改动 |
|---|---|
| `PixelItem` | 实现 `IConveyorItem`（加 `public Transform Transform => transform;`） |
| `ContainerGroup` | 新增 `FindFrontContainerInFrontOf` / `ConsumePixel`；`ProcessConsumption` 保留为 fallback（gatheredItems 空时天然 no-op） |
| `CrowdBufferZone` | 新增 `conveyorZone` 字段、`CollectNearest()`、`DetachPhysics()`；`TryRelease` 在 conveyor 模式下直接 return，`Release` 退化为 fallback-only |
| `GameController` | 新增 `conveyorZone` 字段；`UpdateCountText` 改为显示占用/总容量 |
| `ArcPathController` | 不加 Gizmos；由 `Editor/ArcPathEditor.cs`（skill 的 Editor 模块）提供 Scene 预览 + Inspector 测试控制 |

### 9.4 UI（R5 确认）

`GameController.UpdateCountText`：

```csharp
if (gatherCountText != null)
{
    if (conveyorZone != null)
        gatherCountText.text = conveyorZone.OccupiedSlots + " / " + conveyorZone.TotalSlots;
    else
        gatherCountText.text = gatheredItems.Count.ToString();
}
```

---

## 10. 数据流

```
ConveyorBelt.Update：offset 推进 → ApplyPositions（移动 carrier + 过关口检测）
    │  某槽位世界 X 从 > gateX 跨到 ≤ gateX → SlotPassedEntry(i)
    ▼
ConveyorBeltZone.OnSlotPassedEntry(i)
    │  槽位空? → crowdBuffer.CollectNearest()（取缺口最近像素 + 解物理）
    │  → belt.TryEnter(pixel, i)（reparent，不瞬移）→ SettleRoutine（localPosition→0）
    ▼
ConveyorBelt 继续循环 → 像素随 carrier 循环
    │  CheckLeave 每帧对占用槽调 ShouldLeave
    ▼
ShouldLeave = 远侧 + 同色前排容器正前方（2D 范围）
    │  true → 解绑 carrier → OnLeave
    ▼
ContainerGroup.ConsumePixel(pixel, container)
    │  Consume() → 像素 Lerp 进容器 → Destroy → 若 isLast → DisappearAndRefill
```

> fallback 数据流（`conveyorZone == null`）与现状一致：`TryRelease → Release → MoveToCollect → OnArrived → gatheredItems → ProcessConsumption`。

---

## 11. 边界情况

| 场景 | 处理 |
|---|---|
| 槽位过关口但出口无球 | `CollectNearest()` 返回 null，本次收集跳过；像素留在 gap 物理队列排队 |
| 槽位过关口但该槽仍被占（异常） | `GetItem(slotIndex) != null` → 直接 return，跳过本次收集 |
| 上一像素尚未到 local0，下一槽位过关口 | 每个槽位独立收集、`SettleRoutine` 每像素一条协程，互不阻塞，立即取新像素 |
| 多个像素同时在远侧正前方 | 各自命中各自列的容器；同列同色由 `CheckLeave` 逐槽位触发，`ConsumePixel` 开头 `IsEmpty` 兜底 |
| 像素到远侧但正前方无同色前排容器 | `ShouldLeave=false`，像素无限绕圈等待（R3 确认接受） |
| 某颜色容器全部耗尽 | 该颜色像素永久绕圈（既有边界，传送带下更显眼，本期不兜底） |
| 传送带空 | `Update` 只推进 offset，无槽位写操作，零开销 |
| 像素在传送带上被销毁（异常） | `ApplyPositions` 用 `slots[i].Transform == null` 跳过；正常流程只在 `OnLeave` 后销毁（槽已先清），不触发 |
| carrier scale 非 1 | 会导致像素世界尺寸缩放；实现时强制 carrier scale=1 |
| `entryGate` 未赋值 | 关口 X 回退用 `transform.position.x`（belt 根 X）；建议显式放置一个空 GameObject 标定 |
| 传送带反向（近侧沿 +X） | 把 `ConveyorBelt.ApplyPositions` 的过关口判断号翻转（`< gateX && currX >= gateX`） |
| `conveyorZone` 未赋值 | `CrowdBufferZone` 回退到旧 `collectPoint`+`gatheredItems`（向后兼容） |

---

## 12. 调参指南（初值 + 方向）

| 参数 | 影响 | 调大 | 调小 |
|---|---|---|---|
| `slotCount` | 传送带总容量 / 承载上限 | 更宽裕、更少背压等待 | 更挤、更像排队 |
| `cycleTime` | 循环一周时长 | 更慢、更从容 | 更快、节奏更紧 |
| `speed` | 运动倍率（等价于 `1/cycleTime` 缩放） | 更快 | 更慢 |
| `matchRangeX` | 正前方横向判定 | 更易命中（跨列） | 更严格对齐列 |
| `matchRangeZ` | 正前方纵向判定 | 更易命中 | 只在紧贴容器时才匹配 |
| `entryGate.x` | 收集关口位置 | 越靠缓冲区缺口越早收集 | 越靠传送带远侧越晚收集 |
| 直线段长度 | 覆盖容器横向跨度 | — | 需 ≥ `columns × xSpacing` |
| 圆弧半径 | 传送带宽度（近/远侧 Z 间距） | 更宽、两段分离更远 | 更窄、更紧凑 |

---

## 13. 决策记录（已确认）

1. **轨迹可视化**：`ArcPath` 是 `[Serializable]` 普通类不能挂 gizmo；用 skill 的 `ArcPathEditor`（Editor 模块，`OnSceneGUI` + Inspector 测试控制）提供 Scene 视图预览，`ArcPathController` 本体不加 Gizmos。
2. **进入方式（R1）**：`ConveyorBelt` 加 per-slot carrier，belt 移动 carrier；像素 reparent 到 carrier + `localPosition→0`，无瞬移进入。
3. **背压/入口（R2）**：改为「关口驱动的槽位直接收集」——释放完全由槽位过关口驱动，`minReleaseInterval` 仅保留给 fallback 路径。
4. **匹配失败（R3）**：无限绕圈，不做兜底。
5. **fallback（R4）**：保留无传送带时的旧 `collectPoint`+`gatheredItems` 路径（零代价）。
6. **UI（R5）**：`gatherCountText` 显示「占用槽位数 / 传送带总容量」。
7. **闭环 + 颜色一致**：闭环体育场传送带，近侧上车、远侧匹配，匹配仍要求 `colorId` 相同。
8. **槽位直接收集（R6）**：把「能进传送带」与「进哪个槽」合并——由「哪个槽过关口」天然决定进哪个槽；每槽独立收集，去掉全局单飞 `_boarding`。

---

## 14. 实现清单（已完成）

1. 拷贝并改造 skill 文件到 `Assets/Scripts/Gameplay/Curve/`（`ArcPath.cs`、`ArcPathController.cs`）与 `Assets/Scripts/Gameplay/Conveyor/`（`ConveyorBelt.cs`、`IConveyorItem.cs`），统一包进 `namespace CrowdMatch`；把 `ArcPathEditor.cs` 放到 `Assets/Scripts/Editor/`（同样包进 `CrowdMatch`）。
2. `ArcPathController` 不加 Gizmos；由 `ArcPathEditor` 提供 Scene 预览 + Inspector 测试控制。
3. `ConveyorBelt` 改造：加 `carriers[]`、`OccupiedCount`、`GetSlotWorldPosition`、`entryGate`、`SlotPassedEntry`、`_prevSlotX[]`；`ApplyPositions` 写 carrier + 过关口检测；`TryEnter`/`ClearSlot`/`CheckLeave` 处理 reparent/解绑。
4. 新增 `ConveyorBeltZone.cs`：注入钩子、订阅 `SlotPassedEntry`；`OnSlotPassedEntry`/`SettleRoutine`。
5. `PixelItem` 实现 `IConveyorItem`。
6. `ContainerGroup` 新增 `FindFrontContainerInFrontOf` + `ConsumePixel`（复用现有 `MovePixelToContainer`/`DisappearAndRefill`）。
7. `CrowdBufferZone` 加 `conveyorZone` 字段、`CollectNearest()`/`DetachPhysics()`；`TryRelease` 在 conveyor 模式下 return，`Release` 退化为 fallback-only。
8. `GameController` 加 `conveyorZone` 字段，`UpdateCountText` 改显示（§9.4）。
9. 场景配置：`Path`（ArcPathController，4 段闭环）→ `ConveyorBelt`（belt，连 path + 放 `entryGate` 空物体）→ `ConveyorBeltZone`（连 belt/containerGroup/crowdBuffer）→ `CrowdBufferZone.conveyorZone`、`GameController.conveyorZone` 连线；`gatherCountText` 保持。
10. 自测：过闸释放 → 槽位过关口直接上车（无瞬移）→ 循环到远侧 → 同色前排容器正前方匹配 → 吸收进容器 → 容器耗尽补位；出口无球或槽位满时像素在 gap 排队；UI 显示「占用/总容量」。
11. 按 §12 调参，达到"上车 → 运送 → 远侧被吸收"的节奏感。
