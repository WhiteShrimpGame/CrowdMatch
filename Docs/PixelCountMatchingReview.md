# Pixel 数量匹配总检查（口径盘点 · 差异清单）

> 目的：把「像素数量」在工程里**哪些地方要对齐、什么会改数量、每处检查有没有把机制算对** 一次性列清。
> 本文只做核对与记录。行号对应**核对时**的版本；F2/F3 修完后 `GameController.cs` 里该处之后的行号整体 +2
> （`TotalPixelCount` 那行现在是 :188），`PixelGroupEditor.LogColorCounts` 现在从 :643 起。

---

## 0. 一句话结论

「数量匹配」有三条会被真正踩到的口径：**① 容器总容量 == 可交付像素数**、**② 逐色容量 == 逐色像素数**、
**③ Record 序列 == 统计口径**。工程里有 **6 处**各自统计像素，其中 **3 处口径原本已经分叉**——
本文写作时已修掉 2 处（**F2 / F3 ✅已修**），**只剩「批量导关自带一套」（F4）**未修；
另有 **几类"名义计入但永远交付不了"的像素**——它们会让"容量 > 可交付"，
而这一侧**既不会判胜也不会判失败**（见 F1），是当前最主要的卡关风险。

---

## 1. 为什么"数量"必须对齐

胜负判定现在的形态决定了三条不变量：

| # | 不变量 | 不成立会怎样 | 谁在保证 |
|---|---|---|---|
| **A1** | **容器总容量 ≤ 可交付像素总数**（实践上取等号） | 容量 > 像素 → 有车**永远填不满** → 判胜（板上不存在"未完成匹配"的车）永不成立；且带上一旦没像素，失败判定的门禁 3（传送带未满）也不触发 → **既不胜也不败** | 「生成 Containers」抽干像素池（ContainerGroupEditor.cs:161-170）、「按 Record 重排容器」（ContainerRearranger） |
| **A2** | **逐色容量 ≤ 该色可交付像素数** | 某色车多、像素少 → 同样卡死在"车填不满" | 「生成 Containers」由 planner 逐色抽 pack 自然保证；「检查并修复容器颜色」逐色对齐（ContainerColorRepair.cs:178-309） |
| **A3** | **Record 序列 == 统计口径**（条数与逐色计数） | 重排/校验用的序列与关卡不符，容器重排后容量对不上 | 「按 Record 重排容器」的前置校验（ContainerRearranger.cs:81-190） |

> **已被替换的旧口径**：`GameData.ClearedPixelCount >= TotalPixelCount`（像素侧计数判胜）。
> 见 GameController.cs:262-281 的说明。**现在 `TotalPixelCount` / `ClearedPixelCount` 已无判定职责**
> （grep 全工程：`ClearedPixelCount` 只有 4 个自增写入点，**没有任何读取点**）。

「A1/A2 里的"**可交付**"是关键词**：像素被统计进来 ≠ 玩家能把它送出去。见 §4 的可达性一列。

---

## 2. 逐处统计点盘点（谁在哪算，算什么）

| # | 统计点 | 位置 | 用途 | 时机 |
|---|---|---|---|---|
| ① | 运行时像素总数 `TotalPixelCount` | GameController.cs:188（修前 :186）→ `CountPixels()` + `CountPipePixels()` + `CountGateExtraPixels()` | Record 文件名里的 `total`（:581） | 进关一次 |
| ② | 运行时"已清除" `ClearedPixelCount` | ContainerGroup.cs:426、:455；ContainerItem.cs:450；GameController.cs:516 | **无消费者**（旧判胜口径遗留） | 每次上车/销毁 |
| ③ | **规划底座** `CollectPlanningPixels()` / `CollectPlanningDetail()` / `CollectPlanningSources()` | PixelGroup.cs:1792 / :1813 / :1868（同一次枚举） | 生成 Containers（ContainerGroupEditor.cs:137）、颜色修复（ContainerColorRepair.cs:183）、门校验（PixelGroupEditor.cs:742） | 编辑器 |
| ④ | **统计颜色总数** `LogColorCounts()` | PixelGroupEditor.cs:643 起（按钮 :69）→ **已改为消费 ③ 的明细** | 判断"每色能否被 3 整除" | 编辑器 |
| ⑤ | **Record ↔ 关卡校验** `Validate()` | ContainerRearranger.cs:81-190（含倍率图 :198-231） | 「按 Record 重排容器」前置 | 编辑器 |
| ⑥ | **批量图片导关**自带统计 | ImageBatchLevelExporter.cs:267-300 | 一键生成 关卡+容器 | 编辑器 |
| ⑦ | 木箱覆盖像素数 | PixelGroup.cs:813-830（CrateItemEditor.cs:48 显示） | 木箱 Insp | 编辑器 |
| ⑧ | 死锁提示 | CrateItemEditor.cs:50-70、IceItemEditor.cs:154 | 只提示 | 编辑器 |

