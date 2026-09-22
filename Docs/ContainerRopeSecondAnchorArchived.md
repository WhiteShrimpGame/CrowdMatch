# 绳组：第二组端点（一对车两条绳）+ 端点显隐 归档

> 本文档归档了 2026-09-22 做过、随后按需求**回退**的两项改动：
> **A. 第二组端点（一对相邻车之间可以再多一条绳）** 与 **B. 端点物体随绳显隐**。
>
> 两者都已从项目代码中移除（`ContainerItem.cs` / `ContainerRopeLink.cs` / `ContainerGroup.cs` /
> `ContainerItemEditor.cs` 已回到单端点版本），**绳子的运行时行为与之前完全一致**。
> 如需恢复，按 §4「重新应用步骤」把 §3 的各段代码贴回对应位置即可 —— §3 已按 A / B 标注归属，
> 可以只恢复其中一项。

## 1. 两项改动各自是什么

| | A. 第二组端点 | B. 端点显隐 |
|---|---|---|
| 效果 | 一对相邻车之间最多两条绳：第一组端点一条、第二组端点一条 | 端点物体（可能挂着绳头装饰）默认隐藏，只有**真建出绳**时才显示 |
| 入口 | `ContainerItem.ropeAnchorLeft2` / `ropeAnchorRight2` | 无需新字段；`ContainerGroup.ApplyAnchorVisibility` |
| 判定粒度 | **逐段**：某一段两端都配了才多一条绳 | 逐端点：该端点所属的那条绳存在才显示 |
| 是否影响玩法 | **否**，纯视觉/物理；绳组的出库判定仍只看 `ropeGroupId` | 否 |

## 2. 当时确认的口径（逐条与用户核对过）

| # | 问题 | 结论 |
|---|---|---|
| 1 | 第二组端点怎么配 | `ContainerItem` 上加第二对字段（不是数组），**逐段判定**；只配一端则该段只建 1 条绳 |
| 2 | 第二条绳的参数 | 与第一条**完全共用**（材质 / 节数 / 直径 / 微晃），绳根名字加 `_2` 后缀 |
| 3 | 「绳子未激活」指什么 | 与绳子创建统一：**建绳成功才显示端点，端点物体默认隐藏**；覆盖「洗牌激活不建绳」「绳子总开关关」「某段端点缺失该绳没建出来」三种路径 |
| 4 | 端点怎么恢复 | **建绳即强制显示**（不记忆预制体原始值 —— 默认值本来就是隐藏） |
| 5 | 作用范围 | 只管绳组成员（`ropeGroupId != 0`）；不在绳组里的车端点完全不碰，可留作装饰 |

## 3. 逐文件改动（可直接贴回）

### 3.1 `Assets/Scripts/Gameplay/ContainerItem.cs` —— 【A】

在 `ropeAnchorRight` 之后插入：

```csharp
        [Tooltip("绳子左端点·第二组（空物体）。第二组是**可选**的：某一段的两端都配了这一组，" +
            "才会在它们之间再多建一条绳，参数（材质 / 节数 / 直径 / 微晃）与第一条共用")]
        public Transform ropeAnchorLeft2;

        [Tooltip("绳子右端点·第二组（空物体）。与 ropeAnchorRight2 配对，见 ropeAnchorLeft2 说明")]
        public Transform ropeAnchorRight2;
```

### 3.2 `Assets/Scripts/Gameplay/ContainerRopeLink.cs`

**A —— 新增字段**（`rightCar` 之后）：

```csharp
        [Tooltip("这一条绳用**第二组**端点（左车 ropeAnchorRight2 / 右车 ropeAnchorLeft2）而不是第一组。" +
            "一对车之间最多两条绳：第一组一条、第二组一条，由 ContainerGroup 逐段决定建不建")]
        public bool useSecondAnchors;
```

**A —— `StartAnchor` / `EndAnchor`：由 private 表达式属性改为 public 且按 `useSecondAnchors` 分支**
（public 是为了让 `ContainerGroup.ApplyAnchorVisibility` 读到「这条绳实际用了哪两个端点」）：

