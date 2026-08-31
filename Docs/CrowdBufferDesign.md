# CrowdMatch「挤地铁」缓冲区效果 — 功能设计文档

> 状态：**已实现**。本文描述"像素离开网格后，穿过一段扇形缓冲区、从缺口依次通过、
> 再匀速抵达集结位置"的完整玩法效果，以及它与现有 `GameController` / `ContainerGroup` 的集成方式。
> 第 13 节的所有决策已按推荐项（A）确认并落地。

---

## 1. 概述

当前 Demo 里，玩家点击匹配后，被匹配的 `PixelItem` 直接飞到聚集点（`gatherPoint`）并**随机散布**，
随后 `ContainerGroup` 从 `gatheredItems` 里按颜色吸收。

本次效果把"飞到聚集点"这一步，替换成一段更有节奏的**过闸体验**（挤地铁）：

1. 像素离开 `PixelGroup` 后，进入一段**扇形缓冲区**（宽口朝像素群，窄口朝集结位置）。
2. 在缓冲区内，像素被**约束在扇形范围内**（不越界），并朝缺口方向前进。
3. 像素之间保持**期望中心间距 d**：距离 > d 互不影响，距离 < d 互斥挤开。
4. 像素抵达缺口附近后**解除约束**，从当前位置**匀速**移动到集结位置。
5. **依次**通过缺口：两个先后解除约束的像素之间有**最小时间间隔**，形成"一个一个过闸"的效果。

最终像素抵达集结位置后，沿用现有契约——置 `arrivedAtGatherPoint = true` 并加入 `gatheredItems`，
`ContainerGroup` 照常消费，无需改动。

---

## 2. 目标与非目标

### 目标
- 替换现有"直接散布到聚集点"的移动，改为"缓冲区 → 缺口 → 匀速到集结点"三段式过闸。
- 缓冲区内实现：边界约束、间距 d 的互斥分离、朝缺口的前进倾向。
- 缺口处实现：单文件通过 + 最小时间间隔的释放调度。
- 与现有 `gatheredItems` / `arrivedAtGatherPoint` 契约无缝衔接。

### 非目标（本期不做）
- 不做像素之间真实的刚体碰撞（用简化分离模型代替）。
- 不做性能优化（空间哈希等）——Demo 规模下 O(n²) 分离足够。
- 不做像素在缓冲区内"变道/插队"等复杂行为。
- 不改变 `ContainerGroup` 的消费逻辑。

---

## 3. 概念与术语

| 术语 | 含义 |
|---|---|
| **扇形缓冲区（Buffer Zone）** | 宽端（入口）朝像素群、窄端（缺口）朝集结位置的梯形/扇形区域，像素在其中被约束与推挤 |
| **入口（Entrance）** | 缓冲区的宽边，像素进入缓冲区的位置 |
| **缺口（Gap）** | 缓冲区窄端的小开口，像素单文件通过，是"解除约束"的触发点 |
| **间距 d（Spacing）** | 期望的中心点间距；< d 时互斥，> d 时互不影响 |
| **解除约束（Release）** | 像素抵达缺口附近、被释放出缓冲区约束的那一刻 |
| **集结位置（Collect Point）** | 释放后匀速移动的终点，即现有 `gatherPoint` |
| **最小时间间隔（Min Interval）** | 两个先后解除约束的像素之间的最小时间差，实现"依次通过" |

---

## 4. 几何定义（2D XZ 平面）

游戏单位在 XZ 平面（y=0）。缓冲区定义为 XZ 平面上的一个**梯形**：

```
        入口边（宽 entranceWidth）  ← 面向 PixelGroup（像素群）
        ═══════════════════════════
          ╲                       ╱
           ╲      前进方向 →      ╱    ← 两条侧边（软约束：越界时给向后推力）
            ╲                   ╱
             ╲                 ╱
              ━━━━━━━━━━━━━━━━
        缺口（窄 gapWidth）  ← 面向集结位置 / gatherPoint
```

### 参数与推导

| 输入 | 说明 |
|---|---|
| `transform.position`（组件自身） | **入口中心** |
| `gapPoint`（Transform 引用） | **缺口中心**（世界坐标） |
| `entranceWidth`（float） | 入口边宽度 |
| `gapWidth`（float） | 缺口宽度（0 = 单点；≈ 像素直径 = 单文件） |

推导：
- **轴向** `axis = normalize(gapPoint.position - transform.position)`。
- **入口边**：过 `transform.position`、垂直于 `axis`、长 `entranceWidth`。
- **缺口边**：过 `gapPoint.position`、垂直于 `axis`、长 `gapWidth`。
- **梯形** = 入口边两端点 + 缺口边两端点围成的凸四边形。

### 边界语义（重点）

