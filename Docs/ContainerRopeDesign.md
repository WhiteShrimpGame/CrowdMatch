# CrowdMatch「相邻列绳子连接」设计文档

> 状态：**已实现（代码完成，待 Unity 内实测）**。本文描述「相邻列的车用绳子连接、必须全部匹配完毕才可同时移出」的完整设计。
> 已植入 unity-rope skill（`Rope.cs` / `UltimateRope.cs` / `UltimateRopeLink.cs` + 材质贴图），
> 并改动 `ContainerGroup.cs`、`ContainerItem.cs`、`LevelData.cs`、`LevelLoader.cs`、`LevelDataExporter.cs`、
> `ContainerItemEditor.cs`、`ContainerRearranger.cs`，新增 `ContainerRopeLink.cs`。
>
> **仍需你在 Unity 里做的三步见 §10.1**（建 `Rope` 层、预制体挂两个端点、指定绳子材质）。

---

## 1. 需求确认

| 你的原话 | 设计落点 |
|---|---|
| 为 ContainerItem 左右绑定两个点，作为绳子的端点 | 预制体上新增两个空物体 `ropeAnchorLeft` / `ropeAnchorRight`（§2） |
| 左侧车的右端点与右侧车的左端点通过绳子链接 | 一组 N 车按列序成链，共 N-1 条绳（§3.2） |
| 非运行状态下可选中 N 个车，点按钮标记为连接 | `ContainerItemEditor` 新增一节（§4） |
| 至少选 2 个；同列只能选 1 个；必须处在相邻的 N 列 | 校验规则表（§4.2） |
| 或取消连接（选中任意一个被连接的车，取消整组） | §4.3 |
| 运行状态下绳子长度始终保持和端点实际距离完全一致 | 无物理「绷直驱动」，每帧铺骨骼（§5.2，核心） |
| 当被链接的部分车完成匹配时，留在前排 | 组未齐时满车不出库、占住前排、该列不补位（§5.4） |
| 全部匹配同时出车 | 全组一起满足出库条件，但**按列序依次延迟**出库（最左先出）：头车 → 第一个后车、后车 → 后车用**两段独立配置的间隔**；且头车走正常出车、被绳子连接的后车走「跳过倒车」的变体（§5.4） |
| 绳子需要支持关卡 JSON 的导入导出 | `ContainerItemData.ropeGroupId`（§3.1、§3.3） |
| 运行模式下洗牌激活时，所有绳子失效 | 标记时强制关洗牌 + 运行时兜底忽略（§4.4、§5.5） |
| 按 Record 重排时，清除所有绳子信息 | 重排路径天然丢弃，另加显式兜底（§3.3） |
| 一组可以跨排；同一列也可以出现两个不同绳组的车 | 允许（§3.2、§10） |
| 但两组的绳子不能交叉出现（会死锁） | 编辑器交叉校验（§4.2） |
| 一辆车不能属于多个绳组 | `ropeGroupId` 为单值 + 标记要求选中车未连接，天然保证（§4.2） |

### 已确认的三条（本次问答）

| 问题 | 你的选择 | 影响 |
|---|---|---|
| 组内车是否必须同排 | **允许不同排** | 绳子可以斜向，补位时端点距离**连续变化** → 每帧同步绳长是硬需求（§5.2） |
| 洗牌与绳子的关系 | **标记时强制关闭洗牌** | 标记成功后 `shuffleContainers = false`（带 Undo），运行时不再需要判断 |
| 同组同时出库时绳子怎么处理 | **跟着车出库** | 出库期间持续同步绳长，车销毁时销毁绳子（§6） |

---

## 2. 前置条件与植入

### 2.1 植入内容与手工步骤

| 位置 | 要求 | 状态 |
|---|---|---|
| 脚本 | 植入 `unity-rope` 的三个 `.cs`（三者同程序集、均无命名空间） | **已完成**（已校验字节一致） |
| 资源 | `Rope.mat` / `Rope_d.mat` + 两张贴图，**必须连同 `.meta` 一起复制** | **已完成**（无 GUID 冲突） |
| 层 | 新增名为 `Rope` 的层 | **待你做**（`TagManager.asset` 用户层全空，只有内置层 + `Click`） |
| 预制体 | ContainerItem 预制体新增两个空物体作为端点（§2.3） | **待你做** |
| 材质引用 | ContainerGroup 的 `Rope Material` 字段指定 `Rope.mat` | **待你做**（§10.1） |

### 2.2 植入路径与依赖检查

| 项 | 实际目标 |
|---|---|
| `Rope.cs`、`UltimateRope.cs`、`UltimateRopeLink.cs` | `Assets/Scripts/Rope/`（本工程无 asmdef，落 Assembly-CSharp，游戏代码可直接引用） |
| `Rope.mat`、`Rope_d.mat` | `Assets/CrowdMatch/Materials/`（含 `.meta`） |
| `rope_diffuseheight.tga`、`rope_normal.png` | `Assets/CrowdMatch/Texture/`（含 `.meta`） |

> 资源没有按 skill 默认的 `Assets/Art/...` 放，而是跟随本工程既有的美术目录 `Assets/CrowdMatch/Materials|Texture`；也刻意避开 `Assets/Resources`（会被打进构建资源）。

- **Toony Colors Pro 2 已安装**（`Assets/Plugins/JMO Assets/Toony Colors Pro`），附带的 `Rope.mat` shader 能正常解析，不会走兜底材质。
- **`Rope` 层必须新建**：UltimateRope 会把该层赋给绳根与每个绳节；层不存在时 `Rope.cs` 回退到 `Default` 并打 warning，绳节会与像素/车互撞——这是不可接受的。
- `Rope` 层建议在 Physics 碰撞矩阵中与所有层取消勾选（绳子纯视觉，不参与碰撞）。

> `UltimateRope.cs` **保持字节级原样**；所有封装与驱动逻辑都在我们自己的文件里（§5.2 说明了为什么必须这样）。

### 2.3 ContainerItem 预制体新增端点

