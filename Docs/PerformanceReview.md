# CrowdMatch「潜在性能问题」排查文档

> 状态：**分析 + 已落地 9 处改动**（P3 / P4 / P5 / P6 / P7 / P10 / P11 / P12，见下）。
> 排查日期：2026-10-08
>
> **后续更新（2026-10-08）**：
> - **已改码（9 处，两个程序集离线编译均 0 错误）**：
>   **P7** 物理帧里的 `GetComponent` → `PixelItem.bufferBody` 缓存（§3.2）；
>   **P11** UI 计数 / 进度文本改为「值没变就不刷新」（§3.6）；
>   **P10** 外层循环裁到 `maxOpenRows`（§3.1）—— 同时**更正**了原判定：实际量级远小于原估，
>   且原建议的「`gatheredItems.Count == 0` 早退」会破坏开盖，**已作废**；
>   **P12** `StepExtracting` 复用快照缓冲 + 同一像素的 `transform.position` 读取 6→2 次（§3.3）
>   —— 其 `ApplyEntryQueue` 的 O(E²) 那层**未做**，先测 E 再定；
>   **P3** `LevelLoader` 每次进关的 `RebuildGrid` **8 次 → 4 次**（§2.3）—— 同时**更正**了两处原描述：
>   实际是 8 次不是 16 次，且原建议的「合并为一次」**会破坏箱子容量**（中间两步承重），**已作废**；
>   **P4 + P5 的共同根因** `IsActivePipeBlocked` 记忆化（§2.4 / §2.5）—— 它原先被两条 BFS **逐格**调用、
>   每次重走全部活跃管道折线，现改为按「活跃管道数」版本号失效的覆盖掩码，**语义逐字不变**，
>   同时消掉 P4 的内层与 P5 的 `O(格数 × 管道数)` 那一层；
>   **P4** 另把「只被规划阶段用」的 `score` 字典与 `body` 格子列表挪到就绪判定之后（未就绪是常态：
>   `capacity` 恒比本体大 8~32 格，每次点击都会白建一张几百项的字典），并给 `CollectConnectedEmpty`
>   加了 `adjacent.Count == 0` 的等价早退（箱子被像素围死是最常见的未就绪形态）；
>   **P5** `RefreshExposed` 的 4 张 `bool[,]` + 队列 + 每同色连通块的 `List`/`Queue` 改成员缓冲复用、
>   4 邻偏移提成 `static readonly` —— 每次点击的分配从 **63~195 个容器（约 5~20 KB）降到 0**（§2.5 的 (b)）；
>   同时**更正**一处原描述：所谓「`NotifyClickMovedOut` 每次点击重复算冰状态」**不成立**，
>   那两行只在**真有冰组融化**时才走到（§2.5 的 ⚠️），该条**作废**；
>   **P6** `SameColorMergeWatcher.Notify`：新增「无新像素↔同色可见旧像素 4 邻对就返回」的**等价早退**、
>   连通块标号改**按格索引**（内层 2400 次哈希查找 → 数组读）、全部缓冲（含每标签的成员列表）**static 复用**
>   —— 每次事件的分配与哈希都降到 0（§2.6）。
>   **P4 的回溯预算那部分（原 §7 第 13 条）未做** —— 每关最多跑 4 次，先量化再定。
> - **已处理 / 免做**：**P1** 已禁用（`FrameItem` 不再被调用，§2.1）；**P2** 已在场景 / 预制体关闭调试开关（§2.2）
> - **已定方案、未改码**：**P13** `IceItem` 改为仅关卡开始时重建一次（§4.1）
> - **只记录、不处理**：§2.3 末尾的 **E1 / E2**（升降台相关，待该功能正式启用）
>
> 其余 **P6、P8、P9、P14~P16** 均**未处理**；**P4 / P5 只做了其中一部分**（各自剩什么见 §2.4 / §2.5）。
> 改动清单见 §7。
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
| P3 | `LevelLoader` 一次加载调 `RebuildGrid()` **8 次**（原文档误写 16 次） | 每次进关 | ✅ **已减到 4 次**（另更正了原判定，见 §2.3） | §2.3 |
| P4 | `BoxItem.TryOpen()` 每次点击重算候选格（内层逐格走管道折线）+ 5 万预算回溯 | **每次点击 × 每个未就绪的箱子** | 🟠 **部分处理**：候选格内层的管道测试已缓存、`score` 字典挪到就绪判定之后；**回溯预算未动** | §2.4 |
| P5 | `PixelGroup.RefreshExposed()` 每次点击整盘重算（6 个全网格 pass + 每同色连通块一对容器） | 每次点击 | ✅ **已处理**：`O(格数×管道数)` 那层已消（管道掩码）＋ 全部缓冲改成员复用（每次点击分配 63~195 个容器 → 0） | §2.5 |
| P6 | `SameColorMergeWatcher.Notify()` 每次事件整盘重算（全网格扫描 + 两遍连通块 + ~10~15 KB） | 每次动态事件（每关 10~40 次） | ✅ **已处理**：早退 + 标签改按格索引 + 缓冲全复用（哈希与分配降到 0）；「第二遍只跑受影响分量」未做 | §2.6 |
| P7 | `CrowdBufferZone.FixedUpdate` 在物理帧循环里 `GetComponent<Rigidbody>()` | 每物理帧 × 每个物理像素 | ✅ **已处理**：改读 `PixelItem.bufferBody` 缓存 | §3.2 |
| P8 | `SweepOnce()` 每次 sweep 大块分配 + 在 `while` 里 `Sort` | 每个提取 tick | 🟠 | §3.4 |
| P9 | `CanExit()` 逐 seed 重算 `MinActivePipeTrackRow()` / `MustWalkToGate()` | 每个提取 tick × 每个像素 | 🟡 | §3.5 |
| P10 | `ContainerGroup.Update → ProcessConsumption` 每帧扫车盘 —— **原判定的复杂度与建议均已更正**（实际远小于原估，且原建议的早退会破坏开盖） | 每帧 | ✅ **已处理**：外层循环裁到 `maxOpenRows` | §3.1 |
| P11 | `GameController.UpdateCountText` 每帧字符串拼接 + 写 UI `Text` | 每帧 | ✅ **已处理**：值没变就不拼串 / 不赋值 | §3.6 |
| P12 | `StepExtracting` 提取期间每帧 `new List<ExtractState>` + 同一像素重复读 `transform.position` | 提取期间每帧 | ✅ **已处理**：复用快照缓冲 + 位置读取 6→2 次（O(E²) 那层未做） | §3.3 |
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
| `CrowdBufferZone.Update` | `StepExtracting()` + `TryRelease()`（已改：快照缓冲复用 + 位置少读） | `O(批次×像素)`，见 §3.3 |
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