- **两条侧边**：**软约束**。像素越界时沿轴向后（`-axis`）给一个与越界深度成正比的推力，把像素推回更宽的区域，而非横向投影；横向间距交给互斥维持。
- **入口边**：像素的**起始位置**。像素从入口边进入，之后朝缺口前进，通常不会回退；如需防回退可对入口边做单侧钳制。
- **缺口边**：**不做墙**。像素接近缺口（进入 `releaseRadius`）即"可释放"，穿过缺口即离开缓冲区。缺口是"出口"而非"障碍"。

> 放置建议：缓冲区组件摆在 `PixelGroup` 与 `gatherPoint` 之间，入口边贴近像素群前排，缺口朝 `gatherPoint`。

---

## 5. 像素生命周期状态机

```
InGrid ──(点击/FloodFill 匹配)──▶ Matched
                                   │ 从 grid 移除；关闭碰撞体
                                   ▼
                                InBuffer
                                   │ 约束：朝缺口前进 + 间距 d 分离 + 边界钳制
                                   ▼
                            ReadyAtGap（距缺口 ≤ releaseRadius）
                                   │ 等待释放调度（最小时间间隔）
                                   ▼
                               Released（解除约束）
                                   │ 匀速移动到集结位置
                                   ▼
                               Arrived
                                   │ arrivedAtGatherPoint = true
                                   │ 加入 gatheredItems
                                   ▼
                          ContainerGroup 消费（现有逻辑，不改）
```

> `ReadyAtGap` 不是独立列表，而是 `InBuffer` 像素的布尔条件（`distToGap ≤ releaseRadius`）。
> 释放调度器（第 7 节）决定哪个 `ReadyAtGap` 像素此刻真正 `Released`。

---

## 6. 缓冲区内运动模型（每帧）

对每个 `InBuffer` 像素，每帧计算合成速度并积分位置。三部分叠加：

### 6.1 朝缺口前进（Steer）

```
dirToGap = normalize(gapCenter - p)         // 朝向缺口中心（或缺口边最近点）
v_steer  = dirToGap * crowdSpeed
```

### 6.2 间距 d 互斥分离（Separation）

对缓冲区内其它像素 `j`：

```
delta = p_i - p_j
dist  = |delta|
if 0 < dist < d:                            // 只有小于 d 才互斥（大于 d 互不影响）
    dir  = delta / dist                     // 指向远离 j 的方向
    mag  = (d - dist) / d * separationStrength   // 线性衰减：dist=d 时 0，dist→0 时最大
    v_sep += dir * mag
```

> 对称：`j` 受到等大反向的推力，实现"互斥挤开"。双方都小于 d 时才互相推开。

### 6.3 侧边软向后推力（Boundary Push）

```
over = lateralDist - halfWidthAt(t)          // 越界深度，> 0 表示越出侧边
if over > 0:
    v_push = -axis * (over * boundaryPushStrength)   // 沿轴向向后推，与越界深度成正比
p' = p + (v_steer + v_sep + v_push) * dt
```

> 侧边**不做横向投影**，只给向后推力，把越界像素推回更宽的区域；横向自由度交给间距互斥，
> 因此在缺口附近像素能横向保持 `spacing`，而不是被压进窄缝。
> `boundaryPushStrength` 越大越难凸出侧边（偏硬），越小越容易凸出（偏软）。
> y 恒保持 0（或像素原 y），本模拟只在 XZ 平面进行。

### 6.4 说明

- 这是**简化速度模型**（把互斥视为对速度的修正），不是严格加速度/力模型；对 Demo 的观感足够。
- 若需要更稳定的表现，可把步进放到 `FixedUpdate`；本期默认 `Update`（见第 13 节决策 #5）。

---

## 7. 缺口释放调度（Gate Scheduler）

目的：让像素"一个一个"过缺口，先后之间满足**最小时间间隔**。

维护一个字段 `_lastReleaseTime`。每帧：

```
if 缓冲区为空: return

front = 距缺口最近的 ReadyAtGap 像素（distToGap ≤ releaseRadius 且 distToGap 最小）

if front != null 且 Time.time - _lastReleaseTime ≥ minReleaseInterval:
    Release(front)                          // 解除约束 → 进入 Released 状态
    _lastReleaseTime = Time.time
```

### 语义要点

- **每次最多释放一个**：即使多个像素同时在 `releaseRadius` 内，也只释放最靠前（距缺口最近）的那个。
- **最小间隔是下限而非强制节拍**：若人群太慢、间隔已过但还没有像素就位，就等下一个像素就位再释放；
  若像素很快，则被间隔限制，形成稳定的"一个接一个"节奏。
- **等待期的像素仍受约束**：`ReadyAtGap` 但未获释放的像素仍在缓冲区约束内（间距 d 会让后续像素在缺口后排队），不会"插队"穿透。