| 节点 | 要求 |
|---|---|
| `ropeAnchorLeft` | 车体**左侧**外沿、绳子挂点高度（建议车体中心高度） |
| `ropeAnchorRight` | 车体**右侧**外沿、与左侧同一高度 |

- 两者都作为车体根节点（`ContainerItem` 所在物体）的**子物体**，这样出库动画的位移/旋转/缩放会自然带着端点走。
- 端点**不要**挂 Rigidbody（UltimateRope 的 `CreateRopeJoints` 会自己加一个运动学刚体，我们的驱动会把它清掉，见 §5.3）。
- 端点不要放在 `elasticScaleAxle` 之下——否则上车弹性缩放会把端点位置一起缩放，绳子会跟着抽动。放在车体根下最稳。

---

## 3. 数据模型

### 3.1 序列化字段

`ContainerItemData`（`LevelData.cs:51`）新增一个字段：

```csharp
public int ropeGroupId;   // 0 = 未连接；同 id 的车成组（旧 JSON 无此字段 → 默认 0）
```

**为什么放在「每个车」而不是「关卡级的一组关系」**：
- 容器是稀疏平铺列表（`ContainerData.items[]`，每项自带 `x`/`y`），组关系用「同 id」表达最省管线；
- 洗牌只会改写每项的 `x`/`y`（`LevelLoader.cs:267 ShuffleContainers` 把位置对洗牌后重新分配），不会重排 `items` 数组——但洗牌与绳子互斥（§4.4），所以不构成风险；
- 重排工具 `ContainerRearranger.Distribute` 是**新建** `ContainerItemData`（`ContainerRearranger.cs:306-333`），不带这个字段 → 绳子信息天然被清掉，正好满足「按 Record 重排时清除绳子」。

运行时镜像：`ContainerItem.ropeGroupId`（`public`，序列化），以及两个端点引用（§2.3）。

### 3.2 成组语义

| 规则 | 说明 |
|---|---|
| 分组 | `ropeGroupId` 相同且非 0 的车属于同组 |
| 列约束 | 同组车分处 **N 个相邻列**，每列恰好 1 个（由编辑器标记时保证） |
| 排序 | 组内按 `gridX` **升序**成链 |
| 绳子数量 | N 车 → **N-1** 条绳：左车的 `ropeAnchorRight` ↔ 右车的 `ropeAnchorLeft` |
| 排约束 | 无——同组车可以在不同 `gridZ`，绳子因此可能是斜的 |
| 车的同一性 | 绳连接的是**车实例**。车在列内补位前移时绳子跟着走，不会因为换排而断开 |

### 3.3 JSON 往返：所有需要改的地方

| 文件 | 位置 | 改动 |
|---|---|---|
| `LevelData.cs` | `ContainerItemData:51-60` | 加 `ropeGroupId` |
| `LevelDataExporter.cs` | `BuildLevelData` 容器循环 `:255-262` | 导出时写入 `item.ropeGroupId` |
| `ContainerGroup.cs` | `SpawnContainer:457` | 加 `int ropeGroupId = 0` 形参并赋值 |
| `LevelLoader.cs` | `ApplyContainer` 内 `:256` 的调用 | 传 `it.ropeGroupId` |
| `ContainerGroupEditor.cs` | 「生成 Containers」按钮 | 新生成的车 `ropeGroupId` 保持 0 |
| `ContainerRearranger.cs` | `Rearrange:199-203` | `Distribute` 已天然丢弃；**另加显式清空 + 注释**，防日后改成保留式实现时漏掉 |
| `LevelColorReplacer.cs` | 只改 `colorId` | 不受影响（顺手确认，无需改） |
| `LevelDataCache` | JSON 克隆快照 | 自动带上，无需改 |

**兼容性**：`LevelData.version` 从未被检查，全靠 `JsonUtility` 的「缺字段取默认值」语义。旧 JSON 没有 `ropeGroupId` → 反序列化为 `0` → 无绳子。与 `question` 字段的既有做法一致。

---

## 4. 编辑器标记

### 4.1 入口

在 `ContainerItemEditor.OnInspectorGUI` 的「交换颜色」一节之后，新增一节 **「绳子连接（仅非运行模式）」**，含两个按钮：

| 按钮 | 作用 |
|---|---|
| 标记选中车为连接 | 给选中车分配同一个新组 id；并强制关闭 `ContainerGroup.shuffleContainers` |
| 取消选中车的连接 | 把选中车所属的**整组**组 id 清零 |

沿用本文件已有的模式：`SelectedCars()`（`:63-72`，来自 `targets`，因为类上标了 `[CanEditMultipleObjects]`）、`ValidateSelection` 风格（返回 `null` = 通过，否则中文错误串）、`EditorGUI.DisabledScope(Application.isPlaying)`、逐对象 `Undo.RecordObject` + `EditorUtility.SetDirty`，以及现成的 `MarkShuffleOff`（`:197-210`）。

### 4.2 标记的校验规则

| 规则 | 不满足时的提示 |
|---|---|
| 选中数 ≥ 2 | 请选中至少 2 辆车 |
| 全部属于同一个 `ContainerGroup` | 选中的车不属于同一个 ContainerGroup |
| 同列只能选 1 个（`gridX` 互不相同） | 同一列只能选 1 个车：第 c 列选了多个 |
| 必须处在相邻的 N 列（`max(gridX) - min(gridX) == 数量 - 1`） | 选中的车必须处在相邻的 N 列：缺少第 c 列 |
| 组内每辆车都**尚未连接**（`ropeGroupId == 0`） | 某某车已属于绳组 N，请先取消它的连接 |
| 两端点都已配置（`ropeAnchorLeft` / `ropeAnchorRight` 非空） | 某某车未配置 ropeAnchorLeft / ropeAnchorRight（预制体上需有两个端点空物体） |
| **与任何已有绳组不交叉** | 与已有绳组 N 在第 c–c+1 列之间交叉，两组会互相等待造成死锁 |

> **「一辆车不能属于多个绳组」由第 5 条自动保证**：`ropeGroupId` 只有一个 int，选中车必须未连接，因此一个车只会被赋予一个新组 id。

