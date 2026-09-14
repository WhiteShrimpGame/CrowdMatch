# CrowdMatch「箱子（Box）」功能设计文档

> 状态：**已实现，随功能演进**。本文描述"箱子"玩法：箱子在 PixelGroup 网格内占据一个矩形区域，
> 内含若干隐藏 Pixel 并标注容量；当箱子相邻空格全部清空时开箱，把隐藏 Pixel 确定性分配到「本体 + 相邻」空格，
> 分两段动画（本体外 Pixel 依次 Jump、本体内 Pixel 原地站起、箱子放大缩小消失）释放为普通可点击 Pixel。
>
> 位置分配采用**确定性分布**（详见 §7.2）：候选格按 (row 升序, col 升序) 排序，颜色按 colorIds 顺序一一对应，
> 不再随机。容量由周围环境自动计算（详见 §5.1 / §11）。

---

## 1. 概述

当前 PixelGroup 里只有三种"格子"：普通像素（`grid`）、墙体（`wallGrid`）、管道（`pipeGrid`）。
本次新增第四种**箱子（Box）**：

1. 箱子占据一个矩形区域（由左上、右下两个格子坐标确定），区域内的格子是障碍，不参与暴露/寻路/匹配。
2. 箱子内含 `capacity` 个**隐藏 Pixel**（每个有自己的 `colorId`），开箱前不可见、不可点击、不进入 `grid`。
3. 箱子标注一个**容量** `capacity`（= 隐藏 Pixel 数量，也是触发阈值）。
4. 当**相邻空格全部清空**（可用格 == `capacity`）时开箱，把隐藏 Pixel 确定性分配到「本体 + 相邻」空格：
   候选格按 (row 升序, col 升序) 排序（同排先左后右），颜色按 `colorIds` 顺序一一对应，不再随机。
5. 分配后分两段动画：**本体外**目标 Pixel 按 (row, col) 顺序逐个在箱子下方出现并 Jump（起跳间隔可配）；最后一个 Jump 起跳后再过一个间隔，
   **本体内**目标 Pixel 原地出现并站起（`SetExposed(true)`），**同时箱子先放大后缩小消失**。
6. 箱子视觉由 3 个预制体（角/边/中心）按格子拼接而成。
7. 箱子随关卡 JSON 导入导出；所有统计像素数量的逻辑都要计入箱内隐藏 Pixel。

开箱后，释放出的 Pixel 与普通 Pixel 完全一致，参与后续点击匹配 → 提取 → 传送带 → 容器消费的整条链路。

---

## 2. 目标与非目标

### 目标
- 箱子作为网格内的一块矩形障碍区域，可由编辑器创建、可随关卡 JSON 导入导出。
- 箱内隐藏 Pixel 正确计入 `TotalPixelCount` 与容器生成规划，保证胜负判定与可解性不因箱子而失真。
- 开箱触发条件明确：可用格（本体 + 周围空，相邻固定 8 方向、连通空格不纳入）≥ 容量。
- 位置分配优先级明确：本体 > 相邻 > 其他连通格（按距离）。
- 两段动画：本体外 Jump 起跳间隔可配；本体内原地站起；箱子放大缩小消失。
- 复用现有 `SetExposed`（站起）、`DOLocalJump`/`DOJump`（跳）、`wallGrid/pipeGrid` 障碍模型，尽量少改既有契约。

### 非目标（本期不做）
- 箱子不做"多层嵌套"（箱子套箱子）。
- 箱子不做"颜色约束"（本体内 pixel 颜色与周围无关，颜色仅由 colorIds 指定）。
- 不做箱子在开箱过程中被再次点击/匹配（开箱期间箱子整体视为进行中）。
- 不做箱子容量动态变化（capacity 静态，由 JSON/编辑器指定）。
- 不做箱子视觉的高级动画（掀盖/碎裂），消失用现成的 `DisappearWithPop`（先放大后缩小）。

---

## 3. 概念与术语