```csharp
        /// <summary>
        /// 起点：左车的右端点（<see cref="useSecondAnchors"/> 为真时取第二组）。
        /// 公开给 <see cref="ContainerGroup.ApplyAnchorVisibility"/> 读 —— 它按「这个端点有没有绳连着」
        /// 决定端点物体显隐，所以要知道每条绳实际用的是哪两个端点。
        /// </summary>
        public Transform StartAnchor => leftCar != null
            ? (useSecondAnchors ? leftCar.ropeAnchorRight2 : leftCar.ropeAnchorRight)
            : null;

        /// <summary>终点：右车的左端点（<see cref="useSecondAnchors"/> 为真时取第二组）。</summary>
        public Transform EndAnchor => rightCar != null
            ? (useSecondAnchors ? rightCar.ropeAnchorLeft2 : rightCar.ropeAnchorLeft)
            : null;
```

**A —— `Build()` 开头的端点缺失警告**（把写死的第一组字段名按组切换）：

```csharp
                Debug.LogWarning("[ContainerRopeLink] 端点缺失（左车 " + NameOf(leftCar) +
                    (useSecondAnchors ? " 的 ropeAnchorRight2 / 右车 " : " 的 ropeAnchorRight / 右车 ") +
                    NameOf(rightCar) + (useSecondAnchors ? " 的 ropeAnchorLeft2" : " 的 ropeAnchorLeft") +
                    "），该组不建绳。", this);
```

**B —— 新增字段 `_group`**（`_ready` 之后），并在 `Build()` 的端点检查通过后赋值：

```csharp
        /// <summary>所属 ContainerGroup（端点显隐由它统一裁决，绳消亡时通知它重算）。</summary>
        private ContainerGroup _group;
```

```csharp
            _group = leftCar.GetComponentInParent<ContainerGroup>();
```

**B —— 新增 `OnDestroy()`**（放在 `LateUpdate()` 之后）：

```csharp
        /// <summary>
        /// 绳没了（车出库后绳根自毁 / 关卡重建清空绳根）→ 请 ContainerGroup 重算端点显隐：
        /// 这条绳连着的端点不再算「有绳连着」，其中**还活着的那一端**（另一端随车一起销毁了）
        /// 就此隐藏。本组件自己不碰端点显隐，避免与 ContainerGroup 各改一半、时序上互相打架。
        /// </summary>
        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;
            if (_group != null)
                _group.RequestAnchorRefresh();
        }
```

### 3.3 `Assets/Scripts/Gameplay/ContainerGroup.cs`

**B —— 新增字段**（`_ropeRoots` 之后）：

```csharp
        /// <summary>
        /// 端点显隐待重算。绳根是**延迟销毁**的（<c>Destroy</c> 到帧末才生效），而
        /// <see cref="ApplyAnchorVisibility"/> 靠「绳根还活着吗」判断端点有没有绳连着 ——
        /// 所以绳消亡时只置位、下一帧再算，否则会读到一个「本帧还活着但已注定消失」的绳根，
        /// 把该隐藏的端点继续当成有绳连着。
        /// </summary>
        [System.NonSerialized] private bool _anchorsDirty;
```

**B —— `Update()` 尾部追加**：

```csharp
            // 绳消亡（车出库）那一帧只置位，延到下一帧算 —— 见 _anchorsDirty 的说明
            if (_anchorsDirty)
            {
                _anchorsDirty = false;
                ApplyAnchorVisibility();
            }
```

**A + B —— `BuildRopes()` 整体替换为**：

```csharp
        public void BuildRopes(bool shuffleEnabled)
        {
            ClearRopes();

            if (ropeEnabled && !shuffleEnabled)
            {
                CollectRopeGroups();

                foreach (var list in _ropeGroups.Values)
                {
                    for (int i = 0; i + 1 < list.Count; i++)
                    {
                        CreateRope(list[i], list[i + 1], false);

                        if (HasSecondAnchors(list[i], list[i + 1]))
                            CreateRope(list[i], list[i + 1], true);
                        else
                            WarnHalfConfiguredSecondAnchors(list[i], list[i + 1]);
                    }
                }
            }

            // 端点显隐兜底：上面没给某组端点建绳（总开关关 / 洗牌 / 该组没配齐），它就不该露着
            ApplyAnchorVisibility();
        }

        /// <summary>请求在下一帧重算端点显隐（供 <see cref="ContainerRopeLink"/> 在消亡时调用）。</summary>
        public void RequestAnchorRefresh() { _anchorsDirty = true; }
```