> **交叉才是真正的死锁**。两个绳组若跨**同一对相邻列**且行序相反（X 形），就会互相等待。
> 例子：两列 col1 / col2，A = {(col1,row0), (col2,row2)}、B = {(col1,row2), (col2,row0)}。
> 于是 A 在 col1 的满车要等 A 在 col2 的车前移，而 col2 前移必须等 col2 的前排车（= B 的车）出库；
> B 在 col2 的满车又要等 B 在 col1 的车前移，而 col1 前移必须等 col1 的前排车（= A 的车）出库 → 闭环。
> 校验就按这个定义做：对每对相邻列，若两组都跨了它，则两组在该列对上的「行值差」必须同号。
> 只共享**一列**的两组（例如 A 跨 0–1、B 跨 1–2）不会交叉——它俩只在该列上形成单向的「后车等前车」，没有第二条边可成环。

> **不把 `maxOpenRows` 作为拦截条件**。一开始以为「超出 maxOpenRows 的车永远收不到像素 → 全组卡死」是错的：`IsOpen` 确实要求 `row < maxOpenRows`，但该车会随着它所在列不断出库补位而**逐排前移**，最终进入可开启范围。所以深排绳组车只是「要等更久」，不是死锁。

### 4.3 取消连接

- 选中任意 1 个（或多于 1 个但**属于同一组**）被连接的车即可；
- 选中多个不同组时提示「选中的车分属多个绳组，请一次只取消一组」；
- 清零范围是**整组**（遍历该 `ContainerGroup` 下所有 `ropeGroupId == id` 的车），不是只清选中的那几个。

### 4.4 强制关闭洗牌

标记成功后立即调用已有的 `MarkShuffleOff(group)`：`Undo.RecordObject(group)` → `shuffleContainers = false` → `SetDirty`。
副作用是导出时 `lockContainer = !shuffleContainers = true`，关卡被标记为「不洗牌」，与绳子互斥的关系在数据层面就锁死了。

### 4.5 组 id 分配

标记时扫一遍同组下所有车的 `ropeGroupId`，取 `max + 1` 作为新 id。id 只需在单个关卡内唯一。

### 4.6 编辑器可视化（Gizmos）

非运行模式下光看 Inspector 的数字看不出谁连着谁，所以 `ContainerGroup.OnDrawGizmos` 把绳连画出来：

| 画什么 | 怎么画 |
|---|---|
| 绳 | 同一个 `ropeGroupId` 的车按列升序串成链，相邻两车之间画一条线 |
| 端点 | 线的两端取 `ropeAnchorRight` / `ropeAnchorLeft`——**就是运行时真正建绳的那两点**，所以看到的就是运行时绳子的位置与走向；两端各画一个小球 |
| 整体抬升 | 线和小球统一加 `ropeGizmoYOffset`（默认 1 米）——端点就在车体侧面，不抬会被车完全挡住 |
| 漏配端点 | 某辆车没配端点 → 该端画成**亮红小球**并落在车体位置 |
| 运行时不会建绳 | 洗牌开着或 `ropeEnabled = false` → 用**同色低透明度**（alpha × 0.35）画，一眼能分辨 |

可调参数（都在 `ContainerGroup` 上，归在「绳连 Gizmos」分组）：

| 字段 | 默认 | 作用 |
|---|---|---|
| `ropeGizmoYOffset` | 1 | 整条预览的 Y 偏移（米）——小球被车挡住的解法就是这个 |
| `ropeGizmoColor` | 亮青 `(0.1, 1, 1)` | 预览颜色；「运行时不会建绳」那档自动取它的 35% 透明度 |
| `ropeGizmoAnchorRadius` | 0.1 | 端点小球半径（米）——**小球半径就在这里调** |

三点说明：

- **线宽恒为 1px**：用的是 `Gizmos.DrawLine`，这个 API **没有宽度参数**，加不了粗。曾改用编辑器专用的
  `Handles.DrawAAPolyLine`（可指定宽度）实现过一版，但那样必须把整段预览代码塞进 `#if UNITY_EDITOR`、并在文件顶部
  条件编译引入 `UnityEditor`，为了线宽付这个复杂度不值当，**已放弃、换回 `DrawLine`**。
- **不再需要条件编译**：换回 `DrawLine` 后不依赖任何编辑器 API，所以这几个字段和这个方法都是普通运行时成员，
  文件顶部也不用再条件编译引入 `UnityEditor`。
- **运行时不画**：那时绳子是真渲染出来的，再叠一层 Gizmos 只会糊。用的是 `OnDrawGizmos`（不是 `OnDrawGizmosSelected`），
  所以不用选中就能看到全场景的绳连；想临时藏起来就在 Scene 视图的 Gizmos 下拉里关掉这个组件。

---

## 5. 运行时

### 5.1 建绳时机与对象结构

关卡应用完成后（`LevelLoader.Apply` 之后，`GameController.InitLevel` 内）建绳：

```
ContainerGroup (无缩放)
├── Container_0_0  (ContainerItem)      ← 车，子物体含 ropeAnchorLeft / ropeAnchorRight
├── Container_1_1  (ContainerItem)
└── Rope_0          (Rope + UltimateRope + ContainerRopeLink)
      └── Link0 … Link{N-1}              ← UltimateRope 生成的骨骼
```

- 每个相邻对生成一个 `Rope` GameObject，父物体挂在 `ContainerGroup` 下（与车同级）；
- **要求 `ContainerGroup` 无缩放**：骨骼按世界坐标摆放，父级若带缩放会失真（车的位置本来就是 `GetLocalPosition` 直接取值，隐含了同一前提）；
- 参数：`length = 0`（自动取两端点当前距离）、`buildOnStart = false`（由我们显式 `Build()`）、材质/直径/骨骼数来自 `ContainerGroup` 上的可调字段（§7）。

### 5.2 绳长同步：无物理「绷直驱动」（核心决策）

**需求**：绳长在任何时刻都**恰好等于**两端点的实际距离。

#### 为什么不能用现成 API

