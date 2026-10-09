# 道具2「磁铁」— 移植说明（自包含）

> 本文用于把「磁铁」这个道具功能从一个版本搬到**同项目的另一个版本**。
> 目标：对方版本那边**只看这一份文档就能照搬**，不需要访问源版本仓库。
>
> 源版本：CrowdMatch（`Assets/Scripts/Gameplay/GameController.cs` + `Assets/-------/_Script/GameInnerUI.cs`）
> 移植日期：2026-10-09

---

## 0. 这是什么功能（行为规格）

**道具2**（源版本里是 `ItemType.Magnet`）从"空壳只打日志"接成真功能：

| # | 行为 |
|---|---|
| 1 | 点道具2 → 把**最前面一排**车（每列 `grid[col, 0]`）用**棋盘上的同色像素**喂满 |
| 2 | 喂进去的表现是**瞬移**（像素原地弹一下 → 出现在车的座位上），不播走路 |
| 3 | **装满的车走正常出库链路**（`StartContainerExit` → 补位 → 回池） |
| 4 | **装不满的车留在前排等待** —— 能吸多少吸多少，不强求出库 |
| 5 | **点按钮就扣**道具 |
| 6 | 木箱盖住 / 冰组冻住 / 未揭晓问号 的像素**不捞** |
| 7 | 木箱、冰冻结界、传送带堆积门槛等**照旧拦截**（磁铁只放宽"能否连通到首排"这件事） |

**能力边界**：磁铁的价值是"**无视前排连通限制**" —— 那些被别的像素围住、普通点击永远点不动的同色像素，靠它才能直接送进车。

**像素来源：只捞棋盘。** 不碰传送带 / 缓冲区 —— 那里的像素本来就会按正常流程匹配到同色车，而把它们抽出来只有**破坏性**的 Drain API（复活那套会把没匹配上的像素直接销毁），磁铁不该带这种副作用。

---

## 1. 前提与版本差异检查（★ 先做这一步）

本功能**不依赖任何 prefab / 美术 / 场景资源 / ScriptableObject**。

| # | 依赖符号 | 说明 / 缺失时怎么办 |
|---|---|---|
| 1 | **`ContainerGroup.ConsumePixelInstant(PixelItem, ContainerItem)`** | ⚠️ **核心依赖**：像素"弹出 → 瞬间出现在车上落点"的那条路。<br>**若目标版本没有它**：用 `ConsumePixel(pixel, car)` 替代（它走跳车动画，表现不同但语义一致）；或在 `ContainerGroup` 里把这条公开入口补回去 |
| 2 | `ContainerGroup.grid`、`columns` | 取每列的 `grid[col, 0]` |
| 3 | `ContainerItem.IsEmpty` / `colorId` / `OpenLid()` | 判断还没装满、匹配颜色、开盖（幂等） |
| 4 | `PixelGroup.grid` / `columns` / `TotalRows` | 扫棋盘找同色像素 |
| 5 | `PixelGroup.GateMultiplierAt(col,row)` | 记进度分子（见 §6.4） |
| 6 | `PixelGroup.TryOpenBoxes()` / `TryAdvanceElevators()` / `RefreshExposed()` | **收尾四步**里的前三步（见 §6.2） |
| 7 | `GameController.RefreshFrame()` | 收尾第四步（**private**，本功能写在同一个类里所以能调） |
| 8 | `PixelItem.IsCovered` / `IsFrozen` / `isQuestion` / `revealed` | 决定哪些像素不捞 |
| 9 | `PixelItem.SetExposed / SetClickable / SetWalking / gridX / gridZ` | 移出网格的常规处理 |
| 10 | **`PixelItem.SetPropGlow(bool)`** | ⚠️ **这是「道具3」的功能**！磁铁的 `DetachPixelForMagnet` 里调了它。<br>**若目标版本不打算移植道具3**：把 `DetachPixelForMagnet` 里那一行 `item.SetPropGlow(false);` **删掉**即可 ✓（它只是清掉道具3的强制取出高亮，与磁铁无关） |
| 11 | `GameData.RemovePixelCount` / `ProgressPixelCount` | 进度口径（若目标版本没有，删掉这两行；见 §6.4） |
| 12 | `GameController.Instance.containerGroup` | UI 侧入口 |
| 13 | `GameInnerUI.CousmeProp(ItemType)` / `UpdateCurrentButtonInfo()` | 扣道具与刷新显示 |

