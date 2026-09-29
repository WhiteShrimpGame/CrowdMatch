# CrowdMatch「墙体 T 字 / 十字交叉」功能设计文档

> 状态：**方案待确认，尚未实现**。本文描述如何把墙体编辑从「任意两墙不得重叠」扩展为
> 「允许 T 字 / 十字交叉，交叉点用对应的 T 字 / 十字预制体显示」，并保持**端点（节点）不得重合**。
>
> 结论先行：**数据模型零改动**（墙仍是「端点序列 `points` + `closed`」），改动集中在
> **墙块分类/渲染**与**画布校验**两处。`wallGrid` / `IsWall` / `IsBlocked` / 暴露 / 寻路 /
> 计数 / 关卡 JSON 导入导出**全部零改动**。
>
> 文末 §10 有 4 条待确认项，未定之前不动代码。

---

## 1. 需求原述与拆解

需求原文：

> 墙体扩展为支持 t 字交叉和十字交叉：在画布模式下，允许绘制墙体的某个线段跨过另一墙体的首尾端点，
> 或相反（绘制墙体的首尾端点落在另一墙体的线段）；也允许墙体的线段横跨另一墙体的线段
> （包括自身的另一线段），但不允许墙体的端点/节点重合。交叉点改为用对应 T 字和十字预制体显示。

拆成四条：

| # | 需求 | 归类 |
|---|---|---|
| R1 | 新墙的**线段跨过**已有墙的**端点** | 画布校验放行 + 渲染交叉块 |
| R2 | 新墙的**端点落在**已有墙的**线段**上（R1 的反向） | 同上 |
| R3 | 线段横跨线段（含**同一面墙**的自身另一线段） | 同上（自身那条今天已放行，只是画错） |
| R4 | **不允许端点 / 节点重合** | 画布校验新增一条拒绝 |

R1–R3 的本质是同一件事：**一个格子上出现 ≥ 3 条臂时，它就是一个交叉点**。
因此判定与渲染都只该看**格子的邻接关系**，而不是「这个格属于哪面墙」。

---

## 2. 现状（三条关键事实）

### 2.1 数据模型：墙 = 端点序列

`WallItem.points: List<Vector2>`（`x = 列 col`、`y = 行 row`）+ `closed: bool`；
相邻两点构成一段，每段必须轴对齐（`WallItem.IsValid`）。
`WallItem.CollectOccupiedCells(points, closed, set)` 把线段枚举成占格集合
（含两端与中间格；`closed` 时补首尾闭合段）。

关卡 JSON 侧只有一个 `LevelData.WallData { Vector2[] points; bool closed; }`
（`Assets/Scripts/Core/LevelData.cs:73`），**与 `WallItem` 一一对应，没有额外字段**。

> ⇒ **本方案不动数据模型**，所以 §2 的导入导出、`CollectOccupiedCells`、
> `PixelGroup.RebuildGrid` 里的 `wallGrid` 并集、运行时障碍与暴露判定，全部零改动。

### 2.2 渲染：按格拼预制体，分类只看本墙自己的格

`WallItem.BuildVisual`（`WallItem.cs:169`）对每个占格调 `ClassifyPiece`
（`WallItem.cs:224`），依据是**上下左右有没有墙格**：

| 相邻墙格数 | 类型 | yaw（绕 Y） |
|---|---|---|
| 0 | `Single` | 0° |
| 1 | `End` | `DirYaw(dc,dr)`：组件本地 **+Z** 指向该相邻方向 |
| 2，共线（左右 / 前后） | `Edge` | 水平（左右）90°、竖直（前后）0° |
| 2，垂直 | `Corner` | `CornerYaw`：组件本地 **+X** 与 **+Z** 两臂分别对齐 |
| **≥ 3** | **`Corner` 兜底** ← 错的根源 | `CornerYaw` 只用前两个方向，第 3/4 个方向被忽略 |

两个关键缺陷：