| 方案 | 为什么不行 |
|---|---|
| `Rope.SetLength(d)` | 内部走 `Regenerate()` → `DeleteRope()` + 重建全部骨骼、重建 SkinnedMesh、重建关节。补位期间距离**每帧**都在变，逐帧全量重建不可接受（GC 尖峰 + 视觉跳变） |
| `Rope.ExtendBy(δ)` | 是「预先折叠的绳节逐段展开」机制（`UltimateRope.cs:3442 ExtendRopeLinear`），有 `ExtensibleLength` 预算、是**动画式**推进、超出预算静默截断。用于「展开线圈」而非「任意时刻精确等于距离」 |
| 交给物理（关节） | 两端点都是代码驱动的运动学物体，关节会让绳节抖动/滞后；绳节还带质量与重力，松一点就下垂。且 UltimateRope 自己会报错：`fNodeDistance > fLength` 时打印 *"Segment length should be larger than the distance ... or this segment will have buggy physics"*（`UltimateRope.cs:3143`）——**绳长一旦小于端点距离，绳子就废了** |

#### 采用方案

UltimateRope 只当**网格/骨骼生成器**用，运行时的绳形由我们自己每帧摆骨骼：

```
node = rope.RopeComponent.RopeNodes[0]        // Rope.RopeComponent 是 skill 明确提供的逃生口
a    = leftCar.ropeAnchorRight.position
b    = rightCar.ropeAnchorLeft.position

for i in 0 .. node.segmentLinks.Length - 1:
    t = (i) / (Count - 1)
    node.segmentLinks[i].transform.position = Lerp(a, b, t)
    node.segmentLinks[i].transform.rotation = LookRotation((b - a).normalized)
    node.segmentLinks[i].transform.localScale = 原样（不动）

node.fLength = (b - a).magnitude            // 让内部状态与实际一致
```

- 直接写**世界坐标**——这正是物理本来的做法（`CreateRopeJoints` 会先把骨骼的现场世界位姿快照下来，再把它们重置成规范姿态交给关节，`UltimateRope.cs:3192-3196`），蒙皮网格因此能正确跟随；
- 结果：**绳长恒等于端点距离**，零抖动、零下垂、无重建、无 GC。

> **骨骼首尾的取点映射待实测**：UltimateRope 的规范姿态里最后一个骨骼落在一格之内的位置（`fSegmentLength = fLinkLength × (Count-1)`，`UltimateRope.cs:3195`），与网格边缘的对应关系需要对着实际资源看一眼。若绳端与挂点有可见偏差，按下面任一映射改用即可，**属调参不属重构**：

| 映射 | t |
|---|---|
| 均匀铺满两端（默认候选） | `i / (Count - 1)` |
| 首尾各让半格 | `(i + 0.5) / Count` |
| 首尾各让一格（贴 `Rope.cs` 关节链的 N+1 约束） | `(i + 1) / (Count + 1)` |

#### 为什么把驱动放在项目侧而不是 `Rope.cs`

- skill 明确把 `RopeComponent` / `IsBuilt` 作为**给人用的逃生口**提供，正是为了这种情况；
- `Rope.cs` 保持与 skill 副本逐行可比，日后 skill 升级可直接覆盖；驱动逻辑混进去就得走三方合并；
- 「锚点是车端点」这件事是游戏语义，不该进通用组件。

### 5.3 建绳后的物理清理

`Rope.Build()` 在 Play 模式下会经 `CreateRopeJoints()` 做两件我们不要的事（`UltimateRope.cs:3040`）：

1. 给**两个锚点物体**各加一个运动学 `Rigidbody`（skill 文档已声明的副作用）；
2. 给绳节建 `ConfigurableJoint` 链。

驱动方案要求骨骼是**纯 Transform**，因此 `Build()` 之后立刻：

| 动作 | 原因 |
|---|---|
| 每个 `segmentLinks[i]` 的 `Rigidbody.isKinematic = true` | 让刚体不参与积分；关节随之失效 |
| 销毁骨骼上的 `ConfigurableJoint` | 清理无效开销 |
| 销毁两个锚点上的 `Rigidbody` | 把 UltimateRope 的副作用从车预制体上抹掉 |

> `CreateRopeJoints` 只在 `Application.isPlaying` 下执行（`UltimateRope.cs:1649`），所以编辑器里生成的绳子本来就没有关节，这套清理只在运行时生效。

### 5.4 出库规则

改动集中在 `ContainerGroup.TryExitIfAtFront`（`:333-342`）。

现状：车在前排（`grid[col,0] == item`）且 `IsEmpty` → 立刻 `StartContainerExit`。

新规则：

```
若 item.ropeGroupId == 0          → 保持现状（无条件出库）
否则：
    组就绪 = 组内每辆车都满足 IsEmpty && grid[car.gridX, 0] == car
    就绪   → 链首（列最小 = 最左）立即出库（正常出车）；
             第一个后车再等 ropeExitHeadGap，其余后车之间各等 ropeExitStagger；
             后车一律走「跳过倒车、直接切前轴」的变体
    未就绪 → 直接 return（该满车留在前排，不补位、不出库）
```

行为推演：

| 情况 | 结果 |
|---|---|
| 组内车满但不在前排（`gridZ > 0`） | 它不满足就绪条件。它所在列的前车照常出库 → 它被 `RefillColumn` 推到前排，然后**停在前排等待**（该列不再补位，因为它占着 `grid[col,0]`） |
| 组内部分车满、其余未满 | 满的那辆停在前排等；未满的照常收像素。玩家看到的正是「已匹配的先排着队等」 |
| 全组都满且都在前排 | `TryExitIfAtFront` 被最后一次触发时启动出库协程：链首（最左）立即出库，第一个后车在 `ropeExitHeadGap` 之后、其余后车之间各间隔 `ropeExitStagger`。**不是同一帧**——前面的车先走，把后面的车依次拽出去 |
| 全组出库完成 | 每辆车的 `ContainerExitDriver` 各自回调 `RefillColumn(自己的列)`。因为出库是错峰的，补位也**依次**发生（`onRefill` 在「转正瞬间」触发，所以补位比出库启动还要早一点） |

**必须同时处理的既有路径**：