**无需新增任何 Inspector 字段** ✓（本功能没有可调参数）

---

## 2. 改动一：`GameController.cs` 新增三个方法

放在「道具3」那组方法旁边（源版本里它们挨着，便于一起维护）。

### 2.1 主逻辑 `MagnetClearFrontRow()`

```csharp
/// <summary>
/// 道具2「磁铁」：把**最前面一排**车（每列 <c>grid[col, 0]</c>）用同色像素喂满，装满的车随即走正常出库链路。
///
/// 口径（已与用户核对）：
/// · 像素**从棋盘上捞**，无视前排连通限制（磁铁本来就该能捞被围住的）；
/// · **不碰传送带 / 缓冲区**：那里的像素本来就会按正常流程匹配到同色车，而把它们抽出来只有破坏性的
///   Drain API（复活那套会把没匹配上的像素直接销毁），磁铁不该带这种副作用；
/// · 装不满的车**留在前排等待**（能吸多少吸多少，不强求出库）；
/// · 未揭晓的问号像素不捞 —— 捞走会泄露它的颜色。
///
/// 表现走 <see cref="ContainerGroup.ConsumePixelInstant"/> 的瞬移（弹出即出现在车上），不播走路。
/// </summary>
public void MagnetClearFrontRow()
{
    if (pixelGroup == null || containerGroup == null)
        return;

    var carGrid = containerGroup.grid;
    if (carGrid == null)
        return;

    bool detachedAny = false;

    for (int col = 0; col < containerGroup.columns; col++)
    {
        var car = carGrid[col, 0];
        if (car == null)
            continue;

        car.OpenLid();   // 幂等：前排车本来就是开盖的

        while (!car.IsEmpty)
        {
            var pixel = FindBoardPixelOfColor(car.colorId);
            if (pixel == null)
                break;   // 场上没有同色像素了 → 装不满，留在前排等待

            DetachPixelForMagnet(pixel);
            detachedAny = true;
            containerGroup.ConsumePixelInstant(pixel, car);
        }
    }

    // 收尾必须与 ResolveMatch 的移出收尾**逐条对齐**，少一条就会留下「看不见的遮挡」：
    //   TryOpenBoxes / TryAdvanceElevators —— 箱子与升降台的占格要跟着释放；
    //     不释放那些格依旧是障碍（IsBlocked），旁边的人既不会亮、也点不出去。
    //   RefreshExposed —— 重算暴露。
    //   RefreshFrame   —— 重建整体描边。场景里有 FrameItem 时描边是它统一画的
    //     （PixelItem 自身描边被关掉），不重建就还是旧轮廓 → 「该发白光的没发」。
    if (detachedAny)
    {
        pixelGroup.TryOpenBoxes();
        pixelGroup.TryAdvanceElevators();
        pixelGroup.RefreshExposed();
        RefreshFrame();
    }
}
```

### 2.2 找同色像素 `FindBoardPixelOfColor(int)`

```csharp
/// <summary>
/// 在棋盘上找一个该颜色的像素（无视**其他像素**的阻挡）。
/// 但**不捞**与「能点」集合一致的那三类：被木箱盖住、被冰组冻住、未揭晓问号。
/// 前两者捞走会破坏木箱 / 冰组的账——它们记着自己在哪些格上，格不释放就变成看不见的障碍；
/// 问号则是因为颜色本身就是秘密。这几类本来点了也没反馈，不捞它们不会让磁铁显得失灵。
/// </summary>
private PixelItem FindBoardPixelOfColor(int colorId)
{
    var grid = pixelGroup.grid;
    if (grid == null)
        return null;

    int cols = pixelGroup.columns;
    int totalRows = pixelGroup.TotalRows;

    for (int row = 0; row < totalRows; row++)
        for (int col = 0; col < cols; col++)
        {
            var p = grid[col, row];
            if (p == null)
                continue;
            if (p.colorId != colorId)
                continue;
            if (p.IsCovered)
                continue;                          // 木箱盖住
            if (p.IsFrozen)
                continue;                          // 冰组冻住
            if (p.isQuestion && !p.revealed)
                continue;                          // 未揭晓问号：会泄露颜色

            return p;
        }
    return null;
}
```