（方法上的 doc 注释同时补了「第二组端点可选、逐段判定」一段说明，原注释前半段不变。）

**A —— `CreateRope` 改为带 `useSecondAnchors` 参数**：

```csharp
        /// <summary>
        /// 建一条绳。<paramref name="useSecondAnchors"/> 决定用哪一组端点；两条绳的参数（材质 / 节数 /
        /// 直径 / 微晃）完全一致，只是绳根名字加 "_2" 后缀便于在层级里区分。
        /// </summary>
        private void CreateRope(ContainerItem left, ContainerItem right, bool useSecondAnchors)
        {
            var go = new GameObject("Rope_" + left.gridX + "_" + right.gridX + (useSecondAnchors ? "_2" : ""));
            // …（绳根父子/位置/旋转/缩放四行不变）…
            var link = go.AddComponent<ContainerRopeLink>();
            link.leftCar = left;
            link.rightCar = right;
            link.useSecondAnchors = useSecondAnchors;
            // …（material / linkCount / diameter / ropeLayerName / 四个 sway 参数不变）…

            ShowAnchorsForBuild(left, right, useSecondAnchors);   // 见该方法说明：读端点前先打开它们

            if (link.Build())
                _ropeRoots.Add(go);
            else
                Destroy(go);   // 端点缺失 / 生成失败：不留空壳
        }
```

**A + B —— 在 `CreateRope` 之后插入四个新方法**：

