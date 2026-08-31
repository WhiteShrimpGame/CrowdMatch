# ContainerItem 生成逻辑重设计

> 参考 `unity-colorpool` skill 的「分层颜色池 / pack 抽取」模型，仅借鉴其**逻辑**，不照搬其代码。
> 目标：把 `ContainerGroupEditor.GenerateContainers` 从「按颜色平均深度排序 + 每色切块」，
> 改为「分层颜色池 + 逐层抽同色 pack」，使容器前后排颜色顺序更贴合 PixelGroup 的真实分层。

---

## 1. 背景与目标

当前 `ContainerGroupEditor` 生成容器的流程：

1. 扫描 PixelGroup 的 `PixelItem`，统计每个 `colorId` 的**总数**与**gridZ 总和**；
2. 用 `gridZSum / count`（平均 gridZ）对颜色做**降序**排序（前排颜色在前）；
3. 每种颜色用 `SplitRun(count, minCap, maxCap)` 切成 `[minCap, maxCap]` 的块；
4. 把所有 `(color, capacity)` 块按行主序（前排 row 0 在前）铺到容器网格。

它能保证**数量完全匹配**（9 色 225 像素 → 15 个容器），但有一个结构性问题：
**用单一"平均 gridZ"代表一种颜色的深度，丢失了分层信息**。

当一个颜色同时出现在前排和后排时，它的平均 gridZ 会落在一个"中间值"，
导致该颜色被整块放到一个不贴合的中间排——而不是在前排放一个、后排放一个。
这正是 `unity-colorpool` 用**分层**结构去解决的核心问题。

---

## 2. 参考逻辑：ColorPool 的五个核心思想

| # | ColorPool 概念 | 含义 |
|---|---|---|
| 1 | **层 = 深度** | `_pool[layer][color]`，layer 0 = 最浅/最暴露，深层在浅层清空前不出现 |
| 2 | **同色成组（pack）** | 一个发射器 = 一个同色 pack = 一次 `TryGetPack`；一个容器 = 一个同色 pack |
| 3 | **层塌缩** | 空层被移除，索引 0 恒为最浅 |
| 4 | **`TryGetPack(min, max, span)`** | 从最浅层起、可跨 `span` 层抽一个同色 pack；含死区收尾避免剩余卡在 `(0, min)` |
| 5 | **无场景耦合** | 纯 C#，调用方把场景扫描成扁平 `(layer, color)` 元组传入 |

---

## 3. 当前实现的不足（为什么要改）

1. **单值平均深度丢失分层**：同色跨前后排时被压成一个中间深度，前后排映射失真。
2. **无层结构**：容器排布是"全局排序"的副产品，不是从"深度层"推导出来的。
3. **算法内嵌在 Editor 里**：不可单独测试，且与场景层级耦合，复用性差。

---

## 4. 新方案：分层颜色池 + 逐层抽 pack

### 4.1 概念映射（ColorPool → Container 生成）

| ColorPool | Container 生成中的对应 |
|---|---|
| `layer`（0 = 最浅层） | 深度层 `layer = (pixelGroup.rows - 1) - gridZ`，layer 0 = 最前排 |
| `color` | `colorId` |
| `_pool[layer][color]`（每层每色数量） | `tally[layer][color]` 二维数组 |
| `TryGetPack(min, max, span)` | `PullPack(...)`：为下一个容器槽位抽一个同色 pack，容量落在 `[minCap, maxCap]` |
| `RemoveEmptyLayers()` | 层清空后移除，让更深层颜色浮出 |
| `aggressivePackFinish`（死区收尾） | 尾部剩余 `< minCap` 时全部取出，避免卡死 |
| `sinkByColor`（关键颜色下沉） | 可选：某颜色视为更深层，放到后排容器 |

> **关键的反转**：PixelGroup 前排是 `gridZ` **大**；ContainerGroup 前排是 `row` **小**。
> 所以映射为 `layer = (pixelGroup.rows - 1) - gridZ`，让"最前排像素"对应"layer 0"。

