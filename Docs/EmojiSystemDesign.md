# CrowdMatch「表情系统」设计文档

> 状态：**已实现**。本文描述像素/车辆在各类游戏事件下播放表情（开心 / 犯困 / 生气）的完整机制：
> 表情从 SpawnPool 生成、跟随或不跟随挂点、按 tag 播放加速、到时自动回池、以及「按锚点定向回收」。
> 涉及文件：`EmojiManager.cs`（核心，`Core/`）、`PixelItem.cs`、`ConveyorBeltZone.cs`、`ContainerGroup.cs`、
> `ContainerItem.cs`、`GameController.cs`、`CrowdBufferZone.cs`、`Conveyor/ConveyorBelt.cs`、`GameManager.cs`。

---

## 1. 概述

表情系统把「什么时候播哪个表情」这件事集中在 `EmojiManager` 一处，游戏逻辑侧只负责**上报事件**（谁上带、
谁被点击、谁匹配完）与**提供候选**，概率、节流、挑选、播放、回收全在管理器里。

- **生成**：一律经 `GameManager.spawnPool`（`SpawnPool`），tag 由调用方或管理器配置给出；未配置对象池 / tag 不存在时静默跳过。
- **挂点**：`PixelItem.emojiNode`（预制体上手工挂的 Transform，通常放头顶）。所有剧情表情都用**跟随**模式，
  即表情挂成该节点的子物体、随像素移动。
- **生命周期**：`emojiDuration` 秒后自动回池；期间若发生特定事件（像素被匹配 / 被移出 / 上传送带等）会**提前定向回收**。
- **独立性**：三类表情（开心 / 犯困 / 生气）的触发、概率、节流各自独立；生气表情的三个来源
  （点击阻挡 / 插队 / 排队）判定也各自独立，**只共用同一个 emoji（tag / 缩放 / 加速）**。

---

## 2. 概念与术语

| 术语 | 含义 |
|---|---|
| **锚点（anchor）** | 表情挂靠的 Transform，一律是某个像素的 `emojiNode` |
| **跟随模式** | `follow = true`：表情挂成锚点的子物体，随锚点移动（本项目全部剧情表情都用它） |
| **世界缩放** | 表情最终显示尺寸恒等于 `emojiScale`，与锚点自身缩放无关（生成时按 `lossyScale` 换算） |
| **登记（Booking）** | 每次播放记一条 `{自增 id, 表情实例, 锚点, tag}`，供按锚点定向回收 |
| **定向回收** | 按「锚点 + tag」把该锚点上还在播的表情提前回池 |
| **候选（candidates）** | 满足某条表情触发条件的像素集合；也是「概率 × 人数」里人数的来源 |
| **发号器 `_clickSeq`** | `GameController` 上单调递增的点击序号；同一次点击移出的整组共用一个序号 |

---

## 3. 场景配置（必须手工完成）

| 位置 | 要求 |
|---|---|
| 场景 | 建一个物体挂 `EmojiManager`，拖到 `GameManager.emojiManager`（**不再由 GameManager 自动创建**，方便策划调参）；留空则所有表情播放静默跳过 |
| SpawnPoolConfig | 至少三条：`EmojiHappy`、`EmojiSleep`、`EmojiAngry`（tag 名可在管理器上改） |
| Pixel 预制体 | 挂一个 `emojiNode`（空物体即可，放头顶）；**没配的像素不参与任何表情** |
| 表情预制体 | 建议按 `scale = 1` 制作（尺寸统一由 `EmojiManager.emojiScale` 控制）；朝向由预制体自身决定，代码不设旋转 |

---

## 4. 通用播放机制

### 4.1 `PlayEmoji(Transform anchor, string tag, bool follow = false)`

```
pool = GameManager.Instance.spawnPool         // 没有池 → 返回 null
emoji = pool.Spawn(tag, follow ? anchor : null)
  follow = true  → 挂成 anchor 子物体；localPosition 归零（归位到节点自身，也避免换模式复用时带着上次的局部偏移）
  follow = false → 挂到池根下；只取 anchor 当前世界位置
ApplyWorldScale(emoji)      // 世界缩放 = emojiScale
ApplyPlaySpeed(emoji, tag)  // 按 tag 查 speeds 表
登记一条 Booking，并排一个 emojiDuration 秒的回收
```