### 2.3 P3 · `LevelLoader` 一次加载重建网格 8 次 → ✅ **已减到 4 次（并更正原描述）**

> **⚠️ 更正（2026-10-08）**：本节最初两处写错：
>
> 1. **不是 16 次，是 8 次。** `grep` 命中的 16 处只是**源码位置数** —— 每个 `ApplyXxx` 都有
>    「`data == null` 分支」和「末尾」两处，二者**互斥**，每次加载每步只执行一个。
>    所以每次 `Apply()` 实际执行 **8 次** `pg.RebuildGrid()`（外加 1 次 `cg.RebuildGrid()`，那是另一套）。
>    派生的「144 个二维数组、128 次层级扫描」相应减半为 ~72 / ~64。
> 2. **「每步都重建是浪费」这个判断是错的 —— 其中两步是承重的。** 见下。

**位置**：`Core/LevelLoader.cs:48-80`（`Apply`）+ 各 `ApplyXxx`

#### 为什么中间的重建不能全删：箱子容量要读障碍表

```
ApplyBoxes → SpawnBox                                    (PixelGroup.cs:1809)
 └─ BoxItem.ComputeCapacity                               (BoxItem.cs:129)
     └─ CountIfEmpty → group.IsBlocked
         └─ IsWall | IsPipe | IsBox | IsCrateCell  →  读 wallGrid / pipeGrid / boxGrid / crateMask
```

而 **`SpawnWall` 不写 `wallGrid`**（`PixelGroup.cs:1575`）、**`SpawnPipe` 不写 `pipeGrid`**（`:1625`）、
**`ClearWalls` / `ClearPipes` 只把表清零**（`:1363` / `:1381`）—— 表的内容**完全靠重建时那次
`GetComponentsInChildren` 扫描**。所以 `ApplyWalls` / `ApplyPipes` 的重建**不能删**：
删了箱子会把墙格 / 管道格算成空格，`BoxItem.capacity` 偏大 → **箱子提前开**。
这是会静默溜进正式关卡的玩法 bug，不是性能问题 —— 原作者的「每步都重建」不是懒。

#### 已做的改动（2026-10-08）· 只删「没有读者」的四步

删掉 `ApplyGates` / `ApplyIces` / `ApplyBoxes` / `ApplyElevators` 各自的 `RebuildGrid()`
（含 `data == null` 分支那次）。依据：**这四步的产物在加载中途没有任何读者**（逐点 `grep` 核对过）：

| 产物 | 加载中途的读者 |
|---|---|
| `gateGrid` / `gateRegionMask` / `gateMultiplier` | 无（`IsBlocked` **不含门**；`SpawnGate` 不读网格） |
| `iceGroups` / `iceFrozenMask` | 无（`SpawnIce` / `SpawnCrate` 都不读） |
| `boxes` / `boxGrid` | 无（`boxGrid` 由 `SpawnBox` **增量写**维护，供同一批里后面的箱子读） |
| `elevators` | 无（`TryAdvance` 等玩法期才读） |

它们统一留到 `ApplyCrates` 末尾那次「最终权威重建」——它跑在 `ApplyIces` / `ApplyElevators` **之后**，
所以 `elevators.Count` 看到的是本关**最终**集合，比原来「前几次重建带着上一关残留子物体」更准确。

**结果**：每次加载 `pg.RebuildGrid()` **8 次 → 4 次**（保留 `ApplyPixel` / `ApplyWalls` / `ApplyPipes` / `ApplyCrates`），
语义逐字不变。刷新契约已写进 `Apply` 的 XML 注释（`LevelLoader.cs:48-65`），后续改动照着它走。

#### 明确未做（各有理由）

- **轻量刷新变体**（只重扫障碍表、跳过门区域 BFS / `RefreshIceState`）：删到只剩 4 次（其中仅 2 次是中间步骤）
  之后，这个变体只省约 2 次门 BFS + 2 次全网格 pass ≈ **剩余开销的 8%**，噪声级；
  不值得为此给 `RebuildGrid` 加模式参数。
- **`ClearXxx` 里的重复分配**：读代码后**建议不做**。`ClearGates` / `ClearIces` 的 `= null` 是**刻意**的
  （`:1322-1326` 注释：倍率「不在区域内 = 1」，用 0 填充会在重建前被读成倍率 0；三个访问器都对 null 兜底），
  不能改成「清零复用」。剩下四个 `new bool[cols, rows]` 合计每次加载只省 ~340 字节，且发生在**进关时一次**、
  不在帧预算里 —— 改了只是让 diff 变大。

#### 待办（升降台相关，按你的要求只记录、不处理）

- **E1 · 缓存地面 Renderer**：`RebuildGrid` 在 `elevators.Count == 0` 时调
  `RestoreDefaultGroundMaterial`（`:243`）→ `ElevatorItem.FindGroundRenderer()` = **`Object.FindObjectsOfType<Renderer>()`**
  全场景扫描。改成缓存引用即可。等升降台正式启用时做。
  **注意**：它挂在 `RebuildGrid` 里，所以这次重建次数减半已让它的调用次数**从 8 次降到 4 次**。
- **E2 · 先确认 `PixelGroup.defaultGroundMaterial` 挂没挂**（`:322` 有 `if (defaultGroundMaterial == null) return;`）——
  没挂则那条扫描根本不存在。需看 Inspector（预制体，按约定未读）。

**性质**：这条整体是**进关一次性**开销，收益是「进关更快」而非「帧率更高」，
优先度低于 §2 / §3 里那几条每帧 / 每次点击的。

---

### 2.4 P4 · `BoxItem.TryOpen()` 每次点击重算候选格（内层逐格走管道折线）+ 5 万预算回溯 🟠 **部分处理**