| 路径 | 位置 | 问题 | 处理 |
|---|---|---|---|
| 深排原地销毁 | `OnPixelConsumed(..., destroyInPlace: true)` → `DestroyContainerInPlace`（`:349`） | 绳组车在深排装满会被**原地销毁**，绳子锚点直接消失 | 绳组车**禁止**走这条路：改为正常补位前进 |
| 复活匹配 | `Revive` → `MatchPixelsToCars` | 同上，可能把像素塞进绳组车或触发原地销毁 | 绳组车仍可上车；但销毁路径同样要绕开 |

### 5.5 洗牌时绳子失效

双保险：

1. **编辑器**：标记即强制 `shuffleContainers = false`（§4.4），导出的 JSON 因此是 `lockContainer = true`；
2. **运行时兜底**：`InitLevel` 里若 `data.container.lockContainer == false`（即洗牌开启，例如手改过的 JSON），**完全不建绳**，绳组不生效、车各自独立出库。对应你说的「洗牌激活时所有绳子失效」。

### 5.6 清理

| 时机 | 动作 |
|---|---|
| 车被销毁（出库完成） | 与该车相关的绳根销毁（§6） |
| 关卡重载 | `ContainerGroup.ClearContainers`（`:439`）一并销毁所有绳根 |
| 按 Record 重排 | 绳信息在重排输出里不存在（§3.3），无需运行时处理 |
| Record 模式本身 | 像素点击即销毁、车永远装不满 → 绳组永远不会出库，绳子只是挂在那里不动。无特殊处理 |

---

## 6. 出库与绳子的表现

选择的是「跟着车出库」：出库期间绳长**持续同步**，车销毁时销毁绳子。

| 阶段 | 表现 |
|---|---|
| 全组就绪 | 链首（最左）立即 `ContainerExitDriver.Play(onRefill, ropeRearExit: false)` 走**正常出车**；后车按两段间隔依次 `Play(onRefill, ropeRearExit: true)` 走**跳过倒车的变体**（见 §6） |
| 后车的出车方式 | **跳过倒车、直接切前轴**，从正姿（0°）先甩到 `ropeExitMaxAngle` 再归 0 出车；运动参数整组独立（`ContainerExitDriver` 的 Rope Rear Exit 那一组）。详见 `Docs/ContainerExitDesign.md` §6.8 |
| 延迟窗口内 | 左边那辆已经开走、右边那辆还停在原地 → 绳子被拉长，但 tiling 补偿让它表现为「绳长被不断放出」而不是拉稀；车一离开销毁，对应那条绳也消失 |
| 驶出途中 | 车被挂到各自的轴（前轴/后轴/侧翻/弹性）上，端点作为车体子物随车一起走 → 绳子被「拽」着走 |
| 车销毁 | `ContainerExitDriver.Run` 末尾 `Destroy(gameObject)`（`:332`）。绳根由 `ContainerRopeLink` 每帧检测「任一锚点已销毁」→ 自行销毁 |

**已知视觉点**：出库动画含侧翻自转（`rollAxle`）与弹性缩放（`elasticScaleAxle`）。端点挂在车体根下，因此会跟着转——绳端会绕车体摆动，看起来像被甩了一下。**已确认接受**这个甩动（物理上也说得通），不做冻结处理。

---

## 7. 调参一览

挂在 `ContainerGroup` 上的绳配置（新增字段）：

| 参数 | 默认 | 作用 |
|---|---|---|
| `ropeMaterial` | `Rope.mat` | 绳子材质 |
| `ropeLinkCount` | 8 | 每条绳的骨骼数。越大越顺滑，代价是蒙皮开销 |
| `ropeDiameter` | 车距的合理比例（建议先按实际间距试 0.1~0.2） | 绳子粗细 |
| `ropeLayerName` | `"Rope"` | 绳节层 |
| `ropeEnabled` | true | 总开关（调试用：关掉可先只验证出库逻辑） |
| `ropeExitHeadGap` | 0.2 | 出库间隔（秒）：**头车 → 第一个后车** |
| `ropeExitStagger` | 0.2 | 出库间隔（秒）：**后车之间**（第 2 辆起）。与上一行独立配置；两段都为 0 = 全组同一帧 |

### 7.1 纹理密度补偿（「从一端放出」的观感）

端点距离变化时几何被等比拉伸，但**贴图不会跟着被拉稀**——每帧把贴图 tiling 的 U 分量乘 `k = 当前距离 / 建绳距离`，让贴图沿绳长保持固定的世界密度。观感就是「绳子从一端被不断放出」，而不是「绳子被拉长变稀」。

这条成立的依据是 UltimateRope 自己的 UV 约定（`UltimateRope.cs:1544`）：

```
u = fRopeT × TotalRopeLength × RopeTextureTileMeters     // RopeTextureTileMeters 默认 1.0 = 每米一次平铺
fRopeT = nLink / TotalLinks                             // u = 0 落在骨骼链起点 = 左车的右端点
```

- u 本身已经与世界距离成正比（所以 UltimateRope 本来就是按「每米」布贴图的）；
- 几何拉长 k 倍后，单位世界长度摊到的 u 掉到 1/k → 密度掉到 1/k → 乘 k 精确还原；
- tiling 是**绕 u = 0** 缩放 UV 的，所以贴图钉在起点端、向另一端生长，视觉上就是「从出口放出绳长」；
- **与骨骼数量无关**：蒙皮 UV 是顶点属性、不随几何变形改变，所以对任意拉伸倍数都成立，**没有长度上限**。

实现要点（都在 `ContainerRopeLink` 里）：

| 点 | 说明 |
|---|---|
| 写在哪 | `renderer.materials`（返回实例）——用 `sharedMaterial` 会改到共享的 `Rope.mat` |
| 基线 | 建绳时缓存「各槽位原始 tiling」+「建绳距离」；每帧写 `原始 tiling × k`，不累乘 |
| 槽位 | `_BaseMap`（本工程实际采样的槽，基线 tiling 1）、`_MainTex`（兜底材质用）、`_BumpMap`（基线 3，一起乘 k 以保持它相对反照率的重复倍率） |
| 退化 | 两端点几乎重合时跳过补偿 |

