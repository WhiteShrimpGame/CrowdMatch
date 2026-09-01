# CrowdMatch「挤地铁」缓冲区效果 — 功能设计文档

> 状态：**已实现（3D 物理版）**。本文描述"像素在网格内寻路提取离开，穿过漏斗缓冲区（入口→缺口）与游戏区封闭区间、
> 从缺口依次通过、再匀速抵达集结位置"的完整玩法效果，以及它与现有 `GameController` / `ContainerGroup` 的集成方式。
> 早期实现（软力、顺序投影硬约束）已归档到 `CrowdBufferImplementations.md`，本文以最新的 3D 物理实现为准。

---

## 1. 概述

当前 Demo 里，玩家点击匹配后，被匹配的 `PixelItem` 直接飞到聚集点（`gatherPoint`）并**随机散布**，
随后 `ContainerGroup` 从 `gatheredItems` 里按颜色吸收。

本次效果把"飞到聚集点"这一步，替换成一段更有节奏的**过闸体验**（挤地铁），并把缓冲区与游戏区合并为一个**完全封闭的区间**：

1. 像素离开 `PixelGroup` 后，先按**前到后顺序在网格内寻路**（BFS 只走已腾出的格子，绕开未匹配球与尚未离开的同批球），逐格移向入口边（不穿球、不瞬移）。
2. 到达入口边后**附加刚体**（球碰撞体 + 刚体），每个物理帧把速度直接设定为朝出口方向。
3. 封闭区间内由**物理引擎**处理：像素间靠球碰撞体互相挤开（直径即期望间距），漏斗斜边 + 游戏区侧边 + 后墙 + 缺口封口墙把像素框在其中、挤向收窄的缺口，球不会漏出。
4. 像素抵达缺口附近后**移除刚体与碰撞体**，先**匀速**移动到出口位置，再**匀速**移动到集结位置。
5. **依次**通过缺口：两个先后释放的像素之间有**最小时间间隔**，形成"一个一个过闸"的效果。

最终像素抵达集结位置后，沿用现有契约——置 `arrivedAtGatherPoint = true` 并加入 `gatheredItems`，
`ContainerGroup` 照常消费，无需改动。

---

## 2. 目标与非目标

### 目标
- 替换现有"直接散布到聚集点"的移动，改为"匀速进入 → 物理过闸 → 先到出口位置 → 再到集结点"四段式。
- 封闭区间内实现：真实碰撞挤开（像素间间距）、墙约束（漏斗斜边 + 游戏区侧边 + 后墙 + 缺口封口墙，把像素框住并挤向缺口）、朝缺口的前进倾向（每帧设定朝缺口速度）。
- 缺口处实现：单文件通过 + 最小时间间隔的释放调度。
- 与现有 `gatheredItems` / `arrivedAtGatherPoint` 契约无缝衔接。

### 非目标（本期不做）
- 不做性能优化（物理引擎自带 broad-phase，Demo 规模无压力）。
- 不做像素在缓冲区内"变道/插队"等复杂行为（物理自然形成排队）。
- 不改变 `ContainerGroup` 的消费逻辑。
- 不迁移到 XY 平面 / 2D 物理（维持现有 XZ 布局与斜俯视摄像机）。

---

## 3. 概念与术语

| 术语 | 含义 |
|---|---|
| **封闭缓冲区（Buffer Zone）** | 游戏区 + 漏斗合并的封闭区间：窄缺口为唯一出口（已封口）、宽端由后墙封口，漏斗斜边保持不变、入口两端向 -Z 延伸侧边框住游戏区，像素在其中被物理约束与推挤 |
| **缺口 / 出口（Gap / Exit）** | 缓冲区窄端的封口墙（唯一出口，已封口），像素单文件排队，是"移除碰撞体"的触发点与释放后第一段移动终点 |
| **后墙（Back Wall）** | 游戏区宽端（-Z 端）的封口墙（上底封口），把游戏区框在其中 |
| **入口边（Entrance）** | 漏斗宽边（宽 `entranceWidth`），也是游戏区封闭区间与漏斗的共享边；像素匀速到达后切入物理阶段 |
| **碰撞球半径（Radius）** | 像素球碰撞体半径：像素间中心距被物理维持 ≥ `2·radius`（直径即期望间距） |
| **解除约束（Release）** | 像素抵达缺口附近、被移除刚体与碰撞体的那一刻 |
| **集结位置（Collect Point）** | 释放后第二段匀速移动的终点，即现有 `gatherPoint` |
| **最小时间间隔（Min Interval）** | 两个先后释放的像素之间的最小时间差，实现"依次通过" |

