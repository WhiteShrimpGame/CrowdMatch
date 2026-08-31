# CrowdMatch Demo 功能文档

> 人群匹配（Crowd Match）玩法 Demo。玩家点击屏幕最前排的像素，聚集相邻同色单位，
> 与上方同色容器匹配并消耗容量；容器耗尽后消失，后排向前补位。
>
> 文档覆盖：项目结构、核心概念、每个组件的职责与关键 API、完整玩法流程、编辑器工作流、从零搭建步骤。

---

## 1. 项目概述

CrowdMatch 是一个 **点击匹配 + 容器消耗** 的休闲玩法原型，核心循环：

1. 屏幕下方是一个 `PixelGroup` 网格（15×15 等），每个格子一个 `PixelItem`，按**局部同色区域**分布颜色。
2. 玩家点击**最前排**（最靠近屏幕上方）的一个像素，游戏用 **Flood Fill** 找出相邻同色单位，全部移到中间的**聚集点**。
3. 屏幕上方是一个 `ContainerGroup` 网格，每个 `ContainerItem` 有**颜色 ID + 容量**。
4. 聚集点中已到达的像素，会被**同色**的**最前排容器**按列逐一吸收；每吸收一个，容器容量 −1。
5. 容器容量耗尽后消失，该列**后排容器向前补位**。
6. 像素被取走后，`PixelGroup` 各列后排像素**向前补位**，空出前排格子。

整个 Demo 是"编辑器生成 + 运行时交互"的结构：网格和容器在**编辑器里一键生成**，运行时只做匹配与移动。

---

## 2. 技术环境

| 项 | 值 |
|---|---|
| Unity | 2021.3.14f1 |
| 渲染管线 | **内置管线（Built-in）**，材质用 `Shader.Find("Standard")` |
| UI | `com.unity.ugui` 1.0.0，`UnityEngine.UI.Text` |
| 命名空间 | 所有脚本位于 `CrowdMatch` |
| 语言 | C# |

> 若切到 URP/HDRP，`ColorConfigEditor` 里 `Shader.Find("Standard")` 会找不到，需要替换着色器。

---

## 3. 目录结构

```
Assets/
├── CrowdMatch/
│   ├── ColorConfig.asset              # 颜色配置资产（24 色）
│   └── Materials/
│       └── Color_00.mat ... Color_23.mat   # 24 个颜色材质
├── Prefabs/
│   └── ContainerItem.prefab           # 容器预制体（Renderer + 世界空间 Canvas/Text）
├── Scenes/
│   └── SampleScene.unity              # Demo 场景
└── Scripts/
    ├── Core/
    │   └── GameManager.cs             # 全局单例
    ├── Config/
    │   └── ColorConfig.cs             # 颜色配置 ScriptableObject
    ├── Gameplay/
    │   ├── PixelItem.cs               # 像素单位
    │   ├── PixelGroup.cs              # 像素网格
    │   ├── GameController.cs          # 点击匹配 / 聚集 / 补位
    │   ├── ContainerItem.cs           # 容器单位
    │   ├── ContainerGroup.cs          # 容器网格（运行时消费）
    │   └── ContainerGenerationPlanner.cs  # 容器生成规划器（纯 C#）
    └── Editor/
        ├── ColorConfigEditor.cs       # 生成 24 色材质
        ├── ColorConfigLocator.cs      # 查找 ColorConfig
        ├── PixelItemEditor.cs         # 批量设置像素颜色
        ├── PixelGroupEditor.cs        # 生成网格 / 区域生长配色
        └── ContainerGroupEditor.cs    # 生成容器
```

---

## 4. 核心概念与坐标约定

### 4.1 坐标与前后排（重点：两组方向相反）

- **PixelGroup（像素网格）**
  - `gridX`（列，X 方向）、`gridZ`（行，Z 方向）。
  - **`gridZ` 越大越靠前**；`row = rows - 1` 是最前排（+Z）。
  - `GetLocalPosition(col, row)`：`z = (row - (rows-1)*0.5f) * CellSize`，以自身为中心。

