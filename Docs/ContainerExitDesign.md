# CrowdMatch「小车出库」功能设计文档

> 状态：**已实现**。本文描述「容器耗尽后，不再瞬间 `Destroy`，而是让已做成小车模型的 Container
> 先向右后方小幅倒车、再向左前方出车并转正、沿 -X 方向开出场景」的完整动画，以及它与现有
> `ContainerGroup` / `ContainerItem` 的集成方式。倒车 / 出车的驱动采用「前轴 / 后轴 + 父物体切换」的
> 真实挂载模型（先写文档，再实现）。

---

## 1. 概述

当前 Demo 里，`ContainerGroup.MovePixelToContainer` 吸收最后一个像素时（`isLast == true`），调
`DisappearAndRefill`：把前排容器从网格里移除并 `Destroy`，然后后排容器依次补位。

本次把「`Destroy` 瞬间消失」替换为一段**出库动画**：

1. 容器（小车，车头朝 -X）先向**右后方**（+X，车尾方向）小幅**倒车**，同时车头甩一个负角度。
2. 倒车进行到某个时间点（`reverseSquashDelay`）后，车体沿 X **匀减速缩小**到 `reverseSquashScale`（覆盖剩余倒车
   + 等待，呈现惯性挤压夸张），随后小车**向左前方**（-X，车头方向）**加速出车**：车头先**进一步甩角**到出车最大
   角度 `exitMaxAngle`，再立即反向角速度、加速转正到水平——角度变化全程与前进（线性位移）同步；车体 X 缩放也在
   出车开始时**匀加速回到 1**；小车自出车开始即沿前进轴**先匀加速后匀减速侧翻**到 `rollMaxAngle`。
3. 转正瞬间，小车切换到「整车直行」，后排容器开始**补位**；小车仍挂在**侧翻自转轴**下，侧翻角**匀加速归 0**。
4. 侧翻归 0 后自转轴归位，小车换到**弹性缩放轴**下做 XZ 放大 / Y 缩小的弹性（先匀减速到最大、再立即匀加速复原），复原后继续加速至最大速度后匀速，再持续给定时间后**销毁**（开出场景）。

驱动方式复用「前轴 / 后轴」挂载：倒车时把小车挂到**后轴**上，由后轴带动；出车时把小车挂到**前轴**上，
由前轴带动；转正后恢复位移轴与缩放轴的子父级，由整车驱动。倒车 / 出车期间，另有一个**倒车缩放轴**夹在驱动轴
与车体之间只缩放 X，最深层还有一个**侧翻自转轴**（小车挂在其下）绕前进轴侧翻。整个过程用
`SetParent(…, worldPositionStays: true)` 保证**无瞬移**。

---

## 2. 目标与非目标

### 目标
- 用「前轴 / 后轴 + 父物体切换」的挂载模型，实现真实的后轴倒车、前轴出车，而非对整车整体做位移/旋转。
- 倒车段：匀加速（二次曲线）位移 + 负角度；出车段：匀加速到最大速度 + 角度先进一步甩大到 `exitMaxAngle` 再反向加速转正（角度变化全程与前进同步）。
- 出车开始即沿前进轴做轻微惯性侧翻（先匀加速后匀减速到 `rollMaxAngle`），转正后侧翻角匀加速归 0。
- 转正后触发后排补位；侧翻归 0 后再做一次 XZ 放大 / Y 缩小的弹性缩放（匀减速到最大、匀加速复原），随后整车直行出场景并销毁。
- 与现有 `ContainerGroup.ConsumePixel` / `DisappearAndRefill` 契约平滑替换，轴未配置时回退到旧的「直接销毁 + 补位」。

### 非目标（本期不做）
- 不做小车与像素/传送带的物理碰撞（出车过程纯动画，不与其它物体交互）。
- 不做多个小车同帧出车的排队/避让（各列独立出库，互不干扰）。
- 不改 `ContainerGroup` 的容器生成 / 匹配逻辑。
- 不做小车轮子滚动等次级动画（只做车体 + 轴的刚体位移/旋转）。

---

## 3. 坐标系与几何

- **世界 +X**：小车**车尾**方向；**世界 -X**：小车**车头**方向（出车方向）。
- **水平（转正）**：`eulerY = 0`。此时 `transform.right = +X`（车尾），`transform.left = -X`（车头）。
- **倒车**：小车沿自身 `right`（+X，车尾）位移，车头甩出**负** `eulerY`（`reverseAngle` 为正值，赋值 `-reverseAngle`）。
- **出车**：小车沿自身 `left`（-X，车头）位移，`eulerY` 从 `-reverseAngle` 回到 `0`。
- **侧翻**：小车绕**前进轴**（局部 X）自转，角度写**自转轴 `localRotation` 的 `eulerX`**（正值向一侧翻），与偏航正交、互不干扰。
- 轴的父物体（动画期间）= 小车的原始父物体（即 `ContainerGroup`）。偏航（`eulerY`）写**世界 `rotation`**（世界角），
  不依赖父物体旋转；位移沿轴自身的 `right` / `left` 世界方向。

---

## 4. 状态机

```
Idle（在网格中，等待匹配）
   │  isLast → StartContainerExit（清空格子 grid[col,0]=null）
   ▼
Reverse（倒车：后轴驱动）
   │  reverseDuration 到点
   ▼
ReverseWait（倒车等待 reverseWait）
   │  延时到点 → 切前轴挂载
   ▼
ExitTurn（出车转正：前轴驱动，线性加速 + 角度 -reverseAngle → -exitMaxAngle → 0；出车开始即自转轴侧翻 0 → rollMaxAngle）
   │  eulerY 达到 0 → 转正，恢复位移轴/缩放轴，保留自转轴作父物体
   ▼
ExitStraight（整车直行：侧翻 rollMaxAngle → 0 匀加速归 0，再换弹性缩放轴 XZ 放大/Y 缩小并复原，然后加速至最大速度后匀速，exitDriveDuration 到点）
   ▼
Destroyed（销毁）
```

> 补位（后排前移）在 `ExitTurn → ExitStraight` 的**转正瞬间**触发（见 §6.3 第 5 步），
> 与旧 `DisappearAndRefill`「先销毁再补位」不同——补位提前到小车转正时，队列视觉上更连续。

> 倒车缩放（squash）从 `Reverse` 中某个时间点（`reverseSquashDelay`）开始，覆盖剩余 `Reverse` + `ReverseWait`
> （X 匀减速缩到 `reverseSquashScale`），出车开始（`ExitTurn`）时在 `exitScaleRecoverDuration` 内匀加速回到 1（见 §6.5）。