---

## 4. 几何定义（2D XZ 平面）

游戏单位在 XZ 平面（y=0）。缓冲区 + 游戏区定义为一个**完全封闭的区间**，由「漏斗梯形 + 游戏区梯形」共享入口边拼成：

```
        后墙（宽 backWidth，上底封口）  ← 把游戏区（像素群）框在其中
        ═══════════════════════════════
         ╲                          ╱
          ╲        游戏区侧边       ╱    ← 两条新碰撞体（从入口两端向 -Z 延伸）
           ╲                      ╱
            ╲                    ╱
       入口（宽 entranceWidth）═══════════   ← 共享边（漏斗宽边 = 游戏区前边）
             ╲                   ╱
              ╲     漏斗斜边     ╱        ← 保持不变（入口 → 缺口）
               ╲               ╱
                ━━━━━━━━━━━━━━━━
            缺口（窄 gapWidth，唯一出口，已封口）  ← 面向集结位置 / gatherPoint
```

### 参数与推导

| 输入 | 说明 |
|---|---|
| `transform.position`（组件自身） | **入口中心**（漏斗宽边中心，也是游戏区封闭区间与漏斗的共享边） |
| `entranceWidth`（float） | 入口边宽度（漏斗宽边 = 共享边） |
| `gapPoint`（Transform 引用） | **缺口中心 / 出口**（世界坐标） |
| `gapWidth`（float） | 缺口宽度（收窄后的排队口，唯一出口，已封口） |
| `backWidth`（float） | 后墙宽度（上底封口，应能把游戏区框在其中，通常 ≥ entranceWidth） |
| `backDepth`（float） | 后墙到入口中心的距离（游戏区封闭区间沿 -Z 的深度） |

推导：
- **轴向** `axis = normalize(gapPoint.position - transform.position)`（朝缺口，+Z）。
- **入口边**：过 `transform.position`、垂直于 `axis`、长 `entranceWidth`。
- **缺口边**：过 `gapPoint.position`、垂直于 `axis`、长 `gapWidth`。
- **后墙中心** `backCenter = transform.position - axis * backDepth`；**后墙边**：过 `backCenter`、垂直于 `axis`、长 `backWidth`。

### 边界语义（重点）

- **漏斗斜边（两条）**：运行时创建的 static `BoxCollider` 墙，连接入口边两端与缺口边两端，**保持不变**（收窄的漏斗）。
- **游戏区侧边（两条，新增）**：从入口边两端（即斜边的两个端点）向 **-Z** 延伸的新 `BoxCollider` 墙，连接到后墙两端，把游戏区框在其中。
- **后墙（上底封口）**：运行时创建的 static `BoxCollider` 墙，封住宽端（-Z 端），确保球不会从后方漏出。
- **缺口边（缺口封口墙）**：**封口**（static `BoxCollider` 墙，是唯一出口）。像素距缺口中心 ≤ `releaseRadius` 即"可释放"，移除碰撞体后先匀速穿过封口墙到出口位置、再匀速到集结位置。
- **入口边**：**开放（共享边，无墙）**，像素从游戏区匀速穿过它进入漏斗。

> 放置建议：缓冲区组件摆在 `PixelGroup` 与 `gatherPoint` 之间，缺口朝 `gatherPoint`；
> `entranceWidth` / `backWidth` / `backDepth` 需足够大，使整个像素网格落在封闭区间内。

---

## 5. 像素生命周期状态机