```csharp
        /// <summary>
        /// 建绳前先把这一组的两个端点打开。
        ///
        /// 端点在车预制体上是**默认关的**，而 UltimateRope 建网格与骨骼时要用端点的世界坐标：
        /// 这里显式打开，让「读端点位置」这件事永远发生在激活的物体上，不去赌
        /// 「未激活物体的 Transform 世界坐标是否仍然新鲜」。建不成的（另一端缺失、Build 返回 false）
        /// 由 <see cref="BuildRopes"/> 收尾的 <see cref="ApplyAnchorVisibility"/> 关回去 ——
        /// 同一帧内开关，中间不出图，不会闪。
        /// </summary>
        private static void ShowAnchorsForBuild(ContainerItem left, ContainerItem right, bool useSecondAnchors)
        {
            if (!Application.isPlaying)
                return;

            var start = useSecondAnchors ? left.ropeAnchorRight2 : left.ropeAnchorRight;
            var end = useSecondAnchors ? right.ropeAnchorLeft2 : right.ropeAnchorLeft;
            if (start != null)
                start.gameObject.SetActive(true);
            if (end != null)
                end.gameObject.SetActive(true);
        }

        /// <summary>该段的第二组端点是否配齐（两端都要有）。配齐 = 这一对车之间建第二条绳。</summary>
        private static bool HasSecondAnchors(ContainerItem left, ContainerItem right)
        {
            return left.ropeAnchorRight2 != null && right.ropeAnchorLeft2 != null;
        }

        /// <summary>
        /// 第二组端点只配了一端时提醒一句。逐段判定的直接结果就是该段第二条绳不建，
        /// 但十有八九是漏配 —— 不提醒的话在场景里只看到「绳少了一条」，找不到原因。
        /// </summary>
        private static void WarnHalfConfiguredSecondAnchors(ContainerItem left, ContainerItem right)
        {
            bool leftHas = left.ropeAnchorRight2 != null;
            bool rightHas = right.ropeAnchorLeft2 != null;
            if (leftHas == rightHas)
                return;   // 两端都没配（正常：这一段就一条绳）或两端都配了（上面已走建绳分支）

            Debug.LogWarning("[ContainerGroup] 第 " + left.gridX + "–" + right.gridX +
                " 列之间第二组端点只配了一端（左车 ropeAnchorRight2：" + (leftHas ? "有" : "空") +
                "，右车 ropeAnchorLeft2：" + (rightHas ? "有" : "空") + "），该段第二条绳不建。");
        }

        /// <summary>
        /// 端点显隐：维持「端点可见 ⟺ 它连着的绳存在」这条不变式。
        ///
        /// 端点物体（空物体 + 可能挂着的绳头装饰）在车预制体上**默认是关的**，运行时由本方法统一裁决：
        ///   · 建绳成功 → 端点算「有绳连着」→ 显示；
        ///   · 绳消亡（车出库后绳根自毁，见 ContainerRopeLink.OnDestroy）→ 下一帧重算 → 隐藏；
        ///   · 根本没建绳（总开关关 / 洗牌）或某段某组端点没配齐 → 那些端点没有绳连着 → 隐藏。
        ///
        /// 判定依据是「当前活着的绳根各自用了哪两个端点」：绳根一被销毁就从活着的集合里消失，
        /// 不需要额外记账也不会漏。只管绳组成员（ropeGroupId != 0）——
        /// 不在绳组里的车端点完全不碰，可以留作装饰。
        ///
        /// 仅运行模式执行：编辑模式下没有真绳（只有 OnDrawGizmos 的预览），
        /// 端点显隐交给预制体本身，免得编辑期偷改场景、把状态写进预制体。
        /// </summary>
        private void ApplyAnchorVisibility()
        {
            if (!Application.isPlaying)
                return;

            var linked = new HashSet<Transform>();
            for (int i = 0; i < _ropeRoots.Count; i++)
            {
                var root = _ropeRoots[i];
                if (root == null)
                    continue;   // 已销毁的绳根：它的端点就此不再算「有绳连着」

                var link = root.GetComponent<ContainerRopeLink>();
                if (link == null)
                    continue;
                if (link.StartAnchor != null)
                    linked.Add(link.StartAnchor);
                if (link.EndAnchor != null)
                    linked.Add(link.EndAnchor);
            }

            var items = GetComponentsInChildren<ContainerItem>(true);   // 含未激活的车：它的端点同样没有绳连着
            for (int i = 0; i < items.Length; i++)
            {
                var car = items[i];
                if (car == null || car.ropeGroupId == 0)
                    continue;
                SetAnchorActive(car.ropeAnchorLeft, linked);
                SetAnchorActive(car.ropeAnchorRight, linked);
                SetAnchorActive(car.ropeAnchorLeft2, linked);    // 只恢复 B 时删掉这两行
                SetAnchorActive(car.ropeAnchorRight2, linked);
            }
        }

        /// <summary>端点物体按「有没有绳连着」显示或隐藏（状态没变就不写，避免无谓地弄脏物体 / 触发回调）。</summary>
        private static void SetAnchorActive(Transform anchor, HashSet<Transform> linked)
        {
            if (anchor == null)
                return;

            bool visible = linked.Contains(anchor);
            if (anchor.gameObject.activeSelf != visible)
                anchor.gameObject.SetActive(visible);
        }
```

**A —— 绳连 Gizmos：`OnDrawGizmos` 的链条循环抽出一个画段方法，并多画第二组**：

```csharp
                for (int i = 0; i + 1 < chain.Count; i++)
                {
                    var leftCar = chain[i];
                    var rightCar = chain[i + 1];

                    DrawRopeGizmoSegment(leftCar.ropeAnchorRight, rightCar.ropeAnchorLeft,
                        leftCar, rightCar, lineColor, live, lift);

                    // 第二组端点：只要有一端配了就画，缺的那端会成为醒目红球（与运行时逐段判定一致）
                    if (leftCar.ropeAnchorRight2 != null || rightCar.ropeAnchorLeft2 != null)
                    {
                        DrawRopeGizmoSegment(leftCar.ropeAnchorRight2, rightCar.ropeAnchorLeft2,
                            leftCar, rightCar, lineColor, live, lift);
                    }
                }
```

