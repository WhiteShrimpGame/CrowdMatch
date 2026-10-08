# CrowdMatch「同色连成一片 → 惊讶表情」设计文档

> 状态：**已实现**（代码已落地、两个程序集离线编译 0 错误；**表现需在 Unity 里按 §11 验收**）。
> 需求原文：
> 「当问号等机制揭示的颜色，或管道、盒子等机制动态产生的 Pixel 颜色，与原本区域内已经存在且揭示的
> 相同颜色连成更大范围的组时，在原本区域范围内和新产生的范围内各随机一个 Pixel 播放 `EmojiSurprise` 表情。
> 如果原本多个分隔的区域被连成一整片，则每个原本分隔的区域内都随机产生一个表情（新产生的范围仍随机 1 个表情）。」
>
> 相关文档：`Docs/EmojiSystemDesign.md`（表情系统总设计，本文是它的**新增触发源**）；
> 相关文件：`EmojiManager.cs`、`PixelGroup.cs`、`PixelItem.cs`、`PipeItem.cs`、`BoxItem.cs`、
> `ElevatorItem.cs`、`GameController.cs`。

---

## 1. 目标与一句话口径

**一句话**：一次「动态事件」把一批像素加进网格（管道推波 / 箱子放货 / 升降台升起）或把一批像素的颜色
显出来（问号揭晓）之后，如果这批像素里有谁和**网格上原本就已经显色的同色像素**连成了同一个 4 连通块，
就在**每个被并进来的原有区域**里随机挑 1 颗、在**这批新像素**里随机挑 1 颗，各播一个惊讶表情。

**判定必须“事后一次”做**，不是「新像素每挨着一颗旧像素就播一次」——后者会因为一个区域挨着多颗旧像素而刷屏。

---

## 2. 术语与口径表

| 术语 | 定义（本功能的判定口径） |
|---|---|
| **动态事件** | 一次「把像素放进网格」或「把像素的颜色显出来」的动作。见 §5 接入点表 |
| **新像素（newPixels）** | 本事件**刚放进网格**或**刚揭晓**的那批像素。由发起方显式传入，不靠 diff 猜 |
| **原有区域（oldRegion）** | 事件发生**之前**网格上就已经显色的同色 4 连通块。等价于「把新像素整批当成隔绝物之后」的连通块（§4 步骤 3） |
| **合并组（mergedGroup）** | 事件之后，同时含 ≥1 新像素、≥1 旧像素的同色 4 连通块。「连成更大范围的组」指的就是它 |
| **显色（颜色可见）** | 像素的颜色玩家能看见。**排除**：未揭晓问号（显问号材质）、被冰组冻住（冰面盖着）、被木箱盖住（渲染器关闭） |
| **就位（settled）** | 像素的 `placing == false`（`MarkPlaced()` 之后）。未就位的像素**不参与连通**（§7 边界 1） |

> 「同色」按 `PixelItem.colorId` 判；问号像素在**揭晓前不参与**（它的颜色对玩家不可见，两侧同色区域看起来是断开的）。

---

## 3. 触发条件（三个必要条件）

必须**同时**满足，才播表情：

| # | 条件 | 反例（不播） |
|---|---|---|
| 1 | 本事件有**新像素**落在网格里 | 箱子 `hiddenPixels` 为空 → 直接消失，不触发 |
| 2 | 新像素里**至少有一颗**与原有区域同色相连 | 管道推到一片空白区、周围没有同色已显色像素 → 不播 |
| 3 | 连通后该组**严格变大**（即合并组里既有新像素也有旧像素） | 本批新像素自己连成一块、但谁也不挨着原有区域 → 不播 |

**不掷概率**（必出，与「点击受阻生气」同一个风格）：条件满足就播，避免「明明连成一片却没反应」的困惑。
抑制交给 §7 边界 6 的逐像素判重。

---

## 4. 判定算法

### 4.1 伪码