1. **n ≥ 3 掉进 `Corner` 兜底** —— T（3 邻）与十字（4 邻）现在都画成角块。
2. **分类的 `occupied` 是 `BuildVisual` 里现算的「本墙自己的占格集合」** ——
   跨墙交叉时两面墙互相看不见，交叉格各画一个 `Edge`（或 `End`）叠在一起，z-fighting。

预制体字段只有 4 个，在 `PixelGroup.cs:51-60`：
`wallSinglePrefab` / `wallEndPrefab` / `wallEdgePrefab` / `wallCornerPrefab`，
由 `WallItem.ChoosePrefab`（`WallItem.cs:208`）按类型取。

### 2.3 画布：一刀切禁止任何重叠，`_wallCells` 单归属

`PixelColorBrushWindow`（菜单 `CrowdMatch/像素颜色画布`，`PixelColorBrushWindow.cs:361`）：

- 「添加墙体」拖动把经过的格按顺序记进 `_wallStroke`（`AppendWallCell` :2280，只跳过**连续**重复格）。
- 松手即创建：`FinishWallStroke` :2540 → `TryValidateWallStroke` :2387 → `CreateWallFromStroke` :2431。
- 端点 = 轨迹 → `SimplifyPoints` 化简为拐点（:2336）→ `PendingWallPoints` 收尾
  （首尾相邻则去掉重复尾点、交给 `closed` 补段，:2361）。
- **体检三条**（:2387）：① ≥ 2 格；② 相邻两格必须同行或同列；③ **`occupied` 与 `_wallCells` 有任一交集即拒绝**。
- `_wallCells: Dictionary<Vector2Int, WallItem>`（:169）由 `RebuildWallCellMap`（:696）
  逐面墙枚举占格建成，**先到先得（同一格只记一面墙）**。用途：删墙模式反查「点到的是哪面墙」、
  添加模式判「新墙是否压到已有墙」。
- 建墙时把覆盖格上的 `PixelItem` 直接移除（不回填）；删墙整面销毁、同样不回填。

**重要**：`AppendWallCell` 只跳过连续重复格，所以**一笔自身重复经过同一格是被允许的**
（例如一笔折返画出 T / 十字）—— 这类笔画的**格集已经是对的**，只是 §2.2 把它画错了。

创建墙的入口**只有两处**：

| 入口 | 位置 | 是否调 `BuildVisual` |
|---|---|---|
| 画布「添加墙体」 | `PixelColorBrushWindow.CreateWallFromStroke` :2431 → `PixelGroup.SpawnWall` | ✅（`SpawnWall` 内 `wall.BuildVisual(this)`，`PixelGroup.cs:1384`） |
| 场景菜单「创建 ▸ 墙体」 | `WallCreator.CreateWallFromSelection`（`WallItemEditor.cs:248`） | ❌ **完全没有**（见 §7.1） |

`PixelGroup.SpawnWall`（`PixelGroup.cs:1375`）**只重建新墙自己的墙块**，
所以关卡导入时先建的墙看不到后建墙造成的交叉。

---

## 3. 口径：允许什么、禁止什么

交叉格上，把「哪些方向有墙」列出来，**只看格子，不看属于哪面墙**：

| # | 情形 | 示意（`.` 空、`▪` 墙格、`▫` 该墙的端点） | 今天 | 改后 |
|---|---|---|---|---|
| 1 | 新墙**线段中间**跨过已有墙的**端点**（R1） | 已有 `▫`，新墙竖着穿过 `▫` | 拒绝 | ✅ **T**（3 臂） |
| 2 | 新墙**端点**落在已有墙的**线段中间**（R2） | 新墙竖笔尾点落在 `▪▪▪` 中间那格 | 拒绝 | ✅ **T** |
| 3 | 线段跨线段（R3，两墙） | `▪` 横竖各一 | 拒绝 | ✅ **十字**（4 臂） |
| 4 | 线段跨线段（R3，**同一面墙**） | 一笔折返画出 `▪` | 允许（画成角） | ✅ **十字**（画对） |
| 5 | 新墙**端点**落在已有墙**端点**上（**违反 R4**） | 两个 `▫` 撞在同一格 | 拒绝 | ❌ **仍拒绝** |
| 6 | 新墙**沿着**已有墙同轴贴合 ≥ 1 格 | 两条横线叠在 row 1 | 拒绝 | ❌ **仍拒绝**（见 §10-Q3） |