> 侧翻（roll）自 `ExitTurn` 出车开始即用时间时钟驱动，在 `rollOutDuration` 内先匀加速后匀减速侧翻到 `rollMaxAngle`
> （到点后保持）；转正后进入 `ExitStraight`，侧翻角在 `rollRecoverDuration` 内匀加速归 0（见 §6.6）。
>
> 弹性缩放（elastic）在侧翻归 0 后**单独应用**（弹性轴与其他轴不共存）：XZ 在 `elasticScaleDuration` 内匀减速放大到
> `elasticTargetScale`、到位后立即在 `elasticRecoverDuration` 内匀加速复原（见 §6.7）。

> **绳连后车变体**（`Play(onRefill, ropeRearExit: true)`）：被绳子连接的后车**跳过 Reverse 与 ReverseWait 两个状态**，
> 换轴照旧、只是根节点换成**单独配置的转轴** `ContainerItem.ropeExitAxle`（留空退回 `frontAxle`），
> 起始角是正姿 **0°**（不是 `-reverseAngle`）。运动参数走独立的一组 `ropeExit*`（见 §6.8）。
> 因为换轴逻辑一致，**侧翻与弹性两段对这条路径同样生效**。

```
Idle ──(ropeRearExit)──▶ ExitTurn（绕 ropeExitAxle：从 0° 甩到 -ropeExitMaxAngle 再归 0）──▶ ExitStraight ──▶ Destroyed
```

---

## 5. 轴挂载与 reparenting（无瞬移）

小车初始层级（静止在网格中）：

```
ContainerGroup（= 小车原始父物体）
└── Cart（ContainerItem + ContainerExitDriver）
    ├── FrontAxle（空物体）
    ├── RearAxle（空物体）
    ├── ReverseScaleAxle（空物体，倒车缩放轴）
    ├── RollAxle（空物体，侧翻自转轴，最深层）
    └── 小车视觉模型（Renderer / 模型子物体）
```

### 5.1 倒车挂载（后轴驱动）

1. `rearAxle.SetParent(cartParent, worldPositionStays: true)` —— 后轴脱离小车，挂到小车原始父物体下。
2. `rearAxle.localScale = Vector3.one` —— 重置后轴 scale 为 1（轴始终是纯 pivot，不携带自身缩放）。
3. 依次把缩放轴、自转轴挂到链条最深层，最后把小车挂到链条最深层，构成 `RearAxle → ReverseScaleAxle → RollAxle → Cart`
   （缩放轴、自转轴各自可选，缺省时跳过该级）。

```
ContainerGroup
└── RearAxle
    └── ReverseScaleAxle
        └── RollAxle
            └── Cart
                └── FrontAxle（+ 视觉模型）
```

此后移动 / 旋转 `RearAxle` 即带动整辆车；缩放 `ReverseScaleAxle` 的 X 挤压车体；旋转 `RollAxle` 的局部 X 使小车侧翻。

### 5.2 出车挂载（前轴驱动）

> 位移级换轴：先把新轴（前轴）提到与旧位移轴（后轴）同父级并重置 scale，再把直接挂在后轴下的链条最上层节点
> （缩放轴，或无缩放轴时的自转轴，或都无时的小车）整体移到新轴下，最后把旧轴还给小车。小车全程不脱离缩放轴/
> 自转轴，自身缩放不被烘、也不重置（避免缩放 pivot 与车体 pivot 不一致导致瞬移）。

1. `frontAxle.SetParent(cartParent, worldPositionStays: true)` —— 前轴脱离小车，挂到与旧位移轴（后轴）同父级。
2. `frontAxle.localScale = Vector3.one` —— 此刻前轴与系统无牵连，重置 scale 干净。
3. 把链条最上层节点（`scale ?? roll ?? cart`）整体 `SetParent(frontAxle, worldPositionStays: true)` —— 缩放轴/自转轴/小车整体移到前轴下。
4. `rearAxle.SetParent(cart, worldPositionStays: true)` —— 旧轴（后轴）还给小车。
5. `rearAxle.localScale = Vector3.one` —— 后轴归位后重置 scale（空轴，重置不引起瞬移）。

```
ContainerGroup
└── FrontAxle
    └── ReverseScaleAxle
        └── RollAxle
            └── Cart
                └── RearAxle（+ 视觉模型）
```

此后移动 / 旋转 `FrontAxle` 即带动整辆车；缩放 `ReverseScaleAxle` 的 X 挤压车体；旋转 `RollAxle` 的局部 X 使小车侧翻。

### 5.3 转正恢复（保留自转轴，改由自转轴驱动）

1. `reverseScaleAxle` 的 X 先归 1 —— 小车仍在其下，围绕正确 pivot 解除挤压（不瞬移）。
2. `rollAxle.SetParent(cartParent, worldPositionStays: true)` —— 自转轴（带小车）提到原始父物体下（若有自转轴；无则 `cart.SetParent(cartParent)` 直接回原始父物体）。
3. `frontAxle.SetParent(cart, worldPositionStays: true)` —— 前轴改回小车子物体。
4. `reverseScaleAxle.SetParent(cart, worldPositionStays: true)` —— 缩放轴改回小车子物体，X 缩放归 1（若有缩放轴）。

此时 `eulerY = 0`（已转正），但小车仍挂在自转轴下、`eulerX = rollMaxAngle`（侧翻未归零）。整车直行期间继续驱动小车前移，
同时侧翻角归 0（见 §6.6）。

```
ContainerGroup
└── RollAxle
    └── Cart
        ├── FrontAxle
        ├── RearAxle
        ├── ReverseScaleAxle
        └── 视觉模型
```

### 5.4 侧翻归零恢复（自转轴归位）

侧翻角在 `rollRecoverDuration` 内匀加速归 0 后（`eulerX = 0`）：

1. `cart.SetParent(cartParent, worldPositionStays: true)` —— 小车脱离自转轴，回到原始父物体（自转轴已归 0，不烘）。
2. `rollAxle.SetParent(cart, worldPositionStays: true)` —— 自转轴改回小车子物体。

恢复初始层级后，直接移动 / 旋转小车本体（此时 `eulerY = 0`、`eulerX = 0`，`transform.left = -X`）。

### 5.5 弹性缩放挂载（弹性轴单独应用）

侧翻归 0、自转轴归位后，各轴（前/后/缩放/自转）都已回到小车子物体，此刻仅**弹性缩放轴**单独接管小车（与其他轴不共存）：

1. `elasticScaleAxle.SetParent(cartParent, worldPositionStays: true)` —— 弹性轴脱离小车，挂到原始父物体下。
2. `elasticScaleAxle.localScale = Vector3.one` —— 纯 pivot，重置 scale。
3. `cart.SetParent(elasticScaleAxle, worldPositionStays: true)` —— 小车挂到弹性轴下。

```
ContainerGroup
└── ElasticScaleAxle
    └── Cart（+ 前轴/后轴/缩放轴/自转轴/视觉模型）
```

此后缩放 `elasticScaleAxle` 的 XZ/Y 即做弹性夸张（见 §6.7）。复原后：