| 术语 | 含义 |
|---|---|
| **箱子（Box）** | 网格内一个矩形区域，由左上/右下两个格子坐标确定；`BoxItem` 组件承载 |
| **左上 / 右下** | 箱子矩形对角坐标：左上 = (colMin, rowMin)，右下 = (colMax, rowMax)，详见 §4 |
| **箱子本体格子（Body Cell）** | 矩形 `[colMin..colMax] × [rowMin..rowMax]` 内的所有格子，开箱前被箱子视觉占据，是障碍 |
| **相邻格子（Adjacent Cell）** | 与箱子矩形边界邻接的格子：固定上下左右 + 4 个对角共 8 方向（**设计约定：永远含四角**）。需为空（无像素、非墙/管/箱） |
| **连通空格（Connected Empty）** | 从相邻空格出发、**只经上下左右 4 方向**的"空"格 BFS 扩散可达的所有空格（对角不算连通；**设计约定：永远不纳入**） |
| **可用格（Available Cell）** | 开箱时可摆放 Pixel 的格子集合 = 本体格 + 相邻格 [+ 连通空格] |
| **隐藏 Pixel（Hidden Pixel）** | 箱子内尚未释放的 PixelItem，哨兵坐标 `gridX=gridZ=-1`，不进入 `grid` |
| **容量（Capacity）** | 箱子容量 = 隐藏 Pixel 数量 = 开箱触发阈值 = **本体格子数 + 相邻有效格数**（运行时自动计算，见 §5.1） |
| **站起（Stand Up）** | 复用 `PixelItem.SetExposed(true)`：激活 Animator、`exposeMoveTarget` 匀移到 y=0 的起身动画 |
| **Jump** | 隐藏 Pixel 从箱子下方出现点跳向目标格的位移动画（`DOLocalJump`/`DOJump`），落定即完成（不再站起） |

---

## 4. 几何定义

### 4.1 网格坐标约定（沿用 PixelGroup）

- `col ∈ [0, columns)`，X 方向，col 增大 → X 增大（向右）。
- `row ∈ [0, TotalRows)`，Z 方向，row 0 = 最前排（Z 最大），row 增大 → 向 -Z（向后）。
- 俯视（X 右、Z 上）时，"上" = 前排 = row 小，"左" = col 小。

### 4.2 箱子矩形

- **左上 = (colMin, rowMin)**，**右下 = (colMax, rowMax)**，满足 `colMin ≤ colMax`、`rowMin ≤ rowMax`。
- 箱子占据的格子集合 = `{ (c, r) | colMin ≤ c ≤ colMax, rowMin ≤ r ≤ rowMax }`。
- 本体格子数 `bodyCount = (colMax - colMin + 1) × (rowMax - rowMin + 1)`。
- 编辑器选中两个 Pixel 时**与选中顺序无关**：取两者 `gridX/gridZ` 的 min/max 归一化为左上/右下。
- 边界越界（超出 `columns`/`TotalRows`）时裁剪到网格内（见 §12 边界情况）。

---

## 5. 数据结构

### 5.1 `BoxItem : MonoBehaviour`（运行时 + 序列化）

挂在箱子根 GameObject（PixelGroup 子物体）下：

```csharp
public class BoxItem : MonoBehaviour
{
    // 区域（左上 + 右下）
    public int colMin, rowMin, colMax, rowMax;

    // 内容
    public int capacity;                 // 容量 = 隐藏 Pixel 数量 = 触发阈值 = 本体 + 相邻有效格（运行时自动计算）
    public int[] colorIds;               // 每个隐藏 Pixel 的颜色 ID，长度 == capacity

    // 行为
    // （相邻固定 8 方向含四角、连通空格固定不纳入，已不再作为可配字段）
    public float jumpStartInterval = 0.1f; // 本体外 Pixel 起跳间隔（秒）
    public float jumpSpawnYOffset = 0.5f;  // 外跳 Pixel 出现位置：箱子中心下方（-Y）偏移量

    // 视觉（3 个预制体，占一格）
    public GameObject cornerPrefab;
    public GameObject edgePrefab;
    public GameObject centerPrefab;

    // 运行时状态
    [System.NonSerialized] public bool opened;                       // 是否已开箱
    [System.NonSerialized] public readonly List<PixelItem> hiddenPixels = new(); // 隐藏 Pixel

    public bool IsInBody(int c, int r);      // 该格是否在箱子本体矩形内
    public void EnumerateBody(List<Vector2Int> outList);  // 枚举本体格子
    public int BodyCount { get; }
}
```