第 6 条不是需求原话里提到的，是**建议保留的禁止项**。理由：同轴贴合不是「交叉」而是「重复画线」，
会变成两面完全一样的墙 —— 删掉一条另一条还在，表现为「删不掉」。**待确认（§10-Q3）。**

用四张图把边界钉死（`#` 新墙的格、`▪` 已有墙的格、`▫` 该墙的端点；「交叉格」= 两墙共有的那一格）：

```
R1  新墙线段跨过已有墙的端点         R2  新墙端点落在已有墙线段上
       #                                  ▪ ▪ ▪
       ▫ ▪   ▫ = 已有墙端点              #     # = 新墙末格就在中间那个 ▪ 上
       #     新墙给 上+下                #
             已有墙给 右                      新墙给 下
             共 3 臂                           已有墙给 左+右 ⇒ 共 3 臂
    ✅ T                              ✅ T

R3  线段跨线段（两墙，或同一面墙的自身两段）
       #
     ▪ ▪ ▪   ▪ 新墙给 上+下；已有墙给 左+右 ⇒ 共 4 臂
       #
    ✅ 十字

端点撞端点（违反 R4）
     ▫ ▪   ▫ 同一格既是已有墙端点（已有墙给 右）
     #       也是新墙端点（新墙给 下）⇒ 只有 2 臂正交
    ❌ 拒绝
```

> 注意最后一张：端点撞端点时掩码是 2 臂正交 ⇒ 按 §5.2 的分类会画成 `Corner`，
> 看上去像个正常的墙角，但结构上是**两面独立的墙**（删一面另一面还在，交叉块归属也不确定）。
> 这正是要禁止它的实际后果。

---

## 4. 判定规则（两条，取代「一律禁止重叠」）

对笔画占格集合 `occupied` 的每个格 `c`：

先分别求出**新墙在 `c` 上的轴向**与**已有墙 `W` 在 `c` 上的轴向** —— 轴向 = {横,竖} 的子集，
由该墙**自己的**相邻占格算出（左右任一 ⇒ 横；前后任一 ⇒ 竖）。

### 规则 A：同轴冲突 → 拒绝

> 若存在一面已有墙 `W` 包含 `c`，且 **新墙在 `c` 的轴向集合 ∩ `W` 在 `c` 的轴向集合 ≠ ∅**，
> 判为「沿已有墙体重叠」，拒绝该笔画。

**必须逐面墙比，不能取已有墙的轴向并集** —— 否则两面已有墙各占一个轴时，
任何新墙穿过该格都会被误判（并集 = {横,竖}，必然相交）。

### 规则 B：端点撞端点 → 拒绝

> 若新墙化简后的某个端点格 == 某面已有墙的某个端点格，判为「端点落在已有墙体端点上」，拒绝。

### 覆盖性自检

| 上表情形 | 规则 A | 规则 B | 结果 |
|---|---|---|---|
| 1（跨端点） | 新墙竖、旧墙横 ⇒ 轴不相交，**不触发** | 新墙在 `c` 无端点（`c` 是线段中间）⇒ **不触发** | ✅ T |
| 2（端点落线段） | 新墙在 `c` 只有 1 臂 ⇒ 竖；旧墙横 ⇒ **不触发** | `c` 是旧墙的**线段中间**、不是端点 ⇒ **不触发** | ✅ T |
| 3（十字） | 新竖旧横 ⇒ **不触发** | 双方在 `c` 都无端点 ⇒ **不触发** | ✅ 十字 |
| 4（自身十字） | 只与「已有墙」比，不与自己比 ⇒ **不触发** | 同上 ⇒ **不触发** | ✅ 十字 |
| 5（端点撞端点） | 正交时**不触发** | 双方端点同格 ⇒ **触发** | ❌ 拒绝 |
| 6（同轴贴合） | 同轴 ⇒ **触发** | — | ❌ 拒绝 |