1. `elasticScaleAxle.localScale = Vector3.one` —— XZ/Y 归 1。
2. `cart.SetParent(cartParent, worldPositionStays: true)` —— 小车脱离弹性轴。
3. `elasticScaleAxle.SetParent(cart, worldPositionStays: true)` —— 弹性轴改回小车子物体，`localScale` 归 1。

> 弹性轴位置即「弹性缩放 pivot」——放在车体中心则对称放大/压扁，由场景摆放决定。

> 全部 `SetParent` 都用 `worldPositionStays: true`，确保切换父物体时世界位姿不变、零瞬移。

> `worldPositionStays: true` 会把「世界缩放」烘进 `localScale`。但**小车（Item）不能直接重置自身缩放**：它的缩放
> pivot 与缩放轴不一致，重置会让视觉瞬移。因此换轴时只重置「空轴」的 scale（§5.1 第 2 步、§5.2 第 2/5 步），
> 并让小车全程留在缩放轴/自转轴下，避免小车自身缩放被烘、被重置。侧翻归 0 后自转轴已 identity，小车脱离时不烘。

### 5.6 出车的准备门槛：车身必须先回到 ContainerGroup 下

`ContainerExitDriver.Run` 开头会**只读一次** `cartParent = transform.parent`（`:156`），后面 6 处
`SetParent(cartParent, …)` 全用它——「小车原始父物体 = ContainerGroup」是整条出车链条的前提。

小车的父物体在四种情况下不是 ContainerGroup：

| 期间 | 车身挂在哪 | 谁挡住出车 |
|---|---|---|
| 补位侧倾 | `rollAxle` | `isRefilling`（要等 `RestoreRollAxle` 之后才置 false）✅ |
| 补位前移夹着上车动画 | `elasticScaleAxle` | 同上（`RefillRoutineViaElasticAxle` 自己也等 `_elasticPhase == 0` 才回调）✅ |
| **上车动画前半：跳车中** | `ContainerGroup`（还没换轴） | **原本没人挡** ❌ → 已补 |
| **上车动画后半：播弹性** | `elasticScaleAxle` | **原本没人挡** ❌ → 已补 |

**缺口成因：车身变空是瞬间的，而上车动画有两段。** `ConsumePixel` 里 `container.Consume()` 一执行，
`IsEmpty` 就为 true；但像素还要跳 `boardJumpDuration` 才落到车上（**这一段车身还在 ContainerGroup 下**），
落定那一帧才 `PlayBoardElastic()` 换到弹性轴下播弹性。只有弹性播完才走
`onBoarded → OnPixelConsumed → TryExitIfAtFront`。于是整个上车动画期间
「装满 + 在前排 + 不在补位」都成立，**任何提前的触发都会把出车放行**。

> **这里踩过一次坑**：第一版只堵了「车身挂在弹性轴下」，重跑后仍有一例失败——正是**跳车那一段**：
> 车已 `IsEmpty`、但弹性还没开始，`IsCarUnderElasticAxle()` 还是 false，门控看不见它。
> 所以判据必须是「**整个上车动画是否结束**」，不能只看换轴状态。

实际发生过两条提前触发：

- **绳组**：整组出库由**某个成员**的事件触发，而 `IsRopeGroupReady` 只看 `IsEmpty` / `isRefilling` / 是否在前排
  → 另一个成员的完成事件会把还在跳车或还在播弹性的成员一起拉出车；
- **普通车**：`MoveContainer` 在没有 roll 轴时走纯 Lerp 回退路径，结束后直接 `OnCarArrivedFront → TryExitIfAtFront`，
  完全不看上车动画。

**为什么"挂错父物体"表现为"歪着开远"而不是"位置错"**：弹性轴的缩放是**非均匀**的
（`elasticTargetScale` 形如 `1.2 / 0.83 / 1.2`）。`SetParent(…, worldPositionStays: true)` 在非均匀缩放的父物体下
会做矩阵分解，把补偿量烘进子节点的 `localScale` / `localRotation`。误差因此**不在某一个 rotation 上**，
而在祖先的 scale 与被烘进去的补偿量里 → 后面 `drive.rotation = Quaternion.Euler(0,0,0)` 的转正**拉不回来**；
再加上直行段只写 `position`、不写 `rotation`，车的角度就此**完全冻结**着斜飞出去。

**修法（把既有规则搬进门槛，而不是改动画调度）**：新增 `ContainerItem.IsBoarding`
（= 还在跳车 `_boardingPixels.Count > 0` **或** 车身挂在弹性轴下），两个出车门控同查它：

| 门控 | 改动 |
|---|---|
| `ContainerGroup.TryExitIfAtFront` | 上车动画未结束 → `return` |
| `ContainerGroup.IsRopeGroupReady` | 任一成员上车动画未结束 → 整组不就绪 |

**为什么这一条是充分的**：所有能发起一次上车的入口都跳过 `IsEmpty` 的车——`ProcessConsumption`（带上吸收）、
`FindMatchableInColumn`（带闸口）、`ConsumePixel`、`FindCarForColor`（复活）全都有 `IsEmpty` 早退。
所以一辆车变空之后**不可能再开始新的上车**，它的 `IsBoarding` 只会单调变为 false。由此两点：

- 门控一旦通过，即使 `ExitRopeGroupRoutine` 要隔 `headGap` / `ropeExitStagger` 才轮到某个成员，
  那个成员也不可能在这段时间里重新进入上车状态 → **协程里不需要再查一遍**；
- 被挡下的那一次，上车动画结束时必然重新触发（见下）。

**自愈**（不需要轮询，也不需要超时兜底）：`BoardRoutine` 的收尾是
`PlayBoardElastic()` → `WaitForElasticIdle()`（等 `_elasticPhase == 0`）→ `onBoarded`，
而 `ElasticRoutine` 的收尾顺序是 `RestoreElasticAxle()` → `_elasticPhase = 0`。
所以 `onBoarded` 触发时轴已归还、`_boardingPixels` 已清空 → `IsBoarding == false`，
同一帧内 `onBoarded → OnPixelConsumed → TryExitIfAtFront` 必然放行。

> **为什么不在驱动侧兜底**（`Run` 开头等车身不在任何轴下再取 `cartParent`）：那条路会把
> 「`grid[col,0]` 已清空但车还在原地」的窗口多拉长几帧；门控版本让车压根没进入出车流程。
> 而且「出车必须在形变播完之后」本来就是既定规则（`WaitForElasticIdle`、`RefillRoutineViaElasticAxle` 的收尾条件），
> 门控只是把它从"靠调用链自觉"变成"由判据保证"。

> **附带收益**：车不会在还处于挤压形变时就起步开走（以前能看到车带着形变同时位移）。

---

## 6. 运动模型

### 6.1 倒车（Reverse，匀加速二次曲线）

参数：`reverseDuration`（总时长 T）、`reverseDistance`（总位移 S）、`reverseAngle`（总角度 Θ，正值为甩向负 Y）。

由 `s = a·t²/2` 反推加速度，位移与角度都走二次曲线。每帧（`yield return null`）：