### 4.2 世界缩放（而非局部缩放）

锚点可能有缩放（像素被 `pg.unitSize` 缩放过、车身还会播挤压弹性），直接写 `localScale = emojiScale`
会得到「父缩放 × emojiScale」的世界尺寸。故按父节点 `lossyScale` 逐轴换算：

```
localScale.x = emojiScale / parent.lossyScale.x      // 某轴父缩放≈0 时直接取 emojiScale
```

于是表情的世界尺寸恒为 `emojiScale`。注意换算只在**生成那一刻**做一次：锚点之后若带缩放动画
（车身弹性 / 倒车挤压），表情会跟着一起被拉伸。

### 4.3 播放加速（按 tag 配置）

`speeds` 是 `{tag, speed}` 列表。命中该 tag 时，把该表情下**所有** `ParticleSystem.main.simulationSpeed`
与 `Animator.speed` 设为该倍率；**未配置的 tag 一律不写**，保持预制体自身调好的速度。

### 4.4 定时回收

自己排 `DOVirtual.DelayedCall(emojiDuration, ...)`，**不复用 `SpawnPool.SpawnDuration`**：

- `SpawnPool.Despawn` 里 `usingObjDic.ContainsKey(go)` 对已销毁对象仍会命中，紧接着的 `go.SetActive(false)`
  会抛 `MissingReferenceException`——`SpawnDuration` 内部的延时回调没有空守卫，父物体先被销毁就必然踩到；
- 自排的回调带两道守卫：`this == null`（管理器已随场景卸载销毁 → 直接返回）与 `emoji == null`
  （表情已随锚点销毁 → 只清登记，不回收）。

### 4.5 定向回收 `RemoveEmojis(Transform anchor, string tag)`

遍历登记表，匹配「锚点相同 &&（tag 为空或 tag 相同）」的记录 → `pool.Despawn(emoji, true)` 并移除登记。
表情回到池后，它那条还没到期的延时回调会因为登记已被移除而**静默跳过**（`Despawn` 的 `isTry` 分支）。

> 登记用**自增 id** 而不是对象引用做删除键：两个都已销毁的 Unity 对象会被 `==` 判为相等，
> 用引用比对会误删别人的登记。`RemoveEmojis` 里也是先判 `emoji == null` 再比锚点，同一个原因。

---

## 5. 触发点总览

| 表情 | tag（默认） | 触发时机 | 候选 | 概率 | 节流 | 提前回收时机 |
|---|---|---|---|---|---|---|
| **上车开心** | `EmojiHappy` | 车上最后一个像素匹配到、准备上车（`ContainerGroup.OnLastBoarding`），且**该车在前排** | 该车全部乘客（含正在跳跃上车的）+ 当前这个尚未挂上座位的 pixel，均有 `emojiNode` | `happyChance` | 无（每次车满判一次） | 无（随车出库销毁） |
| **犯困** | `EmojiSleep` | 传送带宿主按随机间隔检测（`UpdateSleepCheck`） | ① 带上已绕圈 ≥ 1 圈的像素；② 前 `sleepCarCheckRows` 排车上等待 ≥ `sleepCarWaitSeconds` 的乘客；均有 `emojiNode` | `sleepChancePerPixel × 候选数` | 检测间隔随机 `sleepCheckIntervalMin~Max` | ① 像素离开传送带匹配上车时；② 该车匹配到最后一个像素时（收全车乘客的） |
| **点击受阻生气** | `EmojiAngry` | 点击无法移出的连通组（`GameController.PlayBlockedFeedback`） | 被点击连通组里配了 `emojiNode` 的像素 | `angryChance` | 全局 CD `angryCooldown` | ① 像素被点击**成功移出**时（遍历整组）；② 上传送带时 |
| **插队生气** | `EmojiAngry` | 某像素上带时，缓冲区里还有**更早点击且颜色不同**的像素在排队（`ConveyorBeltZone.CheckQueueJump`） | 等待队列中 `clickSeq` 更小、颜色不同、有 `emojiNode` 的像素 | `angryJumpChancePerPixel × 被插队人数` | 独立全局 CD `angryJumpCooldown` | 同上 |
| **排队生气** | `EmojiAngry` | 传送带宿主按随机间隔检测（`UpdateQueueAngryCheck`，**独立计时**） | 等待队列中 `bufferedAt` 距今 ≥ `queueAngryWaitSeconds`、有 `emojiNode` 的像素 | `queueAngryChancePerPixel × 候选数` | 检测间隔（**无 CD**） | 同上 |