### 5.2 `LevelData.BoxData`（JSON 序列化）

```csharp
[Serializable]
public class BoxData
{
    public int colMin, rowMin, colMax, rowMax;
    public int capacity;
    public int[] colorIds;
    public float jumpStartInterval;
    public float jumpSpawnYOffset;
}
```

`LevelData` 顶层新增：

```csharp
public BoxData[] boxes = new BoxData[0];
```

### 5.3 JSON 结构示例

```json
{
  "version": 1,
  "pixel": { "columns": 5, "rows": 5, "tailRows": 0, "unitSize": 1, "cells": [ ... ] },
  "container": { ... },
  "walls": [ ... ],
  "pipes": [ ... ],
  "boxes": [
    {
      "colMin": 1, "rowMin": 1, "colMax": 3, "rowMax": 3,
      "capacity": 4,
      "colorIds": [0, 1, 2, 0],
      "jumpStartInterval": 0.1,
      "jumpSpawnYOffset": 0.5
    }
  ]
}
```

- 箱子区域的格子在 `pixel.cells` 里写 **0**（占位，与墙/管道区域一致），开箱后的 Pixel 颜色由 `boxes[].colorIds` 提供。
- `capacity` 应等于 `colorIds.Length`（§12 校验）。

---

## 6. 箱子视觉拼接（角 / 边 / 中心）

箱子由 3 个预制体拼接，每个预制体占一个格子，实例化后定位到该格中心（`PixelGroup.GetLocalPosition(c, r)`），scale = `unitSize`。

对每个本体格 `(c, r)`：

```
isLeft   = (c == colMin)
isRight  = (c == colMax)
isTop    = (r == rowMin)
isBottom = (r == rowMax)

若 (isLeft || isRight) && (isTop || isBottom)  → 角（cornerPrefab）
否则若 isLeft || isRight || isTop || isBottom → 边（edgePrefab）
否则                                        → 中心（centerPrefab）
```

退化情况（编辑器创建时强制长宽 ≥ 2，以下仅保证不报错）：
- **1×1**：四角全真 → 按规则取角（统一处理成"左上"角，即 `cornerPrefab`）。
- **1×N（单行）/ N×1（单列）**：两端为角（左上/右上 或 左上/左下），中间为边（上/左），无中心。
- **2×2 / 2×N / N×2**：只有角与边，无中心，为正常逻辑。

> 三个预制体均占一格、自身视觉可溢出格子边界（拼起来形成完整箱子外形），但**逻辑占用严格按格子**。

---

## 7. 触发条件与位置分配

### 7.1 触发条件

开箱前，箱子区域是障碍；每当网格状态变化（一次匹配移除像素后），检查所有未开箱箱子：

```
可用格 = 箱子本体格子 ∪ 相邻空格（直接相邻，固定 8 方向）
触发条件：可用格数量 ≥ capacity
```

> 由于 `capacity = 本体 + 相邻有效格`，且连通空格固定不纳入，此条件等价于「**相邻有效格全部为空**」。

- **相邻空格**：与箱子矩形边界邻接、且 `IsEmpty`（无像素、非墙/管/箱）的格子；固定上下左右 + 4 个对角共 8 方向。
- **连通空格**：设计约定**永远不纳入**，代码不再收集连通空格（对角不算连通）。

> 触发判定用的"周围空格"与位置分配的"相邻/其他格子"口径一致：false = 仅直接相邻；true = 相邻 + 连通。

### 7.2 位置分配（确定性分布）

对 `capacity` 个隐藏 Pixel，从可用格（本体 + 相邻）中分配位置，**完全确定、不随机**：