### 2.3 移出网格 `DetachPixelForMagnet(PixelItem)`

```csharp
/// <summary>
/// 把像素从像素网格摘掉（磁铁捞走后立刻瞬移上车，不走传送带）。
/// 与 <see cref="ResolveMatch"/> 的移出口径保持一致：置空格、关暴露/点击、清道具高亮，
/// 并记上「已点出」与进度分子（不记的话进度条永远到不了 100%）。
/// </summary>
private void DetachPixelForMagnet(PixelItem item)
{
    int col = item.gridX;
    int row = item.gridZ;

    pixelGroup.grid[col, row] = null;
    item.SetExposed(false);
    item.SetPropGlow(false);          // ← 若目标版本不移植道具3，删掉这一行（见 §1 第 10 项）
    item.SetClickable(false);
    item.SetWalking(false);   // 瞬移上车，不播走路

    GameData.RemovePixelCount++;
    GameData.ProgressPixelCount += Mathf.Max(1, pixelGroup.GateMultiplierAt(col, row));
}
```

> ⚠️ **`ConsumePixelInstant` 是给"已经离格"的像素用的**（它只动 transform，不碰 `pixelGroup.grid`）。
> 所以**必须**先 `DetachPixelForMagnet` 把像素从网格摘掉 —— 否则网格里会留下一颗**隐形幽灵像素**堵着路（不亮、点不动、还挡着旁边的组）✗✗

---

## 3. 改动二：`GameInnerUI.cs` 按钮接线

找到道具2的按钮处理方法（源版本叫 **`OnMagnetBtnClick`**，由 `magnetBtn?.onClick.AddListener(OnMagnetBtnClick);` 绑定），把它的 `else` 分支换成：

```csharp
else
{
    // 道具2「磁铁」：把最前面一排车用同色像素喂满，装满的车随即出库。
    // 像素从棋盘上捞（无视阻挡）；传送带 / 缓冲区不碰 —— 那里的像素本来就会按正常流程匹配到同色车。
    Debug.Log("使用了道具2磁铁：最前面一排磁吸同色像素");
    var gc = GameController.Instance;
    if (gc != null)
        gc.MagnetClearFrontRow();

    CousmeProp(itemType);   // 点按钮就扣
    UpdateCurrentButtonInfo();
    PlayerPrefs.Save();
}
```

> **⚠️ 别按名字认道具**：源版本的 `ItemType` 枚举被改过名
> （`None=0, Refresh=1, Magnet=2, Remove=3`）。**按按钮的处理方法定位**，看它内部用的哪个枚举成员。

---

## 4. 移植顺序与检查点

```
1. GameController.cs 加三个方法（MagnetClearFrontRow / FindBoardPixelOfColor / DetachPixelForMagnet） → 编译
2. GameInnerUI.cs 换掉 else 分支 → 编译
3. 跑 Play 验收（§5）
```

**每步编译**：能立刻暴露"目标版本没有 `ConsumePixelInstant` / `SetPropGlow` / `ProgressPixelCount`"这类差异，比最后一起报错好定位。

---

## 5. 验收清单

| # | 预期 |
|---|---|
| 1 | 点道具2 → 每列**最前面那辆车**把棋盘上同色像素**瞬移**吸进去（弹出即出现在车上） |
| 2 | 吸满 → 车**正常出库**并补位 |
| 3 | 场上同色像素不够 → 车**留在前排**，不强行出库 |
| 4 | **被围住的像素也能被吸走**（磁铁的核心价值） |
| 5 | 未揭晓的问号像素**没被吸走**；木箱盖住 / 冰组冻住的也**没被吸走** |
| 6 | 进度条和「已点出」计数正常增长 |
| 7 | 道具数 **-1** |
| 8 | **不留幽灵**：吸完之后，旁边原本被堵住的像素**能亮、也能点出去**（这条是 §6.1 / §6.2 的验收） |
| 9 | **普通玩法完全没变** |