```
t ∈ [0, T]；p = t / T
s(p)  = reverseDistance * p * p
θ(p)  = reverseAngle    * p * p

Δs = s(p) - s(p_prev)                     // 累积值相减，得该帧位移
rearAxle.position += rearAxle.right * Δs   // 沿自身 right（+X，车尾）位移
rearAxle.rotation = Euler(0, -θ(p), 0) // 直接赋值负角度（世界角）
```

> 「记录上一帧累积值、相减得该帧位移」是为了匀加速下每帧位移递增（越倒越快），而不是匀速均分。

### 6.2 倒车等待（ReverseWait）

倒车结束到切前轴之间等待 `reverseWait` 秒。期间**继续**应用倒车阶段开始的**匀减速缩放**（见 §6.5），到等待结束
缩放轴 X 缩到 `reverseSquashScale`。等待结束后切换到 §5.2 的前轴挂载。

### 6.3 出车转正（ExitTurn，前轴驱动，线性加速 + 角度先甩大再归 0）

参数：`exitAcceleration`（线性加速度 a）、`exitAngularAcceleration`（角度加速度 α）、
`exitMaxAngle`（出车最大角度 Θmax，正值为甩向负 Y，须 > `reverseAngle`）、`exitMaxSpeed`（最大速度 vmax）。

出车从切前轴后开始，**线性位移与角度变化全程同步**（车一边前进、一边转）。线性速度用速度积分（带上限），
角度用角度速度积分，分两段：

```
v = min(v + exitAcceleration * dt, exitMaxSpeed)          // 速度积分，封顶（全程）
frontAxle.position += frontAxle.left * (v * dt)           // 沿自身 left（-X，车头）位移（全程）

angle = -reverseAngle；ω = 0；swung = (exitMaxAngle ≤ reverseAngle)
// 第一阶段（swung=false）：角加速度使角度向更负方向加速变大，直到 -exitMaxAngle
if !swung:
    ω -= α * dt                     // 加速变大（甩头）
    angle += ω * dt
    if angle ≤ -exitMaxAngle: angle = -exitMaxAngle; ω = -ω; swung = true   // 立即反向角速度，锁定进入第二阶段
// 第二阶段（swung=true）：反向后加速归 0
else:
    ω += α * dt                     // 加速归 0
    angle += ω * dt
    frontAxle.rotation = Euler(0, angle, 0)   // 世界角
```

当 `angle >= 0` 时：

1. `angle = 0`，`frontAxle.rotation = Euler(0, 0, 0)` —— 转正。
2. 恢复 §5.3 的轴子父级（小车脱离前轴、前轴归位）。
3. **触发补位回调** `onRefill()`（后排容器前移）。
4. 进入 §6.4 的整车直行。

> `exitMaxAngle ≤ reverseAngle` 时第一阶段不进入、直接进入第二阶段加速归 0（等价旧行为：角度单调归 0）。

### 6.4 整车直行（ExitStraight，加速至最大后匀速，然后销毁）

转正后，小车水平（`transform.left = -X`），继续用**同一个** `v` 积分（越过最大速度后匀速）：

```
for exitDriveDuration 秒：
    v = min(v + exitAcceleration * dt, exitMaxSpeed)
    cart.position += cart.left * (v * dt)    // 水平时 cart.left = -X
```

到点后 `Destroy(gameObject)`。轴此时已全部归位为小车子物体，随小车一并销毁。

### 6.5 倒车缩放（Squash，惯性夸张）

参数：`reverseSquashDelay`（从倒车开始计时，多久后开始缩放）、`reverseSquashScale`（缩放目标值，1=不变）、
`exitScaleRecoverDuration`（出车开始时缩回 1 的时长 T_r）。缩放轴 `reverseScaleAxle` 若未配置则跳过整个缩放
（不影响其它动画）。

从倒车开始后的 `reverseSquashDelay` 时刻起，缩放轴 X 沿车体长度**匀减速**缩小到 `reverseSquashScale`，覆盖剩余的
倒车 + 倒车等待时间（总时长 `T_s = reverseDuration + reverseWait - reverseSquashDelay`，ease-out quad，起始最快、
末速归零）：

```
t ∈ [0, T_s]；p = clamp01(t / T_s)
e = 1 - (1-p)²                            // 匀减速
scaleX = 1 - (1 - reverseSquashScale) * e
```

> 实现用一个从倒车开始计时的时钟 `τ`（贯穿倒车与等待两个循环），在 `τ ≥ reverseSquashDelay` 时以
> `(τ - reverseSquashDelay) / T_s` 作为进度；`τ < reverseSquashDelay` 时 `e = 0`、缩放保持 1。

出车开始时（切前轴后），在 `exitScaleRecoverDuration` 内**匀加速回到 1**（ease-in quad）：

```
t ∈ [0, T_r]；p = clamp01(t / T_r)
scaleX = reverseSquashScale + (1 - reverseSquashScale) * p²
```

> 只改 `localScale.x`（保留 y/z）。缩放轴的本地位置即「挤压 pivot」——若放在后轴处，倒车时车头向车尾挤压；
> 若放在车体中心，则对称挤压。由场景摆放决定。

### 6.6 侧翻（Roll，惯性轻微侧翻）

参数：`rollMaxAngle`（侧翻最大角度，正值为绕前进轴的一侧）、`rollOutDuration`（侧翻到位时长，自出车开始计时）、
`rollRecoverDuration`（转正后侧翻归 0 的时长 T_rr）。自转轴 `rollAxle` 若未配置则跳过整个侧翻（不影响其它动画）。
侧翻写自转轴的 `localRotation`（`eulerX`），与偏航正交。

**侧翻到最大角（自出车开始）**：出车切前轴后立即开始，用时间时钟 `τ`（自出车开始计时）在 `rollOutDuration` 内
**先匀加速后匀减速**（ease-in-out quad）从 0 到 `rollMaxAngle`（到点后保持在 `rollMaxAngle`）：

```
p = clamp01(τ / rollOutDuration)
e = EaseInOutQuad(p)                     // 先匀加速后匀减速
roll = rollMaxAngle * e
rollAxle.localRotation = Euler(roll, 0, 0)
```

偏航 `angle >= 0`（转正）时侧翻补齐到 `rollMaxAngle`（若 `rollOutDuration` 偏长则已到），随后进入整车直行。

**侧翻归 0（转正后匀加速）**：整车直行期间，在 `rollRecoverDuration` 内**匀加速归 0**（ease-in quad）：

```
t ∈ [0, T_rr]；p = clamp01(t / T_rr)
roll = rollMaxAngle * (1 - p²)
rollAxle.localRotation = Euler(roll, 0, 0)
```

归 0 后 `rollAxle.localRotation = identity`，按 §5.4 把自转轴还给小车。

### 6.7 弹性缩放（Elastic Scale，侧翻归 0 后的弹性夸张）