```
Notify(group, newPixels, tag):
    if group == null || group.grid == null || newPixels 为空 → return
    emoji = EmojiManager.Instance;  if emoji == null || tag 为空 → return
    if group.suppressMergeSurprise → return            // 关卡加载期抑制，见 §7 边界 5

    // 步骤 1：只留「真正落在网格里」的新像素（管道波次可能在动画期间就被匹配移出）
    newSet = { p ∈ newPixels : p != null && group.IsInRange(p.gridX,p.gridZ)
                               && group.grid[p.gridX,p.gridZ] == p }
    if newSet 为空 → return

    // 步骤 2：网格上「颜色可见且已就位」的像素全集 V
    //   逐格扫 group.grid，留下满足全部条件的：
    //     · 在范围内、非 null
    //     · !placing                       （未落地的不算，见 §7 边界 1）
    //     · !IsFrozenCell                  （冰盖着的颜色看不见）
    //     · !IsCrateCell / !IsCovered      （木箱盖着的看不见）
    //     · !(isQuestion && !revealed)     （问号未揭晓，颜色看不见）
    //     · !IsBlocked                     （障碍格上不该有像素，兜底）
    V = { ... }

    // 步骤 3：两组连通块（同色 + 4 连通 + 只在 V 内扩散）
    compsAll = 分量(V)                 // 把 newSet 也算进来 → 事件「之后」的组
    compsOld = 分量(V \ newSet)        // 把新像素当隔绝物 → 事件「之前」的原有区域

    // 步骤 4：逐个合并组出表情
    emojis = 0
    foreach G in compsAll:
        if G ∩ newSet == ∅            → continue      // 跟本事件无关的组
        olds = { C ∈ compsOld : C ⊆ G }                // 被这块新像素并进来的「原本分隔区域」
        if olds == ∅                  → continue      // 新像素自成一块，没有连成更大范围
        foreach C in olds:  emoji.PlaySurpriseEmoji(C 里随机一颗)      // 每个原有区域各 1 个
        emoji.PlaySurpriseEmoji(G ∩ newSet 里随机一颗)                 // 新产生的范围 1 个
```

### 4.2 为什么「olds 可能 > 1」

这是需求里「原本多个分隔的区域被连成一整片」的直接体现：新像素像一座桥，把原本互不相邻的
C₁、C₂、C₃ 三块同色区域焊成了一个 `G`。此时 `compsOld` 里 C₁/C₂/C₃ 仍是三块，于是三块各出一个表情，
新像素那边再出一个 —— 一次事件共 4 个。

### 4.3 为什么不需要「事件前快照」

`compsOld = 分量(V \ newSet)` 就是事件前的分组：把**本批新像素整体当隔绝物**，剩下的像素之间的
连通关系与事件前完全一致（事件只加了这一批像素，没动别的）。所以判定只需在事件**之后**跑一遍，
不必在事件前存一份分组再比对。

### 4.4 「随机一颗」的挑法

与既有候选口径一致（`EmojiManager.PickRandomWithEmojiNode`）：在该区域的像素里**只挑配了
`emojiNode` 的**，随机取一颗；该区域一个都没配 → 这个区域跳过（不退回别的区域）。

> 由此得到一个好性质：**同一次事件里没有一颗像素会被播两次**——各 `olds` 之间两两不相交，
> `G ∩ newSet` 与它们也不相交。

---

## 5. 接入点表

**六个触发源**，都在「像素已就位 / 已显色」的那一瞬间调用 `Notify`：

