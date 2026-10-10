# 道具1「刷新」— 移植说明（自包含）

> 本文用于把「刷新（洗牌）」这个道具功能从一个版本搬到**同项目的另一个版本**。
> 目标：对方版本那边**只看这一份文档就能照搬**，不需要访问源版本仓库。
>
> 源版本：CrowdMatch（`Assets/Scripts/Gameplay/ContainerGroup.cs` + `Assets/-------/_Script/GameInnerUI.cs`）
> 移植日期：2026-10-09

---

## 0. 这是什么功能（行为规格）

**道具1**（源版本里是 `ItemType.Refresh`）从"空壳只打日志"接成真功能：

| # | 行为 |
|---|---|
| 1 | 点道具1 → 把**盘面上还没开走**的车的前后顺序**随机重排** |
| 2 | **可以跨列**：车会换到别的列去（不只是同列上下换） |
| 3 | **载着乘客的车也参与**，乘客跟着车一起走（乘客是车的子物体） |
| 4 | **绳组车原地不动**（绳链靠相邻列各车的相对偏移成立，动一辆会扯断） |
| 5 | **换位是瞬间的**（瞬移），不播动画 |
| 6 | **点按钮就扣**道具 —— 即使某列只有 1 辆车、洗了看不出变化，也照扣 |
| 7 | 已经开走的车不参与（它们在点击那一刻就已经被移出 `grid` 了，天然排除） |

**能力边界**：只改「谁先出库」，**不改车数、不改容量、不改颜色** —— 所以判胜用的"未完成匹配的车数"完全不受影响。

> 「洗的是谁占哪个格，**格集合本身不动**」是这套做法的关键：每列的**占用行集合不变**，所以"列内压紧连续"（`DescribeColumnHoles` 守着的那个不变量）依然成立，不会洗出空洞。

---

## 1. 前提与版本差异检查（★ 先做这一步）

本功能**不依赖任何 prefab / 美术 / 场景资源 / ScriptableObject**，是纯代码移植，只动 2 个文件。

| # | 依赖符号 | 说明 / 缺失时怎么办 |
|---|---|---|
| 1 | `ContainerGroup.grid`（`ContainerItem[,]`）、`columns`、`rows` | 都是 `public` ✓ 核心依赖 |
| 2 | `ContainerGroup.GetLocalPosition(col,row)` | 把格坐标换成车该在的局部位置 |
| 3 | `ContainerItem.gridX / gridZ` | 车的当前格（**会被本功能改写**） |
| 4 | `ContainerItem.ropeGroupId` | **绳组车不洗**；若目标版本没有绳子系统，可以整段跳过（但要保留别的守卫） |
| 5 | `ContainerItem.isRefilling` / `IsBoarding` | 正在补位 / 正在上车的车跳过（会和动画打架） |
| 6 | `ContainerItem.HideLid()` | 落到第一排的车开盖（与 `RebuildGrid` 同口径） |
| 7 | `ContainerGroup.TryExitIfAtFront(item, col)` | 落位后触发一次出库检查（**private**，本功能写在同一个类里所以能调） |
| 8 | **数据层**：`_cells` / `HasData` / `CellIndex(col,row)` / `ContainerCell`（含 `col,row`） | 懒实例化那套。**若目标版本没有数据层**（老版本：网格里每格都有实例），把 `cells` 相关的行整段删掉即可 —— 删掉后本功能依然正确（那套只是为了让未实例化的深排车也保持状态） |
| 9 | `GameController.Instance.containerGroup` | UI 侧取容器组的入口 |
| 10 | `GameInnerUI.CousmeProp(ItemType)` / `UpdateCurrentButtonInfo()` | 扣道具与刷新显示 |
| 11 | `ContainerItem` / `ContainerCell` 所在命名空间 | 源版本是 `CrowdMatch` |

**无需新增任何 Inspector 字段** ✓（本功能没有可调参数）

---

## 2. 改动一：`ContainerGroup.cs` 新增 `ShuffleRemainingCars()`

放在 `MoveCell(...)` 之后（`MoveCell` 是数据层搬格子的 helper，本功能用的是它的同一套思路）：