1. **候选格排序**：把本体格 + 相邻格合并，按 `(row 升序, col 升序)` 排序——即"一排一排、同排先左后右"。（无连通空格这一类格子。）
2. **颜色顺序**：隐藏 Pixel 保持 `colorIds` 顺序（不随机打乱），与排序后的候选格**一一对应**：`colorIds[i]` → 第 i 个候选格。

分配算法（伪代码）：

```
candidates = (本体格 ∪ 相邻格) 按 (row, col) 升序排序
for i in 0..capacity-1:
    colorIds[i] 对应的隐藏 Pixel → candidates[i]
```

要点：
- 由于 `capacity = 本体格数 + 相邻有效格数`，开箱时相邻空格已全部清空，`candidates.Count == capacity`，全部格子恰好填满、无剩余。
- 本体 + 相邻（含四角）整体是一个矩形（无墙/管/箱相邻时），按 (row, col) 排序就是最自然的"逐行从左到右"。
- 动画顺序（本体外依次 Jump）也按 (row, col) 排序，整条链路无随机。

### 7.3 多箱同时满足时的开箱顺序

一次网格变化（匹配移除像素）后，可能同时有多个未开箱箱子满足触发条件。开箱只会**新占用**格子（释放的 Pixel 落到格子），**不会腾出格子**，所以不能简单"全都开"，必须按固定优先级串行判定、逐个分配，后一个箱子基于前一个箱子**已占用的格子**重新判定：

1. **排序**：未开箱箱子按"左上角最前排优先、同排横坐标小者优先"排序，即 `(rowMin 升序, colMin 升序)`。
2. **逐个判定**：按序取箱子，基于**当前格子占用状态**（含前面已开箱箱子新占用的格子）计算可用格：
   - 满足 `可用格 ≥ capacity` → 分配位置、立即占用这些格、开箱；
   - 不满足 → **跳过**（保持关闭，等下一次网格变化再判定）。
3. 全部箱子判定完，本次开箱流程结束。多个箱子开箱动画**同时**播放。

要点：
- "同时"指**动画重叠播放**，但**位置分配是串行**的：前面的箱子选位并占用后，后面的箱子才看到新占用，避免位置冲突。
- 被跳过的箱子不因本次其余箱子开箱而重判（它只在下一次网格变化时再查）。

---

## 8. 开箱动画时序

分配完成后，按以下时序播放：

1. **本体外 Pixel 外跳（箱子保持可见，不消失）**：
   - 目标位置在本体外的 Pixel，随机顺序，每隔 `jumpStartInterval` 秒启动一个。
   - 每个外跳 Pixel 在"箱子中心 + 下方 `jumpSpawnYOffset` 偏移"处**出现**（`SetActive(true)` 激活，并以站起状态就位），随即 `DOLocalJump` 跳向目标格。
   - 落定即完成——**不需要落定后再站起**。
2. **本体内 Pixel 站起 + 箱子消失**（最后一个外跳**起跳**后一个间隔触发）：
   - 本体内目标 Pixel 在各自格子位置"正常出现"（`SetActive(true)`），随即播放站起过程（`SetExposed(true)` 起身动画）。
   - **同时**箱子 3 类预制体整体"先放大后缩小"消失（`DisappearWithPop`）。

时间轴示意（设 3 个本体外、2 个本体内，`jumpStartInterval = 0.1s`）：

```
t=0.0  外跳 #0 在箱子下方出现（站起状态）→ 立即起跳
t=0.1  外跳 #1 出现 → 起跳
t=0.2  外跳 #2 出现 → 起跳（最后一个外跳起跳）
t=0.3  本体内 #0/#1 原地出现 → 开始站起；箱子同时 DisappearWithPop
```

要点：
- 外跳是"起跳"间隔，不是"完成"间隔——只要起跳就开始计时下一个，保证间隔均匀。
- 外跳期间箱子**保持可见**；箱子消失与本体内站起**同时**发生。
- 外跳 Pixel 出现即站起（无落定后站起）；本体内 Pixel 出现后播放站起动画。

### 8.1 与现有动画的映射