| # | 源 | 代码位置 | 传什么 | 时机 |
|---|---|---|---|---|
| 1 | **管道推波** | `PipeItem.SpawnWave`（`PipeItem.cs:310`），就位循环 `:383-391` 之后 | 本波 `items`（轨道格上的 n 颗，同一颜色） | `MarkPlaced()` 循环 + `Group?.RefreshExposed()` 之后（`:396`） |
| 2 | **箱子放货** | `BoxItem.FinalizeRelease`（`BoxItem.cs:893`） | `assignments` 里通过复核的全部 `pixel` | `group.RefreshExposed()` 之后（`:915`） |
| 3 | **升降台升起** | `ElevatorItem.RiseGroupRoutine`（`ElevatorItem.cs:534`），`MarkPlaced` 循环之后 | 本组 `pixels` | `group.RefreshExposed()` 之后（`:579`） |
| 4 | **问号揭晓** | `PixelGroup.RefreshExposed`（`PixelGroup.cs:1009`） | 「本次调用里由未揭晓 → 揭晓」的那些像素 | 循环内预判收集，循环后集中 `Notify`（`:1249`） |
| 5 | **冰化开** | `PixelGroup.NotifyClickMovedOut`（`PixelGroup.cs:750`） | 刚融化那组 `CellSet` 上的像素（`CollectIcePixels`） | `RefreshIceState` + `BuildVisual` + `RefreshExposed()` 之后（`:788`） |
| 6 | **木箱被拆** | `CrateItem.RevealCoveredPixels`（`CrateItem.cs:633`） | 木箱 `Cells` 上的像素 | `group.RefreshExposed()` 之后（`:661`） |

**必须放在 `RefreshExposed()` 之后调用**（源 1/2/3）：`RefreshExposed` 负责让像素站起来、
`SetExposed` 负责问号换材质，情绪表情落在「已经站好、颜色已经对了」的像素头上才读得通。
源 4 本身就在 `RefreshExposed` 内部，顺序上先收集再判定即可。

### 5.1 问号揭晓的收集点（细节）

`RefreshExposed` 第 4 步逐个 `item.SetExposed(active[c,r])`，揭晓发生在 `PixelItem.SetExposed`
内部（`PixelItem.cs:377-392`，`revealed = true` 在 `:385`）。所以要**在调用前**判断：

```csharp
if (item.isQuestion && !item.revealed && active[c, r])
    newlyRevealed.Add(item);          // 收集
// ...
item.SetExposed(active[c, r]);
```

不改 `PixelItem` 一行代码。

### 5.2 与「同一帧里多个生产者」的关系

一次点击可能同时触发多个箱子开箱（`PixelGroup.TryOpenBoxes` 串行判定、动画并行）。
判定次序 = 各自动画结束的次序（`FinalizeRelease` 各自调用）。**先结束的那个只把自己那批算作新像素**，
另一个还在空中的箱子像素因为 `placing == true` 被步骤 2 排除、不会被我方误当成「原有区域」（§7 边界 1）。
副作用是：先结束的箱子看到的「原有区域」比后结束的小一点，表情落点因此可能略有差别 —— 可接受。

---

## 6. 新增接口设计

### 6.1 判定器（新文件 `Assets/Scripts/Gameplay/SameColorMergeWatcher.cs`）

纯静态类，不挂 MonoBehaviour（与 `GateRegion` / `IceRegion` 同类）：

```csharp
public static class SameColorMergeWatcher
{
    /// <summary>事件入口。见 Docs/EmojiSurpriseMergeDesign.md。</summary>
    public static void Notify(PixelGroup group, IList<PixelItem> newPixels);
}
```

tag 不在参数里：`TryPlaySurpriseEmoji` 用的是 `EmojiManager.surpriseTag`（与其它表情的 tag 一样集中配在管理器上）。

### 6.2 `EmojiManager` 新增

```csharp
[Tooltip("同色连成一片时的惊讶表情在 SpawnPool 配置里的 tag")]
public string surpriseTag = "EmojiSurprise";

/// <summary>从一组像素里随机挑一个配了 emojiNode 的播惊讶表情（逐像素抑制，见 §7 边界 6）；
/// 该 tag 未在池里注册 / 全部候选被排除 / 都没配 emojiNode 时返回 false 且不播。</summary>
public bool TryPlaySurpriseEmoji(IList<PixelItem> pixels);
```