参数：`elasticTargetScale`（最终缩放值 `Vector3`，直接定义到位目标）、`elasticScaleDuration`（匀减速缩放到该值的时长 T_e）、
`elasticRecoverDuration`（复原到 1 的时长 T_r）。弹性轴 `elasticScaleAxle` 若未配置则跳过（不影响其它动画）。

侧翻归 0 后，按 §5.5 把小车挂到弹性轴下（此时其他轴都已归位，弹性轴**单独**驱动），只改弹性轴的 `localScale`：

**放大 / 缩小（匀减速 ease-out）**：从 `(1,1,1)` **匀减速**缩放到 `elasticTargetScale`（三个分量各自插值，可同时放大 XZ、缩小 Y）：

```
t ∈ [0, T_e]；p = clamp01(t / T_e)
e = 1 - (1-p)²                          // 匀减速
elasticScaleAxle.localScale = Lerp(Vector3.one, elasticTargetScale, e)
```

**复原（匀加速 ease-in）**：到达 `elasticTargetScale` 后**立即**在 `elasticRecoverDuration` 内匀加速回到 `(1,1,1)`：

```
t ∈ [0, T_r]；p = clamp01(t / T_r)
e = p²                                  // 匀加速
elasticScaleAxle.localScale = Lerp(elasticTargetScale, Vector3.one, e)
```

归 1 后按 §5.5 把弹性轴还给小车，继续整车直行到 `exitDriveDuration` 后销毁。

### 6.8 绳连后车变体（Rope Rear Exit，跳过倒车、切自己的转轴）

被绳子连接的后车（绳组里**非头车**，由 `ContainerGroup` 决定，见 `Docs/ContainerRopeDesign.md`）走这条路径：
跳过 `Reverse` / `ReverseWait`，**换轴逻辑与正常出车完全一致**，只是把根节点从 `frontAxle` 换成
**单独配置的转轴** `ContainerItem.ropeExitAxle`（留空则退回 `frontAxle`，即原版行为）。
然后从正姿起转。

> **为什么要单独一个转轴**：绕前轴甩头时车身左右端点会被大幅扫出去，而绳子的两端就在车体左右两侧——
> 后车每个都甩一遍，绳端就跟着乱晃。把转轴单独配置，就可以把它摆在车体中心（或任何想要的位置）来减小端点位移，
> 而**不必动正常出车的前轴**（两者互不影响）。

**与正常出车的差别**：

| 环节 | 正常出车 | 绳连后车 |
|---|---|---|
| 倒车 + 等待 | 有（`Reverse` / `ReverseWait`，后轴驱动） | **跳过**（后轴全程不参与） |
| 换轴根节点 | `frontAxle`（前轴） | **`ropeExitAxle`**（可单独配置；留空退回 `frontAxle`） |
| 换轴逻辑 | 逐层挂 缩放轴 → 自转轴 → 车体 | **完全相同**（所以侧翻 / 弹性照样生效） |
| 旋转 pivot | 前轴的位置 | `ropeExitAxle` 的位置——摆到车体中心即「原地转身」 |
| 驱动对象 | 前轴（`drive = front`，车体被链条带着走） | 转轴（`drive = ropeAxle`，同样靠链条带着车体走） |
| 倒车挤压 | 有（`reverseSquashScale` 压到 0.6） | 没有 → 出车段的「缩放恢复 1」也随之跳过（否则会从 0.6 弹回 1，凭空缩一下） |
| ExitTurn 起始角 | `-reverseAngle`（倒车已把头甩出去 35°） | **0°**（正姿起转） |
| ExitTurn 走向 | 从 -35° 继续甩到 `-exitMaxAngle` 再归 0 | 从 0° 甩到 `-ropeExitMaxAngle` 再归 0 |
| 运动参数 | `exitAcceleration` / `exitAngularAcceleration` / `exitMaxAngle` / `exitMaxSpeed` / `exitDriveDuration` / `exitDriveAcceleration` | **完全独立的一组** `ropeExit*`（同名同义） |
| 侧翻 / 弹性 | `roll*` / `elastic*` | **共用同一组参数**（因为换轴一致，两段都生效） |
| 起步音效 / 震动时机 | 倒车开始时（音效）、倒车结束时（震动） | 都在**开始出车**那一刻 |

**运动公式与 §6.3 / §6.4 完全相同**，只是把六个参数换成 `ropeExit*`、把起始角换成 0、把驱动轴换成 `ropeExitAxle`：

```
angle = 0；ω = 0；swung = (ropeExitMaxAngle ≤ 0)
v = min(v + ropeExitAcceleration * dt, ropeExitMaxSpeed)
第一阶段：ω -= ropeExitAngularAcceleration * dt 直到 angle ≤ -ropeExitMaxAngle
第二阶段：ω += ropeExitAngularAcceleration * dt 直到 angle ≥ 0 → 转正 → 补位回调
整车直行：for ropeExitDriveDuration 秒，v 用 ropeExitDriveAcceleration 积分
```

> 实现上是同一份代码：`Run` 在开头按 `ropeRearExit` 把六个参数选进局部变量（`maxAngle` / `angAccel` /
> `linAccel` / `maxSpeed` / `driveDur` / `driveAccel`），并把**驱动轴**也选成一个局部变量
> `drive`（正常出车 = `front`，绳连后车 = `ropeAxle`），两个循环的位移与偏航都写 `drive`，
> 转正收尾也由 `drive.SetParent(transform, true)` 归位；倒车段被抽成 `ReverseAndSwitchAxle`，
> 只在正常出车时 `yield return` 它。所以两条路径的换轴与运动学不会分叉。
>
> `ropeRearExitEnabled` 是总开关：为 false 时，即使传入 `ropeRearExit: true` 也走正常出车
> （等价于「绳组后车也用原来的倒车出车」）。

---

## 7. 组件与 API

### 7.1 `ContainerItem`（加五个轴引用）

| 字段 | 类型 | 说明 |
|---|---|---|
| `frontAxle` | `Transform` | 前轴（空子物体，出车驱动轴） |
| `ropeExitAxle` | `Transform` | 绳连后车出车转轴（空子物体，可选）：绳组非头车出车时的旋转 pivot；摆到车体中心即「原地转身」。**留空退回 `frontAxle`** |
| `rearAxle` | `Transform` | 后轴（空子物体，倒车驱动轴） |
| `reverseScaleAxle` | `Transform` | 倒车缩放轴（空子物体，夹在轴与车体之间，X 缩放做惯性夸张，可选） |
| `rollAxle` | `Transform` | 侧翻自转轴（空子物体，最深层节点，位于缩放轴与车体之间，绕前进轴侧翻，可选） |
| `elasticScaleAxle` | `Transform` | 弹性缩放轴（空子物体，侧翻归 0 后单独应用的 XZ 放大/Y 缩小弹性缩放 pivot，可选） |

### 7.2 新增 `ContainerExitDriver`（`Gameplay/ContainerExitDriver.cs`，挂在小车 prefab 上）

