# CrowdMatch「潜在性能问题」排查文档

> 状态：**分析 + 已落地 3 处最小改动**（P7 / P10 / P11，见下）。
> 排查日期：2026-10-08
>
> **后续更新（2026-10-08）**：
> - **已改码（3 处，两个程序集离线编译均 0 错误）**：
>   **P7** 物理帧里的 `GetComponent` → `PixelItem.bufferBody` 缓存（§3.2）；
>   **P11** UI 计数 / 进度文本改为「值没变就不刷新」（§3.6）；
>   **P10** 外层循环裁到 `maxOpenRows`（§3.1）—— 同时**更正**了原判定：实际量级远小于原估，
>   且原建议的「`gatheredItems.Count == 0` 早退」会破坏开盖，**已作废**。
> - **已处理 / 免做**：**P1** 已禁用（`FrameItem` 不再被调用，§2.1）；**P2** 已在场景 / 预制体关闭调试开关（§2.2）
> - **已定方案、未改码**：**P13** `IceItem` 改为仅关卡开始时重建一次（§4.1）
>
> 其余 **P3~P6、P8、P9、P12、P14~P16** 均**未处理**。改动清单见 §7。
> 目标平台：移动端 / WebGL（`GameManager.cs` 设 `Application.targetFrameRate = 60`、`vSyncCount = 0`）
> 范围：`Assets/Scripts/Gameplay`、`Assets/Scripts/Core`、`Assets/Scripts/DailyBonus`、`Assets/Scripts/SpawnPool`
> （编辑器工具单列 §5，它们不影响运行时帧率，但影响迭代速度）

---

## 0. 怎么读这份文档

**这份文档全部结论来自静态读代码，没有 Profiler 数据。** 我无法启动编辑器，所以：

- 每条都给了 `文件:行号`，**都是我自己读过并核对过的**；少数标注「未复核」的会写明。
- 严重度是**按代码结构判断的**（调用频率 × 单次代价），不是实测。真实影响取决于关卡规模
  （格子数 / 像素数 / 传送带槽数），所以每条都写了「什么时候会疼」。
- **没有读过任何 `.prefab` / `.unity` / `.asset`**（按既有约定），因此有几件事我无法确认，
  已在 §6 列出。其中最关键的一条：**像素预制体上到底有没有常驻 `Rigidbody` / `Collider`** ——
  这个数字直接决定 §3.2 的严重度。

**建议的第一步不是改代码，是先量。** 用 Unity Profiler 抓一次「满屏像素 + 点击移出一大片」
的帧，看 `GC.Alloc` 那一列和 `Physics.Processing` 的占比，再去 §2/§3 里找对应项。§7 给了具体的抓帧建议。

### 严重度速览

| # | 问题 | 频率 | 判定 | 参考 |
|---|---|---|---|---|
| P1 | `FrameItem.Build()` 每次点击全量 `Destroy` + `Instantiate` 描边块 | — | ✅ **已禁用**（FrameItem 不再被调用） | §2.1 |
| P2 | 6 个调试开关默认全开，其中 `debugMoveLog` 在逐像素热路径打日志 | — | ✅ **已处理**（场景 / 预制体已关，无需改码） | §2.2 |
| P3 | `LevelLoader` 一次加载调 `RebuildGrid()` **16 次** | 每次进关 | 🟠 | §2.3 |
| P4 | `BoxItem.TryOpen()` 每次点击空转重算 + 5 万预算回溯 + 默认开日志 | **每次点击 × 每个未就绪的箱子** | 🟠 | §2.4 |
| P5 | `PixelGroup.RefreshExposed()` 每次点击大块分配 + `O(格数×管道数)` BFS | 每次点击 | 🟠 | §2.5 |
| P6 | `SameColorMergeWatcher.Notify()` 两遍全盘 BFS + 按像素数分配 | 每次动态事件 | 🟡 | §2.6 |
| P7 | `CrowdBufferZone.FixedUpdate` 在物理帧循环里 `GetComponent<Rigidbody>()` | 每物理帧 × 每个物理像素 | ✅ **已处理**：改读 `PixelItem.bufferBody` 缓存 | §3.2 |
| P8 | `SweepOnce()` 每次 sweep 大块分配 + 在 `while` 里 `Sort` | 每个提取 tick | 🟠 | §3.4 |
| P9 | `CanExit()` 逐 seed 重算 `MinActivePipeTrackRow()` / `MustWalkToGate()` | 每个提取 tick × 每个像素 | 🟡 | §3.5 |
| P10 | `ContainerGroup.Update → ProcessConsumption` 每帧扫车盘 —— **原判定的复杂度与建议均已更正**（实际远小于原估，且原建议的早退会破坏开盖） | 每帧 | ✅ **已处理**：外层循环裁到 `maxOpenRows` | §3.1 |
| P11 | `GameController.UpdateCountText` 每帧字符串拼接 + 写 UI `Text` | 每帧 | ✅ **已处理**：值没变就不拼串 / 不赋值 | §3.6 |
| P12 | `StepExtracting` 每帧 `new List<ExtractState>` | **每帧** | 🟡 | §3.3 |
| P13 | `IceItem` 每次融化 `Instantiate`/`Destroy` 角块 + 重建 Mesh | 每次融化 | 🟡 **已定方案**：仅关卡开始重建，去掉动态重建（§4.1） | §4.1 |
| P14 | `EmojiManager` 每次播放都扫一遍层级 | 每次表情 | 🟢 | §4.2 |
| P15 | `AudioManager.GetConfigItem` 线性查找 | 每次播音 | 🟢 | §4.3 |
| P16 | `ContainerRopeLink.LateUpdate` 每帧逐节写 transform | 每帧 × 每条绳 | 🟢 | §3.7 |

---

## 1. 每帧到底在跑什么

先把「常态化」的开销摆出来，这是理解后面所有条目的底。

| 组件 | 每帧做什么 | 复杂度 |
|---|---|---|
| `ConveyorBelt.Update` | 推进相位 + 逐槽写 carrier / cell 的 position+rotation | `O(槽数 × 轨迹段数)` |
| `ConveyorBeltZone.Update` | 逐槽圈数统计（+ 间隔触发的犯困 / 排队生气检查） | `O(槽数)` |
| `CrowdBufferZone.Update` | `StepExtracting()` + `TryRelease()` | `O(批次×像素)`，见 §3.3 |
| `CrowdBufferZone.FixedUpdate` | 逐物理像素设速度 + 转向（已改：读缓存的刚体，不再 `GetComponent`） | `O(物理像素)`，见 §3.2 |
| `ContainerGroup.Update` | `ProcessConsumption()` 扫车盘（已改：只扫前 `maxOpenRows` 排） | `O(列×maxOpenRows²)`，见 §3.1 |
| `GameController.Update` | 每帧刷两个 UI 文本（已改：值没变就不写） | 见 §3.6 |
| `PipeItem.Update` | 检查轨道是否空 → 空了才起协程 | `O(轨道格)` |
| `ContainerRopeLink.LateUpdate` | 逐绳节重铺 | 见 §3.7 |
| `BillboardOutline.LateUpdate` | 每个描边面片一次 `Quaternion.FromToRotation` | `O(描边实例)` |
| `ContainerItem` 若干协程 | `while(true){ … yield return null; }` 轮询 | 见 §3.8 |

**好消息（已经做对的部分，不要在重构里弄坏）：**

- **运行时完全没有 LINQ**（`Gameplay` / `Core` / `DailyBonus` 下 `using System.Linq` 零命中）。
- 没有 `Resources.Load`、没有 `Camera.main` 逐实例查找（`BillboardOutline.cs:20-35` 做了每帧一次的共享缓存）、
  `EmojiBillboard.cs:22-24` 在 `OnEnable` 缓存相机。