> 声称"同一份底座"的三处：PixelGroup.cs:1833、ContainerColorRepair.cs:12-13 说
> **统计颜色总数 / 容器规划 / 通关判定**同底座。③ 与 ④ 原本**不是**同底座（见 §5 F2），**已修为同底座 ✅**。

---

## 3. 影响因素与正确口径

| 机制 | 对总数的影响 | 统计口径要点 | 可交付的条件（"可达性"） |
|---|---|---|---|
| 网格像素 | 基准 | 按 `gridX/gridZ` 是否在范围内收（GameController.cs:217、PixelGroup.cs:1845） | 同色块要能经空/组内格连通首排（暴露） |
| **倍乘门** | **×倍率**（额外 `+Σ(mult−1)`） | 查**像素所在格**的倍率，嵌套门连乘（PixelGroup.cs:1801/1825、:487-492） | 像素真的要从门格离场（CrowdBufferZone.cs:872-899 预算 = 倍数−1） |
| **管道** | **+轨道格数 × 波次颜色数** | 轨道格像素**保留**（开局阻挡）也算普通网格像素；管道自身格不生成像素 | 轨道格清空后才出下一波（PipeItem.cs:241-249） |
| **箱子** | **+min(capacity, colorIds.Length)** | 隐藏像素 `active=false` + 哨兵坐标 → `GetComponentsInChildren` 扫不到，**必须显式累加**（GameController.cs:221-225、BoxItem.cs:204-242） | 开箱空间够（`available >= capacity`，BoxItem.cs:379；BoxItemEditor.cs:58 有告警） |
| **升降台** | **+Σ 各组 (col,row,color) 三元组数** | 地下像素 **active**、哨兵坐标 → 同样显式累加（GameController.cs:227-231、ElevatorItem.cs:177-207） | 区域内像素清空后才升起下一组 |
| **冰** | **0** | 不改总数；冻结 = "视为不暴露" + 破同色连通（PixelGroup.cs:1026、GameController.cs:802-808） | 计数按**点击**推进（`NotifyClickMovedOut`，GameController.cs:1022），不是按像素上带 |
| **木箱** | **0** | 盖住的像素仍在 `grid`、仍 `active`（只关渲染器），**必须先拆箱再点**（PixelItem.cs:405-439、CrateItem.cs:31-32） | 4 邻像素累计移出 `destroyAfterMoves` 次 |
| **墙体 / 管道自身格** | **−**（这些格不生成像素） | 导入时清掉（LevelLoader.cs:89-122）；**运行时统计并不排除它们**（见 F5） | — |
| 问号 Pixel | 0 | 未揭晓不可点、破连通；`colorId` 照样计入统计 | 暴露即揭晓（PixelItem.cs:373-388） |
| Record 模式 | 0 | 像素原地销毁、不进容器；**按所在格倍率补记 N 份**（GameController.cs:999-1018） | — |
| **复活** | **−**（销毁"该色车已全满"的多余像素） | 只 `ClearedPixelCount++`，**不抵消任何车容量**（GameController.cs:510-518）→ **只修过量侧** | 见 F1 |
| 洗牌 | 0 | 只换位置，`colorId/capacity` 跟着车走（LevelLoader.cs:370-397） | 洗牌时不建绳（ContainerGroup.cs:1032-1037） |

---

## 4. 逐处 × 逐机制 核对表

`✓` = 算对 · `✗` = 漏算/多算 · `—` = 不适用

| 统计点 | 网格像素 | 墙/管道格排除 | 倍乘额外 | 管道波次 | 箱子隐藏 | 升降台地下 | 木箱 | 冰 |
|---|---|---|---|---|---|---|---|---|
| ① 运行时 `TotalPixelCount` | ✓ | ✗ 不排除 | **✓（F3 已修）** | ✓ | ✓ | ✓ | ✓ | ✓ |
| ③ 规划底座（生成/修复/门校验） | ✓ | ✗ 不排除 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| ④ 统计颜色总数 | ✓ | ✗ 不排除 | ✓ | ✓ | **✓（F2 已修）** | **✓（F2 已修）** | ✓ | ✓ |
| ⑤ Record 校验 | ✓ | ✓ 排除 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| ⑥ 批量导关 | ✓ | ✗ 不排除 | ✓ | 清空管道 | **✗ 漏** | **✗ 漏** | ✓ | ✓ |