- **站起过程** = `PixelItem.SetExposed(true)`（已有：激活 Animator、`exposeMoveTarget` 匀移到 y=0、开启描边 `outlineRenderer`）——本体内 Pixel 走完整站起过程。
- **站起状态（外跳出现时）** = 外跳 Pixel 出现即以站起姿态就位（`SetExposed(true)`；实现时可视需要把 `exposeMoveTarget` 直接置 y=0 以跳过起身动画），随即 Jump，落定不再站起。
- **Jump** = 一次 `DOLocalJump` / `DOJump`（参考 `ContainerItem.BoardRoutine`），从箱子下方出现点跳向目标格。
- **箱子消失** = `Transform.DisappearWithPop()`（已有扩展：先放大 1.1× 再缩小到 0，回调清理），与本体内站起同时。
- 隐藏 Pixel 在箱内初始 `SetActive(false)`（利弊见 §14.8），开箱时按需激活。

---

## 9. 与现有系统的集成

### 9.1 障碍模型（PixelGroup）

沿用 `wallGrid/pipeGrid` 的模式，新增箱体占用：

- `PixelGroup` 新增 `List<BoxItem> boxes`（或 `bool[,] boxGrid`）。
- `IsBlocked(col, row)` 增加 `|| IsBoxBlocked(col, row)`，使箱子区域自动成为：
  - `RefreshExposed` 的障碍（箱子格不暴露，且阻断连通块 BFS）。
  - `CrowdBufferZone.IsObstacle` / `IsEmptyForExtraction` 的障碍（提取寻路避开）。
  - `GameController.CanReachFront` 的障碍（箱子格不可穿过）。
- 开箱后：箱子从 `boxes` 移除占用（或 `boxGrid` 清空），本体内 Pixel 落到 `grid`，相邻/连通格 Pixel 也落到 `grid`，随后 `RefreshExposed`。

### 9.2 开箱触发时机

在 `GameController.ResolveMatch` 移除像素并 `pixelGroup.RefreshExposed()` **之后**，调用一次
`pixelGroup.TryOpenBoxes()`（按 §7.3 的顺序逐个判定未开箱箱子，满足条件即开箱）。开箱过程中产生的格变化再触发一次 `RefreshExposed`。

> 触发检查点集中在 `PixelGroup.TryOpenBoxes()`，由 `GameController` 在每次匹配移除后调用；多箱同时满足时的开箱顺序见 §7.3。

### 9.3 失败判定阻塞

开箱动画期间（外跳未落定 + 本体内未站起完成）应视为"进行中"，避免 `TryCheckFail` 误判死锁：

- 复用或新增一个"箱体释放中"计数器（类似 `ContainerGroup.consumingCount`），
- `GameController.IsFail` 的静止门槛增加"无箱子正在释放"条件。

---

## 10. 像素数量统计（关键影响点）

箱子隐藏 Pixel **必须**计入所有统计像素数量的逻辑。隐藏 Pixel 采用**哨兵坐标 `gridX=gridZ=-1`**（未落地、不进入 `grid`），因此现有"遍历 `grid`"或"`IsInRange` 过滤"的逻辑会**漏掉**它们。需要逐处处理：