- **ContainerGroup（容器网格）**
  - `gridX`（列）、`gridZ`（行），**`gridZ = 0` 是最前排**，后排向 +Z 延展。
  - `GetLocalPosition(col, row)`：`x = (col - (columns-1)*0.5f) * xSpacing; z = row * zSpacing`。
  - 前排 `row 0` 的 Z = 父物体原点（0）。

> **两者 Z 方向相反**：像素前排朝 +Z（屏幕上方），容器前排朝 −Z（屏幕下方）。场景里把
> `ContainerGroup` 摆在 `PixelGroup` 上方，两者**面对面**。玩家点击的"最前排像素"与"最前排容器"
> 在空间上最接近，匹配关系自然成立。

### 4.2 颜色系统

- `ColorConfig` 存放 24 个材质，`colorId` 0..23 作为颜色索引。
- 24 色由 `ColorConfigEditor.GenerateMaterials` 用 HSV 生成，分四档：
  1. 无饱和 3 色（黑 / 灰 / 白）；
  2. 中饱和暗色 6 色；
  3. 中饱和亮色 6 色；
  4. 高饱和 9 色。
- 色相经过 `WarpHue` 做**人眼敏感度 warping**（红色附近更密集、蓝绿更稀疏），使相邻颜色视觉差异尽量大。
- 材质关闭高光/金属（`_Glossiness=0, _Metallic=0`），颜色更纯粹。

### 4.3 容量与容器

- `ContainerItem` = 颜色 ID + 容量 `capacity`，运行时 `_remaining` 表示剩余可吸收像素数。
- 每个像素被吸收 → `Consume()` → `_remaining - 1`；`_remaining <= 0` 即 `IsEmpty`。
- 容器通过 `ApplyMaterial(colorId)` 按颜色取材质。

---

## 5. 运行时组件

### 5.1 GameManager（`Core/GameManager.cs`）

全局单例，`[DefaultExecutionOrder(-1000)]` 保证最先 `Awake`。

| 成员 | 说明 |
|---|---|
| `static Instance` | 单例；重复实例自毁 |
| `ColorConfig colorConfig` | 颜色配置引用，供各组件运行时取材质 |

### 5.2 ColorConfig（`Config/ColorConfig.cs`）

`ScriptableObject`，`[CreateAssetMenu]`。

| 成员 | 说明 |
|---|---|
| `Material[] materials` | 24 个材质，下标 = colorId |
| `int Count` | 材质数量 |
| `GetMaterial(int id)` | 取材质，越界返回 `null`（**边界安全**） |
| `GetColor(int id)` | 取颜色，越界返回 `Color.magenta` 提示 |

### 5.3 PixelItem（`Gameplay/PixelItem.cs`）

像素单位。挂在一个 Cube 上（`PixelGroupEditor` 创建 `PrimitiveType.Cube`）。

| 成员 | 说明 |
|---|---|
| `int colorId` | 颜色 ID |
| `int gridX` / `int gridZ` | 网格坐标 |
| `PixelGroup group` | 所属网格（运行时赋值，不序列化） |
| `bool arrivedAtGatherPoint` | 是否已到达聚集点（运行时标记，不序列化，供容器消费判断） |
| `SetColorId(int id)` | 设颜色并立即应用材质 |
| `ApplyMaterial(config = null)` | 按 colorId 应用材质；config 为空时从 `GameManager.Instance.colorConfig` 取 |

### 5.4 PixelGroup（`Gameplay/PixelGroup.cs`）

管理 `columns × rows` 的像素网格。

| 字段 | 默认 | 说明 |
|---|---|---|
| `unitSize` | 1 | Cube 边长 |
| `spacing` | 0.1 | 相邻表面间距 |
| `columns` / `rows` | 5 / 5 | 网格数量 |
| `colorIds` | `{0..5}` | 生成配色的候选颜色 |
| `minRunLength` / `maxRunLength` | 2 / 5 | 同色区域连续格子数范围 |
| `grid` | — | 运行时 `PixelItem[columns, rows]`（不序列化） |

| 方法 | 说明 |
|---|---|
| `CellSize` | `unitSize + spacing` |
| `RebuildGrid()` | 扫描子物体重建 grid |
| `GetItem(col, row)` | 取格子单位，越界返回 null |
| `IsInRange(col, row)` | 边界判断 |
| `GetLocalPosition(col, row)` | 格子本地坐标（前排 +Z） |