三条生气来源的判定、概率、CD 全部独立，**只共用 emoji**（同一个 tag，故 `speeds` 加速与 `emojiScale` 一致）。

---

## 6. 各触发点细节

### 6.1 上车开心

`ContainerGroup.ConsumePixel` / `ProcessConsumption` 里算出 `isLast`（本像素吃掉最后一格容量）时调
`OnLastBoarding(container, pixel)`：先 `ClearSleepEmojis(container)`，再 `TryPlayHappyEmoji(container, pixel)`。

`TryPlayHappyEmoji` 的判定顺序：

```
car == null || happyChance <= 0        → 返回
car.gridZ != 0                         → 返回（不在前排：后排匹配完只是等补位）
候选为空（车上没有任何配 emojiNode 的） → 返回
Random.value > happyChance             → 返回
播放（跟随模式）
```

候选 = `ContainerItem.CollectPassengers`（**不过滤上车状态**，座位上的全算）+ 当前这个「还没挂上座位 / 走无座位
回退路径」的 `boardingPixel`，再去掉没配 `emojiNode` 的。

> 「车上最后一个像素准备上车」发生在 `StartCoroutine(MovePixelToContainer(...))` **之前**，所以此刻
> `boardingPixel` 还没挂到座位上——这正是要把它显式补进候选的原因。

### 6.2 犯困

由 `ConveyorBeltZone` 驱动，两段独立职责：

**（a）圈数统计**（每帧）——`ConveyorBelt.GetSlotPhase(i)` 暴露槽位的归一化循环相位（含追赶平移），
zone 逐槽记录上一帧相位，**相位回绕即圈数 +1**（`_laps[i]`）。空槽归零；换人（`OnSlotPassedEntry`）、
离开（`OnLeave`）、整带清空时复位。追赶的相位平移也真的把槽位沿轨迹往前推，同样计入。

**（b）定时检测**——间隔在 `sleepCheckIntervalMin~Max` 之间随机（`NextInterval`，每次检测后重排；
`Start` 里先排一次，不会开局立刻检测）：

```
候选 = 带上 _laps[i] >= 1 且配了 emojiNode 的像素
     + 前 sleepCarCheckRows 排车里，!NaN(boardedAt) 且 Time.time - boardedAt >= sleepCarWaitSeconds、配了 emojiNode 的乘客
概率 = sleepChancePerPixel × 候选数   （≥ 1 直接触发，否则掷一次）
命中 → 候选里随机一个，PlaySleepEmoji（跟随模式）
```

### 6.3 点击受阻生气

`GameController.PlayBlockedFeedback`（点击的连通组无法连通到首排时）在音效 / 震动之后调
`TryPlayAngryEmoji(blocked)`：CD 内直接返回（**连概率都不掷**）→ 组内随机一个配了 `emojiNode` 的 →
`Random.value > angryChance` 则返回 → 起 CD 并播放。

> CD **只在真正播放后**才起算：未命中概率不进 CD，下一次受阻点击仍有机会播；这样播放频率被 CD
> 上限住，又不会因为一次没中就哑火整个 CD。

### 6.4 插队生气

- 每次**成功点击移出**时（`GameController.ResolveMatch` 从网格移除那段），`++_clickSeq` 后给整组打上同一个
  `clickSeq`（同组之间因此不算插队，比较用严格小于）。
- 像素**上带**时（`ConveyorBeltZone.OnSlotPassedEntry` → `CheckQueueJump`）取缓冲区等待队列
  （`CrowdBufferZone.CollectWaiting`）交给 `TryPlayAngryEmojiForJumped(waiting, boarding)`：

```
boarding.clickSeq <= 0 或 CD 中        → 返回
候选 = 等待队列里 clickSeq > 0 且 < boarding.clickSeq（更早点击）
       且 colorId != boarding.colorId（同色不算被插队）
       且有 emojiNode
概率 = angryJumpChancePerPixel × 候选数
命中 → 候选里随机一个播放，并起独立 CD
```

### 6.5 排队生气