| # | 位置 | 现状 | 需要改动 |
|---|---|---|---|
| 1 | `GameController.CountPixels()` | `GetComponentsInChildren<PixelItem>()` + `IsInRange` | 增加对每个 `BoxItem.hiddenPixels.Count` 的累加（或在 `PixelGroup` 提供 `CountPixelsIncludingBoxes()`） |
| 2 | `PixelGroup.CollectPlanningPixels()` | `GetComponentsInChildren<PixelItem>()` + `IsInRange` | 把每个箱子的 `colorIds` 计入 `(layer, color)` 列表（layer 取箱子 `rowMin`，见 §14 第 2 条），保证容器生成容量正确 |
| 3 | `GameData.TotalPixelCount` | = `CountPixels() + CountPipePixels()` | 随 #1 自动正确；箱子 Pixel 是"预计入"，开箱落地后**不重复计数**（同一 PixelItem） |
| 4 | 消费计数 `ClearedPixelCount` | 消费时 `++` | 无需改：箱子 Pixel 落地后走正常点击→匹配→消费链路，正常 `++` |
| 5 | `PixelGroup.RebuildGrid()` | `GetComponentsInChildren<PixelItem>()` + `IsInRange` | 哨兵坐标 `-1` 自动被 `IsInRange` 过滤（不进入 `grid`），无需改，但需确认箱子区域不生成 Pixel |
| 6 | `PixelGroup.ClearPixels()` | `GetComponentsInChildren<PixelItem>()` 全部销毁 | 会连带销毁箱内隐藏 Pixel；需 `ClearBoxes()` 配合（清箱子 + 其隐藏 Pixel），见 §9.4 |
| 7 | `LevelDataExporter.BuildLevelData()` | 用 `pg.GetItem(c,r)` 遍历 `grid` | 箱子区域格子是 `null` → 导出 0 占位；箱子数据单独进 `data.boxes`，隐藏 Pixel 颜色进 `colorIds` |
| 8 | `LevelLoader.ApplyPixel()` | 遍历 cells，`skipCells` 跳过墙/管 | `skipCells` 增加箱子本体格子；箱子本体不 SpawnPixel，改由 `ApplyBoxes` 生成 |

### 9.4 生命周期与清理

- `PixelGroup` 新增 `SpawnBox(BoxData)`、`ClearBoxes()`。
- `LevelLoader.Apply()` 增加 `ApplyBoxes(pixelGroup, data.boxes, colorConfig)`（在 `ApplyPixel` / `ApplyWalls` / `ApplyPipes` 之后）。
- `LevelDataExporter.ClearBothGroups()` 增加 `pixelGroup.ClearBoxes()`。
- 箱子隐藏 Pixel 用 `pixelPrefab` 实例化（复用 `PixelGroup.SpawnPixel` 的预制体），`gridX=gridZ=-1`、`SetActive(false)` 隐藏；开箱时激活。

---

## 11. 编辑器功能

### 11.1 选中两个 Pixel 创建箱子

菜单 `CrowdMatch/创建箱子（选中左上、右下两个 Pixel）`：

1. 从 `Selection` 中取**恰好 2 个** `PixelItem`（不足/多余则弹窗提示）。
2. 取两者 `gridX/gridZ` 的 min/max 得到 `colMin/rowMin/colMax/rowMax`（顺序无关）。
3. 弹窗（或 `ScriptableWizard`）填写 `capacity`、`jumpStartInterval`、`jumpSpawnYOffset`，并从 `PixelGroup` 读取 3 个箱子预制体引用（需在 `PixelGroup` 上暴露 `boxCornerPrefab/boxEdgePrefab/boxCenterPrefab`）。
4. 校验：矩形越界/与墙/管/其他箱子重叠/长宽 < 2/`capacity` 与 `colorIds` 长度（编辑器可自动生成 colorIds 或手动填）。
5. 创建 `BoxItem` GameObject（`PixelGroup` 子物体），实例化 3 类预制体拼接箱子，按 `colorIds` 生成隐藏 Pixel。
6. 用 `Undo.RegisterFullObjectHierarchyUndo` 支持撤销，`EditorUtility.SetDirty`。

> 或者更轻量：菜单直接创建默认参数的箱子，再在 `BoxItem` Inspector 里调参（配合 `BoxItemEditor` 自定义 Inspector 绘制区域预览）。

### 11.2 容量自动计算与重算

- **容量 = 本体格数 + 相邻有效格数**（越界/墙/管/其它箱子本体不计数），由 `BoxItem.ComputeCapacity()` 计算。
- 运行时 `PixelGroup.SpawnBox` 会**自动重算**容量（覆盖 JSON 里记录的 capacity）。
- 编辑器 `BoxItemEditor` Inspector 显示"自动容量"，与当前 `capacity` 不一致时提示，并提供 **「按周围环境重算 capacity」** 按钮。
- 创建向导 `BoxCreateWizard` 相邻固定 8 方向（含四角）、连通空格固定不纳入，`capacity` 默认取自动计算值。
- 典型工作流「先建盒后建墙」：建完墙后到箱子 Inspector 点一次重算，再确认 `colorIds` 数量与容量一致。