- `find` 类调用只有三处且都不在每帧路径上（`GameController.cs:149-153` 启动时、`LevelSkipButtons.cs:34` 按钮、`ElevatorItem.cs:126` 见 §2.3）。
- `CornerTileTable` 的表在静态构造里建一次（`:211-231`），`Resolve` 是 256 长度数组下标（`:234-239`）。
- 敌人级隐患——`renderer.material` 材质实例化——**没有命中**：`IceItem.cs:383` 用的是 `sharedMaterial`，`FrameItem` 用共享 sprite。
- 打击乐式的 `PlayOneShot` 每帧分配也没有：`AudioManager` 用的是 `source.Play()` + 按 tag 池化 `AudioSource`。

---

## 2. 高危：重复重活

### 2.1 P1 · `FrameItem.Build()` 每次点击全量销毁重建 ✅ **已禁用**

> **更新（2026-10-08）：已确定 `FrameItem` 不再被调用，本条排除。**
>
> 机制上它之所以归零：`RefreshFrame()` 的第一行是 `if (frameItem == null) return;`
> （`GameController.cs:248`），`frameItem` 为空时整条链的成本为 0。
>
> **下面保留的是「它原本为什么贵」的记录**，供将来万一重新启用描边时参考——
> 注意它当时有两个不同的触发频率：**每次成功点击**（`:1227`、`:1259`）与**每次进关**（`:225`），
> 且与 §4.1 的 `IceItem` 角块是同一个「同图集角块绕开 `SpawnPool`」的模式（§4.1 那条**独立成立、未禁用**）。

**位置**：`Gameplay/FrameItem.cs:88-114`（`Build`）、`:117-130`（`Clear`）、`:138-157`（`Spawn`）
**调用方**：`GameController.cs:1227`（每次成功点击移出后）、`:1259`（拆木箱后）、`:225`（进关）

```csharp
// FrameItem.Build()
Clear();                       // ← 先 Destroy 掉上一批全部角块
...
for (int col = 0; col <= cols; col++)
  for (int row = 0; row <= totalRows; row++) {
      if (Resolve(c, out int sprite, out float yaw))
          Spawn(col, row, sprite, yaw);     // ← 每个角块一次 Instantiate
  }

// Spawn() 里
var copy = Instantiate(atomicObject, transform, false);
```

**代价**：整盘描边的角块数约为 `(列+1)×(排+1)`，比如 7×13 的盘面就是 104 个候选角块，
实际可见的通常几十个。**每一次成功点击**都要把这几十个 `Destroy` + 重新 `Instantiate`
一遍 —— `Destroy` 是延迟到帧末的，`Instantiate` 是一次完整的预制体实例化 + 层级挂载。
点击是玩家的主要交互，也就是**每秒可能好几次**。

**为什么当时是最容易改的一条**：项目**已经有 `SpawnPool`**（`SpawnPool/SpawnPool.cs`），
而且别的地方在用（融化特效 `IceItem.cs:517`、表情 `EmojiManager.cs:143`、车体 `ContainerGroup.cs` 的池化复用）。
唯独同图集的描边角块和冰的角块绕开了池。

**建议（若将来重新启用描边）**：`FrameItem` 改成池化。角块只有「sprite + 朝向 + 位置」三个自由度，
可以把 `SpawnPool` 里按 sprite 索引缓存一批实例、只改 sprite / 旋转 / 显隐，多余的 `SetActive(false)`。
**改动量中等（要动 `FrameItem` 的 Clear/Spawn 两处），收益立竿见影。**
（§4.1 的 `IceItem` 角块虽然是同一个模式，但它走了另一条路 —— 直接去掉动态重建，不需要池化，见那里。）

另外 `Build()` 每个角块都 `new int[4]`（`:101-107`）—— 注意这是**常量数组初始化**，
Roslyn 会把它提到 `<PrivateImplementationDetails>` 里缓存，**不产生每次分配**，别误改。

---

### 2.2 P2 · 6 个调试开关默认全开，其中一个在逐像素热路径 ✅ **已处理**

> **更新（2026-10-08）：已在场景与预制体中关闭，无需改代码。** 本节保留作为「为什么它值得关」的记录。
>
> 一处**遗留提醒**（不是待办，知会即可）：这些开关是**序列化字段**，代码里的初始化
> 仍是 `= true`。场景 / 预制体上的值会覆盖代码默认值，所以现有关卡已经安全；
> 但**今后新建**的组件实例、或从零搭的新场景，仍会带着 `true` 出生。
> 哪天发现「新关卡的 Console 又开始刷屏」，回来把初始化改成 `false` 即可。

**位置**：全部 `public bool … = true`

| 开关 | 位置 | 打日志的时机 | 单次代价 |
|---|---|---|---|
| `debugMoveLog` | `CrowdBufferZone.cs:116` | **每个 sweep 里每颗被选中的像素** | 见下，最贵 |
| `debugClickLog` | `GameController.cs:103` | 每次点击 | 中 |
| `debugFailLog` | `GameController.cs:107` | 每个失败检查点（有去重） | 中 |
| `debugLog` | `PipeItem.cs:34` | 每次蛇头前进 / 等待 | 中（含遍历全部寻路块） |
| `debugOpenLog` | `BoxItem.cs:68` | **每次开箱尝试**（见 §2.4） | 中 |
| `debugLog` | `ElevatorItem.cs:56` | 每次升降台推进 | 中 |

**最严重的是 `debugMoveLog`**（`CrowdBufferZone.cs:760-772`）：

```csharp
if (debugMoveLog && winner.item != null)
{
    Vector3 lp = winner.item.transform.localPosition;
    Vector3 expect = _extractGroup.GetLocalPosition(winner.col, winner.row);
    Debug.Log("[寻路] 批次#" + _batches.IndexOf(batch) + " " + winner.item.name +
        "(id=" + winner.item.GetInstanceID() + ")" +
        " 当前格(" + winner.col + "," + winner.row + ") → 目标格(" + cell.x + "," + cell.y + ")" +
        " 当前位置=" + lp.ToString("F3") +
        (Vector3.Distance(lp, expect) > 0.001f          // ← 为了日志额外算的距离
            ? "  ⚠偏离格心 " + Vector3.Distance(lp, expect).ToString("F4") + … : "") +
        " wait=" + winner.waitCount);
}
```

这一条命中的是**波前传播阶段每颗被空格选中的像素**，即每次 sweep、每颗像素一条。代价构成：

- **6 次以上字符串拼接** → 每次至少 5~7 个临时 string 对象；
- `_batches.IndexOf(batch)` —— 线性扫批次列表；
- **两次 `Vector3.Distance()` 只是为了写日志**；
- `ToString("F3")` 走格式化（比默认 `ToString` 慢得多）；
- `Debug.Log` 本身要走到 native，**编辑器下默认带托管堆栈**（`ScriptStackTraceLogType`），
  单条成本可达几十微秒。真机上虽然不带栈，但字符串分配照样进 GC。

**也就是说：一个几百像素在提取的关卡，默认配置下每秒会打出几百到上千条这样的日志。**
这不是「有点吵」，是会直接把帧率吃掉、并且持续制造 GC 垃圾。

**建议（按性价比排序）**：
1. **把 6 个开关默认值改成 `false`**，一行一个，只有要排障时手动勾上。**这是本次文档里性价比最高的改动。**
2. 再进一步：把 `Debug.Log` 正文用 `if (debugMoveLog)` **完整包住**（现在只包了条件，条件为真才拼串，
   这点是对的）—— 但要确认**没有任何一个 `Debug.Log` 的参数被求值在守卫之外**。
3. `BoxItem` 的日志同理（见 §2.4）。

> 注：`debugFailLog` 在 `Docs/FailDetectionReview.md` §5 里被定位成排障工具，开关默认开是当时的有意选择。
> 本文的建议是**它作为工具保留、但默认关**，排障时再开 —— 这需要你拍板。