> 顺带确认过两个「会不会限制长度」的点：`Regenerate()` 里设了 `skin.updateWhenOffscreen = true`（`UltimateRope.cs:1642`），包围盒每帧按变形后的顶点重算 → 拉多长都不会被视锥剔除；`LinkJointBreakForce` 默认 `Mathf.Infinity` → `FixedUpdate` 里那段逐帧扫三角形、处理断绳的代码**整段跳过**，没有隐藏开销。再加上绳子是直线（纵向分段数不影响观感，只影响弯曲平滑度）与「骨骼径向缩放保持 1、管子不会变细」（§7.2 只动 z），**长度没有上限**。

### 7.2 末端漏一段的修正（骨节长度缩放）

**症状**：绳子渲染出的末端与终点 anchor 之间差一段距离，且「有时候」才出现。

**根因**：`LinkJointBreakForce = Infinity` 使得网格走的是**非 breakable** 分支（`UltimateRope.cs:1488` 的 `nVertices` 三目），那是一根**连续管体**：所有环的 z 偏移都是 0（环就贴在骨骼上），**只有最后一个环**带有固定偏移 `LinkLengths[n-1] = fLength / nNumLinks`。而这个值是按**建绳当时**的 `fLength` 烘焙进顶点的（`UltimateRope.cs:1468`、`:1591`），此后不再变。

于是管体末端恒为 `bone[n-1] + 烘焙骨长`。我把 `bone[n-1]` 摆在 `(n-1)/n × 当前距离` 处，要正好落在终点就需要最后一节等于 `当前距离 / n`；但烘焙值是 `建绳距离 / n`。两者只在「当前距离 == 建绳距离」时相等，**差值就是漏掉的距离**：

```
末端偏差 = (建绳距离 − 当前距离) / n
```

建绳那一刻为 0，之后随补位改变相对位置而出现——所以是「有时候」。偏差量级 ≈ 距离变化量 / 骨节数，8 节、距离变化 1 个单位就是 0.125。

**修法**：把骨骼的 `localScale.z` 乘 `k = 当前距离 / 建绳距离`。骨骼的 +Z 就是绳长方向（`ApplyTaut` 里 `rotation = LookRotation(delta)`），而顶点里的那个 z 偏移会随骨骼缩放一起放大（蒙皮的 `v_world = N · v_local`，N 是当前骨骼矩阵），于是最后一节从 `Lb` 变成 `k·Lb = 当前距离 / n`，末端精确落回锚点。径向由 x/y 决定、保持 1，管子不会变粗变细。

- 所有骨骼都乘 k 是无害的：除末环外其余环的 z 偏移为 0，缩放不移动它们；
- 对 breakable 网格同样正确——那种网格每节都是「绘制骨长 = 节距」，乘 k 后依然首尾相接；
- 近端本来就是准的（`bone[0]` 落在起点锚点，偏移为 0），所以只有远端需要它。

---

## 8. 边界情况

| 场景 | 处理 |
|---|---|
| 旧 JSON 无 `ropeGroupId` | 反序列化为 0，无绳子 |
| 洗牌开启但 JSON 里带绳组 | 运行时完全不建绳（§5.5 兜底） |
| 组内车在 `maxOpenRows` 之外 | **不是错误**：它会随所在列不断出库补位而逐排前移，最终进入可开启范围，只是要等更久（§4.2） |
| 两个绳组交叉（跨同一对相邻列且行序相反） | 会互相等待 → 死锁。编辑器标记时按「行值差同号」拦住（§4.2） |
| 绳子孤锚点（一端车已销毁） | `ContainerRopeLink` 自行销毁绳根 |
| 当前距离 ≠ 建绳距离 | 末端会漏一段 —— 由骨节长度缩放修正（§7.2）；配合 tiling 补偿，观感是「从起点端放出绳长」 |
| `Rope` 层未创建 | `Rope.cs` 回退 `Default` 并打 warning；绳节会与像素互撞 → **必须在植入时建好该层** |
| 端点未在预制体上配置 | 该组不建绳并打 warning；不影响出库（等价无绳组） |
| 组内车被深排原地销毁路径命中 | 禁止走该路径，改为正常补位（§5.4） |
| 组内车数 = 2 且分处相邻两列 | N-1 = 1 条绳，正常 |
| 关卡重载 | 绳根随 `ClearContainers` 一并销毁 |
| Record 模式 | 车永不满 → 绳组不出库，无特殊处理 |
| `ContainerGroup` 带缩放 | 骨骼世界坐标失真 → 要求无缩放（§5.1）；若工程里确实带缩放，改为把绳根挂到 ContainerGroup 的父级 |

---

## 9. 决策记录