**位置**：`Gameplay/BoxItem.cs:344`（`TryOpen`）→ `:518`（`PlanAssignments`）→ `:604`（`SolveConnected`）→ `:695`（`GrowConnectedRec`）→ `:752`（`CanonicalKey`）
**调用方**：`PixelGroup.TryOpenBoxes()`（`:1984`），从 `GameController.ResolveMatch`（`:1260`）调用 —— **每次确认点击一次**

> **⚠️ 更正（2026-10-08）**：本节最初的判定漏了真正的成本大头，并高估了 (b) 的频率。改为按实测的关卡规模重述。

**实测规模**（166 个关卡 JSON）：棋盘 15×15（225 格），最大 21×21；带箱子的关卡只有 8 个，每关 **2~4 个**箱子；
但 **`capacity − 本体格数` 恒 ≥ 8，最大 32**（`Level_C04` = 28/25/20/21、`Level_QBlock` = 26/26、`LevelHard_15` = 20/28）。
管道最多 **9 个 / 35 个折点**。两条推论定了成本模型：

1. 箱子必须靠「连通空格」凑够格数 → `CollectConnectedEmpty` 的 BFS 每次都真跑，不是边角；
2. 一个箱子要额外找 20~32 个空格，而棋盘前中期基本被填满 → **「未就绪」是常态**。

**(a) 未就绪的箱子每次点击都白算一遍。** `TryOpen` 在判定之前就无条件做完这一整套（**下面是改动前的原状**）：

```csharp
var body = new List<Vector2Int>();        EnumerateBody(body);
var adjacent = CollectAdjacentEmpty();    // 4 邻扫描
var connected = CollectConnectedEmpty(adjacent);   // 4 方向 BFS
var score = new Dictionary<Vector2Int, int>();     // 三趟填充，几百项
...
if (available < capacity)
    return false;   // ← 空间不足：上面全部白做，而下次点击还会再做一遍
```

**（已改）** 修法不是「把空格判定提到建容器之前」——`available` 本身就要靠 `connected` 的 BFS 数出来，
判定提不上去。真正能提到判定之后的是**只被规划阶段用**的东西，现在都建在 `:377` 的
`if (available < capacity) return false;` 之后：

| 原来建在判定之前 | 现在 | 省下 |
|---|---|---|
| `score` 字典（`:392`） | 判定后 | 未就绪时不再白建一张几百项的字典 |
| `body` 格子列表（`:390` 的 `EnumerateBody`） | 判定后 | 未就绪时不再白跑 W×H 次 `List.Add`；计数改读现成的 `BodyCount`（`:100`，与 `EnumerateBody` 同一条公式，编辑器 `BoxItemEditor.cs:46` 也是这么取的） |

**「相邻格一个都没有」这一形态**（箱子被像素围死 —— 也就是「未就绪」最常见的形态）还多省一笔：
`CollectConnectedEmpty` 开头加了 `if (adjacent.Count == 0) return result;`（`BoxItem.cs:471`）。
BFS 的队列**只从 `adjacent` 播种**，没有种子结果必然为空，所以这行逐字等价；
它省掉的是函数里那个只为 BFS 扩展服务的 `body` HashSet（W×H 次插入）与 `HashSet×3 + Queue` 四次分配。

> 顺便记下这一形态的实际开销（全部是**便宜**那一路）：`CollectAdjacentEmpty` 里
> `List.Contains` 一次都不会跑（`result` 为空），BFS 一次都不跑，`score` / `body` 也不建。
> 剩下的是 4WH 次便宜比较 + 2W+2H 次 `IsEmptyForBoxRelease`。**不需要为它做进一步的裁剪。**

**(a′) 内层的管道测试才是大头（原文档漏了这条）。** `CollectAdjacentEmpty` / `CollectConnectedEmpty`
的内层每格调 `IsEmptyForBoxRelease` → `IsEmptyForExposure`（`PixelGroup.cs:523`）→
**`IsActivePipeBlocked`**（`PixelGroup.cs:441`），而后者**遍历全部活跃管道、每个再走 `CoversCell` 的整条折线**
（`PipeItem.cs:115`）。也就是每次格判定的成本是 `O(管道数 × 折点数)` 而不是 `O(1)`：

| 环节 | 次数级 | 单次成本 |
|---|---|---|
| `EnumerateBody` | 每箱每点击 | 免费（纯算术） |
| `CollectAdjacentEmpty` | 每箱每点击 | 周长 × 4 次格判定（内层 `List.Contains` 让它再乘个周长） |
| **`CollectConnectedEmpty`** | 每箱每点击 | **≈ 4E 次格判定**（E = 可达空格区），另 `new HashSet×3 + Queue` |
| **每个格判定的内层** | **4E × 箱数** | **`O(管道数 × 折点数)`** ← 乘性项 |

按实测数据粗估（标注为估算）：E ≈ 200 时单箱一次点击 ≈ 4×200×60 ≈ **5 万次内部迭代**，4 个箱子 ≈ 20 万次。
**同一个 `IsActivePipeBlocked` 还被另外两处调用**：`RefreshExposed` 的第 0 趟 BFS（`:1152`、`:1169`，
即 §2.5 的 P5）与 `GameController.cs:1126` 的每次点击邻域扫描 —— 所以 P4 与 P5 **是同一个根**。

**（已改）** `IsActivePipeBlocked` 改为查「活跃管道覆盖掩码」`PixelGroup._activePipeMask`（字段 `:171`、`:173` +
`EnsureActivePipeMask` `:457`；`IsActivePipeBlocked` 本身现在 `:441`）。失效判据用**活跃管道数**：`PipeItem._waveIndex` 在关卡内只增不减（唯一写入在
`PipeItem.cs:313`），所以「还有未释放波次的管道数」是个可靠版本号 —— 先数一遍（≤ 管道总数 次 bool 读、零分配），
与缓存版本不同才重建。重建用现成的 `CoversCell` 逐格问一遍，**判据与旧实现完全同源**，不另写一套遍历以免日后分叉。
`RebuildGrid()`（换关）与 `ClearPipes()` 里显式置 `-1` 失效 —— **这条是必须的**：换关时新旧关卡的活跃管道数
可能恰好相同，只靠计数版本号会误用上一关的掩码。
于是那个乘性项变成常数：P4 的 4 条 BFS、P5 的整盘 BFS、`GameController:1126` 三方同时受益，**语义逐字不变**。
（一处细微差别：新实现先 `IsInRange` 再查表，越界返回 `false`；旧实现不判范围。三个调用点都已各自 `IsInRange` 过，实际无差异。）