`ConveyorBeltZone.UpdateQueueAngryCheck`，间隔 `queueAngryCheckIntervalMin~Max` 随机、**与犯困检测各自
独立计时**（但共用同一套间隔逻辑 `NextInterval`）。到点取等待队列交给 `TryPlayAngryEmojiForQueue(waiting)`：

```
候选 = 等待队列里 !NaN(bufferedAt) 且 Time.time - bufferedAt >= queueAngryWaitSeconds 且有 emojiNode
概率 = queueAngryChancePerPixel × 候选数
命中 → 候选里随机一个播放（无 CD，节流靠检测间隔）
```

---

## 7. 数据支撑（`PixelItem` 上的运行时字段）

| 字段 | 含义 | 谁写入 |
|---|---|---|
| `emojiNode` | 表情挂点（序列化，预制体配） | 美术/策划在预制体上手工配置 |
| `boardedAt` | 上车**落定**时刻；`NaN` = 尚未上车 | `ContainerItem.BoardRoutine` 跳跃落定后、`PlacePixelInstant`（复活瞬移上车） |
| `clickSeq` | 点击序号；同一次点击移出的整组共用；`0` = 非点击移出 | `GameController.ResolveMatch`（发号器 `_clickSeq`） |
| `bufferedAt` | 进入缓冲区**物理队列**排队时刻；`NaN` = 未入队 | `CrowdBufferZone`（加入 `_physical` 时） |

传送带侧另有：`ConveyorBelt.GetSlotPhase(int)`（相位）与 `ConveyorBeltZone` 的 `_laps[]`（圈数）、
`_prevPhase[]`。缓冲区侧另有 `CrowdBufferZone.CollectWaiting`（导出仍在排队的像素）。

---

## 8. 调参一览

| 参数（所在组件） | 默认 | 作用 | 调大 | 调小 |
|---|---|---|---|---|
| `emojiDuration`（Manager） | 1.5 | 生成到回池的时长 | 表情留更久 | 表情更快消失 |
| `emojiScale`（Manager） | 0.5 | 表情**世界**尺寸 | 更大 | 更小 |
| `speeds[]`（Manager） | 空 | 按 tag 播放加速 | 动画/粒子更快 | — |
| `happyChance`（Manager） | 0.8 | 车满且在前排时的触发概率 | 更常见 | 更罕见 |
| `angryChance`（Manager） | 0.5 | 点击受阻触发概率 | 更常见 | 更罕见 |
| `angryCooldown`（Manager） | 3 | 受阻生气的全局 CD | 更少刷屏 | 更密 |
| `angryJumpChancePerPixel`（Manager） | 0.1 | 插队概率系数（× 被插队人数） | 更常见 | 更罕见 |
| `angryJumpCooldown`（Manager） | 3 | 插队生气的全局 CD（独立） | 更少刷屏 | 更密 |
| `queueAngryChancePerPixel`（Manager） | 0.1 | 排队概率系数（× 候选数） | 更常见 | 更罕见 |
| `queueAngryWaitSeconds`（Manager） | 5 | 排多久算「排队太久」 | 更晚才生气 | 更快生气 |
| `sleepChancePerPixel`（Zone） | 0.1 | 犯困概率系数（× 候选数） | 更常见 | 更罕见 |
| `sleepCheckIntervalMin/Max`（Zone） | 3 / 6 | 犯困检测间隔范围 | 检测更稀疏 | 更密 |
| `sleepCarWaitSeconds`（Zone） | 3 | 车上等多久算犯困 | 更晚 | 更快 |
| `sleepCarCheckRows`（Zone） | 3 | 只查前几排的车（0 = 不查车） | 覆盖更深 | 更浅 |
| `queueAngryCheckIntervalMin/Max`（Zone） | 3 / 6 | 排队检测间隔范围（独立计时） | 更稀疏 | 更密 |

---

## 9. 边界情况