规则 A 另外还拦得住一个容易漏的坏例子：**新墙在交叉格拐弯后顺着一路压着旧墙走**
（拐弯点的轴向集合含横+竖，与旧墙的横相交 ⇒ 触发）。

### 状态行提示

`_lastStrokeError`（:183）现有的三类理由扩成四类：`至少需要 2 格` / `跳成对角` /
`沿已有墙体重叠 N 格` / `端点落在已有墙体端点上`。

**可选增强**：松手前的预览里顺带报「将形成 2 处 T 交叉、1 处十字交叉」——
与判定同一趟掩码扫描即可算出，代价为零。是否要做见 §10-Q4。

---

## 5. 渲染：全局掩码 + 两个新部件

### 5.1 新增两个墙块类型

```csharp
public enum WallPieceType { Single, End, Edge, Corner, Tee, Cross }   // 只加两个值（additive）
```

`WallPieceType` 是运行时 public enum，仅用于当帧分类与子物体命名，**没有任何地方序列化它**，
加值不会影响已有场景 / 预制体。

### 5.2 分类改成吃「全组并集」

```csharp
/// 按「格子的四向邻接」分类，与「这个格属于哪面墙」无关。
/// occupied = 全组所有 WallItem 占格的并集。
static void Classify(bool right, bool left, bool front, bool back,
                     out WallPieceType type, out float yaw)
```

| 掩码 | 类型 | yaw |
|---|---|---|
| 0 邻 | `Single` | 0° |
| 1 邻 | `End` | `DirYaw(dc, dr)`（沿用） |
| 2 邻共线 | `Edge` | 水平 90° / 竖直 0°（沿用） |
| 2 邻垂直 | `Corner` | `CornerYaw`（沿用） |
| **3 邻** | **`Tee`** | **由「唯一缺失的方向」定，见 §10-Q1** |
| **4 邻** | **`Cross`** | 0°（四向对称） |

### 5.3 新增全量重建入口（放 `PixelGroup`）

```csharp
/// 用全组墙格并集重建所有墙块。交叉格归「层级顺序最先出现的那面墙」——
/// 与画布 _wallCells 的先到先得同口径，保证交叉格只画一次（不叠块）。
public void RebuildWallVisuals()
{
    var walls = GetComponentsInChildren<WallItem>();

    var occ = new HashSet<Vector2Int>();                  // 全组并集：分类的唯一依据
    foreach (var w in walls)
        foreach (var c in w.EnumerateOccupiedCells())
            if (IsInRange(c.x, c.y)) occ.Add(c);

    var done = new HashSet<Vector2Int>();                 // 已渲染的格
    foreach (var w in walls)                              // 层级顺序 ⇒ 归属确定
    {
        w.ClearPieces();                                  // 原来 BuildVisual 开头那段清子物体
        foreach (var c in w.EnumerateOccupiedCells())
        {
            if (!IsInRange(c.x, c.y) || !done.Add(c)) continue;
            w.SpawnPiece(this, c, /* 按 occ 掩码分类 */);
        }
    }
}
```

`WallItem.BuildVisual(pg)` 退化为「只渲染自己的格」的薄封装（或直接删掉、全部改走新入口）。

预制体字段加两个，与现有 4 个并排（`PixelGroup.cs:51-60`）：

```csharp
public GameObject wallTeePrefab;
public GameObject wallCrossPrefab;
```

`ChoosePrefab` 补两个 case；缺预制体时沿用现有的
`Debug.LogWarning("[WallItem] 缺少" + type + "预制体，跳过墙体格 …")` + 跳过。

### 5.4 为什么必须「全量」而不是「只重建新墙」