**(b) 就绪后走的回溯 —— 未做。** `PlanAssignments` → `SolveConnected` 递归回溯，
预算 `BacktrackBudget = 50000`（`:95`、`:546`）。每个到达叶子（`block.Count == n`）的候选块算一次
`CanonicalKey`（`:752`），而它分配一个 `List`、一次 `Sort(lambda)`、一个 `StringBuilder` 和一个 `string`，
结果塞进 `HashSet<string> seen`（`:703`）。另有 `GrowConnectedRec` 每层 `new HashSet<Vector2Int>`（`:720`）。
最坏情况 5 万次叶子 × 每次 4 个分配。

> **⚠️ 更正**：原文说这是「最可能造成点一下箱子卡一下的地方」。按实测规模，**每关最多 4 个箱子、每个只开一次**，
> 所以这段代码每次进关最多跑 4 次，不是每次点击。它是「**开箱那一帧可能卡一下**」，而且正好落在开箱动画起播的瞬间。
> 值不值得动，取决于那 5 万预算到底有没有被打满 —— 静态读代码看不出来，**建议先加一行计数**（输出
> `BacktrackBudget - budget`，超阈值才 `Debug.LogWarning`），跑几关看数字再定。
> 顺带：原文建议的「给 `TryOpen` 加结果缓存」**不成立** —— 每次点击盘面必变（这一击刚移走像素），命中率是 0。

**(c) `debugOpenLog` 默认 `true`**（`:68`），`:368-376` 每次 `TryOpen` 都拼一条 6 段字符串的日志 ——
包括上面「白算一遍」的那些调用。**本次未动**（按你的要求保持现状）。字段默认值是 `true`；
运行时实际值来自**箱子预制体**（未读 YAML），需要时在 Inspector 确认。

**剩下的**：① (b) 的回溯（先量化）；② `CollectAdjacentEmpty` 的 `List.Contains`（`BoxItem.cs:451`）——
它让相邻扫描的内层变成 O(周长²)，但周长只有几十，收益很小；
③ `adjacent` 非空时 `CollectConnectedEmpty` 仍会分配 `HashSet×3 + Queue`（那 3 个集合 BFS 真的在用），
要彻底去掉得改成复用缓冲 —— 属 GC 优化，改动面比上面大；④ `debugOpenLog`。

---

### 2.5 P5 · `PixelGroup.RefreshExposed()` 每次点击的整盘重算 🟠 **已处理（剩一项待议）**

**位置**：`Gameplay/PixelGroup.cs:1127` 起
**调用方**：每次点击（`GameController.cs:1262`），以及 `BoxItem.cs:920`、`ElevatorItem.cs:574`、`CrateItem.cs:640`、
`PixelGroup.cs:1047`（拆木箱后）、`NotifyClickMovedOut`（`:863`，**只在冰融化时**）

**先给「每次点击到底花在哪」的账**（行号是改动后的现状）：

| 环节 | 行 | 全网格 pass | 分配 |
|---|---|---|---|
| `RefreshIceState()` | `:1134` → `:764` | 清掩码 1 + 逐像素 `SetFrozen` 1 | — |
| `RefreshCrateState()` | `:1138` → `:945` | 清掩码 1 + 逐像素 `SetCovered` 1 | — |
| `reachableEmpty` BFS | `:1146` | 空区 BFS | 复用缓冲 |
| `directlyExposed` | `:1180` | 1 pass | 复用缓冲 |
| `visited` + `active` | `:1197-1198` | 1 pass + 每连通块 BFS | 复用缓冲 |
| 应用 `SetExposed` | `:1320` | 1 pass（值没变早退） | 仅问号揭晓时一个小 List |

合计 **6 个平凡全网格 pass**（15×15 就是 6×225 次），没有别的。`SetExposed` / `SetCovered` 都有
「值没变就早退」（`PixelItem.cs:396` / `:447`），所以最后一个 pass 对绝大多数格子只是一次比较 ——
**CPU 上本来就很轻**。也就是说本条的问题从来不是 CPU，而是分配。

**(a) 嵌套扫描：每次格判定重走管道折线 —— ✅ 已消。** `reachableEmpty` 的 BFS 每格调
`IsEmptyForExposure`（`:1169`）→ `IsActivePipeBlocked`（`:533`→`:441`），后者原先**遍历全部 pipe 及其点**，
于是是 `O(格数 × 管道数)` 而不是 `O(格数)`。现已改为查 `PixelGroup._activePipeMask` 缓存掩码
（`O(管道数 × 折点数)` → `O(1)`）。**这就是 §2.4 的 (a′) 那处改动，一处改了两条链**：P4 的 4 条 BFS
与本条的整盘 BFS 同时受益，还捎带上 `GameController.cs:1126` 的每次点击邻域扫描。
失效机制、为什么「活跃管道数」是可靠版本号、换关必须显式失效，见 §2.4 的 (a′)。

**(b) 每次点击的分配 —— ✅ 已消。** 原先每次调用固定分配 9 个容器：4 × `bool[cols, TotalRows]`
（`reachableEmpty` / `directlyExposed` / `visited` / `active`）、4 × `int[] { … }` 局部数组字面量
（C# 的数组字面量走堆）、1 × `Queue<Vector2Int>`；另**每个同色 4 连通块**再分配一对
`List<Vector2Int>` + `Queue<Vector2Int>`。实测（166 关）：同色连通块数**中位 27、最多 93**
（`Level03`，450 格），也就是每次点击 **63 ~ 195 个容器、约 5 ~ 20 KB**。

现改为成员缓冲复用（`_reachableEmptyBuf` / `_directlyExposedBuf` / `_exposureVisitedBuf` /
`_exposureActiveBuf` / `_exposureQueue` / `_componentCellsBuf`；按需扩容 + `Array.Clear`，见 `ClearedGrid`），
4 邻偏移提成静态只读的 `Dx4` / `Dz4` —— **每次点击的分配降为 0**（除问号揭晓时那个小 List）。
安全前提是 **`RefreshExposed` 不可重入**，已核：它不 yield，且 `SetExposed` 的整条调用链
（`ApplyMaterial` / `RefreshQuestionObject` / `ApplyExposedState`）只写自己的渲染器与 Animator，不回 PixelGroup。