---

## 8. 释放后匀速移动

`Released` 像素：解除约束，从**当前位置**以恒定速度 `releaseSpeed` 直线移动到集结位置（`collectPoint`）。

```
dir = normalize(collectPoint - p)
每帧: p += dir * releaseSpeed * dt
到点条件: dist(p, collectPoint) ≤ arriveEpsilon
到达后:
    item.transform.SetParent(collectPoint, true)   // 与现有 GatherItem 一致
    item.arrivedAtGatherPoint = true               // 契约：供 ContainerGroup 消费
    GameController.gatheredItems.Add(item)
```

> - 这里"匀速"与现有 `GameController.MoveToGatherPoint` 的 `duration = dist / speed` 语义一致，可复用/重构该协程。
> - 到达集结位置后即满足现有消费契约，`ContainerGroup.ProcessConsumption` 无需任何改动。
> - 由于缺口已用最小间隔把像素错开，集结位置的散布（`gatherScatterRadius`）不再必需；是否保留见决策 #3。

---

## 9. 组件与 API 设计

### 9.1 新增组件：`CrowdBufferZone : MonoBehaviour`

职责：拥有缓冲区几何与仿真参数，持有"在途像素"列表，每帧步进仿真并调度释放。

| 分类 | 字段 | 类型 | 默认 | 说明 |
|---|---|---|---|---|
| 几何 | `entranceWidth` | float | 8 | 入口宽 |
| 几何 | `gapPoint` | Transform | — | 缺口中心引用 |
| 几何 | `gapWidth` | float | 0.2 | 缺口宽（单文件） |
| 仿真 | `spacing` | float | 0.7 | 期望中心间距 d |
| 仿真 | `crowdSpeed` | float | 5 | 缓冲区内前进速度 |
| 仿真 | `separationStrength` | float | 3 | 互斥强度 |
| 仿真 | `boundaryPushStrength` | float | 15 | 侧边向后推力刚度（越界越深推力越大） |
| 释放 | `releaseRadius` | float | 0.4 | 距缺口多近可释放 |
| 释放 | `minReleaseInterval` | float | 0.15 | 最小释放间隔（秒） |
| 释放 | `releaseSpeed` | float | 8 | 释放后匀速速度 |
| 引用 | `collectPoint` | Transform | — | 集结位置（= `gatherPoint`） |

> 默认值为**初值建议**，需按场景缩放/像素尺寸实际调参。

| 方法 | 说明 |
|---|---|
| `Enter(PixelItem item)` | 像素离开网格时调用：关闭碰撞体，按列映射到入口边起点，加入 `InBuffer` |
| `Release(PixelItem item)` | 内部：解除约束，启动匀速移动到 `collectPoint` |
| `OnArrived(PixelItem item)` | 内部：置 `arrivedAtGatherPoint`、加入 `gatheredItems`、parent 到 `collectPoint` |
| `Update()` | 每帧：步进仿真（steer + separation + clamp）+ 释放调度 |

### 9.2 进入缓冲区（Enter）

`Enter` 只做两件事：关闭碰撞体 + 加入 `_buffered`。像素**保持当前网格位置出发**，
由 `Update` 的 steer 驱动朝缺口前进，全程受缓冲区约束——不做入口映射、不瞬移。

> 这样左右顺序天然保持（像素本就按网格列分布在左右），也避免了"瞬移到入口边"。
> 侧边钳制在入口上游表现为宽度 = `entranceWidth` 的平行走廊约束，进入缓冲区后逐渐收缩到缺口。

### 9.3 集成改动（现有代码）

| 位置 | 现状 | 改动 |
|---|---|---|
| `GameController` | 新增字段 `crowdBuffer`（`CrowdBufferZone`，可选） | 引用缓冲区组件 |
| `GameController.ResolveMatch` | 对每个匹配像素调 `GatherItem(item)` | 改为：`crowdBuffer != null ? crowdBuffer.Enter(item) : GatherItem(item)`（保留旧散布作 fallback） |
| `GameController.GatherItem` | 关碰撞体 + parent + 加入列表 + 散布协程 | 保留（作为 fallback 与 `Enter` 共享"关碰撞体"逻辑） |
| `GameController.MoveToGatherPoint` | 散布到 `gatherPoint` | 可复用其"匀速移动 + 到点置 flag"部分 |
| `ContainerGroup` | 消费 `gatheredItems` | **不改**（契约不变） |
| `PixelItem.arrivedAtGatherPoint` | 到聚集点置 true | **语义不变**（改为"过闸抵达集结位置后置 true"） |

> 关键不变量：**只有完整走过"缓冲区 → 缺口 → 集结位置"的像素，才会 `arrivedAtGatherPoint = true`**，
> 因此 `ContainerGroup` 只会消费真正到位的像素，过闸节奏天然限流了消费速率。