新墙建出来时，**已有墙在交叉格的块要从 `End` / `Edge` 变成 `Tee` / `Cross`**，
只重建新墙不够。删墙同理（`Tee` 退回 `End`）。所以**建 / 删 / 闭环 / 关卡导入 / 撤销**
这 5 个点各调一次全量重建。

### 5.5 ⚠️ 不要把它塞进 `RefreshSnapshot()`

`RefreshSnapshot`（:523）由 `hierarchyChanged` 置脏驱动（`OnHierarchyChanged` :424 →
`SyncWithSceneIfDirty` :435），而**重建墙块本身会新建/销毁子物体、又触发 `hierarchyChanged`**
⇒ 只要窗口开着就会变成「每帧销毁并重建全部墙块」。**只在 §6 的 5 个显式调用点调。**
（若日后要把重建挂进 `RefreshSnapshot`，必须先把重建改成「按格复用、无差异就不动对象」的幂等版。）

---

## 6. 调用点接线

| 位置 | 改动 |
|---|---|
| `PixelGroup.SpawnWall`（:1375） | `wall.BuildVisual(this)` → `RebuildWallVisuals()`。关卡导入逐面调用也自然正确（每关墙 ≤ 十位数，见 §9） |
| `PixelColorBrushWindow.CreateWallFromStroke`（:2431） | 校验换成规则 A + B（§4）；渲染由 `SpawnWall` 的全量重建带走 |
| `PixelColorBrushWindow.DeleteWall`（:2510） | `RefreshSnapshot()` 之后再补一次 `RebuildWallVisuals()`（被删墙的邻居交叉格要退块） |
| `PixelColorBrushWindow.OnUndoRedoPerformed`（:454） | **必须补一次全量重建** —— 见下方「唯一的真实回归点」 |
| `PixelColorBrushWindow.OnEnable`（:403 之后） | 补一次：窗口关着时按 Ctrl+Z，重开时才对齐 |
| `WallItemEditor.CloseLoop` / `CancelLoop`（:66 / :134） | 闭环 / 取消闭环后重建（闭合段可能正好接到别的墙上） |
| `LevelLoader.ApplyWalls`（`LevelLoader.cs:150`） | 无需额外调用（`SpawnWall` 内部已重建） |
| 运行时 `wallGrid` / `IsWall` / `IsBlocked` / 暴露 / 寻路 / 计数 | **零改动**（并集语义本来如此） |
| 关卡 JSON 导入 / 导出 | **零改动**（`WallData{points, closed}` 不变） |
| `ContainerRearranger.Validate` / `LevelGridBoard` / `ImageBatchLevelExporter` | **零改动**（都只读占格并集） |

### 唯一的真实回归点：撤销「建墙」

今天墙块是**新墙 GO 的子物体**，撤销时随 GO 一起消失，所以不需要额外处理。
改后，**邻居墙上的交叉块是「邻居的子物体」**，撤销「新建墙」不会带走它 ⇒ 邻居墙会留下一个陈旧块
（例如本该退回 `End` 的 `Tee`）。

处理：`OnUndoRedoPerformed`（:454）里补一次 `RebuildWallVisuals()`。

> 补充说明：窗口**关闭**状态下按 Ctrl+Z（例如在 Scene 视图里撤销）不会触发该回调，
> 陈旧块会留到窗口下次 `OnEnable` 的那次重建。这是可接受的（不改场景数据，只是显示滞后），
> 但要在验证清单里覆盖。

---

## 7. 两处附带发现（不属于本次需求的引入项）

### 7.1 场景菜单建的墙没有墙块

`WallCreator.CreateWallFromSelection`（`WallItemEditor.cs:248`，菜单
`CrowdMatch/创建（场景视图 · 选中 Pixel）/墙体`）只做：新建 GO → `AddComponent<WallItem>` →
赋 `points` → `RegisterCreatedObjectUndo` → 移除覆盖格上的 Pixel。
**从头到尾没有调 `BuildVisual`**，而全工程除 `PixelGroup.SpawnWall` 外没有第二处调墙的 `BuildVisual`
⇒ 这条菜单建出来的墙**只有 Gizmos，没有墙块**。