> **⚠️ 更正**：本节原文说「另外一处重复调用 …… **冰状态每次点击算了两遍**」—— **不成立**。
> 读 `NotifyClickMovedOut`（`:830`）可知：`iceGroups` 为空时它直接 return，且 `:856` 有
> `if (!anyMelted) return;` —— 那两行 `RefreshIceState()` + `RefreshExposed()`（`:859` / `:863`）
> **只在真有冰组融化时才走到**，不是每次点击。所以 §7 第 4 条「删掉那次重复调用」的收益只是
> 「每次融化少跑一遍冰刷新」，可以忽略。（那次调用**本身确实是冗余的** —— `IceItem.BuildVisual`
> 完全没读像素的 `IsFrozen` / `IsExposed`，夹在中间的 `BuildVisual` 不依赖它；但不值得为此改代码。）

**还剩一项（不推荐现在做）**：`RefreshIceState()` / `RefreshCrateState()` 每次点击无条件跑，
占了 6 个 pass 里的 4 个，哪怕这一击与冰、木箱毫无关系。加脏标志能把 pass 数腰斩，但 `RefreshExposed`
的注释明说了「放在这里是为了不依赖调用顺序（调用方可能刚改过冰组、刚融化）」——
改成脏标志必须保证**所有**改冰组 / 木箱状态的入口都置位（`IceItem.ConsumeOne`、`SpawnIce` / `ClearIces`、
`CrateItem` 的拆除与消失动画窗口 `IsHidingForVanish`、`SpawnCrate` / `ClearCrates` ……），
漏一处就是「像素该亮不亮 / 该点不到却能点」的玩法 bug。**要先做一次「谁能改冰 / 木箱」的审计才敢动**，
而省下的只是 3 个 225 次的平凡 pass —— 除非 Profiler 打脸，不值得。

---

### 2.6 P6 · `SameColorMergeWatcher.Notify()` 每次事件整盘重算 🟡 **已处理（剩一项待议）**

**位置**：`Gameplay/SameColorMergeWatcher.cs:74` 起
**触发源**：每次动态事件 —— 管道波次（`PipeItem.cs:396`）/ 开箱（`BoxItem.cs:924`）/ 升降台升起（`ElevatorItem.cs:579`）/
问号揭晓（`PixelGroup.cs:1328`）/ 冰融化（`PixelGroup.cs:868`）/ 木箱拆开（`CrateItem.cs:661`）

**频率（实测 166 关）**：

| 触发源 | 数据 | 单关上限 |
|---|---|---|
| 管道波次 | 58 关有管道，波次总数 681 | **28**（`Level_C02`） |
| 问号揭晓 | 28 关有问号，共 989 格 | 按「曝光波次」估 5~20 |
| 开箱 / 冰 / 木箱 / 升降台 | 23 / 1 / 3 / 2 关 | 个位数 |

⇒ **每关 10~40 次，不是每帧**（所以本条从来不是帧率问题）。每次调用的账：

| 环节 | 原来 | 现在 |
|---|---|---|
| 一次全网格扫描建 `visible` + `Dictionary<PixelItem,int>`（键是**像素实例**） | ~150 次字典插入 | 复用 `List<Vector2Int>` + 标签表按格索引，**0 次哈希插入** |
| 两遍 `LabelComponents`（一次含新像素、一次把新像素当隔绝物） | 每遍约 1200 次哈希查找（每邻格一次 `HashSet.Contains` + 一次字典 `TryGetValue`），两遍共 ~2400 | 每邻格：一次边界判定 + **两次数组读**；哈希 **0** |
| 按块统计那两趟 | 逐像素再查 `HashSet` / 字典 | 数组读 |
| 分配 | 约 10~15 KB（`Dictionary` 的 buckets+entries ≈7 KB、两个 `int[]`、四个按标签的数组、**每个标签一个 `List<PixelItem>`**、`Queue<int>` ×2） | **0**（全部 static 复用，成员列表走池） |

**（已改）三件，都逐字等价**：

1. **早退 `AnyMergePair`**（`:152` 调用、`:227` 定义）—— 「本批新像素里没有任何一颗与『同色、可见、且非本批新像素』的格 4 相邻」时直接返回。
   **为什么是当且仅当**：此时任一连通块要么全由新像素组成、要么全由旧像素组成（两类像素之间没有边），
   于是 4a 要的「旧块与新像素同块」与 4b 要的「块内新旧像素都有」都凑不齐；反过来，只要有这样一条边，
   沿同色路径就能推出「存在新像素与可见的非新像素相邻」。所以不会漏播，也不会多消耗任何 `Random.Range`
   （本来就不会播）。它挡掉的是「新区块根本没挨着任何原有同色区域」这类调用 ——
   **这是三件里收益最不确定的一条，取决于这种调用占多大比例**。
2. **标签表按格索引**（字段区 `:39` 起 + `LabelComponents` `:259`）—— 用 `int[,]`（0 = 可见未标号、-1 = 不可见、>0 = 标号）
   取代「`Dictionary<PixelItem,int>` 把像素映射到 `visible` 下标 + `int[]` 标签」；`HashSet<PixelItem> excluded`
   换成按格的 `bool[,] NewMask`。内层从哈希查找变成数组读。
   原来那颗 `HashSet newSet` **整个删掉了**（两处用途都由 `NewMask` 承担 —— 等价理由：步骤 1 已保证新像素就落在它自报的格上）。
   「同一实例出现在两格」那条异常兜底改用一颗复用的 `SeenOnce`（`:132`），保留「只算先扫到的那格」的原口径。
3. **缓冲全部 static 复用**（`:39` 起）—— 3 张网格（两张标签表 + 新像素掩码）、5 张按标签的表、
   `VisibleCells` / `SeenOnce` / `BfsQueue` / `ValidNew`，以及**成员列表的池** `RentMemberList`（`:333`）。
   代价是引入「**`Notify` 不可重入**」这条前提 —— 已核实：它唯一调出去的
   `EmojiManager.TryPlaySurpriseEmoji` → `PlayEmoji` 只做对象池生成 + 世界缩放/朝向 + 一条延时回收，
   不回 PixelGroup、不重入本类。这条约定写进了类注释。