---

### 2.3 P3 · `LevelLoader` 一次加载重建网格 16 次 🟠

**位置**：`Core/LevelLoader.cs:50-67`（`Apply`）+ 各 `ApplyXxx` 末尾的 `pg.RebuildGrid()`

`grep -n RebuildGrid LevelLoader.cs` 命中 **16 处**（`:146,156,169,182,195,208,221,237,250,266,279,292,305,318,331`，外加 `:373` 的车组重建）。
每个 `ApplyXxx` 都是「改一批数据 → 立刻 `RebuildGrid()`」，下一个 `ApplyYyy` 又把它全部推翻重来。

`PixelGroup.RebuildGrid()`（`:165-200`）单次代价不小：

- 分配 **9 个 `[columns, TotalRows]` 数组**（`grid` / `wallGrid` / `pipeGrid` / `boxGrid` / `crateMask` / `gateGrid` / `gateRegionMask` / `gateMultiplier` / `iceFrozenMask`）+ 一个 `columns×TotalRows` 的填充循环；
- 重建 **6 个 `List`**；
- **8 次 `GetComponentsInChildren<T>()`** 全层级扫描（`PixelItem` / `WallItem` / `PipeItem` / `GateItem` / `BoxItem` / `ElevatorItem` / `IceItem` / `CrateItem`）；
- 每道门跑一次 `GateRegion.ComputeRegion`（网格 BFS，`PixelGroup.cs:266`）。

× 16 次 = **144 个二维数组、128 次层级扫描**，绝大部分中间结果是立刻被丢弃的。

**顺带一条**：`RebuildGrid` 里当 `elevators.Count == 0` 时会调
`PixelGroup.RestoreDefaultGroundMaterial`（`:322`）→ `ElevatorItem.cs:126` 的
**`Object.FindObjectsOfType<Renderer>()`** —— 全场景渲染器扫描。这条在 16 次重建里会被触发多次。

**建议**：`Apply` 里加一个「批量模式」——8 个 `ApplyXxx` 全部跑完**只 `RebuildGrid()` 一次**
（把各 `ApplyXxx` 末尾的调用挪到 `Apply` 末尾；有 truly 需要中间态的（如冰 / 木箱盖像素要看到已存在的像素）
用一个 dirty 标记延后到 `Apply` 收尾）。这条属于**进关一次性**开销，收益是「进关更快」而不是「帧率更高」，
优先度低于 §2 里那几条每帧 / 每次点击的，但改动很干净。

---

### 2.4 P4 · `BoxItem.TryOpen()` 每次点击空转重算 + 5 万预算回溯 🟠

**位置**：`Gameplay/BoxItem.cs:344-422`（`TryOpen`）→ `:509-554`（`PlanAssignments`）→ `:595-637`（`SolveConnected`）→ `:686-741`（`GrowConnectedRec`）→ `:743`（`CanonicalKey`）
**调用方**：`PixelGroup.TryOpenBoxes()`（`:1922`），**每次移出像素后调用**

两个独立的问题：

**(a) 未就绪的箱子每次点击都白算一遍。** `TryOpen` 开头无条件做：

```csharp
var body = new List<Vector2Int>();        EnumerateBody(body);
var adjacent = CollectAdjacentEmpty();    // 4 邻扫描
var connected = CollectConnectedEmpty(adjacent);   // 4 方向 BFS
var score = new Dictionary<Vector2Int, int>();     // 三趟填充
...
if (available < capacity)
    return false;   // ← 空间不足：上面全部白做，而下次点击还会再做一遍
```

空格不够时它返回 `false`，但**这个判断要先把 BFS 和三个容器全建出来**。
玩家在推箱子关卡反复点击时，每个还没就绪的箱子每次点击都重付这笔钱。

**(b) 就绪后走的回溯是真的贵。** `PlanAssignments` → `SolveConnected` 递归回溯，
预算 `BacktrackBudget = 50000`（`:95`、`:537`）。每个到达叶子（`block.Count == n`）的候选块算一次
`CanonicalKey`（`:743`），而 `CanonicalKey` 分配一个 `List`、一次 `Sort(lambda)`、
一个 `StringBuilder` 和一个 `string`，结果塞进 `HashSet<string> seen`（`:694`）。
另有 `GrowConnectedRec` 每层 `new HashSet<Vector2Int>`（`:711`）。
**最坏情况 5 万次叶子 × 每次 4 个分配**，全在点击那一帧里同步跑完 —— 这是最可能造成
「点一下箱子卡一下」的地方。另有 `CollectAdjacentEmpty` 用 `List.Contains` 嵌在循环里（`:431-448`）。

**(c) `debugOpenLog` 默认 `true`**（`:68`），`:376-384` 每次 `TryOpen` 都拼一条 6 段字符串的日志 ——
包括上面「白算一遍」的那些调用。

**建议（不动算法，先砍浪费）**：
1. **把空格判定提到建容器之前，用轻量计数**：先数一遍可用空格，`< capacity` 就直接 return，
   再建 `body`/`adjacent`/`connected`/`score`。这一步能吃掉 (a) 的绝大部分。
2. `debugOpenLog` 默认关。
3. (b) 待你决定：是给 `TryOpen` 加结果缓存（同样的盘面状态不重算），还是把 `CanonicalKey` 的
   分配去掉（用不分配的编码，比如把格子坐标打包进 `long`/`int` 的哈希键，避开 `string`）。
   这属于算法改口径，需要你先确认可以动。

---

### 2.5 P5 · `PixelGroup.RefreshExposed()` 每次点击大块分配 🟠

**位置**：`Gameplay/PixelGroup.cs:1047` 起
**调用方**：每次点击（`GameController`），以及 `BoxItem.cs:911`、`ElevatorItem.cs:574`、`CrateItem.cs:640`

每次调用：

- `new bool[cols, TotalRows]`（`:1066`，`reachableEmpty`），文档里的 BFS 还额外建 `Queue<Vector2Int>`；
- 先调 `RefreshIceState()` + `RefreshCrateState()`（`:1054`、`:1058`），各自再走 ~2 遍全网格并逐像素写 `SetFrozen`/`SetCovered`；
- 连通块的逐格 `new Vector2Int` + 每个连通分量一个 `List`/`Queue`。

**还叠了一个嵌套扫描**：`reachableEmpty` 的 BFS 每格调 `IsEmptyForExposure`（`:1090`）→
`IsActivePipeBlocked`（`:453`→`:397`），而后者**遍历全部 pipe 及其点**。
于是是 `O(格数 × 管道数)` 级别，而不是 `O(格数)`。

**另外一处重复调用**：`NotifyClickMovedOut`（`:750`）先调 `RefreshIceState()`，
紧接着调 `RefreshExposed()` —— 而 `RefreshExposed` 开头自己又会调一次 `RefreshIceState()`。
**冰状态每次点击算了两遍。**

**建议**：`RefreshIceState()` 那份重复调用直接删掉（低风险、纯浪费）。
数组与 `Queue` 改成成员缓存（按需扩容，`Array.Clear` 复用），BFS 里的 `IsActivePipeBlocked`
提到外层算一次「活跃管道覆盖掩码」再查表 —— 后者是中等改动。

---

### 2.6 P6 · `SameColorMergeWatcher.Notify()` 两遍全盘 BFS 🟡

**位置**：`Gameplay/SameColorMergeWatcher.cs:45-149`
**触发源**：每次动态事件（管道波次 / 开箱 / 升降台升起 / 问号揭晓 / 冰融化 / 木箱拆开）

每次调用：

- `new HashSet<PixelItem>`（`:49`）；
- **一遍全网格扫描**，建 `List<PixelItem> visible` + `Dictionary<PixelItem,int> visibleIndex`（`:63-85`）——
  注意这个字典的键是**像素实例**，`PixelItem` 上拿 `GetHashCode` 是有成本的；