它同时**没有任何重叠校验**（想怎么叠就怎么叠，直接吞掉覆盖的 Pixel）。

**这是既有现状，不是本次改动引入的。** 要不要一起接上（调 `RebuildWallVisuals` + 复用规则 A/B）
见 §10-Q2。

### 7.2 跨墙闭环的「内部」算不对

`WallItemEditor.ComputeInteriorCells`（`WallItemEditor.cs:170`）做「从网格边界四向 BFS 漫过非墙格」
求闭环内部时，**障碍判据只用本墙的占格**（`wallCells.Contains(cell)`）。
T / 十字让「两面墙合围一个区域」变得很容易，这时它会把合围区域误判成外部。

修法（1 行语义改动）：障碍判据换成 `group.IsWall(cell)`（即 `RebuildGrid` 之后的并集）。
**建议一起改**，因为它正好是本需求解锁的用法。

---

## 8. 改动清单

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Gameplay/WallItem.cs` | `WallPieceType` 加 `Tee` / `Cross`；`ClassifyPiece` 改为吃全组掩码的静态分类；`CornerYaw` 之后补 `TeeYaw`；`BuildVisual` 拆成 `ClearPieces` + `SpawnPiece` |
| `Assets/Scripts/Gameplay/PixelGroup.cs` | 加 `wallTeePrefab` / `wallCrossPrefab` 字段；新增 `RebuildWallVisuals()`；`SpawnWall` 改调新入口 |
| `Assets/Scripts/Editor/PixelColorBrushWindow.cs` | `TryValidateWallStroke` 换成规则 A + B（含新错误文案）；`DeleteWall` / `OnUndoRedoPerformed` / `OnEnable` 补全量重建 |
| `Assets/Scripts/Editor/WallItemEditor.cs` | `CloseLoop` / `CancelLoop` 后补全量重建；**（可选，§10-Q2）** `WallCreator` 接上重建与规则 A/B；**（建议，§7.2）** `ComputeInteriorCells` 障碍判据改用 `group.IsWall` |
| `Assets/Prefabs/Wall05.prefab` / `Wall06.prefab`（或其它） | **你在 Unity 里做**：确认哪个是 T、哪个是十字，并按 §10-Q1 选定的 0° 朝向摆正 |
| 场景里的 `PixelGroup` | **你在 Unity 里做**：把两个新预制体拖进新增的 `wallTeePrefab` / `wallCrossPrefab` 字段（3 个场景：`GameScene` / `GameScene_WY` / `GameScene_zy02`，现有 4 个字段在这三个场景里各配了一份：`GameScene.unity:804-807` 等） |
| 关卡 JSON / `LevelData` | **零改动** |

> 说明：`IReadOnlyList` 级别的接口、`EnumerateOccupiedCells`、`wallGrid` 并集都保持原样，
> 所以这次改动**不进任何数据**，回滚只需回滚代码。

---

## 9. 验证

### 9.1 离线编译（两条，均需 0 错误）

```
dotnet build Assembly-CSharp.csproj
```

```
dotnet build Assembly-CSharp-Editor.csproj
```

若报 `CS0006 未能找到元数据文件 Temp\Bin\Debug\*.dll`，去掉 `-p:BuildProjectReferences=false` 重跑
（见既有约定；本次两条命令都不带该参数即可）。

### 9.2 Unity 手测清单

| # | 操作 | 通过标准 |
|---|---|---|
| 1 | 画一笔 T（起点落在已有墙上） | 交叉格显示 T 块，松手即创建成功 |
| 2 | 画一笔十字（穿过已有墙） | 交叉格显示十字块 |
| 3 | 一笔自身画出十字 / T（折返） | 交叉格显示十字 / T（不是角块） |
| 4 | 反向 T：新墙端点落在已有墙线段中间 | 成功，显示 T |
| 5 | 试画「端点撞端点」 | 拒绝，状态行写明「端点落在已有墙体端点上」 |
| 6 | 试画「同轴贴合」（与已有墙叠同一行） | 拒绝，状态行写明「沿已有墙体重叠 N 格」 |
| 7 | 删掉十字的一笔 | 另一笔的交叉块退回 `Edge` / `End` |
| 8 | 建墙后 Ctrl+Z | 邻居墙的 T 块退回 `End`（**§6 的回归点**） |
| 9 | 导入 `LevelData/Level_Tree3_wall.json` | 交叉点显示正确（不是角块、不叠块） |
| 10 | 缺预制体：把 `wallTeePrefab` 留空再画 T | Console 出现明确 warning + 跳过该格，不崩 |
| 11 | 运行时进关卡 | 墙的阻挡 / 暴露 / 胜负判定与改前逐字一致（交叉不该改变任何玩法） |
| 12 | 编辑模式回归：`ContainerDragWindow` 拖车、木箱封条、冰组、升降台 | 与改前一致 |

### 9.3 数量参照

抽查 `Assets/LevelData/` 下 84 个关卡 JSON：`"closed"` 字段共出现 547 次
⇒ **平均约 6.5 面墙 / 关**；抽查到的最大值为 9 面（多个 `Level_C0x`），
`Level_Tree3_wall.json` 为 5 面。
（注意：早期 JSON 没写 `closed` 字段，这类文件的实际墙数会被低估，例如 `LevelItem039_wall.json`
在本次抽查里计到 0 次 —— 它是老格式或本就没有墙。）

⇒ 「每次改动全量重建墙块」的开销可以忽略，不需要做增量优化。

---

## 10. 待确认项（未定之前不动代码）

### Q1：T 字预制体的 0° 朝向怎么定

既有家族的朝向约定：`Corner` = 组件**本地 +X 与 +Z 两臂**；`Edge` 竖直时 0°、
水平时 90°（长轴沿本地 +Z）；`End` = 本地 **+Z 指向相邻墙格方向**。

T 有三个臂，需要一个「哪个方向是 0°」的约定。因为 T 的四个朝向正好由**唯一缺失的那个方向**
唯一确定，直接把它列成数值表最不容易搞错：

`DirYaw(dc, dr) = Atan2(dc, -dr)`（既有实现，`WallItem.cs:258`）⇒
`+X 右 → 90°`、`−X 左 → −90°`、`+Z 后 → 180°`、`−Z 前 → 0°`。

| 缺失方向（格子） | 世界方向 | `DirYaw` | **(A) 缺口朝本地 +Z** ⇒ yaw | **(B) 主干朝本地 +Z** ⇒ yaw |
|---|---|---|---|---|
| `(−1, 0)` | −X（左） | −90° | **−90°** | **90°** |
| `(0, −1)` | −Z（前） | 0° | **0°** | **180°** |
| `(1, 0)` | +X（右） | 90° | **90°** | **−90°** |
| `(0, 1)` | +Z（后） | 180° | **180°** | **0°** |

- **A** = `yaw = DirYaw(缺失方向)`：本地 `+Z` 指向**那个空着的方向**
- **B** = `yaw = DirYaw(缺失方向) + 180°`：本地 `+Z` 指向**主干**

两种写法只差 180°，但选错就会让所有 T 块朝向全反，**必须按你实际摆预制体的方式定**。
核对办法：把预制体拖进场景、`rotation.y` 设 0，看**哪个方向是空的**，然后对照上表。

一个提示：`Corner` 的臂是本地 `+X` 与 `+Z`；若你是「顺着 `Corner` 再加一条 −X 臂」做出 T 的，
那么 yaw=0 时缺口落在 `−Z`（前）—— 那是 **B**。

**另外请确认：十字预制体四向对称、yaw 恒为 0，对吗？**

### Q2：场景菜单 `WallCreator` 是否一起纳入 §7.1

- (a) 一起改：接上 `RebuildWallVisuals` + 复用规则 A/B（推荐，否则两条建墙路径口径不一致）
- (b) 保持原样不动，只动画布那条路径

### Q3：第 6 类「同轴贴合」是否保留禁止（§3 表第 6 行）

- (a) **保留禁止**（推荐）：同轴贴合不是交叉，是重复画线，删一条另一条还在
- (b) 也放行：那就要决定「删墙时点到重合格删哪一面墙」

### Q4：松手前的预览是否报「将形成 N 处 T 交叉 / M 处十字交叉」

- (a) 报（推荐）：与判定同一趟扫描即可算出，零额外代价，画之前就能看出会在哪里生成交叉
- (b) 不报，保持状态行文案现状

---

## 11. 已知风险与取舍

| 风险 | 说明 | 处理 |
|---|---|---|
| **撤销建墙留陈旧块** | §6「唯一的真实回归点」 | `OnUndoRedoPerformed` + `OnEnable` 各补一次全量重建 |
| **每帧重建（性能陷阱）** | 把重建挂进 `RefreshSnapshot` 会与 `hierarchyChanged` 形成「重建 → 事件 → 重建」的每帧循环 | 只在 5 个显式点调；见 §5.5 |
| **交叉格的归属是任意的** | `Tee` / `Cross` 块挂在「层级顺序最先出现」的那面墙下，删那面墙会连带删掉交叉块 | 全量重建会立刻补回（归属只是「谁的孩子」，块本身与归属无关） |
| **删墙模式的二步确认在交叉格上有歧义** | `_wallCells` 单归属 ⇒ 点交叉格只会选中先到的那面墙 | 点该墙的**非交叉格**即可；状态行本来就会显示高亮的是哪面墙（:1558）。不改 `Dictionary` 结构，风险最低 |
| **预制体接缝** | 新 T / 十字预制体的臂长、厚度、切角必须与既有 `Edge` / `Corner` 同一套格子切法，否则接缝错位 | 美术侧要求，写进 §8 的「你在 Unity 里做」 |
| **包围区域跨墙** | 两面墙合围的内部区域，`ComputeInteriorCells` 今天算不对 | §7.2 的 1 行修法 |

---

## 12. 附：相关代码位置索引

| 主题 | 位置 |
|---|---|
| 墙数据与占格 | `Assets/Scripts/Gameplay/WallItem.cs:24`（points）/ `:115`（EnumerateSegment）/ `:131`（CollectOccupiedCells） |
| 墙块分类与 yaw | `WallItem.cs:224`（ClassifyPiece）/ `:208`（ChoosePrefab）/ `:258`（DirYaw）/ `:264`（CornerYaw） |
| 墙块生成 | `WallItem.cs:169`（BuildVisual） |
| 预制体字段 | `PixelGroup.cs:51-60` |
| 墙的占格表 | `PixelGroup.cs:189`（RebuildGrid）/ `:338`（IsWall）/ `:1205`（ClearWalls）/ `:1375`（SpawnWall） |
| 画布：墙模式 | `PixelColorBrushWindow.cs:93`（Mode.AddWall）/ `:147`（墙状态）/ `:2280`（AppendWallCell）/ `:2336`（SimplifyPoints）/ `:2387`（TryValidateWallStroke）/ `:2431`（CreateWallFromStroke）/ `:2510`（DeleteWall） |
| 画布：格 → 墙表 | `PixelColorBrushWindow.cs:169` / `:696`（RebuildWallCellMap） |
| 画布：快照与重建钩子 | `PixelColorBrushWindow.cs:424`（OnHierarchyChanged）/ `:435`（SyncWithSceneIfDirty）/ `:454`（OnUndoRedoPerformed）/ `:523`（RefreshSnapshot） |
| Inspector 与创建菜单 | `WallItemEditor.cs:66`（CloseLoop）/ `:134`（CancelLoop）/ `:170`（ComputeInteriorCells）/ `:248`（WallCreator） |
| 关卡导入导出 | `LevelData.cs:73`（WallData）/ `LevelLoader.cs:150`（ApplyWalls）/ `LevelLoader.cs:88`（ApplyPixel 里的墙格 skipCells） |