| 分类 | 成员 | 默认 | 说明 |
|---|---|---|---|
| 倒车 | `reverseDuration` | 0.4f | 倒车总时长（s） |
| 倒车 | `reverseDistance` | 0.6f | 倒车总位移（m，+X） |
| 倒车 | `reverseAngle` | 35f | 倒车总角度（deg，正值为负 Y） |
| 倒车 | `reverseWait` | 0.15f | 倒车后等待（s） |
| 倒车缩放 | `reverseSquashDelay` | 0.2f | 倒车缩放起始延迟（从倒车开始计时，多久后开始匀减速缩放，覆盖剩余倒车+等待） |
| 倒车缩放 | `reverseSquashScale` | 0.6f | 倒车缩放目标值（倒车+等待结束时 X 缩放到该值，1=不变） |
| 倒车缩放 | `exitScaleRecoverDuration` | 0.3f | 出车开始时缩放匀加速回到 1 的时长（s） |
| 出车 | `exitAcceleration` | 8f | 出车线性加速度（m/s²） |
| 出车 | `exitAngularAcceleration` | 300f | 出车角度加速度（deg/s²，先甩头到 -exitMaxAngle 再加速归 0） |
| 出车 | `exitMaxAngle` | 55f | 出车最大角度（deg，正值为负 Y；出车转正前先甩到此角再归 0） |
| 出车 | `exitMaxSpeed` | 6f | 出车最大速度（m/s） |
| 出车 | `exitDriveDuration` | 0.8f | 转正后直行时长（s），到点销毁 |
| 侧翻 | `rollMaxAngle` | 12f | 侧翻最大角度（deg，正值为绕前进轴的一侧；出车开始即先匀加速后匀减速侧翻到该角度） |
| 侧翻 | `rollOutDuration` | 0.4f | 侧翻到位时长（s，自出车开始计时，先匀加速后匀减速侧翻到 rollMaxAngle） |
| 侧翻 | `rollRecoverDuration` | 0.4f | 侧翻归零时长（s，转正后匀加速回到 0） |
| 弹性缩放 | `elasticTargetScale` | `(1.2, 0.83, 1.2)` | 最终缩放值（Vector3，侧翻归 0 后从 (1,1,1) 匀减速缩放到该值） |
| 弹性缩放 | `elasticScaleDuration` | 0.15f | 弹性缩放到位时长（s，XZ 匀减速放大、Y 匀减速缩小到最大值） |
| 弹性缩放 | `elasticRecoverDuration` | 0.2f | 弹性缩放复原时长（s，到达最大值后立即匀加速回到 1） |
| 绳连后车 | `ropeRearExitEnabled` | true | 总开关：false 时绳组后车也走正常出车（等于关掉本变体） |
| 绳连后车 | `ropeExitMaxAngle` | 55f | 甩头最大角度（deg，正值为负 Y；从 0° 甩到此角再归 0） |
| 绳连后车 | `ropeExitAngularAcceleration` | 300f | 出车角度加速度（deg/s²） |
| 绳连后车 | `ropeExitAcceleration` | 8f | 出车转正段线性加速度（m/s²） |
| 绳连后车 | `ropeExitMaxSpeed` | 6f | 出车最大速度（m/s） |
| 绳连后车 | `ropeExitDriveDuration` | 0.8f | 转正后直行时长（s），到点销毁 |
| 绳连后车 | `ropeExitDriveAcceleration` | 8f | 转正后直行段线性加速度（m/s²） |
| 方法 | `void Play(Action onRefill, bool ropeRearExit = false)` | — | 启动出库动画；转正瞬间调 `onRefill`。`ropeRearExit: true` = 绳连后车变体（§6.8） |

`Play` 内部：
- 从 `GetComponent<ContainerItem>()` 取 `frontAxle` / `rearAxle` / `reverseScaleAxle`（缩放轴可选）。
- 若任一轴为 null，**回退**到 `onRefill()` + `Destroy(gameObject)`（等价旧 `DisappearAndRefill`）。
- `ropeRearExit && ropeRearExitEnabled` 决定走变体；否则走正常出车（倒车段在 `ReverseAndSwitchAxle` 里）。
- `OnDestroy` 兜底：若中途销毁时轴已游离（父物体不是本物体），销毁游离的轴，避免残留空物体（见 §10）。

### 7.3 `ContainerGroup` 改动

| 位置 | 改动 |
|---|---|
| `MovePixelToContainer` | `if (isLast) DisappearAndRefill(container, col)` → `if (isLast) StartContainerExit(container, col)` |
| `StartContainerExit(gone, col, ropeRearExit = false)` | 清 `grid[col,0]=null` → 取/加 `ContainerExitDriver` → `driver.Play(() => RefillColumn(col), ropeRearExit)` |
| 新增 `RefillColumn(col)` | 从旧 `DisappearAndRefill` 抽出的「后排依次前移」循环（`MoveContainer` 复用） |
| 删除 `DisappearAndRefill` | 逻辑拆分进 `StartContainerExit` + `RefillColumn` |
| `ExitRopeGroupRoutine` | 绳组错峰出库：头车 `ropeRearExit = false`、后车 `true`；两段间隔 `ropeExitHeadGap` / `ropeExitStagger`（见 `Docs/ContainerRopeDesign.md`） |

---

## 8. 数据流

```
ContainerGroup.MovePixelToContainer（isLast=true）
    │
    ▼
StartContainerExit(container, col)
    │  grid[col,0] = null            // 立即清空前排，停止匹配
    │  driver = Get/AddComponent<ContainerExitDriver>
    │  driver.Play(() => RefillColumn(col))
    ▼
ContainerExitDriver.Run（协程）
    │  Reverse（后轴驱动，倒车+负角；自 reverseSquashDelay 起 X 匀减速缩放）
    │  ReverseWait（X 匀减速缩放继续，到结束缩至 reverseSquashScale）
    │  ExitTurn（前轴驱动，线性加速 + 角度先甩大到 -exitMaxAngle 再归 0 + X 缩放回 1 + 侧翻先匀加后匀减到 rollMaxAngle）
    │      └─ 转正 → 恢复位移轴/缩放轴，保留自转轴 → onRefill() → RefillColumn(col)（后排前移）
    │  ExitStraight（整车直行：侧翻匀加速归 0 → 自转轴归位 → 弹性缩放 XZ 放大/Y 缩小并复原 → 加速→匀速）
    ▼
Destroy(gameObject)
```

> `ProcessConsumption` / `FindFrontContainerInFrontOf` 只读 `grid[col,0]`，一旦 `StartContainerExit`
> 清空该格，匹配即不再指向正在出库的小车，无需额外屏蔽。

---

## 9. 实现清单