- `new int[visible.Count]` ×2（`:90`、`:93`）；
- **`LabelComponents` 跑两遍**（`:91`、`:94`，一次含新像素、一次把新像素当隔绝物）—— 两遍连通块 BFS；
- `new int[labelCount+1]` ×3、`new List<PixelItem>[labelCount+1]`（`:97-99`），以及 `:117-118` 的又两批。

**建议**：`visible` / `visibleIndex` / 各计数数组改成成员缓存复用；两遍 BFS 若可合并则合并。
**这是「每次事件一次」而不是每帧，所以优先度低于 §2 里那几条每帧 / 每次点击的**，但它是这条链上最重的一环。
另注意 `LabelComponents` 里若也用 `IsBlocked` / `IsGateBlockedFor`，会继承 §3.5 的 HashSet 查找成本。

---

## 3. 中危：每帧的常态化开销

### 3.1 P10 · `ContainerGroup.Update → ProcessConsumption()` 每帧扫车盘 ✅ **已处理（并更正原描述）**

> **⚠️ 更正（2026-10-08）**：本节最初的判定有两处错误，已改：
>
> 1. **复杂度写重了。** 原文按 `O(列 × 排²)` 算，但 `IsOpen` 对 `row >= maxOpenRows` **立刻返回 false**
>    （`ContainerGroup.cs:317-318`），所以内层 `O(排)` 只在 `row < maxOpenRows` 时成立。
>    `maxOpenRows` 默认 4、`rows` 通常 12+，实际是 `O(列 × maxOpenRows²)` ≈ 几十次，
>    **不是**原文估的「≈500 次/帧」。它属于「可以不管」那一档，不是本批里的「最重」。
> 2. **最初建议的「加一行 `if (gatheredItems.Count == 0) return;`」是错的 —— 会破坏开盖。**
>    循环体在 `IsOpen` 通过后会无条件调 `item.OpenLid()`（`:381`），而 **`OpenLid()` 是「盖子真的打开」的唯一入口**
>    （`:322-335`，`lidOpened` 锁存、永不复位）。提前 return 会让「没有聚集像素时」车盖不再打开，
>    连带影响失败判定的门禁 6 分支 B（`HasPendingFrontTransition` 要读 `lidOpened`，
>    见 `Docs/FailDetectionReview.md` §3.1）。**该建议已作废，未采用。**

**位置**：`Gameplay/ContainerGroup.cs:357-395`

```csharp
private void Update() { ProcessConsumption(); }     // 每帧

private void ProcessConsumption()
{
    var gc = GameController.Instance;
    if (gc == null || gc.gatheredItems == null) return;

    for (int col = 0; col < columns; col++)
      for (int row = 0; row < rowLimit; row++)      // ← 改后：rowLimit = min(rows, maxOpenRows)
      {
          var item = GetItem(col, row);
          if (item == null || item.IsEmpty || item.isRefilling) continue;
          if (!IsOpen(col, row)) continue;
          item.OpenLid();                           // ← 必须保留：敞开盖子只此一处
          var pixel = FindMatchingPixel(gc.gatheredItems, item.colorId);
          ...
      }
}
```

**实际代价（更正后）**：每帧 `列 × 排` 次 `GetItem` + `IsEmpty`/`isRefilling` 属性读，加
`列 × maxOpenRows` 次 `IsOpen`（每次内部 ≤ `maxOpenRows` 步）。**全是不分配内存的数组 / 字段访问**，
量级很小，但确实是**恒定的、永不停机的**。

**已做的改动（2026-10-08）· 只裁外层循环**：

```csharp
// 只有前 maxOpenRows 排可能开放（IsOpen 对 row >= maxOpenRows 恒返回 false），
// 更深的排在这条循环里从来做不了任何事 —— 与 FindMatchableInColumn 的 limit 同口径。
int rowLimit = Mathf.Min(rows, Mathf.Max(0, maxOpenRows));
```

- **为什么安全**：`row >= maxOpenRows` 时 `IsOpen` 恒 false，且循环体在到达 `IsOpen` 之前
  没有任何副作用（`GetItem` / `IsEmpty` / `isRefilling` 全是只读判断），所以裁掉这些排**逐字节等价**。
  与既有的 `FindMatchableInColumn`（`:418` 的 `int limit = Mathf.Min(rows, maxOpenRows);`）同口径。
- **收益**：每帧迭代数从 `列 × 排` 降到 `列 × maxOpenRows`（12 排 → 4 排，约 3 倍），
  **量级仍很小，不算显著优化**，只是顺手把无效迭代去掉。
- **明确不做**：`gatheredItems.Count == 0` 早退（会破坏开盖，见上）。
  「维护一份『当前可开放的车』候选列表，车状态变化时才重算」是可行的进阶方向，
  但要动到门禁 6 所依赖的 `lidOpened` 时序，风险不成比例，**未做**。

---

### 3.2 P7 · `CrowdBufferZone.FixedUpdate` 在物理帧循环里 `GetComponent` ✅ **已处理**

**位置**：`Gameplay/CrowdBufferZone.cs:455-503`

```csharp
private void FixedUpdate()
{
    if (_physical.Count == 0) return;
    RefreshGeometry(out _, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);
    for (int i = _physical.Count - 1; i >= 0; i--)
    {
        var p = _physical[i];
        ...
        var rb = p.bufferBody;                   // ← 改后：读缓存，不再 GetComponent
        if (rb == null) { _physical.RemoveAt(i); continue; }
        ...
        rb.velocity = dir.normalized * p.bufferCrowdSpeed;
        p.transform.rotation = Quaternion.RotateTowards(...);
    }
}
```

`GetComponent<Rigidbody>()` 不是免费的（要查组件表 + 类型匹配），而这段是
**每物理帧（默认 50Hz）× 每个在缓冲区等待的物理像素**。缓冲区里堆十几到几十颗时，
这是每秒上千次 `GetComponent`。

`RefreshGeometry` 每物理帧跑一次，本身只是几个向量运算，可接受，未动。

**已做的改动（2026-10-08）**：

| 位置 | 改动 |
|---|---|
| `PixelItem.cs`（`bufferAimOffset` 之后） | 新增 `[System.NonSerialized] public Rigidbody bufferBody;` |
| `CrowdBufferZone.EnterPhysical`（`:1250-1252` 拿到刚体之后） | `item.bufferBody = rb;` |
| `CrowdBufferZone.FixedUpdate`（`:472`） | `p.GetComponent<Rigidbody>()` → `p.bufferBody` |
| `CrowdBufferZone.DetachPhysics`（`:1421`） | 销毁刚体后补 `item.bufferBody = null;` |

- **为什么安全**：像素的刚体**只由 `EnterPhysical` 添加**（全工程作用于像素的
  `AddComponent<Rigidbody>` 仅 `:1252` 一处），而 `_physical.Add(item)` 在 `:1285`，
  **排在添加刚体之后** —— 所以 `_physical` 里的每个像素必然已经写好缓存。
  刚体若被外部销毁，缓存会变成「已销毁引用」，而 Unity 的 `== null` 对已销毁对象**仍为 true**，
  于是 `:475` 那条「刚体不见了就移出物理队列」的既有分支行为**逐字不变**。
  `DetachPhysics` 的三个调用点（`:445`、`:1365`、`:1416`）都紧跟 `_physical.Remove`，清空缓存不会漏。
- **收益**：把「每物理帧 × 每像素」的 `GetComponent` 归零。

> 注意：`EnterPhysical` / `DetachPhysics` 里**也**各有一次 `GetComponent`（取刚体 / 取 `SphereCollider`），
> 那些是**事件级**（每次进入 / 离开缓冲区一次），不在每帧预算里，**保持原样未动**。

---

### 3.3 P12 · `StepExtracting` 每帧分配 🟡