> **⚠️ 更正**：§7 第 17 条原先写的理由「（合并两遍 `LabelComponents`）属改口径」**不成立** —— 见下条。

**还剩一项（已证明等价，但属结构重写，暂不做）**：第二遍连通块只跑「含新像素的 after 分量」。
未被新像素碰到的原有区域，其 after 标签里 `newCount == 0`，在 4a 里本来就被跳过；所以第二遍只需在
「含新像素的那些 after 分量」内部做（多加一条 `labelAll[格] == li` 的守卫即可 —— 同色相邻必然同 after 分量，不会丢格）。
**等价性包括随机数序列**：被跳过的旧分量本来就不消耗 `Random.Range`，而 `TryPlaySurpriseEmoji` 无全局状态、
各标签的像素集互不相交，所以输出逐像素一致。收益：实测整盘同色连通块中位 27 个、受影响的通常 1~2 个
⇒ 第二遍本身降 ~90%，折算到整个调用约 20~25%。**但它要引入「只跑受影响分量」的控制流分支，
改动面明显大于上面三件**，所以留着 —— 除非以后 Profiler 指到这里。

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

### 3.3 P12 · `StepExtracting` 每帧分配 ✅ **已处理（并补齐了原判定漏掉的主项）**

**位置**：`Gameplay/CrowdBufferZone.cs:517-626`

> **补充（2026-10-08）**：本节原来只写了那个 `List` 分配。重读调用链后发现，
> **提取期间真正的主项是 `ApplyEntryQueue` 的 O(E²) 次 `transform.position` 读取**，不是那个 `List`。
> 已按下三层处理，第 3 层（O(E²) 本身）**未做**。

**前提**：`StepExtracting` 开头就是 `if (_batches.Count == 0) return;`（`:519-520`），
所以**不在提取时成本为 0** —— 下面所有开销只在「有批次在走」时存在。
E = 本批「已出网格、正在飞向入口」的像素数。

```csharp
for (int b = _batches.Count - 1; b >= 0; b--)
{
    var exiting = new List<ExtractState>(batch.extracting.Count);   // ← 原：每批次、每帧一次新的 List
    ...
    foreach (var st in exiting)
    {
        Vector3 target = ComputeEntryTarget(st.item.transform.position, entrance, perp);   // 读 ①
        Vector3 moveTarget = ApplyEntryQueue(st, target, ...);      // 内部又读 ②（:1171）
        MoveToward(st, moveTarget);                                 // 内部读 ③④（:1204/:1213），并写位置
        bool reachedTarget = XZDistance(st.item.transform.position, ...);                   // 读 ⑤
        bool enteredRange = ... Vector3.Dot(st.item.transform.position - entrance, ...);    // 读 ⑥
    }
}
```

**已做的改动（2026-10-08）**

**第 1 层 · 复用 `exiting` 快照（省 GC 分配）** —— 新增字段 `:172`，每批次开头重建（`:534`）：

```csharp
private readonly List<ExtractState> _exitingBuffer = new List<ExtractState>();
...
var exiting = _exitingBuffer;
exiting.Clear();
```

- 快照本身**必须保留**：处理过程中会边删 `batch.extracting`（`:566`），而 `ApplyEntryQueue`
  需要**整份** exiting 集合（同列前不追尾）。**不能**改成倒序就地删除。
- 复用安全：这段不会重入 —— 循环体里的 `EnterPhysical` 只 `StartCoroutine`，
  其首段（`SmoothScaleToTarget`）碰一下 `localScale` 就 yield，不会回调 `StepExtracting`。

**第 2 层 · 同一像素每帧的 `transform.position` 从 6 次降到 2 次（逐字等价）**

读 ①②③④ 全在 `MoveToward` 写位置**之前**（中间无任何写入 → 值必然相同），读 ⑤⑥ 在写**之后**（也相同）。
改成开头读一次传进去（`ComputeEntryTarget` 本来就收 `pos`；`ApplyEntryQueue` `:1168` 与
`MoveToward` `:1202` 各加一个 `Vector3 pos` 参数），`MoveToward` 返回后再读一次给 ⑤⑥ 复用。
顺带删掉 `MoveToward` 里那句自赋值 `pos.y = st.item.transform.position.y;` ——
`dir.y` 恒为 0（`to.y` 已清零），它本来就是空操作。

**第 3 层 · `ApplyEntryQueue` 的 O(E²)（未做）**

内层 `foreach (var other in exiting)` 每对读一次 `other.item.transform.position`（`:1178`）
→ E×(E−1) 次 native 读，这是提取期间最大的一块。**未做**，原因：

- 最容易想到的「帧初把所有位置缓存一次」**不等价** —— `exiting` 是**按序**处理的，
  每个像素 `MoveToward` 后位置就变了，所以后面处理的像素读到的「前面那些」已是**移动后**的值。
  缓存帧初值会把「部分已更新」这套语义抹平。
- 等价的写法要把 `prog`/`lat` 存进 `ExtractState`、在 `MoveToward` 写完位置后**同步刷新**，
  并在 `entrance`/`axis`/`perp` 变化时整体失效（这三个值每帧由 `RefreshGeometry` 重算，
  取决于 `gapPoint.position`）。把「派生值缓存」和「每帧重算的几何」绑在一起，
  **风险与收益不成比例**。
- **先测**：确认 E 的典型值。E ≤ 20 时这点算术可以不管；上百才值得动。

**明确不做**：合并 `:524` 与 `:625` 那次 `HasGridPathfindingPixels` —— 语义是
「本帧**开始**还有 / 本帧**结束**还有没有」两个**不同时刻**，是刻意的边沿判定（`:623-626`），
而且 `&&` 已经短路，正常情况下每帧只算 1 次。

**顺带（不属于 P12，未改）**：`SmoothScaleToTarget` 每次进入物理都
`new WaitForSeconds(scaleDelay)`（`:1314`）—— 事件级、很小，要抠可换成缓存的等待对象。

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
    return false;                                   // ← MustWalkToGate: 遍历所有门 (PixelGroup.cs:594-614)

int minTrackRow = _extractGroup != null ? _extractGroup.MinActivePipeTrackRow() : int.MaxValue;
                                                    // ← MinActivePipeTrackRow: 遍历所有管道 × 所有点 (PixelGroup.cs:494-512)

