# 编辑器菜单清单与来源归档

> 归档日期：2026-09-23
> 范围：Unity 菜单栏里**不属于 `CrowdMatch` 根菜单**的自定义菜单，以及 `CrowdMatch` 菜单的排序归属。
>
> **本文只做记录，不改任何代码。** 下面这两个菜单的作者不是做菜单整理的人（`CrowdMatch` 那一支），
> 是否合并 / 停用 / 改名，待与作者确认后再动。

---

## 0. 一句话结论

| 菜单 | 文件 | 谁加的 | 什么时候 | 判定 |
|---|---|---|---|---|
| `工具 ▸ 排列工具 ▸ 打开排列面板` | `Assets/Editor/ArrangeWindow.cs` | bobmu-git | 2026-09-01（提交 `1d4e1df`，message「1」） | 团队**自写**的美术摆位工具 |
| `Tools2 ▸ GameViewSize ▸`（4 项） | `Assets/-------/_Script/GameViewTools.cs` | codexiaobai001 | 2026-09-22（提交 `da38cbd`，message「困难关卡」） | **从网上抄来的现成脚本** |

两份文件在 git 里**各自只有「新增」那一条提交，之后从未被修改**；当前工作区也没有未提交改动。

---

## 1. `工具 ▸ 排列工具 ▸ 打开排列面板`（ArrangeWindow）

### 1.1 来源

- 文件：`Assets/Editor/ArrangeWindow.cs`（全局命名空间，没有 `namespace`）
- 引入：`1d4e1df`，2026-09-01，作者 **bobmu-git**
- 判为自写的依据：同一笔提交**新建了 `Assets/Editor/` 文件夹**、新增 `Assets/Prefabs/Block.prefab`，
  并把 `Assets/Scenes/ART_Scene.unity` 改了 19732 行 —— 典型的「美术场景线开工」提交，
  而这个面板的用途正好是那件事（批量摆位）。代码风格也是团队自用的样子（无命名空间、中文注释、`B方案` 之类的过程化措辞）。

### 1.2 功能

打开一个「模型排列面板」窗口，对**当前多选的场景物体**按 `Selection.gameObjects` 的顺序批量摆位置：

| 控件 | 作用 |
|---|---|
| `X轴横向间距` / `Z轴纵向间距` | 两轴独立间距，默认都是 `1.2` |
| `矩阵每行数量` | 矩阵模式的每行个数，默认 `10` |
| `横向X轴 排成一排` | 以**第一个**物体的 position 为起点，沿 +X 按 X 间距依次排开 |
| `纵向Z轴 排成一排` | 同上，沿 +Z 按 Z 间距 |
| `二维网格矩阵排列` | X 用横向间距、Z 用纵向间距铺成矩阵，每行 `矩阵每行数量` 个 |
| `随机打乱(交换点位不重叠)` | **复用现有全部点位**，让物体互相交换位置 —— 不新增坐标，所以不会重叠 |

使用要点：

- 少于 2 个物体时弹窗拦截，不执行；
- 每个物体落位前都 `Undo.RecordObject`，所以整次排列可 **Ctrl+Z 一步撤销**；
- 是纯 Transform 位置操作，不涉及任何 `CrowdMatch` 玩法数据（像素 / 容器 / 墙体…）。

### 1.3 目的（推断）

美术搭 ART 场景时省手工拖：要么把一批方块/装饰等间距排整齐，要么把已经摆好的一批点位随机换位置。
与玩法无关，属于场景制作期的辅助工具。

---

## 2. `Tools2 ▸ GameViewSize ▸`（GameViewTools）

### 2.1 来源

- 文件：`Assets/-------/_Script/GameViewTools.cs`（全局命名空间）
- 引入：`da38cbd`，2026-09-22，作者 **codexiaobai001**
- 同批进来的还有 `Assets/-------/_Script/ScreenShot.cs`
- 所在目录 `Assets/-------/` 是**UI 框架目录**：由同一位作者从 2026-09-16 起按
  「设置界面 → 部分ui → ui2 → ui3 → …」逐步搭起来的（`UIManager` / `MainPanel` / `FailPanel` /
  `RevivePanel` / `WinPanel` / `SettingPanel` / `StaminaSystem` / `DailyBouns` …）。
  目录名用 7 个减号是常见手法：让它在 Project 窗口里排到最上面。

### 2.2 判为「外部抄来的」的形态证据

先说清性质：这是**形态推断**，不是直接证据 —— 本人照文章敲一遍也会长成这样。无论哪种，它都不是为这个工程专门设计的。

| 痕迹 | 说明 |
|---|---|
| 菜单名 `Tools2/...` | 不是本工程的命名习惯（`CrowdMatch` / `工具`），更像是随手起的临时菜单 |
| 反射 Unity 内部 API | 全程反射 `UnityEditor.GameViewSizes` / `GameViewSize` / `GameView`，这是「给 Game 视图加自定义分辨率」那类文章的经典写法 |
| 成段注释掉的重复调用 | `//AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Android, 2048, 2732, "-iPad");` 与现役的 `AddCurCustomSize(...)` 逐行并排保留 |
| 调试残留 | `Debug.Log("-->>>" + maxIndex)` |
| 冗余的 `#if UNITY_EDITOR` | 文件整体已包一层，里面**每个方法**又各包一层 |
| 重复声明的枚举 | 自己又写了一个 `GameViewSizeType`（Unity 内部本来就有同名的） |