1. **绳长同步用「每帧摆骨骼」而不是 `SetLength`**：`SetLength` 会全量重建骨骼与蒙皮网格，补位期间逐帧重建不可接受（§5.2）。
2. **不用 `ExtendBy`**：它是「展开预折叠绳节」的动画式机制，有预算上限，语义是「放长绳子」而非「恒等于距离」（§5.2）。
3. **不用物理关节**：两端点是代码驱动的运动学物体，物理只会带来抖动/下垂；且 UltimateRope 自身要求 `fLength ≥ 端点距离`，物理方案需要持续重建才能维持（§5.2）。
4. **驱动逻辑放项目侧**（`ContainerRopeLink`），用 skill 提供的 `RopeComponent` 逃生口，保持 `Rope.cs` 与 skill 副本可比、可覆盖升级（§5.2）。
5. **`UltimateRope.cs` 一字不改**（skill 的硬约束）；`Rope.cs` 也不改。
6. **建绳后清理刚体与关节**：把绳节降级为纯 Transform，并把 UltimateRope 加在车预制体端点上的刚体删掉（§5.3）。
7. **组关系用 per-item `ropeGroupId`** 而不是关卡级关系列表：管线最短，且重排工具天然丢弃（§3.1）。
8. **链式相邻两两成绳**（N 车 N-1 绳），而不是「一组一根总绳」：端点定义就是「左车的右端点 / 右车的左端点」，天然是两两配对（§3.2）。
9. **标记即强制关洗牌**：让「绳子存在」与「洗牌开启」在数据层面互斥，运行时不必处理洗牌打乱绳组的中间态（§4.4）。
10. **满车「留在前排」靠占位实现**：满车不出库 → 占住 `grid[col,0]` → 该列不补位。不新增「冻结列」之类的状态（§5.4）。
11. **禁止绳组车走深排原地销毁路径**：该路径会直接销毁车，锚点随之消失（§5.4）。
12. **交叉校验放在编辑器**：两组交叉会让出库条件成环 → 死锁，这是会让关卡卡死的数据错误，必须在编辑期拦（§4.2）。
13. **不拦 `maxOpenRows`**：一度以为「超出可开启排数的车永远收不到像素」会死锁，实际它会随补位逐排前移进入可开启范围，只是更慢（§4.2）。
14. **`ropeGroupId` 用单个 int**：天然保证「一辆车只属于一个绳组」，无需额外校验（§4.2）。
15. **绳根的排布公式照抄 UltimateRope**：`t = i/(count-1) × (dist - dist/count)/dist`，与 `CreateRopeJoints` 的 Reposition 段（`UltimateRope.cs:3257-3272`）一致；这样建绳瞬间骨骼排布与 UltimateRope 自算的完全相同（两者张成的跨度都是 `dist×(count-1)/count`），蒙皮只受一个刚体变换，绳形不会扭曲。
16. **靠 tiling 补偿解决纹理密度，而不是实时改骨骼数量**：改骨骼数量必须重新生成网格（骨骼与网格环一一对应）→ 走 `Regenerate()` 销毁重建全部骨骼 + 关节 + 蒙皮网格，而补位期间距离逐帧连续变化，节流等于每帧触发，不可行。而密度与骨骼数量本来无关（UV 是顶点属性），乘 tiling 即可精确解决（§7.1）。
17. **不做「停稳后重建一次」的补偿**：既然 tiling 已经精确消除拉伸，重建只会带来跳变与开销，没有收益。
18. **长度不设上限**：已确认包围盒（`updateWhenOffscreen`）与断绳逻辑（`LinkJointBreakForce = Infinity`）都不构成限制，直线绳的纵向分段数也不影响观感（§7.1）。
19. **末端漏一段靠骨节长度缩放修，而不是改摆位**：管体最后一节的长度是**建绳时烘焙**进顶点的常量（`fLength / nNumLinks`），摆位改不动它；改骨骼 `localScale.z` 能让蒙皮把那个 z 偏移一起放大，末端才回得到锚点。摆位公式（`t = i/n`）本身已经是对的（§7.2）。
20. **只缩放 z、不缩放 x/y**：顶点径向偏移由 x/y 承载，动了就会让管子变粗变细。
21. **绳组按列序错峰出库**：链首（列最小 = 最左）立即出库，其余依次延迟。`Chain` 已按 `gridX` 升序排好，所以「最左先出」不需要额外判定。错峰还带来一个副作用：延迟窗口内左车已走、右车未动，绳子被拉长——正好由 §7.1 的 tiling 补偿表现为「绳长被放出」。
22. **两段间隔独立配置**（`ropeExitHeadGap` 头车→第一个后车、`ropeExitStagger` 后车→后车）：头车要先倒车（`reverseDuration + reverseWait`）才开始前进，后车是直接出车，两者的「起步延迟」手感不同，共用一个值必然有一边不对。实现上就是协程里 `i == 1` 用前者、`i >= 2` 用后者。
23. **后车用「跳过倒车」的出车变体**：后车本来是被前面的车拽出去的，再做一遍倒车会让整条链的节奏打架（后车先退再进，绳长反复伸缩）。变体的细节与参数见 `Docs/ContainerExitDesign.md` §6.8；这里只负责决定「谁是头车、谁是后车」——`chain[0]` 走正常出车，`chain[1..]` 走变体。
24. **错峰用协程而不是逐车排队**：`TryExitIfAtFront` 的绳组门槛（`IsRopeGroupReady` 要求全组都在前排）在链首出库的瞬间就不再成立，所以协程只会被启动一次，天然幂等；不需要额外的「本组已开始出库」标记。

---

## 10. 已确认的决策（问答定稿）

| # | 问题 | 结论 | 落点 |
|---|---|---|---|
| 1 | 组内车是否必须同排 | **允许不同排** | 绳子可斜；补位时端点距离连续变化 → 每帧同步绳长（§5.2） |
| 2 | 出库时的甩动 | **接受** | 不做冻结，出库期间持续同步（§6） |
| 3 | 绳粗 / 骨骼数初值 | 先给保守初值，进 Play 后实际调 | `diameter = 0.15`、`linkCount = 8`（§7） |
| 4 | 端点位置 | **由策划自己调整** | 我只保证端点从 `ropeAnchorLeft/Right` 读取；位置由你在预制体上摆（§2.3） |
| 5 | `Rope` 层 + 预制体端点改动 | **由策划自己改**（涉及 YAML） | §10.1 给出精确步骤 |
| 6 | 一组允许 ≥3 车且跨排 | **允许** | 只要求列相邻、每列 1 个 |
| 7 | 同一列出现两个不同绳组 | **允许，但不得交叉**；且一辆车不能属于多个绳组 | 交叉校验（§4.2）；「一车一组」由 `ropeGroupId` 单值天然保证 |
| 8 | 纹理拉伸 / 长度上限 | **改成「从一端放出绳长」的观感**，沿绳长保持固定纹理密度、长度不设上限 | tiling 每帧补偿（§7.1） |

### 10.1 需手工完成的三步

| 步骤 | 位置 | 说明 |
|---|---|---|
| 1. 新增层 | `ProjectSettings/TagManager.asset` → Tags and Layers | 加一个名为 **`Rope`** 的层；建议在 Physics 碰撞矩阵里把 `Rope` 与所有层的勾选**全部取消**（绳子纯视觉） |
| 2. 挂端点 | ContainerItem 预制体 | 在车体根下建两个空物体，命名 `ropeAnchorLeft` / `ropeAnchorRight`，位置按你要的挂点摆；不要挂 Rigidbody，也不要放在 `elasticScaleAxle` 之下 |
| 3. 配材质 | 场景里 ContainerGroup 的 Inspector | 把 `Assets/CrowdMatch/Materials/Rope.mat` 拖到新增的 `Rope Material` 字段（可再调 `Rope Link Count` / `Rope Diameter`） |