```csharp
/// <summary>
/// 道具1「刷新」：把**还没开走**的车的前后顺序随机重排（含载有乘客的车——乘客是车的子物体，会跟着一起走）。
///
/// 口径（已与用户核对）：
/// · **全盘一起洗（可跨列）**：洗的是「谁占哪个格」，**格集合本身不动**，所以每列的占用行集合不变、
///   「压紧连续」仍然成立。颜色 / 容量 / 乘客都跟着车走，不是重刷颜色；
/// · **绳组车（ropeGroupId != 0）原地不动**：绳链靠相邻列各车的相对偏移成立，动其中一辆会扯断；
/// · **正在补位 / 正在上车的车跳过**：挪它会和正在播的动画打架；
/// · 只洗**当前有实例的格**。运行模式是懒实例化（只有视窗内的排有实例，深排只有数据层），
///   把深排数据替进视窗会得到「有数据没实例」的隐形车。
///
/// 换位是**瞬间**的（直接改 grid / gridX / gridZ / localPosition），不播动画。
/// </summary>
public void ShuffleRemainingCars()
{
    if (grid == null)
        return;

    var slots = new List<Vector2Int>();    // 参与洗牌的格 (col, row)
    var cars = new List<ContainerItem>();  // 与 slots 一一对应的车
    var cells = new List<ContainerCell>(); // 与 cars 配对的数据层条目

    for (int col = 0; col < columns; col++)
        for (int row = 0; row < rows; row++)
        {
            var item = grid[col, row];
            if (item == null)
                continue;                          // 空格
            if (item.ropeGroupId != 0)
                continue;                          // 绳组：原地不动
            if (item.isRefilling || item.IsBoarding)
                continue;                          // 正在动：跳过

            slots.Add(new Vector2Int(col, row));
            cars.Add(item);
            cells.Add(HasData ? _cells[CellIndex(col, row)] : default(ContainerCell));
        }

    if (cars.Count < 2)
        return;                                    // 0/1 辆：洗了也不变

    // Fisher-Yates：车与数据层条目一起洗，保持二者配对
    for (int i = cars.Count - 1; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);
        var tc = cars[i]; cars[i] = cars[j]; cars[j] = tc;
        var td = cells[i]; cells[i] = cells[j]; cells[j] = td;
    }

    // 全部落位。格集合与车集合是 1:1，逐个写入即可覆盖所有格，不会留下旧的 grid 残留。
    var frontCars = new List<ContainerItem>();
    for (int i = 0; i < cars.Count; i++)
    {
        int col = slots[i].x;
        int row = slots[i].y;
        var item = cars[i];

        grid[col, row] = item;
        item.gridX = col;
        item.gridZ = row;
        item.transform.localPosition = GetLocalPosition(col, row);

        if (HasData)
        {
            var c = cells[i];
            c.col = col;
            c.row = row;
            _cells[CellIndex(col, row)] = c;
        }

        if (row == 0)
        {
            item.HideLid();   // 与 RebuildGrid 同口径：落到第一排的车直接开盖
            frontCars.Add(item);
        }
    }

    // 落位之后再触发出库检查：新落到第一排的车可能已经装满，该走就得走。
    // 必须放在落位循环之后 —— 出库会引发补位、改动盘面，边放边查会错位。
    for (int i = 0; i < frontCars.Count; i++)
    {
        var item = frontCars[i];
        if (item != null)
            TryExitIfAtFront(item, item.gridX);
    }
}
```

**requires**：文件顶部要有 `using System.Collections.Generic;`（源版本本来就有，`List` / `Dictionary` 一直在用）。

---

## 3. 改动二：`GameInnerUI.cs` 按钮接线

找到道具1的按钮处理方法（源版本叫 **`OnRefreshBtnClick`**，由 `refreshBtn?.onClick.AddListener(OnRefreshBtnClick);` 绑定），把它的 `else` 分支换成：

```csharp
else
{
    // 道具1「刷新」：把每列还没开走的车的前后顺序随机重排（绳组车原地不动）。
    // 按约定「点按钮就扣」—— 即使某列只有 1 辆车、洗了看不出变化，也照扣。
    var cg = GameController.Instance != null ? GameController.Instance.containerGroup : null;
    if (cg != null)
        cg.ShuffleRemainingCars();

    Debug.Log("使用了道具1刷新：随机重排未开走的车");
    CousmeProp(itemType);
    UpdateCurrentButtonInfo();
    PlayerPrefs.Save();
}
```