判定器只调这一个入口（不额外提供「在指定像素上播」的单颗版本——没有调用方）。
内部复用既有的 `PlayEmoji(anchor, tag, follow: true)`（`EmojiManager.cs:129`）与「只挑配了 `emojiNode` 的」
口径（`PickRandomWithEmojiNode`，`EmojiManager.cs:574`），抑制用 `HasEmoji`（`EmojiManager.cs:191`），
另用 `SpawnPool.HasTag` 先拦一道，避免 tag 未注册时逐个区域刷 `Pool Dict not contains tag` 错误。

### 6.3 `PixelGroup` 新增

```csharp
/// <summary>关卡加载期间的抑制开关（见文档 §7 边界 5）：true 时 Notify 直接返回。</summary>
[System.NonSerialized] public bool suppressMergeSurprise;
```

由 `GameController.InitLevel`（`GameController.cs:150`）在 `LevelLoader.Apply`（`:184`）之前置 `true`、
在 `pixelGroup.RefreshExposed()`（`:191`）之后置 `false`。**不这么做的话**，开局就有问号像素
邻着首排 / 连着出口空格，首次 `RefreshExposed` 会把它们揭晓 → 一进关就撒一片惊讶表情。

### 6.4 一个可选的整理（不在本需求内）

「同色 4 连通块」这段 BFS 目前在 `PixelGroup.RefreshExposed`（`:1085-1148`）、
`GameController.FloodFill`（`:1090`）、`GameController.FloodFrozenSameColor`（`:1163`）各写了一份，
判定器会是第四份。**建议**顺手抽一个共用 helper（连通规则可配：是否穿问号 / 是否穿冰冻 / 是否穿木箱），
但它是独立改动，不改也不影响正确性。

---

## 7. 边界与豁免

| # | 场景 | 处理 |
|---|---|---|
| 1 | **同一个事件批次里，别的生产者还没落地** | 未就位（`placing == true`）的像素不参与连通 → 不会被当成「原有区域」 |
| 2 | 管道波次的像素在动画期间就被匹配移出 | 步骤 1 用 `grid[x,z] == p` 复核，已移出的从 `newSet` 里剔掉 |
| 3 | 波次一条轨道被占满（`TrackEmpty()` 前提） | 波次写入的一定是空格，不会与静止像素抢位 |
| 4 | 箱子 `hiddenPixels` 为空 | 不发事件（没有新像素） |
| 5 | **关卡加载期的问号揭晓** | `suppressMergeSurprise` 抑制（§6.3） |
| 6 | 同一颗像素被连续事件反复选中 | **已实现：逐像素抑制** —— `TryPlaySurpriseEmoji` 用 `HasEmoji(pixel.emojiNode, surpriseTag)` 跳过「这颗头上惊讶还没播完」的，与「点击受阻生气」的策略一致 |
| 7 | 录制模式（`GameController.recordMode`） | **已实现：整体不播** —— 判定器第一道守卫就返回（像素原地 `Destroy`、`placing`/暴露链路都不完整，表情没意义） |
| 8 | 倍乘门裂变分身 | **不触发**。`CrowdBufferZone.SpawnGateClone`（`:905`）生成的像素只落在缓冲区，从不写 `grid`，不属于「动态产生到网格上」 |
| 9 | 升降台最后一组（`FinalizeLast`，`ElevatorItem.cs:588`） | 只是收尾（恢复地面、隐藏外框），**不产生像素** → 不发事件 |
| 10 | 未配置 SpawnPool / 该 tag | `PlayEmoji` 返回 null，静默跳过（既有行为） |
| 11 | 该区域一颗都没配 `emojiNode` | 该区域跳过，不退回别处挑（既有候选口径） |
| 12 | 网格为空 / 事件不产生任何新像素 | 步骤 1 早退 |

---

## 8. 举例（带数字）

### 8.1 箱子放货，把两块原有区域焊成一片