> ④ 已改为直接走 ③ 的底座（`CollectPlanningDetail()`），所以除"墙/管道格排除"这一条（见 F5）外与 ③ 完全一致。

---

## 5. 已确认的差异与风险

### F1 🔴 「可交付像素 < 车容量」时既不胜也不败（复活救不了这一侧）

- **判胜的唯一口径**是"板上不存在未完成匹配的车"（ContainerGroup.cs:340-364 → GameController.CheckWin）。
  只要有一辆车**永远填不满**，判胜就永远不成立。
- **失败侧也不兜底**：失败判定要求传送带**满**（门禁 3，GameController.cs:343）。
  当那批"点不动"的像素始终进不了带、带上存量又被耗空之后，门禁 3 不成立 → **失败也不再触发** → 本关卡死。
- **复活只修过量侧，不修不足侧**（这一点容易看反）：`Revive` 销毁的是 `MatchPixelsToCars` 里
  `FindCarForColor` 找不到非空车的像素（GameController.cs:508-518）——那种情形**蕴含像素数 ≥ 该色容量**
  （否则必然还有空车），所以销毁是在把"多出来的"削掉，对"容量 > 可交付"**毫无作用**。
- **不足侧的三个现实来源**：F5（障碍格 / 被闭环围住的像素）、F6（越界升降台三元组 / 缺 prefab）、
  F8（木箱/冰的部分死锁）—— **都是"名义计入、实际交付不了"**。
  （F4 批量导关那种是另一类：容量算小了、像素是真的、复活反复销毁多余像素后**能收敛**，属于表现退化而非僵局。）
- **建议方向**（本次不改）：给编辑器补一条"可交付 vs 容量"的静态核对（把 §4 那类不可达像素先挑出来）；
  运行时则可加一条终局兜底：`可交付像素 == 0 且仍有未完成车` → 判定为数据错误而不是静默卡住。

### F2 ✅已修 「统计颜色总数」漏算箱子内容，与它自己声称的"同一份底座"不符

- **原因**：PixelGroupEditor 只遍历 `GetComponentsInChildren<PixelItem>()`（**默认不含 inactive**）；
  箱子隐藏像素是 `active=false`（BoxItem.cs:240），因此**扫不到**。对比 ③ 会显式加上箱子内容
  （PixelGroup.cs:1900-1910，箱子段）。
  - 附带差异：该统计**不过滤坐标**，所以升降台地下像素（active、哨兵 -1,-1）被计入 —— 结果正确，但纯属巧合；
    同理越界/墙格上的像素也会被算进来。
- **后果**：含箱子的关卡上，这个按钮给出的"每色能否被 3 整除"结论**可能偏少**（本该报 ✗ 却显示 ✓）。

### F2-FIX 修法（已落地）

1. **PixelGroup**：把底座的源列表带上机制标签 —— `CollectPlanningSources` 改为
   `List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)>`（新增公开枚举
   `PixelGroup.PlanningSourceKind` = Grid / Pipe / Box / Elevator），并新增公开方法
   `CollectPlanningDetail()`（与 `CollectPlanningPixels` / `CountGateExtraPixels` **同一次枚举**，没有第二条路径）。
2. **PixelGroupEditor.LogColorCounts**：整个改为消费 `CollectPlanningDetail()` ——
   逐色合计与"网格/管道/箱子/升降台"四列明细都从这一份数据出，**不再各自重算**，
   倍率仍按 `GateMultiplierAt(所在格)` 展开（与规划同一算法）。输出格式保持"只列非 0 的机制 + 合计 + %3 判定"。
3. 顺带把按钮的 HelpBox、`CollectPlanningSources` / `CountGateExtraPixels` 的注释改成与实现一致，
   并说明运行时 `TotalPixelCount` 按"实际生成的物体"统计、正常情形与底座逐项相等（差异只来自"声明了没生成"的源，见 F6）。

> 于是「统计颜色总数 / 生成 Containers / 检查并修复容器颜色」现在真的是同一份底座（注释不再是过期承诺）。

### F3 ✅已修 运行时 `TotalPixelCount` 漏算倍乘门额外像素；配套的包装方法是死代码

