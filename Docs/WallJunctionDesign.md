# CrowdMatch「墙体 T 字 / 十字交叉」功能设计文档

> 状态：**代码已实现（2026-09-29），预制体与场景接线待做**。本文描述如何把墙体编辑从「任意两墙不得重叠」
> 扩展为「允许 T 字 / 十字交叉，交叉点用对应的 T 字 / 十字预制体显示」，并保持**端点（节点）不得重合**。
>
> 结论先行：**数据模型零改动**（墙仍是「端点序列 `points` + `closed`」），改动集中在
> **墙块分类/渲染**与**画布校验**两处。`wallGrid` / `IsWall` / `IsBlocked` / 暴露 / 寻路 /
> 计数 / 关卡 JSON 导入导出**全部零改动**。
>
> **实施进度**
>
> | # | 项 | 状态 |
> |---|---|---|
> | 1 | `WallPieceType` 加 `Tee` / `Cross`；`Classify` 改吃四向掩码；`TeeYaw` | ✅ 已做 |
> | 2 | `WallItem.BuildVisual` 拆成 `ClearPieces` + `SpawnPiece`（分类交给调用方） | ✅ 已做 |
> | 3 | `PixelGroup.wallTeePrefab` / `wallCrossPrefab` 字段 | ✅ 已做 |
> | 4 | `PixelGroup.RebuildWallVisuals()`（全组并集分类 + 一格只画一块） | ✅ 已做 |
> | 5 | `SpawnWall` 改调整组重建 | ✅ 已做 |
> | 6 | 画布判定换成规则 A（同轴冲突）+ B（端点撞端点） | ✅ 已做 |
> | 7 | 画布 `DeleteWall` / `OnUndoRedoPerformed` / `OnEnable` 补整组重建 | ✅ 已做 |
> | 8 | `WallItemEditor` 闭环 / 取消闭环后补整组重建 | ✅ 已做 |
> | 9 | 状态行交叉预览（Q4 = a） | ✅ 已做 |
> | 10 | 离线编译两个程序集 | ✅ 0 错误 |
> | 11 | T / 十字预制体的 0° 朝向（Q1） | ⏳ 见 §10-Q1，**先按 B 实现**，实测反了改一行 |
> | 12 | 场景 `PixelGroup` 上拖两个新预制体字段 | ⏳ **你在 Unity 里做** |
> | 13 | §7.1 场景菜单 `WallCreator`（Q2 = b） | ⏳ **本次不动**（保持现状） |
> | 14 | §7.2 跨墙闭环的 `ComputeInteriorCells` | ⏳ **本次不动**（见 §7.2 的说明） |
> | 15 | **追加**：1×1 墙（画布单击 = 单格墙） | ✅ 已做，见 §11 |
> | 16 | **口径修正**：规则 C（墙内线段不许重叠）+ 掩码改为「覆盖本格的墙」并集 | ✅ 已做，见 §3 表第 7/8 行、§4、§5.2 |
> | 17 | **口径再修正**：分类改为按「**线段连接关系**」累加臂 —— 墙内与跨墙统一到同一条口径 | ✅ 已做，见 §5.2、§3 表第 9 行 |
>
> §10 的 4 条待确认项已定：Q1 = 先按 B、Q2 = b、Q3 = a、Q4 = a。

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
| 画布「添加墙体」 | `PixelColorBrushWindow.CreateWallFromStroke` → `PixelGroup.SpawnWall` | ✅（`SpawnWall` 内整组 `RebuildWallVisuals()`） |
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
| 4 | 线段跨线段（R3，**同一面墙**） | 一笔画出**不折返**的自交（回环走位） | 允许（画成角） | ✅ **T / 十字**（画对） |
| 5 | 新墙**端点**落在已有墙**端点**上（**违反 R4**） | 两个 `▫` 撞在同一格 | 拒绝 | ❌ **仍拒绝** |
| 6 | 新墙**沿着**已有墙同轴贴合 ≥ 1 格 | 两条横线叠在 row 1 | 拒绝 | ❌ **仍拒绝**（Q3 = a） |
| 7 | **新墙内**线段重叠（一笔折返压线） | 一笔 A→B→A | 允许（数据退化） | ❌ **新增拒绝**（规则 C，见 §4） |
| 8 | 两面墙**相邻但不重叠** | A 的端点在 (1,1)、B 的端点在 (2,1) | 被并成一条连续墙 | ✅ **各自独立**：各留自己的 `End`（见 §5.2） |
| 9 | **同一面墙**折返贴着自己（∏ 形：两条竖臂分别走 col 0 / col 1） | 两臂并排、格子相邻但不相连 | 误判成 `Tee`（按格子相邻） | ✅ **各自 `Edge`**：只认线段连接（见 §5.2） |