> **⚠️ 别按名字认道具**：源版本的 `ItemType` 枚举被改过名
> （`None=0, Refresh=1, Magnet=2, Remove=3`；更早是 `None=0, Add=1, Remove=2, Clear=3`）。
> **按按钮的处理方法定位**，看它内部用的是哪个枚举成员，以那个为准。
> 上面的代码里 `itemType` 就是该方法里已经声明好的那个局部变量，直接用。

**方法顶部原有的门槛不要动**：解锁等级检查、`GameState.IsGameStart` 检查、`if (!CanUseProp(itemType))` 才能走到 `else` —— 这些是通用的道具闸门，本功能只是替换了 `else` 里的内容。

---

## 4. 移植顺序与检查点

```
1. ContainerGroup.cs 加 ShuffleRemainingCars()  → 编译
2. GameInnerUI.cs 换掉 else 分支                → 编译
3. 跑 Play 验收（§5）
```

**每步编译**：能立刻暴露"目标版本没有某个符号"（比如没有数据层、没有 `ropeGroupId`），比最后一起报错好定位。

---

## 5. 验收清单

| # | 预期 |
|---|---|
| 1 | 点道具1 → 车**跨列**瞬间换位（不只是同列上下换） |
| 2 | 每列仍然**压紧连续**、没有空洞 |
| 3 | **各列的车数不变**（只是谁占哪些格变了） |
| 4 | **载客的车**换位后，乘客还坐在车上（没有落在原地） |
| 5 | **绳组车原地不动**，链子没歪 |
| 6 | 换到第一排的车**盖子打开**；本来就装满的话**立刻出库** |
| 7 | 道具数 **-1**，Console 打 `使用了道具1刷新：随机重排未开走的车` |
| 8 | **普通玩法完全没变**（这条最重要） |

---

## 6. 设计要点 / 易错点

### 6.1 为什么"格集合不动"是安全的
洗的是**谁占哪个格**，不是"把车挪到别的空格"。所以：
- 每列的**占用行集合**不变 → 「列内压紧连续」不变量保住 ✓
- 车数、容量、颜色都不变 → 判胜口径（`_unfinishedCars`）不受影响 ✓

若改成"给车另找空格"，就会洗出空洞、破坏压紧前提 ✗

### 6.2 数据层必须跟着一起洗
`_cells[col,row]` 与它上面的车是**配对**的（懒实例化下深排车只有数据层）。车换了格而数据层没跟着换，等这辆车补位滚进视窗被实例化时，会**水合到错误的颜色** ✗
所以 `cars` 和 `cells` 用**同一套 Fisher-Yates 交换**，保持配对 ✓

### 6.3 `HideLid()` 会锁存"已开盖"
落到第一排的车调 `HideLid()`，它会 `lidOpened = true` 并揭晓问号车。之后这辆车被洗到后排也**不会变回未开盖** —— 反复洗牌会让更多问号车提前揭晓。
不算泄密（玩家确实在第一排见过它），但如果希望问号车严格只在"正常推进到前排"时揭晓，就得单独处理。

### 6.4 出库检查必须在**全部落位之后**
`TryExitIfAtFront` 可能引发 `StartContainerExit` → `RefillColumn`，那会**当场改动本列布局** ✗
边放边查会错位，所以收集所有落到第一排的车，循环结束后再逐个查 ✓

### 6.5 只洗"有实例的格"
运行模式是懒实例化：只有视窗内的排有实例，深排只有数据层。
把深排的数据替进视窗会得到**「有数据、没实例」的隐形车** ✗
所以遍历 `grid`（有实例的格才非 null）天然只覆盖视窗内的排 ✓ 深排不动 ✓

---

## 7. 附：本次移植涉及的文件一览

| 文件 | 改动性质 |
|---|---|
| `Assets/Scripts/Gameplay/ContainerGroup.cs` | 1 个新方法（约 75 行） |
| `Assets/-------/_Script/GameInnerUI.cs` | 换掉 1 个 `else` 分支 |

**零 prefab / 零场景 / 零美术 / 零 ScriptableObject / 零 Inspector 字段。**