- **原因**：GameController 的 `TotalPixelCount = CountPixels() + CountPipePixels();`（修前 :186，现 :188）—— **没加** `CountGateExtraPixels()`；
  该包装方法定义在 `:257-260` 却**全工程无调用点**（grep 只有定义 + 三处编辑器调用 `PixelGroup.CountGateExtraPixels`）。
  与两处注释直接矛盾：`:253-260`「计入胜利判定」、`:1006-1007`「后者含 CountGateExtraPixels，文件名里的 total 与 rec 对得上」。
- **后果**：有倍乘门的关卡，Record 文件名的 `total` 会比实际条数（`rec`）小，正好与注释承诺相反。
  因为判胜已改事件驱动，**不影响胜负**，只影响记录/诊断可信度。

### F3-FIX 修法（已落地）

1. GameController.cs：`TotalPixelCount = CountPixels() + CountPipePixels() + CountGateExtraPixels();`（一行），
   并把该行注释写成口径公式（网格 + 箱子 + 升降台 + 管道 + 倍乘额外，即规划底座）。
2. `CountGateExtraPixels()` 的注释改准：它服务的是 **Record 文件名的 total 与 rec 对得上**，
   而**不是**判胜（判胜是容器侧事件驱动，见 `CheckWin`）；`PixelGroup.CountGateExtraPixels` 的注释同理去掉"无法通关"的旧说法。

> **残留的已知近似**（不是本次引入的）：箱子**跨在倍乘门边界上**时，底座按**箱子锚点格**取倍率，
> 而 Record 是按像素**释放后的实际格**补记 —— 这类关卡 total 与 rec 仍会差几颗（PixelGroup 箱子段的注释已标明）。
> 想彻底一致就得让箱子释放**确定格**，属设计变更，不在本轮范围。

### F4 🟡 批量导关自带一套统计，且只清 4 类子物体

- **证据**：ImageBatchLevelExporter.cs:283-294 —— 自己扫 `PixelItem × 倍率`，**不用** `CollectPlanningPixels`，
  因此**不含箱子隐藏像素、不含升降台分组像素**（管道在 :180 已被 `ClearPipes` 清掉，所以管道不构成问题）。
  另外 :178-181 只清 `ClearPixels / ClearWalls / ClearPipes / ClearContainers`，
  **不清 Boxes / Elevators / Gates / Ices / Crates**。
- **后果**：如果目标场景的 PixelGroup 里残留箱子/升降台/冰/木箱（比如上一个关卡刚导入过），
  这次批量导出会：① 按"只有网格像素"算容量（**容量偏小**）；② 却把这些残留机制写进 JSON（`BuildLevelData` 全扫）。
  → 关卡能通，但多出来的像素只能靠**反复「失败→复活→销毁」**抵消，表现是复活次数异常、进度被削；
  若残留的是**交付不了**的机制（如箱子开不出空间），就会转成 F1 的僵局。
- **建议方向**：批量入口先清全部 8 类（与 `ClearBothGroups` 对齐，LevelDataExporter.cs:183-196），统计改用 `CollectPlanningPixels()`。

### F5 🟡 「障碍格上的像素」：运行时算、校验不算 —— 名义计入但永远交付不了

- **证据**：① 与 ③ 只按 `IsInRange` 收像素（GameController.cs:217、PixelGroup.cs:1845），
  **不排除墙格/管道格**；而 ⑤ 与导入导出会排除（ContainerRearranger.cs:121-122、LevelLoader.cs:89-122）。
  同时这类像素因为 `IsBlocked` 恒不暴露（PixelGroup.cs:1003、:1026）→ **永远点不动**。
- **可达性**：正常工具链会在创建墙/管道时清掉那些格的像素（WallItemEditor.cs:316-319、PipeItemEditor.cs:138-142），
  所以只在「手工改 Inspector 里的 `points` 坐标」「手工把像素塞进墙格」时才会出现。
  同类还有**被闭环墙围住的像素**（WallItemEditor.cs:98-113 的"只闭环"分支可以保留内区像素）——
  但这一类 ⑤ 能查出来（Record 总数对不上），**障碍格上的像素查不出来**（⑤ 自己也把它排除了）。
- **后果**：那辆车永远填不满（与 F1 同一后果）。

### F6 🟢 升降台"声明格"与"实际生成格"可能不等（越界 / 无 prefab）

- **证据**：③⑤ 按 `groups` 里声明的三元组计数（PixelGroup.cs:1880-1892、ContainerRearranger.cs:154-167），
  而运行时**逐格判 `IsInRange`**，越界的直接跳过（ElevatorItem.cs:183-188）；`SpawnPixel` 返回 null 时同样跳过。