第 6 条不是需求原话里提到的，是**建议保留的禁止项**。理由：同轴贴合不是「交叉」而是「重复画线」，
会变成两面完全一样的墙 —— 删掉一条另一条还在，表现为「删不掉」。**Q3 定为 a：保留禁止。**

第 7、8 条是 2026-09-29 的口径修正（追加需求）：

- **第 7 条（新规则 C）**：需求原文里「包括自身的另一线段」指的是**垂直自交**那一类（第 4 行）；
  交点两侧**同轴**压线（折返）不算交叉，是退化数据 —— 一个格被两条同向线段各走了一遍。拒绝。
  副作用：**「一笔画出的 T / 十字」从此走不通**（一笔要穿过交点必然折返），实践中就是**画两面墙**。
- **第 8 条**：判定与渲染都只看「**同一格被谁覆盖**」。相邻但不重叠 ⇒ 数据层面没有交叉 ⇒ 各自独立绘制。
- **第 9 条**：把第 8 条的口径从「跨墙」推进到「**墙内也一样**」—— 相邻不等于连着。
  一个格的臂只能来自**真的经过它的线段**（在它内部 = 两条臂，在它端点上 = 一条臂），
  与「谁的格子挨着」无关。第 8、9 条合起来就是 §5.2 的「线段连接」口径。

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

## 4. 判定规则（三条，取代「一律禁止重叠」）

**规则 C 只看这一笔自己的几何**；**规则 A / B 看它与已有墙的关系** —— 对笔画占格集合 `occupied` 的每个格 `c`：

先分别求出**新墙在 `c` 上的轴向**与**已有墙 `W` 在 `c` 上的轴向** —— 轴向 = {横,竖} 的子集，
由该墙**自己的**相邻占格算出（左右任一 ⇒ 横；前后任一 ⇒ 竖）。

### 规则 C：墙内线段重叠 → 拒绝

> 同一面墙化简后的端点序列里，若存在**两条同轴线段**的占格集合相交（共 ≥ 1 格），
> 判为「墙内线段重叠」，拒绝该笔画。

**只在同轴线段之间判**：不同轴的两条线段最多共一个格，那是「自身的两根线段交叉」（§3 表第 4 行，允许）。
化简（`SimplifyPoints`）已经去掉了「同向延续」的多余拐点，剩下能自重叠的只有**折返**：
`A→B→A` 会产生两条方向相反、压在同一批格上的线段；`(0,1),(2,1),(1,1)` 这种「顶到头再退回来」同理。

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

上表不含规则 C：它只与**这一笔自己的线段**比，与已有墙无关 —— 一笔折返（`A→B→A`）被它拦下，
一笔不折返的自交（回环走位）放行。

### 状态行提示

`_lastStrokeError` 的理由现在是这几类（按判定顺序）：
`至少需要 1 格` / `跳成对角` / `墙内线段重叠（规则 C）` / `沿已有墙体重叠 N 格（规则 A）` /
`端点落在已有墙体端点上（规则 B）` / `(x,y) 已有墙体 —— 1×1 墙不能叠在已有墙体上`。

**已实现（Q4 = a）**：松手前的预览会在「可生成 ✓」后追加「将形成交叉：T 字 2 处、十字 1 处」，
口径与真正建出来的完全一致（两边都走 `WallItem.ClassifyOwned`，不会发散）。

---

## 5. 渲染：「覆盖本格的墙」并集掩码 + 两个新部件

### 5.1 新增两个墙块类型

```csharp
public enum WallPieceType { Single, End, Edge, Corner, Tee, Cross }   // 只加两个值（additive）
```

`WallPieceType` 是运行时 public enum，仅用于当帧分类与子物体命名，**没有任何地方序列化它**，
加值不会影响已有场景 / 预制体。