**位置**：`Gameplay/CrowdBufferZone.cs:507-612`，分配点在 `:524`

```csharp
for (int b = _batches.Count - 1; b >= 0; b--)
{
    var batch = _batches[b];
    // 1. 已离开网格的像素……
    var exiting = new List<ExtractState>(batch.extracting.Count);   // ← 每批次、每帧一次新的 List
    ...
}
```

只要还有提取批次在推进，**每帧每个批次**都要建一个新的 `List<ExtractState>`（容量还是按
当前提取数给的）。加上 `HasGridPathfindingPixels`（`:200-218`）在 `:514` 和 `:610`
**每帧被求值两次**，每次全批次全像素扫描。

**建议**：`exiting` 改成成员缓存 `List`（`Clear()` 复用）。`HasGridPathfindingPixels`
在 `StepExtracting` 里只算一次存进局部变量（`:610` 复用 `:514` 之外的新状态，注意语义是
「本帧开始 / 本帧结束」两个不同时刻，**不能简单合并** —— 要保留两个求值点，但可以把
`exiting` 的分配消掉，那是这里唯一纯粹的浪费）。

---

### 3.4 P8 · `SweepOnce()` 每次 sweep 大块分配 + 在 `while` 里 `Sort` 🟠

**位置**：`Gameplay/CrowdBufferZone.cs:633-846`

每次 sweep（每个提取 tick，默认 `_extractTickInterval` 一次）分配：

| 行 | 分配 |
|---|---|
| `:649` | `ComputeExitDistance` → `int[,] dist`（`cols × rows`，`:1043`） |
| `:652` | `new ExtractState[cols, rows]` |
| `:663` | `new bool[cols, rows]`（vacated） |
| `:664` | `new bool[cols, rows]`（claimed） |
| `:666-667` | `seeds` / `exits` / `movers` 三个 `List` |
| `:698` | `new Dictionary<Vector2Int, int>`（snakeOrder） |
| `:715` | `new List<Vector2Int>`（frontier） |
| `:750` | 每一层波前再 `new List<Vector2Int>()`（next） |

**更值得注意的是算法结构**：`frontier.Sort(...)` 在 `:724` 的 `while (frontier.Count > 0)` **里面**（`:726`）：

```csharp
while (frontier.Count > 0)
{
    frontier.Sort((a, b) => {              // ← 每层重排整个 frontier
        bool sa = snakeOrder.ContainsKey(a);   // ← 每次比较 2 次字典查找
        bool sb = snakeOrder.ContainsKey(b);
        ...
        int d = dist[a.x, a.y].CompareTo(dist[b.x, b.y]);
        ...
    });
    ...
    frontier = next;                        // ← 换上一层
}
```

- 每次 `Sort` 都**新建一个闭包对象 + 委托**（`(a,b)=>…` 捕获了 `snakeOrder` 和 `dist`）；
- 比较函数里 **2 次 `Dictionary<Vector2Int,int>` 查找**，所以比较次数 × 2 次哈希；
- 波前有 D 层就 `Sort` D 次，单次 `O(W log W)`。

**另外**：`:698-712` 每 sweep 重建一次 `snakeOrder`，而它只随管道状态变化 —— 可以缓存到管道变化时。

**建议（分两档）**：
- **低风险档**：`dist` / `stateAt` / `vacated` / `claimed` 改成成员字段 + `Array.Clear` 复用；
  `frontier` / `next` / `seeds` / `exits` / `movers` 改成员 `List` 复用。
- **中等档**：把 `Sort` 移出 `while`（改成整个 sweep 一次排序 + 按层过滤），
  或换成分层 bucket（`dist` 已经是整数，本身就适合按 `dist` 分桶，根本不需要比较排序）。
  比较函数里把 `snakeOrder` 的字典查找换成「预先把 snake rank 写进一个 `int[,]`」。
  这需要你先确认可以动这段逻辑。

---

### 3.5 P9 · `CanExit()` 里逐 seed 重算全 pipe / 全 gate 扫描 🟡

**位置**：`Gameplay/CrowdBufferZone.cs:860-891`

`CanExit` 在 `:680` 的 `foreach (var st in seeds)` 里**每颗像素调一次**，而它内部每次都要：

```csharp
if (!exitOnlyFromRow0 && _extractGroup != null && _extractGroup.MustWalkToGate(col, row, out _))
    return false;                                   // ← MustWalkToGate: 遍历所有门 (PixelGroup.cs:514-534)

int minTrackRow = _extractGroup != null ? _extractGroup.MinActivePipeTrackRow() : int.MaxValue;
                                                    // ← MinActivePipeTrackRow: 遍历所有管道 × 所有点 (PixelGroup.cs:414-432)

for (int r = 0; r < row; r++)                       // ← 前方逐格 IsObstacle
    if (IsObstacle(col, r, vacated, claimed, matchedOccupied, batch)) return false;
```

`IsObstacle`（`:986`）每格会调 `IsBlocked`（4 次数组查，便宜）+
`IsGateBlockedFor`（`PixelGroup.cs:575`，内部 `HashSet<GateItem>.Contains`）。

**合计**：`O(像素数 × (门数 + 管道数×点数 + 排数))` **每个 sweep**。其中
`MinActivePipeTrackRow()` 的结果在一次 sweep 内是**常量**（vacated/claimed 不影响它），
却每颗像素重算一遍。

**建议**：把 `minTrackRow` 提到 sweep 开头算一次（纯搬移，零风险）；
`MustWalkToGate` 的「本格需不需要绕门」结果同样在 sweep 开头对全网格算一次掩码。
这两条都是搬移 + 缓存，**不改语义**。

---

### 3.6 P11 · `GameController.UpdateCountText` 每帧字符串 ✅ **已处理**

**位置**：`Gameplay/GameController.cs:871-892`

```csharp
private void Update()
{
    UpdateCountText();
    if (Input.GetMouseButtonDown(0) && GameState.IsGameStart) HandleClick();
}

private void UpdateCountText()
{
    if (gatherCountText != null)
        gatherCountText.text = conveyorZone.OccupiedSlots + "/" + conveyorZone.TotalSlots;
    if (progressText != null)
        progressText.text = GameData.ProgressPercent + "%";
}
```

三个问题：

1. **每帧两次 int→string + 字符串拼接**（`:886`、`:891`），即使数值没变。
2. `conveyorZone.OccupiedSlots` → `belt.OccupiedCount` 是**实时全槽扫描**（`ConveyorBelt.cs:443-458`）。
3. 赋给 `UnityEngine.UI.Text.text`：uGUI 的 setter **会先比较字符串**，
   值没变不会置脏（所以不会每帧重建 Canvas）—— **这一点侥幸没踩坑**，
   但比较本身要在新分配出来的字符串之间做。

**已做的改动（2026-10-08）**：缓存上一次的数值，只在**变化时**才拼串赋值。

```csharp
// GameController 私有字段（[System.NonSerialized]，不进 Inspector / 不落 YAML）
private int _lastBeltOccupied = int.MinValue, _lastBeltTotal = int.MinValue;
private int _lastGatheredCount = int.MinValue, _lastProgressPercent = int.MinValue;

private void UpdateCountText()
{
    if (gatherCountText != null)
    {
        if (conveyorZone != null)
        {
            int occupied = conveyorZone.OccupiedSlots;
            int total = conveyorZone.TotalSlots;
            if (occupied != _lastBeltOccupied || total != _lastBeltTotal)   // 值没变：不拼串、不写 Text
            {
                _lastBeltOccupied = occupied;
                _lastBeltTotal = total;
                gatherCountText.text = occupied + "/" + total;
            }
        }
        else
        {
            int count = gatheredItems.Count;
            if (count != _lastGatheredCount) { _lastGatheredCount = count; gatherCountText.text = count.ToString(); }
        }
    }
    if (progressText != null)
    {
        int percent = GameData.ProgressPercent;
        if (percent != _lastProgressPercent) { _lastProgressPercent = percent; progressText.text = percent + "%"; }
    }
}
```