---

## 12. 边界情况与防御

| 情况 | 处理 |
|---|---|
| 矩形越界（colMin/rowMin < 0 或 colMax/rowMax 超界） | 编辑器拒绝；导入时裁剪到网格内并告警 |
| 箱子与墙/管道/其他箱子重叠 | 编辑器/导入时拒绝或告警（格子不可复用） |
| `capacity != colorIds.Length` | 校验：以 `min(capacity, colorIds.Length)` 为实际数量，告警 |
| 触发时可用格 < capacity | 不开箱，继续等待（格子随匹配逐步腾出） |
| 可用格恰好 = capacity | 全部填满，无剩余 |
| 可用格 > capacity | 正常不会发生（capacity == 本体 + 相邻有效格，开箱时相邻全空 → 恰好填满）；连通空格固定不纳入，此情况不会出现 |
| 长宽 < 2 的箱子 | 编辑器创建时拒绝（长宽至少 2）；导入若出现仅保证不报错，prefab 拼接按 §6 退化规则 |
| 开箱动画期间再次点击/匹配 | 箱子区域已清出但 Pixel 未落定前，本体内 Pixel 尚未进入 `grid`，不会误触发；靠"箱体释放中"计数兜底 |
| 开箱动画期间失败判定 | `IsFail` 增加"无箱子释放中"门槛 |
| 隐藏 Pixel 的 colorId 越界 | 导入/创建时 clamp 或告警 |
| 多箱同时满足触发（一次操作让多个箱子满足开箱条件） | 按 §7.3 串行判定：前箱占格影响后箱，不满足则跳过（保持关闭） |
| 对角相邻 | 固定纳入（8 方向）；连通空格固定不纳入（4 方向 BFS 仅文档参考，代码不收集） |
| capacity 与周围环境不一致 | 运行时 `SpawnBox` 自动重算；编辑器 Inspector 提示并提供「重算」按钮（见 §11.2） |
| 两个箱子相邻 / 环重叠 | **设计约定：不让两个箱子相邻**（本体间 gap ≥ 2，避免一环开箱填充改变另一环所需格子）。代码仅兜底排除其它箱子本体，不做硬检查 |

---

## 13. 实施清单（文件级）

| 文件 | 动作 |
|---|---|
| `Assets/Scripts/Gameplay/BoxItem.cs` | **新建**：BoxItem 组件（区域、内容、视觉预制体、开箱状态、本体格枚举） |
| `Assets/Scripts/Core/LevelData.cs` | 修改：新增 `BoxData` + `LevelData.boxes` |
| `Assets/Scripts/Core/LevelLoader.cs` | 修改：`ApplyPixel` 的 `skipCells` 增加箱子本体格；新增 `ApplyBoxes` |
| `Assets/Scripts/Editor/LevelDataExporter.cs` | 修改：导出/导入/清空均处理 `boxes` |
| `Assets/Scripts/Gameplay/PixelGroup.cs` | 修改：`boxes` 列表 + `IsBoxBlocked` + `SpawnBox/ClearBoxes` + `CollectPlanningPixels` 纳入箱子 + `RefreshExposed` 纳入障碍 + `TryOpenBoxes` |
| `Assets/Scripts/Gameplay/GameController.cs` | 修改：`CountPixels` 纳入箱内 Pixel；`ResolveMatch` 后触发 `TryOpenBoxes`；`IsFail` 增加箱体释放中门槛 |
| `Assets/Scripts/Editor/BoxItemEditor.cs`（或并入 PixelGroupEditor） | **新建**：BoxItem Inspector 区域预览 + 选中两 Pixel 创建箱子流程 |
| `Assets/Prefabs/Box_*.prefab` | **新建**：角/边/中心 3 个占位预制体（美术资产，待替换） |
| `Assets/Scenes/*.unity` / `PixelGroup` | 修改：暴露 `boxCornerPrefab/boxEdgePrefab/boxCenterPrefab` 字段 |

---