### 5.5 GameController（`Gameplay/GameController.cs`）

核心玩法控制器，`[DefaultExecutionOrder(-900)]`（晚于 GameManager，早于 ContainerGroup）。

| 字段 | 默认 | 说明 |
|---|---|---|
| `gatherPoint` | — | 聚集点 Transform |
| `gatherCountText` | — | 显示聚集点数量的 UI Text |
| `pixelGroup` | — | 管理的 PixelGroup，空则自动查找 |
| `gatherSpeed` | 12 | 单位向聚集点移动速度 |
| `refillSpeed` | 10 | 后排补位速度 |
| `gatherScatterRadius` | 0.35 | 到达聚集点后的散布半径 |
| `gatheredItems` | — | 聚集点中的单位列表 |

**流程**（详见第 7 节）：

- `Update()` → 更新计数文本；左键按下 → `HandleClick()`。
- `HandleClick()`：`_refillMovingCount > 0`（补位中）时忽略；`Physics.Raycast` 命中 → 取 `PixelItem`；
  校验该单位仍在其网格位置且是**所在列最前排** → `ResolveMatch()`。
- `FloodFill(start)`：BFS 找相邻同色单位。
- `ResolveMatch()`：匹配单位从 grid 移除 → `GatherItem()` 送去聚集点；随后 `CollapseColumns()` 补位。
- `MoveToGatherPoint()`：关碰撞体 → `SetParent(gatherPoint, true)` → 协程 Lerp 到随机散布点 → 标记 `arrivedAtGatherPoint = true`。
- `CollapseColumns()`：每列把剩余单位挤到最前排，需要移动的走 `MoveToGridCell()`（`_refillMovingCount` 计数）。

### 5.6 ContainerItem（`Gameplay/ContainerItem.cs`）

容器单位，挂一个 Renderer（视觉）+ 一个世界空间 Canvas/Text（显示容量）。

| 成员 | 说明 |
|---|---|
| `int colorId` | 接受的颜色 ID |
| `int capacity` | 总容量 |
| `Text capacityText` | 显示容量的 UI Text，空则自动从子物体查 |
| `int gridX` / `int gridZ` | 网格坐标 |
| `ContainerGroup group` | 所属组（运行时赋值） |
| `int Remaining` | 剩余容量（`_remaining`） |
| `bool IsEmpty` | 剩余 ≤ 0 |

| 方法 | 说明 |
|---|---|
| `Awake()` | 初始化 renderer、`_remaining = capacity`、找 Text、应用材质、刷新文本 |
| `SetCapacity(int cap)` | 设容量并刷新文本（编辑器与运行时都可用） |
| `Consume()` | 容量 −1，返回是否耗尽 |
| `UpdateText()` | 刷新 Text 显示 `_remaining`（会自动找 Text） |
| `ApplyMaterial(config = null)` | 按 colorId 应用材质 |

### 5.7 ContainerGroup（`Gameplay/ContainerGroup.cs`）

管理容器网格 + **运行时消费**逻辑。

| 字段 | 默认 | 说明 |
|---|---|---|
| `containerPrefab` | — | 容器预制体模板 |
| `columns` / `rows` | 5 / 3 | 网格数量 |
| `xSpacing` / `zSpacing` | 1.2 / 1.2 | 间距 |
| `pixelGroup` | — | 读取颜色分布的 PixelGroup（生成用） |
| `minCapacity` / `maxCapacity` | 2 / 5 | 单容器容量范围（生成用） |
| `maxSpanLayers` | 4 | 生成时每个容器最多跨多少像素深度层抽同色 |
| `consumeSpeed` / `refillSpeed` | 10 / 10 | 像素移向容器 / 容器补位速度 |
| `grid` | — | 运行时 `ContainerItem[columns, rows]`（不序列化） |