### 5.2 分类：按**线段连接关系**给格累加「臂」

**不按格子相邻判。** 掩码只有一个来源：**哪条线段真的经过这一格**。

```csharp
/// 把一面墙的臂累加进 arms（格 → 臂掩码）。每条线段逐格登记：
///   线段**内部**的格 → 前后两条臂；线段**端点**格 → 只贡献朝内那一条。
/// 可以对多面墙反复调用同一个字典 —— 同一格被各条覆盖它的线段依次 OR，天然合并。
public static void AccumulateArms(IReadOnlyList<Vector2> points, bool closed,
                                  Dictionary<Vector2Int, int> arms,
                                  Func<int,int,bool> inRange = null)

/// 按臂掩码判类型（0 / 1 / 2共线 / 2垂直 / 3 / 4 ⇒ Single / End / Edge / Corner / Tee / Cross）
public static WallPieceType ClassifyMask(int arms, out float yaw)
```

于是「同一格被**两条线段**覆盖」才是交叉 —— 这两条线段可以来自两面墙，也可以来自**同一面墙的两段**：

| 情形 | 按格子相邻（❌ 旧） | 按线段连接（✅ 现） |
|---|---|---|
| 两面墙**首尾相接**（A 端点在 (1,1)、B 端点在 (2,1)） | 两格都判 `Edge`，连成一条、端点消失 | 各自 `End`，**独立绘制** |
| **同一面墙**折返贴着自己（∏ 形：一条竖臂在 col 0，另一条竖臂在 col 1） | (0,1) 看到邻居 (1,1) ⇒ 误判 `Tee` | (0,1) 只被自己那条竖段覆盖 ⇒ `Edge` |
| 同一格被两条线段真的穿过 | T / 十字 | T / 十字（不变） |