> 未做第 1 步时，`Rope.cs` 会把绳节放到 `Default` 层并打 warning —— 绳节会与像素/车互撞，务必先做。
> 未做第 2 步时，「标记为连接」会被校验拦住并提示缺端点。

---

## 11. 验证清单

1. Unity 编译无报错、Console 无新警告（尤其无 `Rope` 层缺失 warning）。
2. 三个 rope 脚本 + 材质贴图（含 `.meta`）就位；`Rope` 层已建且在碰撞矩阵中不与任何层勾选。
3. 编辑器：选中相邻 3 列的各 1 个车 → 「标记为连接」成功，`shuffleContainers` 变为 false，导出 JSON 里三个车带同一个 `ropeGroupId`、`lockContainer = true`。
4. 编辑器：故意违反每条校验（1 个车、同列 2 个、列不连续、端点未配置、与已有组交叉）→ 提示正确且按钮置灰。
   交叉用例：先标记 A = {(col1,row0), (col2,row2)}，再试 B = {(col1,row2), (col2,row0)} → 应被拒。
5. 编辑器：「取消连接」选中组内任一个车 → 整组 `ropeGroupId` 归零；Undo 可整体回退。
6. **编辑器可视化**：非运行模式下 Scene 视图里能看到绳连——标记过的车之间有线，线的两端落在 `ropeAnchorLeft/Right` 上（小球标出）。
   分项验证：故意清掉某辆车的端点 → 该端小球变**红**；把 `ContainerGroup.shuffleContainers` 打开 → 整条链变**半透明灰**；进 Play → 线条消失（由真绳子接管）。
7. 运行：关卡加载后绳子出现在相邻车之间，两端贴合挂点，**无下垂、无拉伸脱离**。
8. 运行：把其中一辆车补位前移（造成斜向）→ 绳子每帧重新绷直，长度与端点距离一致；
   **且绳子的花纹没有变稀**（贴图沿绳长保持固定世界密度，表现为「从起点端放出更多绳长」）。
9. **末端贴合**：距离变化后（含补位途中、出库途中）绳子的两端都要**正好落在 anchor 上**，不能差一段。
   重点看**终点端**（右车的左端点）——那正是 §7.2 修的偏差；起点端本来就在锚点上。
10. 运行：让组内部分车先装满 → 满车停在前排不动、该列不补位；其余车继续收像素。
11. 运行：全组装满 → **最左那辆先出库，其余依次延迟**（头车 → 第一个后车 = `ropeExitHeadGap`，后车之间 = `ropeExitStagger`）；
    **头车有倒车段，后车没有**（直接切前轴、先甩到 `ropeExitMaxAngle` 再归 0）；绳子随车驶出并在车销毁后消失，
    Console 无空引用异常；补位也随之依次发生。
12. 运行：连续多次出库/补位，绳子不残留、不抖动、绳长始终贴合。
13. 重排：跑一次「按 Record 重排容器」→ 输出 JSON 中所有 `ropeGroupId` 均为 0。
14. 洗牌兜底：手改 JSON 让 `lockContainer = false` 且带绳组 → 运行时**不生成绳子**，车各自独立出库。

---

## 12. 改动清单（文件级）

| 文件 | 类型 | 内容 |
|---|---|---|
| `Assets/Scripts/Rope/Rope.cs` 等 3 个 | **新增**（skill 植入） | 原样复制，不改 |
| `Assets/Art/Materials/{Rope,Rope_d}.mat` + `.meta` | **新增**（skill 植入） | 连 `.meta` 一起 |
| `Assets/Art/Textures/{rope_diffuseheight.tga,rope_normal.png}` + `.meta` | **新增**（skill 植入） | 连 `.meta` 一起 |
| `Assets/Scripts/Gameplay/ContainerRopeLink.cs` | **新增** | 一条绳：建绳、建后清理、每帧绷直驱动、锚点消失自毁 |
| `Assets/Scripts/Gameplay/ContainerItem.cs` | 改 | `ropeGroupId`、`ropeAnchorLeft/Right` 字段 |
| `Assets/Scripts/Gameplay/ContainerGroup.cs` | 改 | 建绳入口 + 绳配置字段；`TryExitIfAtFront` 加绳组门槛；`ExitRopeGroup` 按 `ropeExitHeadGap` / `ropeExitStagger` 错峰出库、头车正常出车 / 后车走变体；`DestroyContainerInPlace` 绕开绳组车；`ClearContainers` 清绳；`SpawnContainer` 加形参；`OnDrawGizmos` 绳连预览（§4.6） |
| `Assets/Scripts/Gameplay/ContainerExitDriver.cs` | 改 | `Play` 加 `ropeRearExit`；绳连后车跳过倒车直接切前轴、从 0° 甩头归 0；新增一组独立的 `ropeExit*` 参数；倒车段抽成 `ReverseAndSwitchAxle` |
| `Assets/Scripts/Gameplay/LevelData.cs` | 改 | `ContainerItemData.ropeGroupId` |
| `Assets/Scripts/Core/LevelLoader.cs` | 改 | 传 `ropeGroupId`；洗牌时忽略绳组 |
| `Assets/Scripts/Editor/LevelDataExporter.cs` | 改 | 导出 `ropeGroupId` |
| `Assets/Scripts/Editor/ContainerItemEditor.cs` | 改 | 新增「绳子连接」一节 + 校验 + 标记/取消 |
| `Assets/Scripts/Editor/ContainerRearranger.cs` | 改 | 显式清空绳信息 + 注释 |
| `ProjectSettings/TagManager.asset` | **手工** | 新增 `Rope` 层（YAML，见 §10.4） |
| ContainerItem 预制体 | **手工** | 新增两个端点空物体（YAML，见 §10.4） |