### 2.3 功能

通过反射操作 Unity 内部的 `GameViewSizes` 单例，**给 Game 视图的分辨率下拉列表增删自定义尺寸**，并在它们之间循环切换。

| 菜单项 | 作用 |
|---|---|
| `Init` | 读当前平台（内部静态字段 `GameView.s_GameViewSizeGroupType`）→ 取该平台的尺寸组 → 初始化全部反射引用，并把当前尺寸总数记为基准（`minIndex` / `maxIndex` / `screenIndex`）。若之前 `AddAll` 过，会先把自己加的那批清掉再重记 |
| `AddAll` | 给当前平台加 4 个固定分辨率的自定义尺寸：`2048×2732 "-iPad"`、`1242×2688 "-iPhoneXs Max"`、`1242×2208 "-iPhone 8Plus"`、`1080×1920 "-Androdi"`（后缀拼写就是原样） |
| `RemoveAll␠` | 逐个删掉 `AddAll` 加的那批（`while (maxIndex - minIndex > 0)`），删之前先把当前尺寸回退一格。**菜单名尾部带一个空格**，Unity 里显示成「RemoveAll 」 |
| `NextLoop  %&N` | 在已注册的尺寸之间循环切到下一个（越界回到第一个）。快捷键 `%&N` = **Ctrl+Alt+N** |

### 2.4 已知脆弱点（只记录，未改）

1. **全是硬编码反射**，且调用处用了 `?.Invoke` —— Unity 升级后内部结构一变就**静默失效**：菜单照点、不报错、毫无反应。当前工程 2021.3.14f1 可用，换版本必须重验。
2. **`Init` 是前置步骤**：`minIndex` / `maxIndex` 只在 `Init` / `AddAll` 里写过。编辑器重启（域重载）后静态值归 0 → 直接点 `RemoveAll` 时 `maxIndex - minIndex == 0`，循环不执行，等于什么都不做。实际用法是「`Init`（或 `AddAll`）→ `RemoveAll`」成对跑。
3. **只作用于当前平台的尺寸组**，切到别的平台要重跑一遍。
4. 菜单名尾部空格（`"RemoveAll "`）与 `Tools2` 这个根菜单名都不符合工程习惯。

### 2.5 目的（推断）

**UI 多机型适配预览**：UI 同事做适配时，一键把目标机型分辨率塞进 Game 视图，再用 `Ctrl+Alt+N` 挨个循环过一遍。这也解释了它为什么落在 UI 框架目录里，而不是编辑器工具目录。

---

## 3. `CrowdMatch` 菜单（顺带记录，2026-09-23 整理）

- 顶层从 **26 项平铺** → **2 个画布 + 4 项关卡操作 + 4 个分组**，组与组之间有分隔线。
- **顺序常量全部集中在 `Assets/Scripts/Editor/MenuPriority.cs`**（含结构树与两条 Unity 规则说明）。
  为避免两处描述互相漂，本文**不重复列举每一项**，要看结构直接读那个文件。
- 整理时踩到并写进该文件的坑：**菜单名里不能出现半角 `/`** —— Unity 按 `/` 切路径，`更多工具（低频 / 一次性）` 会当场多出一层子菜单。
- 整理过程中同步更新了引用旧菜单路径的文档：`Docs/DemoFeatureDocumentation.md`（2 处）、`Docs/BoxDesign.md`（1 处）。

---

## 4. 如何复核这份归档

```bash
# 各文件的完整提交历史（只有一条 = 之后没人改过）
git log --format='%h | %ad | %an | %s' --date=short -- Assets/Editor/ArrangeWindow.cs
git log --format='%h | %ad | %an | %s' --date=short -- 'Assets/-------/_Script/GameViewTools.cs'

# 查看引入那笔提交都改了什么
git show --stat 1d4e1df     # 排列面板：新建 Assets/Editor/ + Block.prefab + ART_Scene 大改
git show --stat da38cbd     # GameViewSize：与 ScreenShot.cs 同批进 UI 目录

# UI 框架目录的搭建过程
git log --format='%h | %ad | %an | %s' --date=short --reverse -- 'Assets/-------/'
```

---

## 5. 待办（均未执行，需先与作者确认）

| # | 事项 | 归属 |
|---|---|---|
| 1 | 是否把「排列面板」并进 `CrowdMatch`（例如 `更多工具`），或维持独立 `工具` 菜单 | bobmu-git |
| 2 | 是否把 `GameViewSize` 那 4 项并进 `CrowdMatch`、或改名 `Tools2` / 去掉 `RemoveAll ` 尾部空格 | codexiaobai001 |
| 3 | 反射失效的兜底提示（Unity 升级后至少打一条明确 warning，而不是静默无效） | codexiaobai001 |
| 4 | 两个文件都不在 `CrowdMatch` 命名空间下，是否统一 | 待定 |

> 结论：**本次只归档，不动代码。** 上面 4 项都涉及别人的工具，先问清还有没有人在用。