1. `ContainerItem` 加 `frontAxle` / `rearAxle` / `reverseScaleAxle` / `rollAxle` / `elasticScaleAxle` 五个 `Transform` 字段。
2. 新增 `ContainerExitDriver.cs`（§7.2，含 §5 reparenting + §6 运动模型 + 回退/清理）。
3. `ContainerGroup` 拆分 `DisappearAndRefill` → `StartContainerExit` + `RefillColumn`，改 `MovePixelToContainer` 调用点。
4. 场景配置：在小车 prefab 上挂 `ContainerExitDriver`，并在 `ContainerItem` 上关联前轴/后轴/倒车缩放轴/侧翻自转轴/弹性缩放轴空物体（车头朝 -X；缩放轴位置即挤压 pivot；自转轴为最深层节点、绕前进轴侧翻；弹性轴为侧翻归 0 后单独应用的缩放 pivot）。
5. 自测：耗尽容器 → 后轴倒车（向右后方 + 负角，自 reverseSquashDelay 起 X 匀减速缩放）→ 等待（缩放继续，到结束缩至 reverseSquashScale）→ 前轴出车（向左前方，边前进边先甩大到 -exitMaxAngle 再转正，X 匀加速回 1，出车开始即侧翻先匀加后匀减到 rollMaxAngle）→ 转正瞬间后排补位、侧翻匀加速归 0 → 自转轴归位 → 弹性缩放（XZ 放大/Y 缩小并复原）→ 整车直行 → 销毁；
   轴未配置时回退为旧「直接销毁 + 补位」。

---

## 10. 边界情况

| 场景 | 处理 |
|---|---|
| 轴未配置（`frontAxle`/`rearAxle` 为 null） | `Play` 回退：`onRefill()` + `Destroy`，等价旧行为 |
| 缩放轴未配置（`reverseScaleAxle` 为 null） | 跳过缩放，倒车/出车照常（等价无缩放轴） |
| 自转轴未配置（`rollAxle` 为 null） | 跳过侧翻，转正后直接恢复整车驱动（等价无自转轴） |
| 关卡重载 `ClearContainers` 时小车正在倒车/出车（轴已游离） | `ContainerExitDriver.OnDestroy` 兜底：游离的轴随本体销毁，不留空物体 |
| 父物体（ContainerGroup）带 Y 旋转 | 偏航写世界 `rotation`（eulerY 世界角）不依赖父物体；侧翻写自转轴 `localRotation`（eulerX），同样不受父物体 Y 旋转影响 |
| `exitDriveDuration` 过短，未到最大速度就销毁 | 允许：仍按速度积分直行，到点销毁（加速到最大速度是上限，不是硬性到达） |
| `rollRecoverDuration ≥ exitDriveDuration` | 允许：侧翻未归 0 就被销毁，自转轴随本体一并销毁（OnDestroy 兜底） |
| `rollOutDuration = 0` | `clamp01(τ / 0)` 返回 1，侧翻瞬间到位（退化为无过渡的瞬时侧翻，建议设 > 0） |
| 弹性轴未配置（`elasticScaleAxle` 为 null） | 跳过弹性缩放，侧翻归 0 后直接整车直行（等价无弹性轴） |
| `elasticTargetScale = (1,1,1)` | 无弹性，动画退化为纯直行；`elasticScaleDuration = 0` 时 `clamp01` 返回 1，瞬间到位后复原 |
| 出车瞬间上车动画还没播完（跳车中 / 车身挂在弹性轴下） | 已由门控排除：`TryExitIfAtFront` / `IsRopeGroupReady` 都查 `IsBoarding`，动画播完会自动重新触发（§5.6） |
| `exitAngularAcceleration` 过大导致角度瞬间越 0 | `θ = min(…, 0)` 封顶，转正只发生一次 |
| `exitMaxAngle ≤ reverseAngle` | 甩头第一阶段不进入，直接加速归 0（等价旧行为） |
| `ropeExitMaxAngle ≤ 0` | 绳连后车同样跳过甩头：从 0° 直接加速归 0（退化为「原地不转头就走」） |
| 绳连后车但 `ropeRearExitEnabled = false` | 忽略变体标记，走正常出车（含倒车），参数也换回 `exit*` 那一组 |
| 绳连后车没有倒车挤压 | 出车段的「缩放恢复到 1」被跳过（否则会从 `reverseSquashScale` 凭空缩一下）。缩放轴仍参与轴链条，只是不做动画 |
| 同列多容器同时耗尽 | 每列独立出库；补位只在本列内，互不干扰 |
| 记录模式（`recordMode`） | 出库在容器侧，与记录小球颜色无关，无需特殊处理 |

---

## 11. 调参指南（初值 + 方向）

| 参数 | 影响 | 调大 | 调小 |
|---|---|---|---|
| `reverseDuration` | 倒车节奏 | 更慢、更从容 | 更快、更干脆 |
| `reverseDistance` | 倒车后退多远 | 退得更远 | 退得更近 |
| `reverseAngle` | 倒车甩角幅度 | 甩角更大 | 甩角更小 |
| `reverseWait` | 倒车后停顿 | 更明显的「顿一下」 | 几乎不停顿 |
| `reverseSquashScale` | 倒车挤压幅度（越小压得越扁） | 压得更扁 | 几乎不压 |
| `reverseSquashDelay` | 挤压起始点（从倒车开始多久后开始压扁） | 更晚开始、更急促 | 更早开始、更从容 |
| `exitScaleRecoverDuration` | 出车缩放回 1 的快慢 | 回弹更慢 | 回弹更快 |
| `exitAcceleration` | 出车起步快慢 | 起步更猛 | 起步更缓 |
| `exitAngularAcceleration` | 甩头 / 转正快慢 | 更快 | 更慢 |
| `exitMaxAngle` | 出车甩头幅度（转正前先甩到的最大角） | 甩头更夸张 | 甩头更收敛 |
| `exitMaxSpeed` | 出车最高速度 | 更快冲出 | 更慢 |
| `exitDriveDuration` | 转正后直行多久销毁 | 更晚销毁 | 更早销毁 |
| `rollMaxAngle` | 侧翻幅度（出车时的惯性侧翻角） | 侧翻更明显 | 几乎不侧翻 |
| `rollOutDuration` | 侧翻到位快慢（出车开始多久侧翻到最大） | 到位更慢 | 到位更快 |
| `rollRecoverDuration` | 侧翻归零快慢（转正后多久摆正） | 摆正更慢 | 摆正更快 |
| `elasticTargetScale` | 弹性最终缩放值（三个分量各自定义，如 XZ 放大、Y 缩小） | 各分量偏离 1 越大越夸张 | 各分量越接近 1 越收敛 |
| `elasticScaleDuration` | 弹性到位快慢（多久放大/压扁到最大） | 更慢、更绵软 | 更快、更干脆 |
| `ropeExitAcceleration` | 绳连后车出车起步快慢 | 起步更猛 | 起步更缓 |
| `ropeExitAngularAcceleration` | 绳连后车甩头 / 转正快慢 | 更快 | 更慢 |
| `ropeExitMaxAngle` | 绳连后车甩头幅度（从正姿甩到此角再归 0） | 甩头更夸张 | 甩头更收敛 |
| `ropeExitMaxSpeed` | 绳连后车最高速度 | 更快冲出 | 更慢 |
| `ropeExitDriveDuration` | 绳连后车转正后直行多久销毁 | 更晚销毁 | 更早销毁 |
| `ropeExitDriveAcceleration` | 绳连后车直行段加速 | 更快到顶速 | 更缓 |