> ⚠️ **两次退回的教训（本节的来历）**：
> 1. 第一版拿「**全组所有墙的格**」当掩码 ⇒ 相邻的两面墙被连成一条、端点块凭空消失。
> 2. 第二版改成「**覆盖这一格的墙**的格集并集」⇒ 跨墙修好了，但**墙内**仍按格子相邻，
>    于是同一面墙折返贴着自己在 ∏ 形里误判成 `Tee`。
> 3. 现在只认**线段**：一个格的臂完全由「哪些线段经过它、它在这些线段里是内部还是端点」决定。
>    跨墙与墙内**同一条口径**，没有特例。

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
/// 重建所有墙块：先把**全组所有墙的线段**累加成一份臂掩码，再逐墙把格画出来。
/// 一格只画一块（归层级顺序最先出现的那面墙），类型按 ClassifyMask 判。
public void RebuildWallVisuals()
{
    var walls = GetComponentsInChildren<WallItem>();

    // 全组共用一份臂掩码：跨墙与墙内同一条口径，不需要「谁覆盖了哪格」的簿记
    var arms = new Dictionary<Vector2Int, int>();
    foreach (var w in walls)
        WallItem.AccumulateArms(w.points, w.closed, arms, IsInRange);   // IsInRange 兼作越界过滤

    var rendered = new HashSet<Vector2Int>();                            // 已渲染的格
    foreach (var w in walls)                                             // 层级顺序 ⇒ 重叠格归属确定
    {
        w.ClearPieces();
        foreach (var c in w.EnumerateOccupiedCells())
        {
            if (!IsInRange(c.x, c.y) || !rendered.Add(c)) continue;
            arms.TryGetValue(c, out int mask);                            // 没有臂 = 1×1 墙 ⇒ 掩码 0
            w.SpawnPiece(this, c, WallItem.ClassifyMask(mask, out float yaw), yaw);
        }
    }
}
```

> **归属**（重叠格归谁）与**掩码**是两件事：归属只决定「块挂在谁名下」，掩码只由线段决定。
> 所以重叠格的 T / 十字块不会因为归属而画错。

`WallItem.BuildVisual` 已拆成 `ClearPieces` + `SpawnPiece`，分类交给调用方（见 §11.3）。

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
| `PixelGroup.SpawnWall` | `wall.BuildVisual(this)` → 整组 `RebuildWallVisuals()` ✅。关卡导入逐面调用也自然正确（每关墙 ≤ 十位数，见 §9） |
| `PixelGroup.RebuildWallVisuals`（新增） | 全组并集分类 + 一格只画一块（归属 = 层级顺序最先出现的墙） |
| `PixelColorBrushWindow.CreateWallFromStroke`（:2431） | 校验换成规则 A + B（§4）；渲染由 `SpawnWall` 的全量重建带走 |
| `PixelColorBrushWindow.DeleteWall` | ✅ `RefreshSnapshot()` **之前**补一次整组重建（被删墙的邻居交叉格要退块）。放前面是为了让 `RefreshSnapshot` 末尾记的 `_lastChildCount` 已经包含新墙块，免得下一帧又被「子物体数变了」兜底判脏 |
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

> **决定（2026-09-29）：本次不动。** 原因：判据换成全组并集后，**别的墙也会变成 BFS 的屏障**，
> 于是「别处墙围出来的封闭区域」一旦落在本墙的包围盒内，就会被算成本墙的内部 ——
> 连**单面封闭墙**（今天唯一的用法）的 Inspector 闭环弹窗里「包围区域格数 / 含 Pixel 个数」
> 都会跟着变。这是独立于 T / 十字的一个口径决定，值得单独确认，不该夹在本次改动里顺手改。
> 现状保持：`ComputeInteriorCells` 仍只把**本墙的格**当障碍。

---

## 8. 改动清单

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Gameplay/WallItem.cs` | `WallPieceType` 加 `Tee` / `Cross`；`ClassifyPiece` → 静态 `Classify`（吃四向掩码）；新增 `AccumulateArms`（按线段给格累加臂）+ `ClassifyMask`（见 §5.2）；`CornerYaw` 之后补 `TeeYaw`；`BuildVisual` 拆成 `ClearPieces` + `SpawnPiece`；`CollectOccupiedCells` 支持 1 端点（§11） |
| `Assets/Scripts/Gameplay/PixelGroup.cs` | 加 `wallTeePrefab` / `wallCrossPrefab` 字段；新增 `RebuildWallVisuals()`（全组线段 → 一份臂掩码 → 逐墙画）；`SpawnWall` 改调新入口 |
| `Assets/Scripts/Editor/PixelColorBrushWindow.cs` | `TryValidateWallStroke` 换成规则 C（墙内线段重叠）+ A（同轴冲突）+ B（端点撞端点）；`CountPendingJunctions` 预览走同一套 `AccumulateArms` / `ClassifyMask`；`DeleteWall` / `OnUndoRedoPerformed` / `OnEnable` 补全量重建 |
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
| 3 | 一笔画出**不折返**的自交（回环走位） | 交叉格显示 T / 十字（不是角块） |
| 4 | 反向 T：新墙端点落在已有墙线段中间 | 成功，显示 T |
| 5 | 试画「端点撞端点」 | 拒绝，状态行写明「端点落在已有墙体端点上」 |
| 6 | 试画「同轴贴合」（与已有墙叠同一行） | 拒绝，状态行写明「沿已有墙体重叠 N 格」 |
| 7 | 删掉十字的一笔 | 另一笔的交叉块退回 `Edge` / `End` |
| 8 | 建墙后 Ctrl+Z | 邻居墙的 T 块退回 `End`（**§6 的回归点**） |
| 9 | 导入 `LevelData/Level_Tree3_wall.json` | 交叉点显示正确（不是角块、不叠块） |
| 10 | 缺预制体：把 `wallTeePrefab` 留空再画 T | Console 出现明确 warning + 跳过该格，不崩 |
| 11 | 运行时进关卡 | 墙的阻挡 / 暴露 / 胜负判定与改前逐字一致（交叉不该改变任何玩法） |
| 12 | 编辑模式回归：`ContainerDragWindow` 拖车、木箱封条、冰组、升降台 | 与改前一致 |
| 13 | 一笔**折返**（拖出去再原路拖回来） | 拒绝，状态行写明「墙内线段重叠」（规则 C） |
| 14 | 两面墙**相邻但不重叠**（A 的端点在 (1,1)，B 的端点在 (2,1)） | 两面各自保留自己的 `End` 块、**不被连成一条**，接缝处是两个端头 |
| 15 | 两面墙在**同一格**相交（T / 十字） | 该格显示 T / 十字块（数据层面真交叉） |
| 16 | 一笔画出 **∏ 形**（两条竖臂并排相邻，中间横杠连顶） | 两条竖臂各段显示 `Edge`、**不出现 T 块**（相邻不等于连着，见 §5.2） |

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

