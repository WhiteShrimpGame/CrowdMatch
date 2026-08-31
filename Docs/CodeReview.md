# CrowdMatch Demo 代码 Review

> Review 范围：`Assets/Scripts/` 下全部 12 个脚本。
> 结论先行：**核心玩法逻辑完整、可运行**，命名清晰、中文注释详尽、边界检查到位（`GetMaterial` 越界返回 null、`IsInRange`、Planner 的 `Clamp`）、编辑器有 Undo 与 try/catch。
> 主要问题集中在**异步移动的时序竞态**，以及少量语义/持久化小问题。按严重度分级如下。

---

## 🔴 高 — 时序竞态

### H1. 容器补位动画期间被当作前排消费，像素移动到错误位置

**位置**：`ContainerGroup.cs` — `ProcessConsumption` + `DisappearAndRefill` + `MoveContainer` + `MovePixelToContainer`。

**问题**：`DisappearAndRefill` 会**立即**更新 `grid[col, newRow]`，但后排容器是通过 `MoveContainer` 协程**异步**移动到新位置的。此时 `grid[col, 0]` 已经指向"补位中"的容器，但它的 `transform.position` 还在旧位置（动画中途）。

下一帧 `ProcessConsumption` 就可能给这个补位中的容器分配像素，`MovePixelToContainer` 里：

```csharp
Vector3 target = container.transform.position;   // ← 读到的是动画中途位置
```

像素会移动到容器的**旧/中途位置**后销毁，而容器继续动画到 `row 0`。视觉上像素在错误位置消失。

**影响**：视觉错乱（像素与容器错位），容量计数不受影响（`Consume` 已正确扣减）。

**建议修复**（二选一）：

- **方案 A（推荐）**：给 `ContainerGroup` 加列级/全局补位锁，补位期间跳过消费（与 `GameController._refillMovingCount` 同思路）：
  ```csharp
  private int _refillMovingCount;   // MoveContainer 自增/自减
  // ProcessConsumption 开头：
  if (_refillMovingCount > 0) return;   // 或改为按列跳过
  ```
- **方案 B**：`MovePixelToContainer` 的目标用容器的**最终网格位置**而非当前 transform：
  ```csharp
  Vector3 target = container.transform.parent.TransformPoint(
      container.group.GetLocalPosition(container.gridX, container.gridZ));
  ```
  但方案 B 仍会有"像素先到空位、容器后到"的轻微不同步，只是消除了"追移动目标"。

---

## 🟡 中 — 语义不一致 / 持久化

### M1. `Consume()` 在像素「开始移动」时扣容量，与文档「到达时消失」语义不符

**位置**：`ContainerGroup.cs` — `ProcessConsumption` / `MovePixelToContainer`；`ContainerItem.cs` — `Consume`。

**问题**：当前 `bool isLast = front.Consume()` 在像素**开始移动**（`StartCoroutine` 前）就扣容量、判定 isLast；而 `DisappearAndRefill` 在像素**到达后**（协程末尾）才执行。

中间态：容器 `_remaining = 0`（`IsEmpty`）但仍挂在场景里显示 `0`，等最后一个像素到达才消失。`IsEmpty` 挡住了重复分配，所以**当前不产生逻辑错误**，但：

- 注释「最后一个 PixelItem 到达时容器消失」与实现不符；
- 若未来需要"像素移动失败/被中断时回滚容量"，当前结构无法回滚（容量已被扣）。

**建议**：把 `Consume()` 移到 `MovePixelToContainer` 协程**末尾**（像素到达后）再执行，或至少更新注释/文档以匹配实际行为。

### M2. 编辑模式下容量文本改动未 `SetDirty`，可能不保存/不刷新

**位置**：`ContainerGroupEditor.cs` — 生成循环里 `item.SetCapacity(plan.capacity)` 之后仅 `EditorUtility.SetDirty(item)`。