---

## 6. 设计要点 / 易错点（本次实现踩过的）

### 6.1 必须先摘网格，再上车
`ConsumePixelInstant` 假定像素**已经不在网格里**（复活路径传的就是已离格的像素）。磁铁捞的却是**还在棋盘上**的像素 —— 直接调它，网格会留下一颗隐形的幽灵像素：那格仍算占用 → 旁边的人连不到首排 → 既不发白光也点不出去 ✗
所以顺序必须是：**`DetachPixelForMagnet` → `ConsumePixelInstant`** ✓

### 6.2 收尾"四步"一条都不能少
移出像素之后必须照抄 `ResolveMatch` 的收尾：

```csharp
pixelGroup.TryOpenBoxes();        // 箱子 / 升降台的占格要跟着释放
pixelGroup.TryAdvanceElevators(); //   —— 不释放，那些格依旧是障碍 → 旁边的人点不出去
pixelGroup.RefreshExposed();      // 重算暴露
RefreshFrame();                   // 重建整体描边 —— 不建就还是旧轮廓 → 「该发白光的没发」
```

**这一条是本次踩坑最久的地方**：当时只调了 `RefreshExposed()`，结果玩家反馈"有像素不发白光、点了不动"。根因就是漏了 `RefreshFrame()`（整体描边由 `FrameItem` 统一绘制，不重建就是旧的）和 `TryOpenBoxes()`（箱子占格没释放，留下看不见的障碍）。

### 6.3 为什么"只捞棋盘"
传送带 / 缓冲区那侧只有**破坏性**的取出 API（`DrainAllPixels` / `DrainBeltKeep`，而且没有"放回"），复活路径正是用它们并把没匹配上的像素**直接销毁**。
磁铁照抄就会把玩家还要用的像素销毁掉 ✗ —— 而且那边的像素本来就会自动匹配到同色车，抽它们既不必要也危险。
**所以磁铁只捞 `pixelGroup.grid`。**

### 6.4 进度口径
`DetachPixelForMagnet` 里那两笔与 `ResolveMatch` 的移出口径**完全一致**：
- `RemovePixelCount++` —— "已点出"计数
- `ProgressPixelCount += max(1, GateMultiplierAt(col,row))` —— 进度分子按**计划口径**累加（乘上倍乘门倍率）

少记 `ProgressPixelCount` 的话，**进度条永远到不了 100%**（因为磁铁带走的像素不走 `ResolveMatch`，那一笔就没人补）✗

### 6.5 ⚠️ 一个既有的副作用（不是磁铁造成的，但会被它"逼出来"）
**磁铁把像素直接喂进车、不走传送带。** 而项目里存在一个**独立的死锁**：管道要"轨道格全空"才投放下一波，可它"还有未释放波次"时又会把整条轨道算作障碍 —— 于是**摆在管道轨道上的像素点不动、管道也永远不放波**。
用磁铁把棋盘上的自由像素大量消耗后，玩家更容易去点那片区域，从而撞见这个死锁。
**它跟磁铁无关**，根治要改 `CanReachFront` 的判定顺序（让"管道格上恰好是本组像素"这种情况放行）。

### 6.6 装不满的车不强求出库
`while (!car.IsEmpty)` 在找不到同色像素时 `break` —— 车留前排等着。这样"磁铁吸不满"不会变成一次性损失，玩家补上像素后车照常出库 ✓

---

## 7. 附：本次移植涉及的文件一览

| 文件 | 改动性质 |
|---|---|
| `Assets/Scripts/Gameplay/GameController.cs` | 1 个公开方法 + 2 个私有方法（约 100 行） |
| `Assets/-------/_Script/GameInnerUI.cs` | 换掉 1 个 `else` 分支 |

**零 prefab / 零场景 / 零美术 / 零 ScriptableObject / 零 Inspector 字段。**