### 4.2 数据结构

```
tally : int[pixelGroup.rows][colorCount]   // 每层每色的像素数量
plan  : List<(int color, int capacity)>    // 生成的容器计划（顺序 = 铺放顺序）
```

- `tally` 由扫描 `PixelItem` 一次性构建（等价于 ColorPool 的 `Rebuild` 输入）。
- `plan` 是纯数据，Editor 只负责"按 plan 实例化"，两者解耦（等价于 ColorPool 的"纯 C# + 薄调用方"）。

### 4.3 生成流程（伪代码）

```
// ---- 第一步：扫描，构建分层 tally ----
tally = zeros[pixelGroup.rows][colorCount]
for each PixelItem p（在范围内）:
    layer = (pixelGroup.rows - 1) - p.gridZ      // 反转：前排 → layer 0
    tally[layer][p.colorId]++

// ---- 第二步：逐槽位抽 pack，生成 plan ----
plan = []
for row = 0 .. containerRows - 1:                // 前排（row 0）优先
    for col = 0 .. columns - 1:
        (color, cap) = PullPack(tally, minCap, maxCap, span)
        if cap <= 0: goto DONE                   // 像素耗尽，剩余槽位留空
        plan.Add((color, cap))
DONE:

// ---- 第三步：Editor 按 plan 实例化（原逻辑不变）----
```

`PullPack(tally, min, max, span)` 的语义（等价于 `TryGetPack` 的确定性版本）：

```
1. shallow = 最靠前（layer 最小）且还有元素的一层；不存在则返回 (-1, 0)
2. color = 从 tally[shallow] 中选一个数量 > 0 的颜色
     （选择策略见「待确认决策 #2」：建议"同层内数量降序"，保证前排大色块优先）
3. total = Σ_{l = shallow .. shallow + span} tally[l][color]
     （跨 span 层累加，解决"同色被分层切碎"的问题——这是 span 的核心作用）
4. cap = ResolvePack(total, min, max)            // 见 4.4
5. 从浅到深依次扣减 tally[l][color]，共扣掉 cap
6. 移除所有全 0 的层（层塌缩）
7. return (color, cap)
```

### 4.4 容量切分与死区处理

`ResolvePack(total, min, max)` 沿用 ColorPool 的 `ResolvePackCountNormal` +
`aggressiveFinish` 思路，但**确定性**（生成期不引入随机）：

```
if total <= max:
    cap = total                // 全取；若 total < min，属不可避免的小尾块（== aggressive 收尾）
else:
    n = ceil(total / max)      // 需要拆成几个 pack
    if n * min > total:        // n 个 [min,max] pack 放不下 min 约束
        n = max(1, total / min)
    cap = 均匀切分：base = total / n, rem = total % n，取其中一块
    // 等价于现有 SplitRun 的均分，但作用域是"跨 span 层的 total"，而非"颜色全局 total"
```

> 与现状的关键区别：现有 `SplitRun` 作用在**颜色的全局总数**上；
> 新逻辑作用在**该颜色在 `[shallow, shallow+span]` 层带内的总数**上，
> 因此同色若分布在多个深度带，会被切成多个、且各放到正确的排。

### 4.5 层压缩（span 的取值）

容器网格的 `rows`（如 3）通常 **远小于** PixelGroup 的深度层数（如 15）。
`span` 控制"一个容器最多跨多少个像素深度层抽色"，从而把 15 层像素**压缩映射**到 3 排容器。

建议默认：`span = max(0, ceil(pixelGroup.rows / containerRows) - 1)`，
例如 15 / 3 = 5 → `span = 4`（每个容器行对应约 5 层像素）。
是否把它做成 Inspector 可配字段，见「待确认决策 #3」。

### 4.6 前后排顺序一致性（效果示例）