```
InGrid ──(点击/FloodFill 匹配)──▶ Matched
                                   │ 从 grid 移除；关闭球碰撞体（点击检测，进入物理阶段时复用）
                                   ▼
                                Extracting（网格寻路提取，并行 sweep）
                                   │ 每个 tick 并行算出所有可移动像素的下一格；可移入"本 tick 即将腾出"的格，整列一波一波连续推进
                                   │ 仅当多球争用同一单格（窄缝）时按"等待次数多者优先"依次通过
                                   ▼
                                Physical（附加刚体：球碰撞体 + 刚体）
                                   │ 每帧速度朝缺口；物理引擎处理碰撞挤开与漏斗斜边 + 游戏区侧边 + 后墙
                                   ▼
                            ReadyAtGap（距缺口 ≤ releaseRadius）
                                   │ 等待释放调度（最小时间间隔）
                                   ▼
                               Released（移除刚体与碰撞体）
                                   │ 先匀速到出口位置，再匀速到集结位置
                                   ▼
                               Arrived
                                   │ arrivedAtGatherPoint = true
                                   │ 加入 gatheredItems
                                   ▼
                          ContainerGroup 消费（现有逻辑，不改）
```

> `ReadyAtGap` 不是独立列表，而是 `Physical` 像素的布尔条件（`distToGap ≤ releaseRadius`）。
> 释放调度器（第 7 节）决定哪个 `ReadyAtGap` 像素此刻真正 `Released`。

---

## 6. 缓冲区内运动模型（真实物理模拟）

不做手写约束求解，交由 PhysX 物理引擎处理碰撞。像素进入物理阶段（`EnterPhysical`）后：

1. **球碰撞体**：`SphereCollider.radius = radius`，直径 `2·radius` 即期望间距。
2. **刚体配置**：`useGravity = false`、`mass = 1`、`drag = 0`、冻结 `PositionY` 与全部旋转、
   `Interpolate` + `ContinuousDynamic`（防穿透）。
3. **速度驱动**：每个物理帧（`FixedUpdate`）直接把速度设定为 `rb.velocity = normalize(gap - pos) * crowdSpeed`（朝出口），不做力/阻尼累计。

### 6.1 碰撞挤开（间距）

像素之间靠 `SphereCollider` 真实碰撞，物理引擎把中心距维持在 ≥ `2·radius`（期望间距）。
碰撞是各向同性的、稳定的，不存在手写软力的"受力平衡低于间距"或顺序投影的"单轮残留"问题。

### 6.2 墙（边界约束）

封闭区间由六条 static `BoxCollider` 墙构成（`wallThickness` 厚、`wallHeight` 高、长度沿墙方向）：
两条漏斗斜边（入口 → 缺口）保持不变；两条游戏区侧边（新增）从入口两端向 -Z 延伸到后墙，框住游戏区；
后墙（上底封口）封住宽端；缺口封口墙封住窄端，确保球不漏出。墙由 `Awake` 的 `BuildWalls` 运行时创建。

### 6.3 直接设定速度

每个物理帧直接重写 `rb.velocity` 为朝缺口方向的 `crowdSpeed`，无恒定力/阻尼累计，像素始终以 `crowdSpeed` 大小朝出口推进；
碰撞挤开与侧边墙带来的分离速度仍由物理引擎叠加，下一物理帧再被重写为朝缺口方向，形成稳定的"向前挤"。

> y 恒被刚体约束冻结在 0，模拟只在 XZ 平面进行。

---

## 7. 缺口释放调度（Gate Scheduler）

目的：让像素"一个一个"过缺口，先后之间满足**最小时间间隔**。

维护一个字段 `_lastReleaseTime`。每帧（`Update`）：

```
if 物理列表为空: return

front = 距缺口最近的 ReadyAtGap 像素（distToGap ≤ releaseRadius 且 distToGap 最小）

if front != null 且 Time.time - _lastReleaseTime ≥ minReleaseInterval:
    Release(front)                          // 移除刚体与碰撞体 → 进入 Released 状态
    _lastReleaseTime = Time.time
```

### 语义要点

- **每次最多释放一个**：即使多个像素同时在 `releaseRadius` 内，也只释放最靠前（距缺口最近）的那个。
- **最小间隔是下限而非强制节拍**：若人群太慢、间隔已过但还没有像素就位，就等下一个像素就位再释放；
  若像素很快，则被间隔限制，形成稳定的"一个接一个"节奏。
- **等待期的像素仍受物理约束**：`ReadyAtGap` 但未获释放的像素仍在物理阶段（球碰撞体让后续像素在缺口后排队），不会"插队"穿透。

> `releaseRadius` 必须 ≥ `radius + wallThickness/2`，否则像素被缺口封口墙挡在释放范围外，永远不触发释放（卡死）。

---

## 8. 释放后匀速移动（两段）