```
网格 5 列 × 若干行；只画第 2 排：
    col  0    1    2    3    4
row2   [3]  [3]  [B]  [B]  [3]          B = 2×2 箱子（本体占 (2,2)(3,2)(2,3)(3,3)）
                    ↑
       原有区域 A = {(0,2),(1,2)}（颜色 3）
       原有区域 B'= {(4,2)}      （颜色 3）    ← 两块原本互不相邻

箱子 colorIds = [3,3] → 容量 2。开箱后本体格可用（优先级 0），两颗落在 (2,2)、(3,2)。
```
事件后：`(0,2) (1,2) (2,2) (3,2) (4,2)` 五格同色相连 → 合并组 `G` 只有 1 个。
`compsOld` 在 G 内有 **A 与 B' 两块** → **每个抽 1 颗**（共 2 个表情），
新像素 `{(2,2),(3,2)}` **再抽 1 颗** → 本次共 **3 个**惊迟表情。

### 8.2 管道推波，两端各焊一块

```
管道轨道 = 第 2 排 col 0..2（3 格），本波颜色 5。
原有：color5 在 (0,3)（A）与 (3,2)（B）—— 两者不相邻（隔了一格 (1,3)/(2,3) 非 5）。
```
波次写入 `(0,2) (1,2) (2,2)`，与 A（下方相邻）、B（右侧相邻）连通 → `G` 只有一个，
`olds = {A, B}` → **2 个** + 新像素 **1 个** = 本次共 **3 个**。

### 8.3 管道推波，周围没有同色已显色像素

```
轨道 = 第 0 排 col 1..3，颜色 2；网格上没有任何已显色的颜色 2 像素。
```
`olds = ∅` → **不播**（本批自成一块，没有「连成更大范围的组」）。

---

## 9. 口径决策（**已确认**：全按推荐选项）

| # | 问题 | 采纳 | 取值 |
|---|---|---|---|
| 1 | **冰块融化**算不算「揭示」？ | **A** | 算。刚解冻的那批像素当「新像素」，它连上的同色已显色区域当「原有区域」（`PixelGroup.NotifyClickMovedOut`） |
| 2 | **木箱被拆**算不算「揭示」？ | **A** | 算（`CrateItem.RevealCoveredPixels`） |
| 3 | 一次事件里**多种颜色**各自成组 | **A** | 每种颜色各自判定、各自出表情对（连通块本身按颜色分开，代码里不需要按色循环） |
| 4 | **节流** | **A** | 必出 + 逐像素抑制（同一颗头上惊讶没播完就跳过）；**无全局 CD**。将来若实测刷屏，再在 Manager 上加 CD 旋钮 |
| 5 | **录制模式**下是否播 | **A** | 不播（判定器直接返回） |
| 6 | 「原本区域」的随机取点是否排除**正在播别的表情**的像素 | **A** | 不排除（只判惊讶自己的 tag）。惊讶只在生产瞬间、生气只在点击受阻时，实际重叠概率很低 |
| 7 | 管道一波里若部分像素**已被移出** | **A** | 只按剩下的像素判定（`grid` 引用复核剔掉已移出的） |

> 未采纳的备选（供日后回看）：1/2 的 B = 只在问号+管道+箱子+升降台触发；4 的 B = 再加全局 CD；5 的 B = 录制模式也播；
> 6 的 B = 排除正在播其它表情的像素；7 的 B = 整波作废。

---

## 10. 场景 / 资产配置（需你在 Unity 里做，我不写 `.asset` / `.prefab`）

| 位置 | 要求 |
|---|---|
| SpawnPoolConfig | 新增一个 tag（建议 **`EmojiSurprise`**），指到惊讶表情预制体 —— 仓库里已有未纳入版本管理的 `Assets/Emojis/Prefabs/Basic emojis/Emoji surprise.prefab`（以及描边变体 / World Space Canvas 版），可直接用 |
| EmojiManager | `surpriseTag` 填同一个 tag；需要的话在 `speeds` / `durations` 里加条目（不配则吃全局 `emojiDuration` / 原速） |
| 惊讶表情预制体 | 与其它表情同样约束：按 `scale = 1` 制作；若带 Canvas 必须是 **World Space**（否则 Console 一条 warning 且不 billboard） |
| Pixel 预制体 | 需要能出表情的像素都要有 `emojiNode` —— **没配的像素不会成为候选**，区域里一个都没配就整块跳过 |
| 场景 | 无需新增物体（判定器是静态类，走 `EmojiManager.Instance`） |