`DirYaw(dc, dr) = Atan2(dc, -dr)`（既有实现）⇒ 世界方向 = `(dc, -dr)`：
`+X 右 → 90°`、`−X 左 → −90°`、`+Z 前 → 0°`、`−Z 后 → 180°`。

| 缺失方向（格子） | 世界方向 | `DirYaw` | **(A) 缺口朝本地 +Z** ⇒ yaw | **(B) 主干朝本地 +Z** ⇒ yaw |
|---|---|---|---|---|
| `(−1, 0)` | −X（左） | −90° | **−90°** | **90°** |
| `(0, −1)` | +Z（前） | 0° | **0°** | **180°** |
| `(1, 0)` | +X（右） | 90° | **90°** | **−90°** |
| `(0, 1)` | −Z（后） | 180° | **180°** | **0°** |

- **A** = `yaw = DirYaw(缺失方向)`：本地 `+Z` 指向**那个空着的方向**
- **B** = `yaw = DirYaw(缺失方向) + 180°`：本地 `+Z` 指向**主干**

两种写法只差 180°，选错会让所有 T 块朝向全反。

> **决定（2026-09-29）：先按 B 实现。** 即 `WallItem.TeeYaw` 里的
> `return DirYaw(-gapC, -gapR);` —— 本地 `+Z` 指向缺口对面（主干）。
> 实测发现整体反了 180° 时，把那行的两个取负去掉（改成 `DirYaw(gapC, gapR)`）即可切成 A，一行的事。
>
> 依据：你是「顺着 `Corner` 再加一条臂」做预制体的可能性最大 —— `Corner` 的臂是本地 `+X` 与 `+Z`，
> 补上一条 `−X` 之后，yaw=0 时缺口落在本地 `−Z`（后），那正是 **B**。
>
> 核对办法：把 T 预制体拖进场景、`rotation.y` 设 0，看**哪个方向是空的**，再对照上表。

**十字预制体**按四向对称处理、`yaw` 恒为 0（`Classify` 里直接返回 0）。

### Q2：场景菜单 `WallCreator` 是否一起纳入 §7.1

- (a) 一起改：接上 `RebuildWallVisuals` + 复用规则 A/B（推荐，否则两条建墙路径口径不一致）
- (b) 保持原样不动，只动画布那条路径

> **决定：b** —— 本次不动，`WallCreator` 保持现状（仍不生成墙块、仍无重叠校验）。

### Q3：第 6 类「同轴贴合」是否保留禁止（§3 表第 6 行）

- (a) **保留禁止**（推荐）：同轴贴合不是交叉，是重复画线，删一条另一条还在
- (b) 也放行：那就要决定「删墙时点到重合格删哪一面墙」

> **决定：a** —— 保留禁止。判定见 §4 规则 A，状态行文案「沿已有墙体重叠 N 格」。

### Q4：松手前的预览是否报「将形成 N 处 T 交叉 / M 处十字交叉」

- (a) 报（推荐）：与判定同一趟扫描即可算出，零额外代价，画之前就能看出会在哪里生成交叉
- (b) 不报，保持状态行文案现状

> **决定：a** —— 已接入状态行（`PixelColorBrushWindow.CountPendingJunctions`），
> 只在「可生成 ✓」时追加，且仅当确有交叉才显示。

---

## 11. 追加：1×1 墙（单格墙）

> 2026-09-29 追加需求：「现在画布模式不支持 1×1 墙，改为支持」。

### 11.1 原先卡在四处

| # | 位置 | 原写法 | 后果 |
|---|---|---|---|
| 1 | `WallItem.CollectOccupiedCells` | 只枚举**相邻端点对** | **1 个端点 = 占 0 格** —— 墙存在，但什么都挡不住、也不出墙块 |
| 2 | 画布 `TryValidateWallStroke` | `_wallStroke.Count < 2` → 拒 | 单击直接被判「至少需要 2 格」 |
| 3 | 画布 `FinishWallStroke` | `_lastStrokeError = ok \|\| Count < 2 ? null : reason` | 单击被当成「不是想建墙」，静默丢弃、连原因都不报 |
| 4 | `LevelLoader.ApplyWalls` / `LevelDataExporter` | `points.Length < 2` → `continue` | **1×1 墙存不进 JSON、也读不回来** |