- **为什么安全**：`gatherCountText` / `progressText` 全工程**只有本方法写**（`grep` 确认，
  无第二处 `Assign`），所以缓存不会与别处的写入脱节。用 `int.MinValue` 当「还没写过」的哨兵，
  **保证首帧一定写一次**。
- **收益**：消除每帧约 4 个小字符串分配（`OccupiedSlots` 那次全槽扫描保留 —— 它是 `O(槽数)`
  的纯数组读，比字符串便宜得多，没有再为它加脏标记的必要）。

---

### 3.7 P16 · `ContainerRopeLink.LateUpdate` 每帧逐节写 transform 🟢

**位置**：`Gameplay/ContainerRopeLink.cs:167-174` → `ApplyTaut`（`:187` 起）

每条绳每帧都要把 `segmentLinks` 全部重铺一遍（写 `position` + `rotation`），
中间还有叠加的正弦微晃。绳在进关时由 `ContainerGroup.BuildRopes`（`:1762`）逐组建好，
每个绳根 `AddComponent<ContainerRopeLink>()`（`:1874`）。
每个 `LateUpdate` 都是一次托管→native 的调用，绳多时有固定开销。

**好消息**：`ContainerRopeLink.cs:144` 把绳节的 `Rigidbody.isKinematic = true`，
即物理不参与驱动 —— 这条刻意设计避免了「物理和逐帧写 transform 打架」的最坏情况。

**建议**：属于「观察项」而非「问题」。若绳组数量大，可以考虑只对**可见**的绳跑 `ApplyTaut`，
或者把绳节铺排并进一个统一的驱动者（省掉每个绳根一次 `LateUpdate` 调度）。
**不建议现在动** —— 先看 Profiler 里 `BehaviourUpdate` 的占比再定。

---

### 3.8 其他常驻协程轮询

| 位置 | 形态 | 备注 |
|---|---|---|
| `ContainerItem.cs:796` `RefillRollRoutine` | `while(true){ … yield return null; }` | 每帧调 `LockBoardingPixelsWorldRotation`（`:1022`）遍历 `_boardingPixels`。有界（补位期间） |
| `ContainerItem.cs:730` `WaitForElasticIdle` | `yield return null` 自旋 | 等到弹性结束，有界 |
| `BoxItem.cs:866,874,882` | 每个隐藏像素 `new WaitForSeconds` | 每次开箱 N 次分配，见 §4.1 同类 |
| `CrateItem.cs:738` → `PixelItem.cs:252/265/275` | 每个被盖像素一个协程 + `new WaitForSeconds(delay)` | 大木箱破开时一次性起 N 个协程 |

这些是「有界的事件级」开销，不是每帧常态。上面两行「每个像素一个协程」的做法
在像素数大时会一次性创建大量 `Coroutine` + `WaitForSeconds` 对象；
**建议**：改成「一个协程按 delay 排序依次处理」而不是「N 个协程各等各的」。

---

## 4. 未池化 / 重建式资源

### 4.1 P13 · `IceItem` 每次融化重建角块与 Mesh 🟡

**Sprite 模式**：`IceItem.cs:317-332` 遍历包围盒，`Spawn`（`:597-616`）对每个有效角 `Instantiate`，
`Clear`（`:416-446`）`Destroy` 上一批。触发点是**任意冰组融化**：`PixelGroup.cs:780-782` 会对
**所有**冰组逐个 `BuildVisual`（不只是融化掉的那一个）。**模式和 §2.1 的 `FrameItem` 一样**
（同图集角块每次重建）—— 但注意 `FrameItem` 已禁用，而这里**没有禁用**，`BuildVisual` 是真的在跑，
所以本条独立成立。

**Mesh 模式**：`BuildMeshVisual()`（`:362`）每次调 `IceMeshBuilder.Build(...)`，
后者内部 `new Mesh()` + `SetVertices` + `SetTriangles` + `RecalculateNormals()` + `RecalculateBounds()`，
外加 `new HashSet`、6 个 `List`、`new Dictionary`、`bool[]` 和耳切法的临时列表
（`IceMeshBuilder.cs:178-186`、`:93`、`:109-139`、`:214`、`:225`、`:422-541`）。
`:372-392` 还 `new GameObject` + `AddComponent<MeshFilter>()` + `AddComponent<MeshRenderer>()`。
结果赋给 `_mesh` 后**每次 `BuildVisual` 重建**，只在 `Clear`（`:438-445`）里释放 —— **没有按 cells 缓存**。

**已经做对的**：`Clear` 确实 `Destroy` 了 mesh 与 mesh 物体，`:383` 用的是 `sharedMaterial`
（不产生材质克隆）。所以**没有材质 / 贴图泄漏**，问题只是「重建而非复用」。

**定案（2026-10-08）· 改为「仅关卡开始时重建一次，去掉融化路径上的动态重建」**

依据（我核对过代码，结论比预期更干净）：

- **冰面形状只由序列化字段 `cells` 决定。** `BuildVisual` 第一件事是 `RefreshCells()`（`:278`），
  而 `RefreshCells`（`:215-220`）只从 `cells` 重算 `_cellSet` / 包围盒；角块的遍历范围就是
  `_colMin.._colMax`（`:317-319`）。`cells` 在关卡加载（`LevelLoader.ApplyIces`）之后**不再改动**。
  → 所以每次融化重建出来的角块、Mesh 与上一次**完全一致**，是纯粹的重算。
- 融化只改两样东西：计数 `remaining`（`:263`）与 `Melted`（`:189`）。
- 因此动态重建在融化路径上的**可见效果只有两个**：① 计数数字刷新；②
  **整组化完时 `BuildVisual` 开头那次 `Clear()`（`:279`）把冰面抹掉**。
  注意 `UpdateDisplay` 只管**数字**的显隐（`:480-482` 只写 `countText.enabled`），
  **它不会移除冰面美术** —— 冰面的消失完完全全靠那次 `Clear()`。

据此：

1. **关卡开始**：`BuildVisual` 照常跑一次，角块 / Mesh 生成后常驻（Mesh 模式的 `_mesh` 保留到 `Clear`）。
2. **融化时**：不再调 `BuildVisual`。把 `PixelGroup.cs:780-782` 那个「对所有冰组 `BuildVisual`」的循环换成
   —— 未融化的组只调 `UpdateDisplay()`（`:773` 本来就在做），**化完（`Melted`）的组只做「移除冰面」这一件事**
   （把 `BuildVisual` 里的 `Clear()` 单独抽成 `HideVisual()` 之类的即可）。
3. **编辑器路径不动**：`IceItemEditor.cs:48,396` 与 `PixelColorBrushWindow.cs:4107,4135,4163,4207`
   的 `BuildVisual` 是编辑操作触发的预览重建，不在帧率预算里。

> **⚠️ 必须保留的一条**：整组化完时**仍要移除冰面**（第 2 条的 `Melted` 分支）。
> 若把动态重建一刀切成「什么都不做」，融化后冰的美术会留在屏幕上不走 —— 那是视觉 bug，不是优化。

**收益**：把「每次融化 × **全部**冰组」的 `Clear()` + 逐角 `Instantiate`（Mesh 模式另加
`new Mesh()` + 重建 GameObject）降为「每次融化 × 仅计数文本」；冰面重建次数从
`O(融化次数 × 冰组数)` 降到 `O(1)`（关卡开始一次）。

**顺带**：`PixelGroup.cs:773` 对每个非融化组调 `UpdateDisplay()`，其中 `remaining.ToString()`（`:484`）
分配字符串、`ApplyTextScale`（`:543-561`）每次重设 `fontSize` 与 `SetSizeWithCurrentAnchors`。
**`remaining` 没变就早退**（同理，`countOffset` / `countFontScale` 没变时 `ApplyTextScale` 也可以省）。