| 场景 | 处理 |
|---|---|
| 未配置 SpawnPool / tag 不存在 | `PlayEmoji` 返回 null，静默跳过；不影响游戏逻辑 |
| 像素没配 `emojiNode` | 不进入任何候选，也不会有表情挂在它身上 |
| 车不在前排就匹配完 | 不播开心（后排只是等补位）；但**犯困表情仍会**按 6.2 规则被收掉 |
| 车上乘客全在上车途中 | `boardedAt` 为 NaN → 不算「在车上等待」，不产生犯困候选 |
| 表情锚点被直接销毁（车出库） | 表情随之销毁：池只少一个可复用实例（下次按需新建），延时回调的空守卫保证不报错 |
| 关卡重建 | `GameManager.CleanupSpawnPool()` → `spawnPool.GC(true)` 连同在用表情一并归还并裁回 preloadCount |
| 追赶把空槽相位瞬移到队首 | 空槽不参与圈数统计（直接清零），不产生虚假圈数 |
| 缓冲区里还在网格内往外走的提取中像素 | 不在 `CollectWaiting` 范围内，因此不算「更早点击被插队」、也不算「排队太久」 |
| 被点击移出的像素还带着生气表情 | `ResolveMatch` 成功移除时遍历整组定向回收 |
| 像素带着表情上了传送带 | 上带时定向回收生气表情；犯困表情则在**离开**传送带（匹配上车）时回收 |
| 表情挂在缓冲区像素上（物理推挤） | 跟随模式下随像素一起被推着走，到期自动回池；该像素上带时被回收 |

---

## 10. 决策记录

1. **挂点用 `PixelItem.emojiNode`**：由预制体显式指定，不做按名字查找（避免美术改层级就失效）。
2. **缩放写世界缩放**：父节点有缩放时直接写 `localScale` 会串味；按 `lossyScale` 逐轴换算，尺寸旋钮统一在 `emojiScale`。
3. **自排定时回收，不用 `SpawnPool.SpawnDuration`**：后者的延时回调没有空守卫，锚点先被销毁就必然抛异常（见 4.4）。
4. **登记用自增 id 做删除键**：两个已销毁对象会被 Unity 的 `==` 判为相等，用引用做键会误删（见 4.5）。
5. **候选统一要求 `emojiNode`**：保证「概率判定通过」就一定有可见结果；副作用是「概率 × 人数」的人数按**筛后**计算。
6. **全部剧情表情用跟随模式**：表情长在像素身上更自然；非跟随模式（只取位置）保留在 `PlayEmoji` 接口里备用。
7. **三条生气来源只共用 emoji**：判定/概率/CD 相互独立——点击受阻与插队各自一个全局 CD，排队靠检测间隔节流（无 CD）。
8. **CD 只在真正播放后起算**：未命中不进 CD，避免「一次没中就哑火一整个 CD」，同时播放频率仍被 CD 上限住。
9. **开心仅限前排车**：后排车匹配完只是等补位，表情留在后排没有意义。
10. **插队要求颜色不同**：同色像素不算「被插队」，且同色的不参与概率里的人数计算。
11. **`EmojiManager` 不再自动创建**：改为场景里单独挂、拖到 `GameManager.emojiManager`，方便策划集中调参。

---

## 11. 验证清单

1. Unity 编译无报错、Console 无新警告。
2. 场景配好 `EmojiManager` 引用与三个 SpawnPool tag、像素预制体挂上 `emojiNode`。
3. **开心**：前排车被喂满时按概率在某一乘客头顶播 `EmojiHappy`；后排车喂满不播。
4. **犯困**：带上绕圈 ≥ 1 圈的像素、以及车上等待超时的乘客，都会在按间隔检测时按 `系数 × 人数` 概率被选中；
   像素匹配上车时其犯困表情立刻消失；车喂满时全车乘客的犯困表情一起消失。
5. **点击受阻生气**：点击无法移出的连通组 → 有概率播 `EmojiAngry`；CD 内再次点击不播；同一像素后来被成功移出时表情被收掉。
6. **插队生气**：让后点的像素先被传送带取走 → 缓冲区里更早点击、颜色不同的像素有机会播 `EmojiAngry`；独立 CD 生效。
7. **排队生气**：让像素在缓冲区滞留超过 `queueAngryWaitSeconds` → 按间隔检测有概率播；与被插队判定互不影响。
8. 表情尺寸与 `emojiScale` 一致（把锚点父级缩放改成非 1，尺寸不变）；`speeds` 里配了加速的 tag 播得更快，
   未配置的保持预制体原速。
9. 连续多次回池复用不出现「残留偏移 / 缩放漂移 / 池里空引用报错」。