- **可达性**：升降台的分组来自"选中的 Pixel"（ElevatorItemEditor.cs:53-65），天然在范围内，
  所以只有**手工改 JSON / 改 Inspector 坐标 / 缺 pixelPrefab** 时才出现。
- **后果**：规划容量 > 实际可交付 → 落入 F1 的不足侧。

### F7 🟢 菜单型填充不排除"门格"

- **证据**：`GridFillUtility.FillEmptyCells` 用 `group.IsEmpty`（= 无像素 且 `!IsBlocked`），
  而 `IsBlocked` 只含墙/管道/箱子/木箱（PixelGroup.cs:378），**门格不在内** →
  「填充全部空格」（PixelFillTools.cs:28）与「矩形填充空格」（:190）**会给门格填上像素**；
  像素画布的画笔则另外有门格守卫（PixelColorBrushWindow.cs:4091-4099）。
- **数量口径**：门格上的像素按所在格倍率计，两处口径一致，**不影响总数**；`ValidateGates` 也不检查门格上有没有像素。
  风险在行为侧（门格同时是"围栏的一段"和"像素格"），**需要实测**。
- **建议方向**：`FillEmptyCells` 的判据加上 `&& !group.IsGateCell(c, r)`（口径最小改动）。

### F8 🟢 死锁提示只覆盖"全被盖住"这一种极端

- **证据**：CrateItemEditor.cs:50-70 只在「网格上的像素**全部**被木箱盖住」时报死锁；
  IceItemEditor.cs:154 同理按「不在冻结中冰组内的像素数 == 0」。
- **缺口**：木箱只被"部分阻断"（4 邻没有可移出的像素，或封条数 > 邻域可点击数）时，
  以及冰组盖住"够不到的那些像素"时，都不会提示，但结果同样是**那辆车填不满**。

---

## 6. 建议的处理优先级

**本轮已修**：F3（`TotalPixelCount` 补倍乘额外，1 行）、F2（统计颜色总数改走与规划同一次枚举的 `CollectPlanningDetail()`）。
两个程序集均 0 错误。

| 优先级 | 待办事项 | 代价 |
|---|---|---|
| 1 | 编辑器侧补一条**静态可达性核对**：把"障碍格上的像素 / 被闭环围住的像素 / 越界升降台三元组"挑出来（F1 的病根） | 一个工具 |
| 2 | F4：批量导关清全部 8 类 + 改用规划底座 | 数行 |
| 3 | F1：给"可交付耗尽但仍有未完成车"补一条终局判定（视为数据错误，不要静默卡住） | 需先定口径 |
| 4 | F5/F6/F7/F8：把"障碍格像素""越界升降台""门格像素""部分死锁"并入现有校验/提示 | 逐步 |
| 5 | 顺手把 §2 里"无消费者"的 `ClearedPixelCount`（4 处自增、0 处读取）删掉或接到调试 HUD 上 | 视需要 |

---

## 7. 怎么自检（Unity 侧，可在编辑器里逐条验）

1. **对容量**：选中 ContainerGroup → 「生成 Containers」看日志
   `扫描到 N 个像素（含管道生成与倍乘门额外像素）`；N 应等于各车容量之和
   （「检查并修复容器颜色」的弹窗里也会给出「需要的车数」，可交叉核对）。
2. **对逐色**：先跑「检查并修复容器颜色」；**含箱子的关卡**再点一次 PixelGroup 的
   「统计颜色总数」，**对比两者的逐色数字** —— F2 修完后这两处同源，数字**必须一致**
   （不一致说明又冒出了第二条统计路径）。
3. **对倍乘**：选中 PixelGroup → 「校验倍乘门」，弹窗里的 `合计 N` 就是"可交付总数"；
   与 Record 文件名里的 `total` **应相等**（F3 修完后的预期）。
   例外是**箱子跨在门边界上**的关卡：底座按箱子锚点格算、Record 按释放后的实际格算，会差几颗（已知近似）。
4. **对记录**：Record 模式跑一遍（`recordMode=true`），看落盘文件名里的 `total` 与文件内实际行数是否一致。
5. **对可达性**（F1 的病根，最值得看）：开 `debugClickLog` / `debugFailLog`，
   如果通道上只剩「未判失败」的日志、带上的像素也没有同色车可匹配，却始终不判失败，
   说明还有"名义计入但交付不了"的像素 —— 逐个机制排查：冰是否化得开、木箱是否拆得掉、
   升降台是否升得完、有没有被闭环墙围住的像素。