`Released` 像素：移除刚体与碰撞体后，先以恒定速度 `releaseSpeed` 直线移动到**出口位置**（`gapPoint`），
再以同一速度直线移动到**集结位置**（`collectPoint`）。

```
// 第一段：出口位置
dir = normalize(gapPoint - p); 每帧 p += dir * releaseSpeed * dt；到点: dist(p, gapPoint) ≤ arriveEpsilon
// 第二段：集结位置
dir = normalize(collectPoint - p); 每帧 p += dir * releaseSpeed * dt；到点: dist(p, collectPoint) ≤ arriveEpsilon
到达后:
    item.transform.SetParent(collectPoint, true)   // 与现有 GatherItem 一致
    item.arrivedAtGatherPoint = true               // 契约：供 ContainerGroup 消费
    GameController.gatheredItems.Add(item)
```

> - 释放时 `rb.isKinematic = true` + `sphere.enabled = false` 先停用物理影响，再 `Destroy` 组件（延迟到帧末）。
> - 像素先经过出口位置再拐向集结位置，确保正好穿过缺口（当 `gapPoint` 与 `collectPoint` 不共线时表现为"过闸后转向"）。
> - 到达集结位置后即满足现有消费契约，`ContainerGroup.ProcessConsumption` 无需任何改动。
> - 由于缺口已用最小间隔把像素错开，集结位置的散布（`gatherScatterRadius`）不再必需。

---

## 9. 组件与 API 设计

### 9.1 组件：`CrowdBufferZone : MonoBehaviour`

职责：拥有缓冲区几何与物理参数，管理"在途像素"（提取寻路阶段 + 物理阶段），步进网格寻路提取、每帧设定朝缺口速度、调度释放。

| 分类 | 字段 | 类型 | 默认 | 说明 |
|---|---|---|---|---|
| 几何 | `entranceWidth` | float | 8 | 入口边宽度（漏斗宽边 = 共享边） |
| 几何 | `gapPoint` | Transform | — | 缺口中心 / 出口引用 |
| 几何 | `gapWidth` | float | 0.4 | 缺口宽（收窄后的排队口，唯一出口） |
| 几何 | `backWidth` | float | 9 | 后墙宽度（上底封口，应能把游戏区框在其中，通常 ≥ entranceWidth） |
| 几何 | `backDepth` | float | 9 | 后墙到入口中心的距离（游戏区封闭区间沿 -Z 的深度） |
| 像素物理 | `radius` | float | 0.25 | 碰撞球世界半径（球视觉直径 = 像素直径 0.5，0.25 即刚好接触；调小可穿插表现拥挤） |
| 像素物理 | `crowdSpeed` | float | 5 | 进入阶段（匀速）与物理阶段的驱动速度（每帧朝缺口方向设定速度） |
| 提取 | `extractSpeed` | float | 5 | 网格寻路提取阶段的移动速度（世界单位/秒） |
| 墙 | `wallThickness` | float | 0.1 | 墙厚度 |
| 墙 | `wallHeight` | float | 2 | 墙高度（应 ≥ 像素直径） |
| 释放 | `releaseRadius` | float | 0.6 | 距缺口多近触发释放（缺口已封口，需 ≥ radius + wallThickness/2） |
| 释放 | `minReleaseInterval` | float | 0.15 | 最小释放间隔（秒） |
| 释放 | `releaseSpeed` | float | 8 | 释放后匀速速度 |
| 引用 | `collectPoint` | Transform | — | 集结位置（= `gatherPoint`） |

> 默认值为**初值建议**，需按场景缩放/像素尺寸实际调参。

| 方法 | 说明 |
|---|---|
| `EnterBatch(List<PixelItem> matched, PixelGroup group)` | 一批匹配像素离开网格时调用：关闭点击碰撞体，建立网格占用表，加入提取阶段（前到后寻路离开） |
| `EnterPhysical(PixelItem item)` | 内部：附加球碰撞体 + 刚体，进入物理阶段 |
| `Release(PixelItem item)` | 内部：移除刚体与碰撞体，先匀速到出口位置，再匀速到 `collectPoint` |
| `OnArrived(PixelItem item)` | 内部：置 `arrivedAtGatherPoint`、加入 `gatheredItems`、parent 到 `collectPoint` |
| `FixedUpdate()` | 每物理帧：把物理阶段像素的速度直接设定为朝出口方向 |
| `Update()` | 每帧：网格寻路提取步进（进入物理）+ 释放调度 |
| `BuildWalls()` | 内部：运行时创建六条墙（漏斗斜边 + 游戏区侧边 + 后墙 + 缺口封口墙，static `BoxCollider`，封闭区间） |