第 1 条是根因：即使绕过校验造出一面 1 点墙，它占 0 格 ⇒ 不是障碍、不生成墙块、导入导出也丢。
第 4 条最隐蔽：编辑器里看着有两面墙，一进关卡就少一面。

### 11.2 口径

- **1 个端点 = 1×1 墙**：那个端点**自己**就是它占据的唯一一格（`CollectOccupiedCells` 特判，不再走线段枚举）。
- 数据表示就是 `points = [(x,y)]`，与线段墙**同一个字段、同一套导入导出**，没有新字段、没有新类型。
  `closed` 恒为 false（闭环需要 ≥ 3 个端点，`CheckClosable` 已拦下）。
- 墙块：这一格只有它自己 ⇒ 四向邻接掩码 = 0 ⇒ `Single`（`wallSinglePrefab` 本来就是为这个形状准备的）。
  **贴着别的墙**放也是 `Single` —— 掩码只取「覆盖这一格的墙」（§5.2），相邻不重叠就是没有邻接。
  所以 1×1 墙**恒为 `Single`** —— 它没有任何线段，一个臂都不贡献给掩码。
  （即使手工改 JSON 把它和别的墙塞在同一格，那一格也只会按**别人那条穿过的线段**来画；
  1×1 墙本身依旧不贡献臂，不会造成交叉。）
- 手势：**单击 = 建一面 1×1 墙**，与拖动共用同一条「松手即创建」路径（`FinishWallStroke`）。
- 重叠口径：1 格笔画没有线段、谈不了轴向，所以在规则 A（§4）里把它视为「横 + 纵都占」——
  **只要该格已被任一已有墙占用就拒绝**。否则在已有墙身上点一下就能叠出一面同格墙
  （两墙占同一格，删一条另一条还在），正是规则 A 要拦的东西。
  与已有墙**相邻**（不同格）照常允许。

### 11.3 改动清单

| 文件 | 改动 |
|---|---|
| `WallItem.CollectOccupiedCells` | 加 `points.Count == 1` 分支：把那个端点自己计入占格（含 `Count == 0` 早退） |
| `WallItem` Gizmos | 新增 `DrawSingleCellPanel()`：1 个端点时画一个立柱方框 —— 否则 Scene 里只剩一个扁平的占格标记，看不出是墙；`OnDrawGizmos` / `OnDrawGizmosSelected` 各调一次，顺手补了 `points == null` 的守卫 |
| `PixelColorBrushWindow.TryValidateWallStroke` | `Count < 2` → `Count < 1`；1 格笔画按「横+纵都占」参与规则 A；失败文案单独一句（带格坐标） |
| `PixelColorBrushWindow.FinishWallStroke` | 1 格不再算「不是想建墙」，失败时照常把原因报进状态行 |
| `PixelColorBrushWindow` 状态行 | 操作提示补「**单击 = 1×1 墙**」 |
| `LevelLoader.ApplyWalls` | `points.Length < 2` → `< 1` |
| `LevelDataExporter`（墙体段） | `points.Count < 2` → `< 1` |
| `WallItemEditor` | 端点下限 2 → 1，文案改为「1 个端点 = 1×1 墙，2 个及以上 = 线段墙体」 |
| 场景菜单 `WallCreator` | **仍要求 ≥ 2 个选中 Pixel**（Q2 = b 保持不动 ⇒ 场景视图那条路建不了 1×1 墙） |

`wallGrid` 并集、暴露、寻路、计数、`ContainerRearranger` / `LevelGridBoard` 全部**零改动** ——
它们都只读「占格集合」，1×1 墙修好占格之后自动成立。

### 11.4 验证