### 4.2 P14 · `EmojiManager` 每次播放扫层级 🟢

**位置**：`Core/EmojiManager.cs:153-154`（每次播放都调）→ `ApplyPlaySpeed`（`:503-520`）、`SetupBillboard`（`:562-585`）

- `ApplyPlaySpeed`：`GetComponentsInChildren<ParticleSystem>(true)` + `<Animator>(true)`
  —— **每次播放两次层级扫描**（仅在配了该 tag 的播放速度时，`:506-507` 有早退）。
- `SetupBillboard`：`GetComponentsInChildren<Canvas>(true)` + 每个 canvas 一次 `GetComponent<EmojiBillboard>()`
  —— **每次播放一次层级扫描**，而表情物体是**池化复用**的，第一次播放后就已配好，后续全是白扫。
- `:159` `DOVirtual.DelayedCall(..., () => ...)` 每次播放一个闭包。
- `_bookings` 的 `HasEmoji`（`:238-255`）/ `RemoveBookings`（`:430-456`）是 O(n) 线性扫。
- `TryPlaySurpriseEmoji` / `ForJumped` / `ForQueue` 每次 `new List<PixelItem>`（`:197`、`:270`、`:348`）；
  `PickRandomPassenger`（`:604-612`）同样每次建列表。

**建议**：表情对象池化后这些解析结果**缓存到实例上**（首播时解析一次，写进组件字段），
后续播放直接读。低成本、低风险。

### 4.3 P15 · `AudioManager.GetConfigItem` 线性查找 🟢

**位置**：`Core/AudioManager.cs:346-355` —— 每次 `Play` 都 `foreach (Config.items)` 比对 tag。
音效配置项通常几十个，每次播音一次线性扫。`AudioSource` 本身按 tag 池化（`:323-340`）是对的，
用的是 `source.Play()` 而非 `PlayOneShot`（**没有每次播放的分配，这点是对的**）。

**建议**：启动时建一份 `Dictionary<string, AudioItem>`。低优先度。

### 4.4 `SpawnPool` 的双重查找 🟢

**位置**：`SpawnPool/SpawnPool.cs:62,75,81,87`（`ContainsKey` 后再下标）、`:103-115`（`Despawn`）
每次 spawn/despawn 多 1~2 次哈希。相对上面几条可忽略，但既然要动池化（§4.1 的 `IceItem`），
顺手用 `TryGetValue` 收掉即可。

---

## 5. 编辑器工具（不影响运行时，影响迭代速度）

这些只在编辑器里跑，不算「游戏性能问题」，但类别和运行时那批一样，列出来备查。
**其中 #5.1 / #5.2 在你编辑大关卡时会明显卡手。**

| # | 位置 | 问题 |
|---|---|---|
| 5.1 | `GateItem.cs:421-431`（`OnDrawGizmos`）+ `:400` | 每次重绘遍历整个 `regionMask` 并逐格 `GetWorldPosition`（`TransformPoint`），还 `new HashSet<Vector2Int>` |
| 5.2 | `LevelGridBoard.cs:710`（`Solve`）、`:989`（`CanReachFront`）、`:1093`（`RefreshExposure`）、`:1288`（`PipePenalty`） | `O(组数²)` 轮全盘 BFS；`CanReachFront` 每次建 2 个 `HashSet` + `bool[,]` + `Queue`；`RefreshExposure` 每次 3 个 `bool[,]`，还嵌在最多 512 次迭代的循环里（Record 生成用） |
| 5.3 | `GateItem.cs:205`、`PixelGroup.cs:592`、`:976` | `RegionCellCount` / `CountPixelsInRegion` / `ValidateGates` 都是 `O(格数)`，后者再乘门数 |
| 5.4 | `PixelGroup.cs:1255-1419` 各 `ClearXxx` | 每个都一次 `GetComponentsInChildren`，进关时全部调用 |

`Assets/Scripts/Editor/` 下的大文件（`PixelColorBrushWindow.cs` 4572 行、
`ContainerDragWindow.cs` 1750 行、`ContainerRearranger.cs` 1122 行）本次**未逐行审计** ——
编辑器窗口的卡顿主要来自 IMGUI 每帧重绘，属于另一类问题，需要时另开一份文档。

---

## 6. 我无法确认的事（限制了上面的判断精度）

1. **像素预制体上有没有常驻 `Rigidbody` / `Collider`** —— 这直接决定 §3.2 的量级，
   也决定 `Physics.Processing` 在 Profiler 里的占比。（按约定没读 `.prefab`。）
   代码只告诉我们缓冲区进出时会 `EnterPhysical`（`:1228`）/ `DetachPhysics`（`:1421`）增删刚体与碰撞球，
   但**静置的像素是否也带碰撞体**必须看预制体。
2. **典型关卡的规模**：格子上限、单关像素总数、传送带槽数、绳组数量。
   本文的数字估算用的是「约 7 列 × 12 排、像素百级、槽位十几」，需要你用真实关卡校正。
3. **`ContainerGroup` 懒实例化下 `gatheredItems` 的典型大小** —— 决定 §3.1 里
   `FindMatchingPixel` 那层的实际权重（如果传送带模式下它恒为空，那这层几乎不花钱，
   §3.1 的「加一行早退」收益就更纯粹）。
4. **`EmojiManager.SetupBillboard` 是否在池化路径下每播必扫** —— 我确认了它每次播放都调（`:154`），
   但没确认对象池是否会在回收时清掉 `EmojiBillboard` 组件（清掉的话就不能只缓存一次）。
5. **实际帧预算**：以上全是静态推断，**没有任何 Profiler 采样**。

---

## 7. 建议的动手顺序

> **状态更新（2026-10-08）**：
> - **✅ 已改码**：第 2 条（P10）、第 3 条（P11）、第 5 条（P7）—— 见 §3.1 / §3.6 / §3.2
> - **✅ 已禁用 / 免做**：第 1 条（P2，场景 / 预制体已关）、第 8 条（P1，`FrameItem` 不再被调用）
> - **已定方案、未改码**：第 15 条（P13，`IceItem` 仅关卡开始重建，见 §4.1）
>
> 各条都保留在原编号位置上，只标状态，**不重排序号**，以免打乱下表的 `#` ↔ `P#` 对应关系。

> **编号说明**：下表的 `#` 是**动手顺序**，`P#` 是**问题编号**（§0 速览表与 §2/§3/§4 各节标题里的那个）。
> 两者**不是一一对应**：一个 P 可能拆成几步做（P5 拆成 4·14·16，P4 拆成 9·13，P8 拆成 11·12），
> 也可能两步合成同一个 P（11·12 都属 P8）。P1~P16 全部在下面出现，无遗漏。

### 第一档：一行到几行，零语义风险

| # | P# | 改动 | 位置 |
|---|---|---|---|
| 1 | **P2** | ~~6 个调试开关默认值改 `false`~~ —— **✅ 已处理**：已在场景 / 预制体关闭，无需改码（§2.2） | — |
| 2 | **P10** | ~~原建议「加 `if (gc.gatheredItems.Count == 0) return;`」~~ —— **已作废**，那会破坏开盖（§3.1 更正）。实做：外层循环裁到 `min(rows, maxOpenRows)` —— **✅ 已处理** | `ContainerGroup.cs:368-374` |
| 3 | **P11** | `UpdateCountText` 只在数值变化时赋值 —— **✅ 已处理**（§3.6） | `GameController.cs:881-902` |
| 4 | **P5** | 删掉 `NotifyClickMovedOut` 里多余的 `RefreshIceState()`（§2.5 末尾那条重复调用） | `PixelGroup.cs:750` 附近 |