### 9.2 进入缓冲区（EnterBatch）

`EnterBatch` 只做：关闭球碰撞体（像素已离开网格，停止点击检测，进入阶段不参与物理；进入物理阶段时复用同一碰撞体）+ 建立网格占用表 `_matchedOccupied` + 加入提取阶段，并按 `CellSize / extractSpeed` 设定 sweep 时间片。像素**保持当前网格位置出发**，
由 `Update` 的 `StepExtracting` 驱动：每到一个 tick，`SweepOnce` **并行**算出所有可移动像素的下一格——BFS 可穿过"本 tick 即将腾出的格"（`vacated`），绕过未匹配球与不会本 tick 离开的同批球，从而整列/整批一波一波同时推进，而非逐个串行；
只有当多个像素争用同一个单格（窄缝）时才依次通过，且按 `waitCount`（等待次数）降序给更久等待者优先权。格子间移动用平滑动画（时长 = tick 间隔），抵达入口边后由 `EnterPhysical` 附加刚体。整批提取完成后触发 `OnBatchExtracted` 回调（`GameController` 借此推迟补位）。

> 视觉：像素在生成时即为 Sphere（`PixelGroupEditor`「生成网格」用 `PrimitiveType.Sphere`，球 primitive 直径 = 1 与 Cube 边长一致，
> scale 直接用 `unitSize`），运行时无需任何网格替换；自带 `SphereCollider` 同时充当点击碰撞体与物理碰撞体。

> 落位点横向 `clamp` 到 `±(entranceWidth/2 - radius)`，保证球碰撞体落在入口边的封闭区间内、不穿墙。

### 9.3 集成改动（现有代码）

| 位置 | 现状 | 改动 |
|---|---|---|
| `GameController` | 新增字段 `crowdBuffer`（`CrowdBufferZone`，可选） | 引用缓冲区组件 |
| `GameController.ResolveMatch` | 对每个匹配像素调 `GatherItem(item)` | 改为：`crowdBuffer != null ? crowdBuffer.EnterBatch(matched, pixelGroup) : GatherItem(...)`（保留旧散布作 fallback）；补位推迟到提取完成回调 |
| `GameController.GatherItem` | 关碰撞体 + parent + 加入列表 + 散布协程 | 保留（作为 fallback） |
| `ContainerGroup` | 消费 `gatheredItems` | **不改**（契约不变） |
| `PixelItem.Awake` | 初始化 renderer + 材质 | **不改**（像素生成时已是 Sphere，无需运行时替换网格） |
| `PixelItem.arrivedAtGatherPoint` | 到聚集点置 true | **语义不变**（改为"过闸抵达集结位置后置 true"） |

> 关键不变量：**只有完整走过"网格寻路提取 → 物理过闸 → 集结位置"的像素，才会 `arrivedAtGatherPoint = true`**，
> 因此 `ContainerGroup` 只会消费真正到位的像素，过闸节奏天然限流了消费速率。

---

## 10. 数据流

```
GameController.ResolveMatch
    │  crowdBuffer.EnterBatch(matched)   ← 替换原 GatherItem（补位推迟到提取完成）
    ▼
CrowdBufferZone._extracting（网格寻路提取，并行 sweep）
    │  Update：SweepOnce 并行推进（BFS 可穿过"即将腾出"的格）→ 抵达入口边 → EnterPhysical
    ▼
CrowdBufferZone._physical（球碰撞体 + 刚体）
    │  FixedUpdate：朝缺口设定速度；物理引擎处理碰撞挤开 + 漏斗斜边 + 游戏区侧边 + 后墙
    │  Update 调度：front 距缺口 ≤ releaseRadius 且间隔已过
    ▼
Release → 移除刚体/碰撞体 → 先匀速到出口位置 → 再匀速到集结位置 → OnArrived
    │  arrivedAtGatherPoint = true
    ▼
GameController.gatheredItems
    │  （与现状一致）
    ▼
ContainerGroup.ProcessConsumption → Consume → 容器消失/补位
```