---

## 11. 验证清单（实现后）

1. Unity 编译 0 报错；两个程序集离线 `dotnet build` 均 0 errors。
2. **箱子**：造 §8.1 的局面 → 开箱瞬间，A、B' 各一颗 + 新像素一颗，共 **3** 个惊讶表情；
   三颗分属不同像素，没有一颗被播两次。
3. **管道**：造 §8.2 的局面 → 推波就位瞬间出 **3** 个；§8.3（周围无同色）→ **0** 个。
4. **升降台**：升起一组像素、其中某色与地面原有关联 → 该色出表情对。
5. **问号揭晓**：让问号块揭晓、且其真色与旁边的已显色同色区域相连 → 原有区域 1 个 + 新揭晓 1 个；
   若问号真色与邻居**不同色** → 不出。
6. **进关不撒表情**：进一个开场就带问号（邻首排/连出口）的关卡 → 开局**没有**惊讶表情（§6.3 抑制生效）。
7. **未就位不误判**：同一次点击开两个箱子 → 先落地那个的表情不会把后落地那个的像素算进「原有区域」
   （表现为表情只落在真正已显色的像素上）。
8. **逐像素抑制**：连续触发两次同一区域的合并（例如管道同色连推两波）→ 同一颗头上不会叠两张惊讶脸。
9. **录制模式**下开关箱子 / 推波 → 无惊讶表情。
10. **回归**：无任何动态机制（纯静态像素 + 点击）的关卡 → 全程无惊讶表情；
    既有三类表情（开心 / 犯困 / 生气）行为与 `EmojiSystemDesign.md` §11 的清单逐条不变。
11. **tag 未注册时**（还没往 SpawnPoolConfig 加 `EmojiSurprise`）：表情不播，且 Console **不**出现
    `Pool Dict not contains tag: EmojiSurprise`（`TryPlaySurpriseEmoji` 里 `HasTag` 先拦了一道）。
12. **编辑模式**：用编辑器工具（`PixelItemEditor` / `WallItemEditor` / `PipeItemEditor` 的重建按钮等）
    触发 `RefreshExposed` → 不播表情、不报错（判定器有 `Application.isPlaying` 守卫）。

---

## 12. 改动清单

| 文件 | 改动 |
|---|---|
| `Gameplay/SameColorMergeWatcher.cs` | **新增**（静态判定器，§4；含录制模式守卫与 `suppressMergeSurprise` 守卫） |
| `Core/EmojiManager.cs` | 新增 `surpriseTag`（默认 `EmojiSurprise`）+ `PlaySurpriseEmoji` / `TryPlaySurpriseEmoji`（逐像素抑制） |
| `Gameplay/PixelGroup.cs` | 新增 `suppressMergeSurprise`；`RefreshExposed` 收集本次新揭晓的问号像素并 `Notify`；`NotifyClickMovedOut`（冰化开）收集刚解冻的像素并 `Notify`（新增私有 `CollectIcePixels`） |
| `Gameplay/PipeItem.cs` | `SpawnWave` 就位后 `Notify(g, items)` |
| `Gameplay/BoxItem.cs` | `FinalizeRelease` 里收集就位像素并 `Notify` |
| `Gameplay/ElevatorItem.cs` | `RiseGroupRoutine` 就位后 `Notify(group, pixels)` |
| `Gameplay/CrateItem.cs` | `RevealCoveredPixels` 在 `RefreshExposed()` 之后收集箱底像素并 `Notify` |
| `Gameplay/GameController.cs` | `InitLevel`：`LevelLoader.Apply` 之前置 `suppressMergeSurprise = true`，首次 `RefreshExposed()` 之后复位 |
| `Docs/EmojiSystemDesign.md` | §5 触发点总览加一行，指向本文（**待补**） |
| `Configs/SpawnPoolConfig.asset` | **你 / 在 Unity 里做**：加 `EmojiSurprise` tag |