```csharp
        /// <summary>画一段绳连预览：一条线 + 两端的小球。端点缺失时退到车体位置画（小球会变醒目红）。</summary>
        private void DrawRopeGizmoSegment(Transform leftAnchor, Transform rightAnchor,
            ContainerItem leftCar, ContainerItem rightCar, Color lineColor, bool live, Vector3 lift)
        {
            Vector3 a = (leftAnchor != null ? leftAnchor.position : leftCar.transform.position) + lift;
            Vector3 b = (rightAnchor != null ? rightAnchor.position : rightCar.transform.position) + lift;

            Gizmos.color = lineColor;
            Gizmos.DrawLine(a, b);

            DrawRopeGizmoAnchor(leftAnchor, a, live);
            DrawRopeGizmoAnchor(rightAnchor, b, live);
        }
```

### 3.4 `Assets/Scripts/Editor/ContainerItemEditor.cs` —— 【A】

**「标记选中车为连接」的 tooltip 追加一句**：

```csharp
                    "选中的车需分处相邻的 N 列、每列恰好 1 个；标记后按列序两两成绳（N 辆车 = N-1 条绳）；" +
                    "某段两端都配了第二组端点（ropeAnchorRight2 / ropeAnchorLeft2）则那一段再多一条绳")))
```

**`OnInspectorGUI` 绳连区块末尾追加**：

```csharp
            // 第二组端点（可选）的配置状态。运行时是**逐段判定**：一段的两端都配了才多建一条绳，
            // 只配一端则该段只有 1 条绳 —— 编辑期就把这件事说清楚，免得在场景里只看到「绳少了一条」。
            foreach (var summary in DescribeSecondAnchors(cars))
                EditorGUILayout.HelpBox(summary.text, summary.warn ? MessageType.Warning : MessageType.None);
```

**新增分析方法**（`OnInspectorGUI` 之后、`SelectedCars` 之前）：

```csharp
        /// <summary>
        /// 选中车所属各绳组的「第二组端点」统计（同组只报一次）。
        /// 逐段口径与运行时 ContainerGroup.BuildRopes 完全一致。
        /// </summary>
        private static List<(string text, bool warn)> DescribeSecondAnchors(List<ContainerItem> cars)
        {
            var result = new List<(string text, bool warn)>();
            var seen = new HashSet<int>();

            foreach (var car in cars)
            {
                if (car == null || car.ropeGroupId == 0 || !seen.Add(car.ropeGroupId))
                    continue;

                var group = car.GetComponentInParent<ContainerGroup>();
                if (group == null)
                    continue;

                var chain = new List<ContainerItem>();
                foreach (var other in group.GetComponentsInChildren<ContainerItem>())
                {
                    if (other != null && other.ropeGroupId == car.ropeGroupId)
                        chain.Add(other);
                }
                if (chain.Count < 2)
                    continue;
                chain.Sort((a, b) => a.gridX.CompareTo(b.gridX));   // 与运行时成链同序

                int full = 0, half = 0, single = 0;
                var halfSegments = new List<string>();
                for (int i = 0; i + 1 < chain.Count; i++)
                {
                    bool leftHas = chain[i].ropeAnchorRight2 != null;
                    bool rightHas = chain[i + 1].ropeAnchorLeft2 != null;
                    if (leftHas && rightHas)
                        full++;
                    else if (leftHas || rightHas)
                    {
                        half++;
                        halfSegments.Add(chain[i].gridX + "–" + chain[i + 1].gridX + " 列");
                    }
                    else
                        single++;
                }

                string text = "绳组 " + car.ropeGroupId + "·第二组端点：" + (chain.Count - 1) + " 段中，两条绳 " +
                    full + " 段、一条绳 " + single + " 段";
                if (half > 0)
                {
                    text += "；只配了一端 " + half + " 段（" + string.Join("、", halfSegments.ToArray()) +
                        "），这些段只会建 1 条绳";
                }
                result.Add((text, half > 0));
            }

            return result;
        }
```