> 提取完成（`_extracting` 清空）后触发 `OnBatchExtracted`，`GameController` 才执行 `CollapseColumns` 补位，避免补位与提取并发冲突。

---

## 11. 边界情况

| 场景 | 处理 |
|---|---|
| 缓冲区空 | `Update` / `FixedUpdate` 为空操作，无开销 |
| 多个像素同时在 `releaseRadius` 内 | 只释放距缺口最近的一个，其余等待 |
| 人群过慢，间隔已过但无人就位 | 不强制释放，等就位再放（间隔为下限） |
| 像素被墙框住并挤向缺口 | 物理碰撞自然处理，无需手写投影 |
| 缺口宽小于像素直径 | 缺口已封口，像素被挡在封口墙前排队，靠 `releaseRadius` 触发释放 |
| `releaseRadius` 过小（< radius + wallThickness/2） | 像素永远到不了释放范围，卡死——调参时需保证该不等式成立 |
| `entranceWidth` / `backWidth` / `backDepth` 过小 | 像素网格落在封闭区间外，球可能漏出——调参时需保证盖住整个游戏区 |
| `crowdBuffer` 未赋值 | `GameController` 回退到现有散布聚集（向后兼容） |
| 缓冲区与 PixelGroup 距离较远 | 像素在网格内寻路到入口边（BFS 逐格），再进入物理（正常表现） |
| 提取中前路被同批球挡住 | 若前排球本 tick 会腾出（`vacated`），后排同一 tick 跟进，整列连续推进；若前排球也被挡（窄缝争用），后排按 `waitCount` 排队依次通过 |
| 提取中某列前方被未匹配球挡住 | BFS 绕行到相邻已腾出（或即将腾出）的格子（横向邻接），不会穿过 |
| 提取进行中 | `GameController` 屏蔽点击（`crowdBuffer.IsExtracting`），补位推迟，保证网格状态一致 |

---

## 12. 调参指南（初值 + 方向）

| 参数 | 影响 | 调大 | 调小 |
|---|---|---|---|
| `radius` | 碰撞球半径 / 拥挤程度 | 更松散、更整齐 | 更挤、更"挤地铁" |
| `crowdSpeed` | 进入与物理阶段驱动速度 | 更快推进、更"冲" | 更慢、更"拥挤排队"感 |
| `extractSpeed` | 网格寻路提取速度 | 更快出网格 | 更慢、更有序 |
| `releaseRadius` | 触发释放的松紧 | 更早释放 | 更贴缺口才释放（不可 < radius + wallThickness/2） |
| `minReleaseInterval` | 过闸节奏 | 更慢、间隔大 | 更快、几乎连过 |
| `gapWidth` | 缺口收窄程度 | 缺口更宽、更易通过 | 更窄、更"单文件"排队 |
| `entranceWidth` | 入口边宽度 / 漏斗宽边 | 入口更宽、更易进入 | 入口更窄、更收敛 |
| `backWidth` | 后墙宽度 / 游戏区宽端 | 框住更宽的游戏区、更分散 | 更窄、更收敛 |
| `backDepth` | 游戏区封闭区间深度（入口 → 后墙） | 框住更深的游戏区 | 更浅、更紧凑 |

---

## 13. 待确认决策

> 落地情况：1-B（先在网格内寻路提取到入口边，再附加刚体）、2-A（封闭区间）、3-A（单点集结）、4-保留 fallback、
> 5-`FixedUpdate`（物理）+ `Update`（提取/释放）、6-单组件 `CrowdBufferZone`（墙由组件运行时创建）、
> 7-提取阶段（网格寻路）。另有：物理引擎选 **3D 物理（SphereCollider + Rigidbody + BoxCollider 墙，XZ 平面）**，而非迁移到 XY 平面的 2D 物理。
>
> 最新落地：**缓冲区 + 游戏区合并为完全封闭区间**——窄缺口为唯一出口（已封口），漏斗两条斜边（入口 → 缺口）保持不变，
> 从斜边两个端点（入口两端）向 -Z 延伸两条新墙至后墙（上底封口，宽 `backWidth`、深 `backDepth`），
> 把游戏区框在其中、球不漏出；释放改为**两段匀速**（先到出口位置，再到集结位置）。