> 绳连后车没有倒车段，所以**倒车那一组参数（`reverse*` / `exitScaleRecoverDuration`）对它完全无效**；
> 侧翻（`roll*`）与弹性（`elastic*`）因为换轴逻辑一致，对两种走法**都生效**、共用同一组参数。
| `elasticRecoverDuration` | 弹性复原快慢（到最大后多久回弹到 1） | 回弹更慢 | 回弹更快 |

---

## 12. 决策记录（已确认）

1. **驱动模型**：用「前轴 / 后轴 + 父物体切换」真实挂载，倒车挂后轴、出车挂前轴、转正后恢复整车（用户指定）。
2. **倒车运动**：匀加速二次曲线 `s = reverseDistance·p²`，每帧累积值相减得位移，沿自身 `right` 位移；角度直接赋值 `-reverseAngle·p²`。
3. **出车运动**：线性速度积分 `v = min(v + a·dt, vmax)` 全程推进；角度速度积分两段——先加速甩大到 `-exitMaxAngle`，再立即反向角速度、加速归 0。
4. **补位时机**：转正瞬间（不是销毁后），让后排更早补位、队列更连续。
5. **回退**：轴未配置时回退到旧「直接销毁 + 补位」，保证不破坏现有场景。
6. **轴归属**：轴引用挂在 `ContainerItem`（用户指定「关联到 ContainerItem 上」），动画参数挂在新的 `ContainerExitDriver`。
7. **出车甩头（修正）**：甩头不是独立阶段，而是出车转正的一部分——切前轴后，角度变化与前进同步；将原本「加速归 0」改为「先加速甩大到 `-exitMaxAngle`，再立即反向角速度、加速归 0」，只新增 `exitMaxAngle` 一个参数。
8. **倒车缩放轴**：新增 `reverseScaleAxle` 夹在驱动轴与车体之间（倒车在后轴下、出车在前轴下）；从倒车开始后 `reverseSquashDelay` 起 X 匀减速缩到 `reverseSquashScale`（覆盖剩余倒车+等待），出车开始匀加速回 1，做惯性夸张。
9. **scale 重置**：只重置「空轴」（换轴时刚取出的新轴、归位的旧轴）的 `localScale` 为 1；小车自身缩放**不重置**（pivot 与缩放轴不一致会瞬移），而是让小车全程留在缩放轴下、避免被烘。
10. **侧翻自转轴**：新增 `rollAxle` 作为最深层节点（缩放轴之下、车体之上）。侧翻自出车开始即用时间时钟驱动（`p = clamp01(τ / rollOutDuration)`，先匀加速后匀减速到 `rollMaxAngle`，到点后保持）；转正后保留自转轴作父物体，侧翻角匀加速归 0 后自转轴才归位。
11. **弹性缩放轴**：新增 `elasticScaleAxle`，在侧翻归 0 后**单独应用**（其他轴均已归位为小车子物体，不共存）。用 `Vector3` 直接定义最终缩放 `elasticTargetScale`，在 `elasticScaleDuration` 内匀减速从 `(1,1,1)` 缩放到该值，到位后立即在 `elasticRecoverDuration` 内匀加速复原，复原后弹性轴归位、继续整车直行。
12. **绳连后车走独立变体（跳过倒车）**：绳组错峰出库时，后车本来是被前面的车「拽」出去的，再做一遍倒车会让整条链的节奏打架（后车先退再进，绳长反复伸缩）。改成跳过倒车，从正姿甩头归 0 出车。
13. **变体的换轴根节点单独配置**（`ContainerItem.ropeExitAxle`，留空退回 `frontAxle`）：绕前轴甩头会把车身左右端点大幅扫出去，而绳子的两端就在车体左右两侧——后车每个都甩一遍，绳端跟着乱晃。把转轴独立出来，就能把它摆到车体中心（或任何想要的位置）来减小端点位移，**且完全不动正常出车的前轴**。交替试过「不换轴、直接转车体」，但那样车体不在自转轴 / 弹性轴下，**侧翻与弹性会一起失效**；改为「换轴照旧、只换根节点」后两段都保住了。
14. **变体的运动参数整组独立**（`ropeExitMaxAngle` / `ropeExitAngularAcceleration` / `ropeExitAcceleration` / `ropeExitMaxSpeed` / `ropeExitDriveDuration` / `ropeExitDriveAcceleration`）：没有倒车段的起始角与预压缩，正常出车那一组值在这个新起点上手感对不上，所以不共用。
15. **实现上不复制换轴与运动学代码**：`Run` 按 `ropeRearExit` 把六个参数选进局部变量，并把驱动轴也选成一个局部变量 `drive`（正常出车 = `front`，绳连后车 = `ropeAxle`），两个循环的位移与偏航都写 `drive`、转正收尾用 `drive.SetParent(transform, true)` 归位；倒车段抽成 `ReverseAndSwitchAxle` 只在正常出车时 `yield return`。这样两条路径不再分叉，将来改公式或改换轴只改一处。
16. **变体带总开关 `ropeRearExitEnabled`**：留着方便对比两种出车观感（关掉即等价于「绳组后车也用原来的倒车出车」），不必回滚代码。
17. **出车的前置条件「上车动画必须已播完」由门控保证，不再靠调用链自觉**：`cartParent` 只在 `Run` 开头读一次，读到时车身必须已是 ContainerGroup 的子物体。补位侧倾那条早有 `isRefilling` 挡着，**上车动画那条一直没人挡**。而这里的坑是**上车动画有两段**：车变空是瞬间的（`Consume` 一执行 `IsEmpty` 就为 true），但像素还要跳一段才落定（这段车身仍在 ContainerGroup 下）、落定后才换到弹性轴播弹性。所以只看「是否挂在弹性轴下」会漏掉跳车那一段（第一版就是这么漏的，重跑仍有一例失败）。最终判据是新增的 `ContainerItem.IsBoarding` = 跳车中 **或** 挂在弹性轴下，`TryExitIfAtFront` 与 `IsRopeGroupReady` 同查它（§5.6）。
18. **不让驱动侧兜底**（不去 `Run` 里等车身不在轴下再取 `cartParent`）：门控版本让车压根不进入出车流程，避免「`grid[col,0]` 已清空但车还在原地」的窗口被拉长；并且「等形变播完」本来就是既定规则，门控只是把它落到判据上。自愈性由既有的 `WaitForElasticIdle → onBoarded → OnPixelConsumed → TryExitIfAtFront` 顺序保证，不需要轮询或超时（§5.6）。