| # | 操作 | 通过标准 |
|---|---|---|
| 1 | 「添加墙体」在空格上**单击** | 生成 1 格墙，出 `Single` 块，Gizmos 是立柱方框 |
| 2 | 单击**已有墙体所在的格** | 状态行报「(x,y) 已有墙体 —— 1×1 墙不能叠在已有墙体上」，不创建 |
| 3 | 1×1 墙**贴着**已有墙放 | 允许；两块接缝连续；已有墙那一格的掩码变化（`Edge` 等）符合预期 |
| 4 | 1×1 墙 + 删除墙体（两次点击） | 高亮 → 删除，不回填 Pixel |
| 5 | 导出关卡 JSON 再导入 | 1×1 墙仍在（`points` 长度 = 1），且仍是障碍 |
| 6 | 运行时进关卡 | 该格挡住像素，行为与线段墙一致 |
| 7 | 撤回：单击建墙后 Ctrl+Z | 该墙整体消失（与线段墙同一条 Undo 路径） |

---

## 12. 已知风险与取舍

| 风险 | 说明 | 处理 |
|---|---|---|
| **撤销建墙留陈旧块** | §6「唯一的真实回归点」 | `OnUndoRedoPerformed` + `OnEnable` 各补一次全量重建 |
| **每帧重建（性能陷阱）** | 把重建挂进 `RefreshSnapshot` 会与 `hierarchyChanged` 形成「重建 → 事件 → 重建」的每帧循环 | 只在 5 个显式点调；见 §5.5 |
| **交叉格的归属是任意的** | `Tee` / `Cross` 块挂在「层级顺序最先出现」的那面墙下，删那面墙会连带删掉交叉块 | 全量重建会立刻补回（归属只是「谁的孩子」，块本身与归属无关） |
| **删墙模式的二步确认在交叉格上有歧义** | `_wallCells` 单归属 ⇒ 点交叉格只会选中先到的那面墙 | 点该墙的**非交叉格**即可；状态行本来就会显示高亮的是哪面墙（:1558）。不改 `Dictionary` 结构，风险最低 |
| **预制体接缝** | 新 T / 十字预制体的臂长、厚度、切角必须与既有 `Edge` / `Corner` 同一套格子切法，否则接缝错位 | 美术侧要求，写进 §8 的「你在 Unity 里做」 |
| **包围区域跨墙** | 两面墙合围的内部区域，`ComputeInteriorCells` 今天算不对 | §7.2 的 1 行修法 |

---

## 13. 附：相关代码位置索引

> 行号是**实现前**（2026-09-29 编码前）的快照，改动后已整体漂移；按符号名搜更可靠。

| 主题 | 位置 |
|---|---|
| 墙数据与占格 | `Assets/Scripts/Gameplay/WallItem.cs:24`（points）/ `:115`（EnumerateSegment）/ `:131`（CollectOccupiedCells） |
| 墙块分类与 yaw | `WallItem.Classify`（原 `ClassifyPiece`，改为吃四向掩码）/ `TeeYaw`（新增）/ `DirYaw` / `CornerYaw` |
| 墙块生成 | `WallItem.ClearPieces` + `WallItem.SpawnPiece`（原 `BuildVisual` 拆开，分类交给调用方） |
| 整组重建 | `PixelGroup.RebuildWallVisuals`（新增） |
| 预制体字段 | `PixelGroup.cs:51-60` |
| 墙的占格表 | `PixelGroup.cs:189`（RebuildGrid）/ `:338`（IsWall）/ `:1205`（ClearWalls）/ `:1375`（SpawnWall） |
| 画布：墙模式 | `PixelColorBrushWindow.cs:93`（Mode.AddWall）/ `:147`（墙状态）/ `:2280`（AppendWallCell）/ `:2336`（SimplifyPoints）/ `:2387`（TryValidateWallStroke）/ `:2431`（CreateWallFromStroke）/ `:2510`（DeleteWall） |
| 画布：格 → 墙表 | `PixelColorBrushWindow.cs:169` / `:696`（RebuildWallCellMap） |
| 画布：快照与重建钩子 | `PixelColorBrushWindow.cs:424`（OnHierarchyChanged）/ `:435`（SyncWithSceneIfDirty）/ `:454`（OnUndoRedoPerformed）/ `:523`（RefreshSnapshot） |
| Inspector 与创建菜单 | `WallItemEditor.cs:66`（CloseLoop）/ `:134`（CancelLoop）/ `:170`（ComputeInteriorCells）/ `:248`（WallCreator） |
| 关卡导入导出 | `LevelData.cs:73`（WallData）/ `LevelLoader.cs:150`（ApplyWalls）/ `LevelLoader.cs:88`（ApplyPixel 里的墙格 skipCells） |