| 方法 | 说明 |
|---|---|
| `RebuildGrid()` | 扫描子物体重建 grid |
| `GetItem(col, row)` / `IsInRange` / `GetLocalPosition` | 网格查询与坐标 |
| `ProcessConsumption()` | 每帧：对每列 front（row 0）容器，找匹配颜色的**已到达**像素 → 移除 → `Consume` → 协程移动 |
| `MovePixelToContainer()` | 像素 world-space Lerp 到容器位置 → 销毁像素 → 若 isLast 则 `DisappearAndRefill` |
| `DisappearAndRefill()` | 销毁前排容器，后排容器逐排前移（`MoveContainer` 协程） |

---

## 6. 编辑器组件

### 6.1 ColorConfigEditor（`Editor/ColorConfigEditor.cs`）

- Inspector 上「生成 / 刷新 24 种颜色材质」按钮。
- 菜单 `CrowdMatch → Create Color Config (24 种颜色)`：在 `Assets/CrowdMatch/` 创建 `ColorConfig.asset` 与 24 个材质。
- `GenerateDistinctColors()`：HSV 生成 24 色；`WarpHue()`：色相敏感度映射。

### 6.2 ColorConfigLocator（`Editor/ColorConfigLocator.cs`）

静态 `Find()`：运行时优先 `GameManager.Instance.colorConfig`；否则 `AssetDatabase.FindAssets("t:ColorConfig")` 取第一个。

### 6.3 PixelItemEditor（`Editor/PixelItemEditor.cs`）

`[CanEditMultipleObjects]`。提供批量设置颜色：调色板点选 colorId，`ApplyColorToAll()` 应用到所有选中单位（含 Undo）。

### 6.4 PixelGroupEditor（`Editor/PixelGroupEditor.cs`）

两个按钮：

1. **「生成网格」**：删除旧 PixelItem 子物体 → 按 `columns × rows` 生成 Cube → 挂 PixelItem → 区域生长配色。
2. **「重新生成颜色分布」**：只改颜色不改几何。

配色算法 `AssignClusteredColors`（**区域生长法**）：随机种子格子 + 随机颜色，向相邻未分配格子扩张，形成 `[minRunLength, maxRunLength]` 大小的同色区域，直到填满。

### 6.5 ContainerGroupEditor（`Editor/ContainerGroupEditor.cs`）

「生成 Containers」按钮，流程：

1. 校验 PixelGroup、containerPrefab、ColorConfig；
2. 扫描 PixelItem → `(layer, color)` 列表，`layer = (pixelGroup.rows - 1) - gridZ`（反转前后排）；
3. 交给 `ContainerGenerationPlanner` 生成容器计划；
4. 校验容器数 ≤ 网格格子数；
5. 删除旧 ContainerItem 子物体；
6. 按计划实例化（预制体走 `InstantiatePrefab`，场景物体走 `Object.Instantiate` 克隆），设置 colorId/capacity/材质/Undo。

### 6.6 ContainerGenerationPlanner（`Gameplay/ContainerGenerationPlanner.cs`，纯 C#）

参考「分层颜色池」模型实现，无 MonoBehaviour，无场景耦合。

| 方法 | 说明 |
|---|---|
| `Rebuild(pixels, layerCount, colorCount, maxSpanLayers)` | 把 `(layer, color)` 列表统计成 `tally[layer][color]`，层 0 = 最前排 |
| `PullPack(minCap, maxCap)` | 抽一个同色 pack：层 0 选**数量最大**的颜色（大色块优先）→ 跨 `[0, maxSpanLayers]` 层累加同色总数 → 均分容量 → 从浅到深扣减 → 层塌缩 |
| `ResolvePack(total, min, max)` | 把总数均分成若干 `[min, max]` 块，返回其中一块（确定性，结果与旧 `SplitRun` 等价） |
| `RemoveEmptyLayers()` | 移除全 0 层，使 layer 0 恒为最浅 |

输出 `List<ContainerPlan>`（`struct { colorId, capacity }`），由 Editor 实例化。

---

## 7. 完整玩法流程

### 7.1 生成阶段（编辑器，非运行时）