**问题**：`SetCapacity → UpdateText` 会写 `capacityText.text`（子物体 `Text` 组件），但只对 `ContainerItem` 调了 `SetDirty`，没有对 `Text` 组件 `SetDirty`。编辑模式下该改动可能不写回场景/场景视图不刷新。

**影响**：编辑模式场景视图的容量数字可能不更新；运行时 `Awake → UpdateText` 会重设，故**运行无影响**。

**建议**：生成时若找到了 `capacityText`，额外 `EditorUtility.SetDirty(item.capacityText)`。

---

## 🟢 低 — 代码质量 / 可维护性

### L1. `FindObjectOfType` 已标记 deprecated

**位置**：`GameController.cs`（`Start`）、`ContainerGroupEditor.cs`（查找 PixelGroup）。

**问题**：`Object.FindObjectOfType<T>()` 在 Unity 2021.3+ 已标记 obsolete。

**建议**：迁移到 `Object.FindFirstObjectByType<PixelGroup>()`（当前场景唯一实例语义）。

### L2. `ApplyMaterial` 用 `sharedMaterial`

**位置**：`PixelItem.cs` / `ContainerItem.cs`。

**问题**：`sharedMaterial` 只改引用，不实例化材质。当前"所有同色单位共享同一材质引用"是**正确且省内存**的。但若未来想让**单个**单位运行时变色（如高亮/闪烁），必须改用 `material`（会创建实例）。

**建议**：当前无需改；仅记录为后续需求时的注意点。

### L3. 补位锁职责不清晰

**位置**：`GameController.cs` — `_refillMovingCount` 只覆盖 PixelGroup 的补位，不覆盖 `ContainerGroup.MoveContainer` 的补位。

**问题**：命名上 `_refillMovingCount` 像全局"补位中"标志，实际只对 PixelGroup 生效，容易误导。这与 H1 也有关系（容器侧缺自己的锁）。

**建议**：重命名为 `_pixelRefillMovingCount`，并为 ContainerGroup 单独加锁（见 H1 方案 A）。

### L4. 魔法数字散落

**问题**：`gatherSpeed=12`、`refillSpeed=10`、`consumeSpeed=10`、`xSpacing=1.2`、`gatherScatterRadius=0.35` 等散落各组件。Demo 规模可接受，但若扩展关卡/难度，建议集中为配置。

### L5. `ContainerGenerationPlanner` 的 `LayerCount` / `ColorCount` 属性当前无调用点

**位置**：`ContainerGenerationPlanner.cs`。

**问题**：`LayerCount`、`ColorCount` 暴露但无外部使用。无害，若为后续测试保留可加注释说明。

---

## ✅ 做得好的地方（值得保留）

1. **边界防御**：`ColorConfig.GetMaterial` 越界返回 null、`PixelGroup/ContainerGroup.IsInRange`、`Planner` 里 `Mathf.Clamp` / `Mathf.Max` 钳制——多处防越界。
2. **解耦设计**：`ContainerGenerationPlanner` 纯 C#、无场景耦合，符合"数据 + 逻辑分离"，可单测、可复用。
3. **编辑器健壮性**：`Undo.RegisterCreatedObjectUndo` / `Undo.DestroyObjectImmediate` / `Undo.AddComponent` 支持撤销；生成流程外层 try/catch + 详尽日志。
4. **注释质量**：中文注释 + Tooltip 齐全，坐标约定（前后排方向相反）有明确标注。
5. **单例与执行顺序**：`GameManager(-1000)` → `GameController(-900)` → `ContainerGroup(0)`，依赖关系清晰。

---

## 修复优先级建议

| 优先级 | 编号 | 动作 |
|---|---|---|
| 立即 | H1 | 补位锁（方案 A）或目标位置改用网格坐标（方案 B） |
| 本次 | M1 | 统一 Consume 时机与文档/注释 |
| 本次 | M2 | 编辑器 `SetDirty(capacityText)` |
| 可选 | L1–L5 | 顺带清理 |