## 4. 重新应用步骤

1. 按 §3 各段贴回代码，**两组端点字段（A）与端点显隐（B）可以分开恢复**：
   - 只恢复 B：跳过 §3.1、`useSecondAnchors`、`HasSecondAnchors`、`WarnHalfConfiguredSecondAnchors`、
     `_2` 后缀、第二组 Gizmos 与 Inspector 统计；`ApplyAnchorVisibility` 里只留
     `ropeAnchorLeft` / `ropeAnchorRight` 两行；`ShowAnchorsForBuild` 去掉参数。
   - 只恢复 A：删掉 `_group` / `OnDestroy` / `_anchorsDirty` / `RequestAnchorRefresh` /
     `ApplyAnchorVisibility` / `SetAnchorActive`，`BuildRopes` 里去掉收尾那次调用。
2. 编译两个程序集（`Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`）确认 0 错误。
3. 在**车预制体**里加两个空物体作为第二组端点并接线；
   **把四个端点物体在预制体里都设为未激活**（这是 B 的前提）。
4. 端点必须是车体根节点的**直接子物体**：别把 `frontAxle` / `ropeExitAxle` 之类的轴挂到端点下面
   —— 端点会被隐藏，挂在它下面的轴会跟着失效。

## 5. 依赖与前置

| 依赖 | 说明 |
|---|---|
| `Rope` / `UltimateRope`（`Assets/Scripts/Rope/`） | 不变；第二组只是多一个绳根、多一套 `ContainerRopeLink` 实例 |
| `ContainerItem.ropeGroupId` | A 与 B 都复用现成的绳组归链（按 `gridX` 升序相邻两两成链），**没有新增任何判定字段** |
| `HashSet<Transform>` / tuple | B 用 `System.Collections.Generic`；编辑器那份用 `List<(string, bool)>`（C# 9 支持） |

## 6. 验证清单（恢复后按此验收）

| # | 检查 | 通过标准 |
|---|---|---|
| 1 | 编译 | 两个程序集 0 错误 |
| 2 | 只有第一组端点 | 与回退后行为**逐字一致**（每段 1 条绳） |
| 3 | 配齐两组 | 该段 2 条绳、4 个端点都显示 |
| 4 | 只配一端 | Console 一条 warning + Gizmos 红球；该段仍只有 1 条绳，第二组端点保持隐藏 |
| 5 | 绳消亡 | 车出库销毁后，存活那一侧的端点隐藏，没有残留装饰 |
| 6 | 洗牌 / 总开关关 | 全组端点隐藏、不建绳；切回非洗牌关卡端点恢复显示 |
| 7 | 无绳组关卡 | 全部 `ropeGroupId = 0` → 与本改动无关，行为不变 |
| 8 | 两条绳的微晃 | 平行摆放的两条完全同相（相位只看 `Time.time`，基底取各自连线） |

## 7. 未采用的原因与恢复时的留意点

- **第二条绳不参与任何判定**：绳组的出库 / 等待关系仍然只看 `ropeGroupId`，
  所以 A 是纯视觉/物理功能 —— 恢复时不要指望它带来玩法差异。
- **开销**：每条绳一套 SkinnedMeshRenderer + 骨骼，配了两组的段在绳上开销翻倍。
- **端点默认隐藏的前提**：B 依赖「预制体里端点未激活」。若美术把端点留成激活状态，
  没建绳的端点就会露着（这种情况下 `ApplyAnchorVisibility` 只在**建绳的那个时刻和绳消亡时**才写状态，
  `ClearRopes` 之后的兜底那次会关掉，但下次进关若仍是同一批物体就要靠那次兜底）。
- **不要赌未激活物体的 Transform 世界坐标**：`ShowAnchorsForBuild` 就是为此存在的
  （建绳时 UltimateRope 要读端点世界坐标，端点默认未激活）。
- **延迟销毁的时序**：`_anchorsDirty` 必须**下一帧**才算 —— `Destroy` 到帧末才生效，
  当帧重算会把「已注定消失的绳根」误判为还连着。