### 第二档：小重构，局部风险

| # | P# | 改动 | 位置 |
|---|---|---|---|
| 5 | **P7** | `PixelItem` 缓存 `Rigidbody`，消掉物理帧里的 `GetComponent` —— **✅ 已处理**（§3.2） | `CrowdBufferZone.cs:472` + `PixelItem.cs` + `:1421` |
| 6 | **P9** | `minTrackRow` 提到 sweep 开头算一次 | `CrowdBufferZone.cs:878` |
| 7 | **P12** | `exiting` 等临时 `List` 改成员缓存复用 | `CrowdBufferZone.cs:524` |
| 8 | **P1** | ~~`FrameItem` 角块走 `SpawnPool`~~ —— **✅ 已禁用**：已确定 `FrameItem` 不再被调用，本条免做（§2.1）；同一手法可留给 §4.1 的 `IceItem` | — |
| 9 | **P4** | `BoxItem.TryOpen` 把空格判定提到建容器之前（即 §2.4 的 (a)） | `BoxItem.cs:361-386` |
| 10 | **P3** | `LevelLoader` 合并为一次 `RebuildGrid` | `LevelLoader.cs:56-63` |

### 第三档：需要你先拍板（改口径 / 改算法）

| # | P# | 改动 | 需要你确认什么 |
|---|---|---|---|
| 11 | **P8** | `SweepOnce` 的 `Sort` 移出 `while`、`dist`/`snakeOrder` 预计算 | 允许改这段寻路结构？ |
| 12 | **P8** | `SweepOnce` 的数组改复用 / 按 `dist` 分桶代替比较排序 | 同上 |
| 13 | **P4** | `BoxItem` 的 `CanonicalKey` 去分配，或给 `TryOpen` 加结果缓存（即 §2.4 的 (b)） | 允许改回溯的键编码？ |
| 14 | **P5** | `RefreshExposed` 的活跃管道掩码预计算（§2.5 的 `O(格数×管道数)` 那层） | 中等改动，动到暴露判定 |
| 15 | **P13 · P14** | **P13 已定方案**：`IceItem` 改为**仅关卡开始时重建一次，去掉融化路径上的动态重建**（保留「化完移除冰面」，见 §4.1 定案）。`EmojiManager` / `IceItem` 另可把层级解析结果缓存到实例 | P13 见 §4.1；P14 需先确认池化回收时不清组件（§6.4） |
| 16 | **P5** | `PixelGroup` 各 `bool[,]` 改成员复用 + `Array.Clear` | 要确认没有「持有上一帧残留」的读法 |

### 第四档：本次不建议动（先看 Profiler 再定）

这三条我**没有排进上面三档**，理由写在下面 —— 不是漏了，是刻意缓做。

| # | P# | 事项 | 为什么不排进去 |
|---|---|---|---|
| 17 | **P6** | `SameColorMergeWatcher.Notify()` 两遍全盘 BFS + 按像素数分配（§2.6） | 是「每次动态事件一次」而非每帧，且改动要合并两遍 `LabelComponents` 的语义，属改口径；先确认它在 Profiler 里真的占位 |
| 18 | **P15** | `AudioManager.GetConfigItem` 建 `Dictionary<tag, AudioItem>`（§4.3） | 配置项只有几十条且非每帧，收益小；顺手做即可 |
| 19 | **P16** | `ContainerRopeLink.LateUpdate` 逐绳节铺排（§3.7） | 已是 `isKinematic` 的刻意设计，无已知坏味道；需先看 `BehaviourUpdate` 占比再决定要不要合并绳根驱动 |

---

## 8. 相关代码索引

| 关注点 | 文件 | 方法 / 行 |
|---|---|---|
| 描边重建 | `Gameplay/FrameItem.cs` | `Build`(88) / `Clear`(117) / `Spawn`(138) |
| 调试开关 | `Gameplay/CrowdBufferZone.cs` / `GameController.cs` / `PipeItem.cs` / `BoxItem.cs` / `ElevatorItem.cs` | 见 §2.2 表 |
| 逐像素日志 | `Gameplay/CrowdBufferZone.cs` | `SweepOnce` 内 `:760-772` |
| 关卡加载重建 | `Core/LevelLoader.cs` | `Apply`(50) / 16 处 `RebuildGrid` |
| 网格重建 | `Gameplay/PixelGroup.cs` | `RebuildGrid`(165) |
| 开箱规划 | `Gameplay/BoxItem.cs` | `TryOpen`(344) / `PlanAssignments`(509) / `SolveConnected`(595) / `CanonicalKey`(743) |
| 暴露刷新 | `Gameplay/PixelGroup.cs` | `RefreshExposed`(1047) |
| 同色合并判定 | `Gameplay/SameColorMergeWatcher.cs` | `Notify`(45) |
| 每帧车盘扫描 | `Gameplay/ContainerGroup.cs` | `Update`(357) / `ProcessConsumption`(362) / `IsOpen`(315) / `IsRowReleased`(337) |
| 物理帧驱动 | `Gameplay/CrowdBufferZone.cs` | `FixedUpdate`(455) / `DetachPhysics`(1421) |
| 提取推进 | `Gameplay/CrowdBufferZone.cs` | `StepExtracting`(507) / `HasGridPathfindingPixels`(200) |
| 寻路 sweep | `Gameplay/CrowdBufferZone.cs` | `SweepOnce`(633) / `ComputeExitDistance`(1043) / `PickBestPixel`(1088) / `CanExit`(860) / `IsObstacle`(986) |
| 门 / 管道全扫 | `Gameplay/PixelGroup.cs` | `MustWalkToGate`(514) / `MinActivePipeTrackRow`(414) / `GateAt`(485) / `IsGateBlockedFor`(575) / `IsActivePipeBlocked`(397) |
| UI 每帧文本 | `Gameplay/GameController.cs` | `Update`(871) / `UpdateCountText`(881) |
| 传送带驱动 | `Gameplay/Conveyor/ConveyorBelt.cs` | `Update`(212) / `ApplyPositions`(276) / `ApplyCellPositions`(321) / `CheckLeave`(355) / `OccupiedCount`(443) |
| 轨迹采样 | `Gameplay/Curve/ArcPathController.cs` | `GetTotalPathLength`(67) / `GetGlobalPosition`(78) / `GetGlobalEulerAngles`(102) |
| 冰重建 | `Gameplay/IceItem.cs` | `BuildMeshVisual`(362) / `Clear`(416) / `UpdateDisplay`(455) / `Spawn`(597) |
| 冰网格 | `Gameplay/IceMeshBuilder.cs` | `Build`(178) 起 |
| 表情 | `Core/EmojiManager.cs` | `PlayEmoji`(153) / `ApplyPlaySpeed`(503) / `SetupBillboard`(562) |
| 音频 | `Core/AudioManager.cs` | `GetConfigItem`(346) |
| 对象池 | `SpawnPool/SpawnPool.cs` | `Spawn`(62) / `Despawn`(103) |
| 绳 | `Gameplay/ContainerRopeLink.cs` | `LateUpdate`(167) / `ApplyTaut`(187) |

---

## 9. 与既有文档的关系

- 本文只谈**性能**。失败判定的漏判问题见 `Docs/FailDetectionReview.md`
  （那份文档 §3 的八道门禁里，门禁 5「网格里还有像素在寻路」正是靠
  `HasGridPathfindingPixels` 每帧扫描实现的 —— **若将来给这个属性加缓存，需要同步看那边**）。
- 本文的 P2（调试开关）与 `FailDetectionReview.md` §5 的 `debugFailLog`
  是同一个开关：那份文档把它当**排障工具**，本文建议**默认关、排障时开**。这两者不冲突，
  但改默认值前建议你确认一下。
- 历史存档：仓库根目录的 `失败判定逻辑分析.md`（旧管线）。