for (int r = 0; r < row; r++)                       // ← 前方逐格 IsObstacle
    if (IsObstacle(col, r, vacated, claimed, matchedOccupied, batch)) return false;
```

`IsObstacle`（`:986`）每格会调 `IsBlocked`（4 次数组查，便宜）+
`IsGateBlockedFor`（`PixelGroup.cs:655`，内部 `HashSet<GateItem>.Contains`）。

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
| `BoxItem.cs:875,883,891` | 每个隐藏像素 `new WaitForSeconds` | 每次开箱 N 次分配，见 §4.1 同类 |
| `CrateItem.cs:738` → `PixelItem.cs:252/265/275` | 每个被盖像素一个协程 + `new WaitForSeconds(delay)` | 大木箱破开时一次性起 N 个协程 |

这些是「有界的事件级」开销，不是每帧常态。上面两行「每个像素一个协程」的做法
在像素数大时会一次性创建大量 `Coroutine` + `WaitForSeconds` 对象；
**建议**：改成「一个协程按 delay 排序依次处理」而不是「N 个协程各等各的」。

---

## 4. 未池化 / 重建式资源

### 4.1 P13 · `IceItem` 每次融化重建角块与 Mesh 🟡

**Sprite 模式**：`IceItem.cs:317-332` 遍历包围盒，`Spawn`（`:597-616`）对每个有效角 `Instantiate`，
`Clear`（`:416-446`）`Destroy` 上一批。触发点是**任意冰组融化**：`PixelGroup.cs:861-862` 会对
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
2. **融化时**：不再调 `BuildVisual`。把 `PixelGroup.cs:861-862` 那个「对所有冰组 `BuildVisual`」的循环换成
   —— 未融化的组只调 `UpdateDisplay()`（`:853` 本来就在做），**化完（`Melted`）的组只做「移除冰面」这一件事**
   （把 `BuildVisual` 里的 `Clear()` 单独抽成 `HideVisual()` 之类的即可）。
3. **编辑器路径不动**：`IceItemEditor.cs:48,396` 与 `PixelColorBrushWindow.cs:4107,4135,4163,4207`
   的 `BuildVisual` 是编辑操作触发的预览重建，不在帧率预算里。

> **⚠️ 必须保留的一条**：整组化完时**仍要移除冰面**（第 2 条的 `Melted` 分支）。
> 若把动态重建一刀切成「什么都不做」，融化后冰的美术会留在屏幕上不走 —— 那是视觉 bug，不是优化。

**收益**：把「每次融化 × **全部**冰组」的 `Clear()` + 逐角 `Instantiate`（Mesh 模式另加
`new Mesh()` + 重建 GameObject）降为「每次融化 × 仅计数文本」；冰面重建次数从
`O(融化次数 × 冰组数)` 降到 `O(1)`（关卡开始一次）。

**顺带**：`PixelGroup.cs:853` 对每个非融化组调 `UpdateDisplay()`，其中 `remaining.ToString()`（`:484`）
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
| 5.3 | `GateItem.cs:205`、`PixelGroup.cs:666`、`:1056` | `RegionCellCount` / `CountPixelsInRegion` / `ValidateGates` 都是 `O(格数)`，后者再乘门数 |
| 5.4 | `PixelGroup.cs:1333-1499` 各 `ClearXxx` | 每个都一次 `GetComponentsInChildren`，进关时全部调用 |

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
> - **✅ 已改码**：第 2 条（P10）、第 3 条（P11）、第 5 条（P7）、第 7 条（P12）、第 9 条（P4）、
>   第 10 条（P3）、第 14 条（P5）、第 16 条（P5）、第 17 条（P6）—— 见各条的 §
> - **✅ 已禁用 / 免做**：第 1 条（P2，场景 / 预制体已关）、第 8 条（P1，`FrameItem` 不再被调用）
> - **✅ 原描述作废、不做**：第 4 条（P5）—— 「`NotifyClickMovedOut` 每次点击重复算冰状态」不成立，
>   那两行只在真有冰组融化时才走到（§2.5 更正）
> - **已定方案、未改码**：第 15 条（P13，`IceItem` 仅关卡开始重建，见 §4.1）
> - **只做了一半**：第 7 条（P12）的 `ApplyEntryQueue` O(E²) 那层**未做**，先测 E 再定（§3.3）；
>   第 9·13 条（P4）的回溯预算那层**未做**，同上先量化（§2.4）
> - **只记录、不处理**：升降台相关的 E1 / E2（§2.3 末尾）
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
| 4 | **P5** | ~~删掉 `NotifyClickMovedOut` 里多余的 `RefreshIceState()`~~ —— **原描述不成立**：那两行只在**真有冰组融化**时才走到（`:856` 的 `if (!anyMelted) return;`），不是每次点击；删掉只省「每次融化一遍冰刷新」，**不做**（§2.5 更正） | — |

### 第二档：小重构，局部风险

| # | P# | 改动 | 位置 |
|---|---|---|---|
| 5 | **P7** | `PixelItem` 缓存 `Rigidbody`，消掉物理帧里的 `GetComponent` —— **✅ 已处理**（§3.2） | `CrowdBufferZone.cs:472` + `PixelItem.cs` + `:1421` |
| 6 | **P9** | `minTrackRow` 提到 sweep 开头算一次 | `CrowdBufferZone.cs:878` |
| 7 | **P12** | `exiting` 快照改成员缓存复用 + 同一像素的 `transform.position` 读取 6→2 次 —— **✅ 已处理**（§3.3）；`ApplyEntryQueue` 的 O(E²) 未做，先测 E | `CrowdBufferZone.cs:172/534` |
| 8 | **P1** | ~~`FrameItem` 角块走 `SpawnPool`~~ —— **✅ 已禁用**：已确定 `FrameItem` 不再被调用，本条免做（§2.1）；同一手法可留给 §4.1 的 `IceItem` | — |
| 9 | **P4** | ~~「把空格判定提到建容器之前」~~ —— **原描述不成立**：`available` 本身要靠 `connected` 的 BFS 数出来，判定提不上去（§2.4 更正）。实做三件：`score` 字典（`:392`）与 `body` 格子列表（`:390`）挪到就绪判定之后、计数改用 `BodyCount` ＋ `CollectConnectedEmpty` 加 `adjacent.Count == 0` 早退（`:471`）＋ `IsActivePipeBlocked` 缓存掩码（与第 14 条同一改动）—— **✅ 已处理** | `BoxItem.cs:363-392`、`:471` + `PixelGroup.cs:441` |
| 10 | **P3** | ~~`LevelLoader` 合并为一次 `RebuildGrid`~~ **原方案作废**（中间两步承重，见 §2.3）。实做：删掉「无读者」的四步刷新 —— **✅ 已处理**，每次进关 `RebuildGrid` 8 → 4 次 | `LevelLoader.cs:48-80` |

### 第三档：需要你先拍板（改口径 / 改算法）

| # | P# | 改动 | 需要你确认什么 |
|---|---|---|---|
| 11 | **P8** | `SweepOnce` 的 `Sort` 移出 `while`、`dist`/`snakeOrder` 预计算 | 允许改这段寻路结构？ |
| 12 | **P8** | `SweepOnce` 的数组改复用 / 按 `dist` 分桶代替比较排序 | 同上 |
| 13 | **P4** | `BoxItem` 的 `CanonicalKey` 去分配 —— **未做**。①「给 `TryOpen` 加结果缓存」**已否决**（每次点击盘面必变，命中率 0）；② 先加「本次回溯消耗了多少预算」的计数，跑几关看是否真被打满（每关最多跑 4 次，见 §2.4 更正） | 允许改回溯的键编码？ |
| 14 | **P5** | ~~`RefreshExposed` 的活跃管道掩码预计算~~ —— **✅ 已处理**（`PixelGroup._activePipeMask` + `EnsureActivePipeMask`），与第 9 条是**同一处改动**：P4 的箱内 BFS、P5 的整盘 BFS、`GameController:1126` 的邻域扫描同时受益，**语义逐字不变**（§2.4 的 (a′) + §2.5） | `PixelGroup.cs:441` |
| 15 | **P13 · P14** | **P13 已定方案**：`IceItem` 改为**仅关卡开始时重建一次，去掉融化路径上的动态重建**（保留「化完移除冰面」，见 §4.1 定案）。`EmojiManager` / `IceItem` 另可把层级解析结果缓存到实例 | P13 见 §4.1；P14 需先确认池化回收时不清组件（§6.4） |
| 16 | **P5** | `RefreshExposed` 的 4 张 `bool[,]` + 队列 + 每连通块的 `List`/`Queue` 改成员复用，`int[]{…}` 提成 `static readonly` —— **✅ 已处理**（每次点击的分配 63~195 个容器 → 0；§2.5 的 (b)） | `PixelGroup.cs:148` + `RefreshExposed` |

### 第四档：本次不建议动（先看 Profiler 再定）

这三条我**没有排进上面三档**，理由写在下面 —— 不是漏了，是刻意缓做。
（第 17 条（P6）已经做掉了，保留在原编号位置上只标状态，见下表。）

| # | P# | 事项 | 为什么不排进去 |
|---|---|---|---|
| 17 | **P6** | ~~`Notify()` 两遍全盘 BFS + 按像素数分配~~ —— **✅ 已处理**：早退 + 标签改按格索引 + 缓冲全复用（§2.6）。剩「第二遍只跑受影响分量」已证明等价（连随机数都不变），但属结构重写，暂不做 | `SameColorMergeWatcher.cs:74` |
| 18 | **P15** | `AudioManager.GetConfigItem` 建 `Dictionary<tag, AudioItem>`（§4.3） | 配置项只有几十条且非每帧，收益小；顺手做即可 |
| 19 | **P16** | `ContainerRopeLink.LateUpdate` 逐绳节铺排（§3.7） | 已是 `isKinematic` 的刻意设计，无已知坏味道；需先看 `BehaviourUpdate` 占比再决定要不要合并绳根驱动 |

---

## 8. 相关代码索引

| 关注点 | 文件 | 方法 / 行 |
|---|---|---|
| 描边重建 | `Gameplay/FrameItem.cs` | `Build`(88) / `Clear`(117) / `Spawn`(138) |
| 调试开关 | `Gameplay/CrowdBufferZone.cs` / `GameController.cs` / `PipeItem.cs` / `BoxItem.cs` / `ElevatorItem.cs` | 见 §2.2 表 |
| 逐像素日志 | `Gameplay/CrowdBufferZone.cs` | `SweepOnce` 内 `:760-772` |
| 关卡加载重建 | `Core/LevelLoader.cs` | `Apply`(48) / 每次加载 4 处 `RebuildGrid`（原 8 处，见 §2.3） |
| 网格重建 | `Gameplay/PixelGroup.cs` | `RebuildGrid`(205) |
| 开箱规划 | `Gameplay/BoxItem.cs` | `TryOpen`(344) / `PlanAssignments`(518) / `SolveConnected`(604) / `CanonicalKey`(752) |
| 暴露刷新 | `Gameplay/PixelGroup.cs` | `RefreshExposed`(1127) |
| 同色合并判定 | `Gameplay/SameColorMergeWatcher.cs` | `Notify`(74) / `AnyMergePair`(227) / `LabelComponents`(259) |
| 每帧车盘扫描 | `Gameplay/ContainerGroup.cs` | `Update`(357) / `ProcessConsumption`(362) / `IsOpen`(315) / `IsRowReleased`(337) |
| 物理帧驱动 | `Gameplay/CrowdBufferZone.cs` | `FixedUpdate`(455) / `DetachPhysics`(1421) |
| 提取推进 | `Gameplay/CrowdBufferZone.cs` | `StepExtracting`(507) / `HasGridPathfindingPixels`(200) |
| 寻路 sweep | `Gameplay/CrowdBufferZone.cs` | `SweepOnce`(633) / `ComputeExitDistance`(1043) / `PickBestPixel`(1088) / `CanExit`(860) / `IsObstacle`(986) |
| 门 / 管道全扫 | `Gameplay/PixelGroup.cs` | `MustWalkToGate`(594) / `MinActivePipeTrackRow`(494) / `GateAt`(565) / `IsGateBlockedFor`(655) / **`IsActivePipeBlocked`(441) + `EnsureActivePipeMask`(457)**（缓存掩码，见 §2.4 (a′)）/ `RebuildGrid`(205) |
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