---

## 10. 数据流

```
GameController.ResolveMatch
    │  crowdBuffer.Enter(item)   ← 替换原 GatherItem
    ▼
CrowdBufferZone._buffered（List<PixelItem>）
    │  Update：steer + separation + clamp
    │  调度：front 距缺口 ≤ releaseRadius 且间隔已过
    ▼
Release → 匀速 move → OnArrived
    │  arrivedAtGatherPoint = true
    ▼
GameController.gatheredItems
    │  （与现状一致）
    ▼
ContainerGroup.ProcessConsumption → Consume → 容器消失/补位
```

---

## 11. 边界情况

| 场景 | 处理 |
|---|---|
| 缓冲区空 | `Update` 为空操作，无开销 |
| 多个像素同时在 `releaseRadius` 内 | 只释放距缺口最近的一个，其余等待 |
| 人群过慢，间隔已过但无人就位 | 不强制释放，等就位再放（间隔为下限） |
| 分离把像素推向侧边 | 侧边给向后推力把像素推回宽区（不横向投影，允许轻微凸出） |
| 缺口宽为 0（单点） | 像素朝缺口中心前进，单文件通过 |
| 像素在缺口附近抖动进出 `releaseRadius` | 可用略大的释放半径或迟滞（hysteresis）避免抖动（调参项） |
| `crowdBuffer` 未赋值 | `GameController` 回退到现有散布聚集（向后兼容） |
| 缓冲区与 PixelGroup 距离较远 | `Enter` 先做短暂匀速靠近入口（决策 #1） |

---

## 12. 调参指南（初值 + 方向）

| 参数 | 影响 | 调大 | 调小 |
|---|---|---|---|
| `spacing` (d) | 拥挤程度 | 更松散、更整齐 | 更挤、更"挤地铁" |
| `separationStrength` | 推挤硬度 | 更硬、难穿透 | 更软、可能短暂重叠 |
| `boundaryPushStrength` | 侧边软硬 | 更硬、更少凸出侧边 | 更软、更容易凸出 |
| `crowdSpeed` | 缓冲区前进快慢 | 更快汇聚到缺口 | 更慢、更"拥挤排队"感 |
| `releaseRadius` | 触发释放的松紧 | 更早释放 | 更贴缺口才释放 |
| `minReleaseInterval` | 过闸节奏 | 更慢、间隔大 | 更快、几乎连过 |
| `entranceWidth` | 入口漏斗张角 | 更宽、更分散 | 更窄、更收敛 |

---

## 13. 待确认决策

> 落地情况：1-从当前位置出发、全程受约束（不瞬移，见 9.2）、2-A（梯形）、3-A（单点集结）、
> 4-保留 fallback、5-`Update` 步进、6-单组件 `CrowdBufferZone`。

1. **入口过渡**：`Enter` 时
   - A（推荐）：入口边紧贴像素群，直接按列映射落位，无过渡；
   - B：先从网格位置匀速靠近入口点，再进入约束（避免瞬移，但多一段逻辑）。
2. **几何形状**：
   - A（推荐）：梯形（入口宽 + 缺口宽）；
   - B：严格扇形/三角形（缺口为单点）。
3. **集结位置散布**：
   - A（推荐）：单点集结（缺口已错开节奏，无需散布）；
   - B：保留现有 `gatherScatterRadius` 小散布。
4. **是否保留旧散布聚集作为 fallback**：推荐保留（`crowdBuffer` 未赋值时回退）。
5. **步进频率**：`Update`（简单）还是 `FixedUpdate`（稳定）。推荐先 `Update`。
6. **代码组织**：单组件 `CrowdBufferZone`（推荐）还是"纯 C# 仿真器 + 薄 MonoBehaviour"（沿用 `ContainerGenerationPlanner` 的分离风格，便于日后测试）。

---

## 14. 实现步骤（文档确认后执行）

1. 新建 `CrowdBufferZone.cs`（含几何推导、仿真步进、释放调度、匀速移动）。
2. 在 `GameController` 增加 `crowdBuffer` 字段，`ResolveMatch` 分流到 `Enter`/`GatherItem`。
3. 场景里新增 `CrowdBufferZone` 物体，配置 `gapPoint`（可用 `gatherPoint` 同物体或独立点）、宽度、间距等参数。
4. 复用/微调 `MoveToGatherPoint` 的"匀速 + 到点置 flag + 加入列表"逻辑。
5. 自测：点击匹配 → 观察像素汇聚进缓冲区、排队、依次过缺口、抵达集结位置、被容器消费。
6. 按第 12 节调参，达到"挤地铁"的拥挤与节奏感。