## 14. 开放问题（实现前确认）

1. **capacity 与隐藏 Pixel 数量的关系** —— ✅ **已确认**：`capacity == colorIds.Length`（容量 = 内容数 = 触发阈值）。
2. **箱子 Pixel 的规划 layer** —— 建议取箱子 `rowMin`（最前排）作为所有箱内 Pixel 的 layer：
   - 理由：`CollectPlanningPixels` 的 layer = 像素 row（`gridZ`），规划器按"layer 0 最前排 → 后排"依次打包。箱子是障碍，玩家清到 `rowMin` 这一排箱子才暴露、Pixel 才可被吸收，故其有效层就是 `rowMin`。
   - 备选：把 `capacity` 个 Pixel 均匀摊到 `[rowMin..rowMax]` 各排（分层更细，但两者误差都很小）。
3. **位置分配的随机粒度** —— ✅ **已定**：改为**确定性分布**（不再随机）。候选格按 `(row 升序, col 升序)` 排序，颜色按 `colorIds` 顺序一一对应，见 §7.2。
4. **开箱视觉表现** —— ✅ **已定**：箱子用 `DisappearWithPop`（先放大后缩小）消失，与本体内站起同时；不用掀盖。
5. **多箱同时满足的开箱顺序** —— ✅ **已定**：见 §7.3（按 `(rowMin, colMin)` 升序串行判定，前箱占格影响后箱，不满足则跳过；动画并行）。
6. **触发检查性能**：每箱开箱都要 BFS 连通空格，箱子数量多时是否需要缓存/增量（Demo 规模可先全量）。
7. **1×1 箱子** —— ✅ **已定**：`cornerPrefab`（统一处理成左上角）；且编辑器强制长宽 ≥ 2，1×1 仅防御。
8. **隐藏 Pixel 的可见性** —— ✅ **已定**：`SetActive(false)`。三方案利弊：
   - `active=false`（采用）：零渲染/逻辑开销；`PixelItem` 只有 `Awake`（实例化跑一次）、无 `OnEnable` 副作用，开箱 `SetActive(true)` 不会重初始化；与"本体 Pixel 正常出现"语义一致。注意：inactive 物体会被 `GetComponentsInChildren<PixelItem>()`（默认不含 inactive）跳过，计数改走 `BoxItem.hiddenPixels` 显式列表（§10 已如此设计）。
   - `scale=0`：状态保持但 Animator 仍在跑（浪费），且开箱要恢复原 scale。
   - `y 很小被视觉遮挡`：依赖箱子视觉完整性，易穿模，最不可靠。

---

## 15. 验收清单

- [ ] 编辑器选中两个 Pixel 创建箱子，矩形与顺序无关，越界/重叠被拒绝。
- [ ] 箱子随 JSON 导出/导入，`pixel.cells` 箱子区域为 0，`boxes[].colorIds` 完整。
- [ ] 箱子区域开箱前是障碍：不暴露、不可点击、提取寻路/连通首排判定避开。
- [ ] `TotalPixelCount` 正确包含箱内 Pixel；开箱落地后不重复计数；消费后 `ClearedPixelCount` 正确。
- [ ] 容器生成规划（`CollectPlanningPixels`）把箱内 Pixel 计入，容器容量足额。
- [ ] 满足触发条件（相邻空格全部清空）时开箱：候选格按 (row 升序, col 升序) 排序，颜色按 `colorIds` 顺序一一对应，无随机。
- [ ] 多箱同时满足时按 `(rowMin, colMin)` 升序串行判定；前箱占格导致后箱可用格不足时后箱跳过。
- [ ] 本体外 Pixel 按 (row, col) 顺序在箱子下方出现并 Jump，起跳间隔 `jumpStartInterval`；外跳期间箱子保持可见；最后一个外跳起跳后一个间隔本体内出现并站起，箱子同时放大缩小消失。
- [ ] 开箱动画期间失败判定不误触发。
- [ ] 相邻固定 8 方向（含四角）生效；连通空格固定不纳入。
- [ ] 编辑器拒绝长宽 < 2 的箱子。