1. **入口过渡**：
   - A：入口边紧贴像素群，直接按列映射落位；
   - B（已选）：先从网格位置匀速移动到入口边落位点，再附加刚体进入物理。
2. **几何形状**：
   - A（已选）：封闭区间（漏斗斜边保持不变 + 入口两端向 -Z 延伸两条侧边 + 后墙 `backWidth` 封口 + 缺口 `gapWidth` 也封口）；
   - B：严格扇形/三角形（缺口为单点）。
3. **集结位置散布**：
   - A（已选）：单点集结（缺口已错开节奏，无需散布）；
   - B：保留现有 `gatherScatterRadius` 小散布。
4. **是否保留旧散布聚集作为 fallback**：保留（`crowdBuffer` 未赋值时回退）。
5. **物理引擎选择**：
   - A（已选）：3D 物理（XZ 平面，冻结 Y）——改动最小，现有点击射线/生成器/坐标/摄像机全保留；
   - B：2D 物理（CircleCollider2D / Rigidbody2D / EdgeCollider2D）——需迁移到 XY 平面，改动大。
6. **代码组织**：单组件 `CrowdBufferZone`（六条墙运行时创建），保留"提取阶段 + 物理阶段"两列表管理。
7. **提取寻路**（本次新增）：
   - 把"匀速进入"替换为**网格寻路提取**：匹配批次按前到后顺序，每球用 BFS 只走"已腾出或本 tick 即将腾出的格子"（这一批匹配像素腾出的空间），绕过未匹配球与不会本 tick 离开的同批球，逐格移向入口边；
   - **并行 sweep**：每个 tick 一次性算出所有可移动像素的下一格，整列/整批同时推进（非逐个串行）；仅当多球争用同一单格时依次通过，按 `waitCount`（等待次数）降序给更久等待者优先；
   - 提取完成触发 `OnBatchExtracted`，`GameController` 推迟 `CollapseColumns` 补位，提取期间屏蔽点击。

---

## 14. 实现步骤（已完成）

1. 重写 `CrowdBufferZone.cs` 为 3D 物理版（封闭几何、物理附加、每帧设定朝缺口速度、释放调度、墙）。
2. `GameController` 增加 `crowdBuffer` 字段，`ResolveMatch` 分流到 `EnterBatch`/`GatherItem`（已改，保留）。
3. 像素改为生成时即 Sphere：`PixelGroupEditor.GenerateGrid` 改用 `PrimitiveType.Sphere`（球 primitive 直径 = 1，scale 仍为 `unitSize`），移除 `PixelItem.Awake` 的运行时换网格逻辑；**在场景里选中 `PixelGroup` 点「生成网格」重新生成一次**。
4. 把缓冲区 + 游戏区改成封闭区间：`BuildWalls` 创建六条墙——漏斗斜边（入口 → 缺口，保持不变）+ 游戏区侧边（入口两端 → 后墙，向 -Z 延伸）+ 后墙（上底封口）+ 缺口封口墙，恢复 `entranceWidth` 字段、新增 `backWidth` / `backDepth`。
5. 释放改为两段匀速：`MoveToCollect` 先 `MoveUniform` 到 `gapPoint`（出口位置），再 `MoveUniform` 到 `collectPoint`。
6. **提取阶段改网格寻路（并行 sweep）**：`CrowdBufferZone.Enter` → `EnterBatch(matched, group)`，新增 `StepExtracting` + `SweepOnce`（并行推进 + "即将腾出" + `waitCount` 公平性）、`extractSpeed` 字段、`IsExtracting` / `OnBatchExtracted`；`GameController.ResolveMatch` 调用 `EnterBatch`，补位推迟到 `OnBatchExtracted` 回调，提取期间屏蔽点击。
7. 场景里配置 `CrowdBufferZone` 组件：`gapPoint`（可用 `gatherPoint` 同物体或独立点）、`collectPoint`（= `gatherPoint`）、`entranceWidth` / `backWidth` / `backDepth` 及其它几何/物理/释放/提取参数（确保后墙盖住整个 PixelGroup）。
8. 自测：点击匹配 → 观察像素前到后寻路出网格（不穿球、绕开未匹配球）、物理挤向缺口、依次过闸、先到出口位置再抵达集结位置、被容器消费、随后补位；确认球不漏出封闭区间。
9. 按第 12 节调参，达到"挤地铁"的拥挤与节奏感。