以一个同色跨前后排的颜色为例（颜色 A：前排 20 个、后排 20 个，minCap=10 / maxCap=20）：

- **现状**：平均 gridZ 落中间 → 颜色 A 被整块放到中排，前后排都错位。
- **新方案**：`span` 跨层累加后，前排 20 → 一个 `cap=20` 的**前排**容器；后排 20 → 一个**后排**容器。
  颜色顺序与像素实际分层一致。

---

## 5. 代码结构设计（不照搬 ColorPool.cs）

```
Assets/Scripts/Gameplay/ContainerGenerationPlanner.cs   // 纯 C#，无 MonoBehaviour
    - Rebuild(tally 或 (layer,color) 列表, colorCount, span)
    - PullPack(minCap, maxCap, span)   // 确定性抽取
    - ResolvePack(...)
    - RemoveEmptyLayers()
    - 输出 plan: List<(color, capacity)>

Assets/Scripts/Editor/ContainerGroupEditor.cs           // 精简为薄调用方
    - 扫描 PixelItem → 调 ContainerGenerationPlanner
    - 按 plan 实例化 ContainerItem（现有实例化逻辑基本不变）
```

- **解耦**：Planner 只认 `(layer, color)` 数据，不碰场景层级（对齐 ColorPool 的"无场景耦合"）。
- **可测试**：纯 C#，便于日后加 Editor 测试 / 单测。
- **不照搬**：`ColorPool.cs` 里的 `Random`、`preferColor`、`sinkByColor`、`Progress` 等运行时概念，
  生成期**不需要**，故不引入（除非「待确认 #4」决定要下沉）。

---

## 6. 边界情况

| 场景 | 处理 |
|---|---|
| 容器数 > 格子数 | 与现状一致：报错并提示增大 `columns/rows` 或调大 `minCap/maxCap` |
| 容器数 < 格子数 | 尾部格子留空（与现状一致），日志提示建议的 `rows` |
| 单色跨多层、且每层都 < minCap | 靠 `span` 跨层累加合并；仍不足则产生一个 `< minCap` 的尾块（不可避免，与现状 `SplitRun` 一致） |
| PixelGroup 空 / 无有效 PixelItem | 与现状一致：报错提示先「生成网格」 |
| 某层仅一种颜色但数量巨大 | 拆成多个 `cap=maxCap` 的容器，依次铺在同一排/相邻排 |

---

## 7. 与运行时消费的关系

- **运行时 `ContainerGroup.ProcessConsumption` 不变**：它按列取 `GetItem(col, 0)` 的最前排容器
  匹配 `gatheredItems`，与生成器输出的容器布局无关。
- 生成器只需保证两件事：① 每色总数与 PixelGroup 完全匹配；② 前后排颜色顺序贴合像素分层（启发式，
  改善视觉/手感，不影响正确性）。

---

## 8. 待确认决策（实现前需你拍板）

1. **层压缩 `span` 是固定公式还是暴露为 Inspector 字段？**
   建议：默认 `ceil(pixelGroup.rows / containerRows) - 1`，可做成字段覆盖。
2. **同层内选色顺序？**
   建议 A（推荐）：数量降序（大色块优先，前排更整齐）；
   建议 B：保持现有"总数量降序"；建议 C：随机（贴近 ColorPool 原行为，但生成不可复现）。
3. **`span` 是否让用户可调，还是自动推导？**
4. **是否需要"关键颜色下沉"（`sinkByColor` 类似物）？**
   生成期一般不需要，除非你希望某颜色**固定**放到后排。

---

## 9. 落地步骤（待确认后执行）

1. 新建 `ContainerGenerationPlanner.cs`（纯 C#）。
2. 改写 `ContainerGroupEditor.GenerateContainers` 为"扫描 → Planner → 按 plan 实例化"。
3. 保留现有实例化/材质/撤销逻辑与日志。
4. 自测：9 色 225 像素 → 仍 15 个容器，但前后排颜色顺序更贴合分层。