```
1. ColorConfig：菜单生成 24 色材质 → ColorConfig.asset
2. PixelGroup：Inspector 点「生成网格」→ 创建 columns×rows 个 Cube + PixelItem，区域生长配色
3. ContainerGroup：Inspector 点「生成 Containers」
   → 扫描 PixelGroup 颜色分布 → 分层颜色池 → 生成同色、容量匹配的 ContainerItem 网格
```

### 7.2 运行时匹配循环

```
玩家左键点击
   │
   ▼
GameController.HandleClick
   ├─ 补位中？ → 忽略
   ├─ Raycast 命中 PixelItem？
   ├─ 仍是网格成员 + 所在列最前排？
   ▼
ResolveMatch → FloodFill(相邻同色)
   ├─ 匹配单位从 pixelGroup.grid 移除
   ├─ GatherItem → 移到 gatherPoint（散布）
   └─ CollapseColumns → 各列后排向前补位
   ▼
ContainerGroup.Update → ProcessConsumption
   对每列 front 容器（row 0）：
   ├─ 找 gatheredItems 中「已到达 + 同色」的像素
   ├─ 移除该像素 → Consume（容量 -1）
   └─ MovePixelToContainer → 像素移向容器 → 销毁像素
       └─ 若 isLast → DisappearAndRefill → 容器消失 + 后排补位
```

### 7.3 数据流

```
PixelItem(colorId, gridX, gridZ)
    │  FloodFill / GatherItem
    ▼
GameController.gatheredItems（List<PixelItem>，arrivedAtGatherPoint 标记）
    │  FindMatchingPixel(按 colorId + arrived)
    ▼
ContainerItem.Consume → _remaining -1 → 文本刷新
    │  isLast
    ▼
DisappearAndRefill → 销毁容器 + 后排前移
```

---

## 8. 从零搭建步骤

1. **创建颜色配置**：菜单 `CrowdMatch → Create Color Config`，生成 `ColorConfig.asset` + 24 材质。
2. **搭建场景对象**：
   - `GameManager`：挂一个空物体，引用 `ColorConfig`。
   - `PixelGroup`：挂一个空物体，配置 `columns/rows/colorIds/minRunLength/maxRunLength`。
   - `GameController`：挂一个物体，指定 `gatherPoint`（一个空物体）与 `gatherCountText`（屏幕 UI Text）。
   - `ContainerGroup`：挂一个空物体（放在 PixelGroup 上方），配置 `containerPrefab`、`columns/rows`、`minCapacity/maxCapacity`、`maxSpanLayers`。
   - `ContainerItem` 预制体：一个 Cube（Renderer）+ 世界空间 Canvas/Text（显示容量），挂 `ContainerItem` 组件。
3. **生成网格**：选中 PixelGroup → 「生成网格」。
4. **生成容器**：选中 ContainerGroup → 「生成 Containers」（若有编译错误先解决）。
5. **运行**：进入 Play，点击最前排像素观察聚集与容器消耗。

> 建议：容器数 = `ceil(总像素 / maxCapacity)` 上下，`rows` 设为 `ceil(容器数 / columns)`（例如 15 个容器、5 列 → `rows = 3`）。`maxSpanLayers` 约等于 `ceil(pixelGroup.rows / containerRows) - 1`。

---

## 9. 关键设计决策与权衡

| 决策 | 理由 | 权衡 |
|---|---|---|
| 两组网格前后排方向相反 | 让像素与容器在场景里面对面，匹配关系自然 | 生成器里必须做 `layer = (rows-1) - gridZ` 反转 |
| 编辑器生成、运行时只交互 | 网格/容器参数可快速迭代，无需手摆大量物体 | 运行时动态改网格需额外处理 |
| `ContainerGenerationPlanner` 纯 C# 解耦 | 可单测、可复用、无场景耦合（参考分层颜色池） | 调用方（Editor）需自写扫描逻辑 |
| 分层颜色池 + 大色块优先 | 容器前后排颜色贴合像素分层 | 层压缩 `maxSpanLayers` 需人工调 |
| Flood Fill 相邻同色匹配 | 经典消除玩法手感 | 仅上下左右 4 邻接，不含斜向 |
| 聚集点 + 容器消费两段移动 | 视觉上有"先聚集再吸收"的节奏 | 两个异步移动系统需注意时序（见 review） |
