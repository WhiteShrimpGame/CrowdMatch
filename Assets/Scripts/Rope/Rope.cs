using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在两点之间生成一条绳子，支持跟随锚点移动、改变长度，并自带材质兜底。
/// Generates a rope between two points; it follows its anchors, can be resized, and
/// falls back to a generated material when the assigned one is unusable.
///
/// 本类是对第三方组件 UltimateRope 的薄封装——只做参数装配与生命周期管理，
/// 绳子的网格生成与物理模拟全部由 UltimateRope 承担。
/// This is a thin wrapper over the third-party UltimateRope component: it only assembles
/// parameters and manages the lifecycle; mesh generation and physics belong to UltimateRope.
/// </summary>
[RequireComponent(typeof(UltimateRope))]
public class Rope : MonoBehaviour
{
    [Header("Anchors / 锚点")]
    [Tooltip("起点锚点 / Start anchor")]
    public Transform startAnchor;
    [Tooltip("终点锚点 / End anchor")]
    public Transform endAnchor;

    [Header("Shape / 形态")]
    [Tooltip("绳节数量 / Number of links")]
    public int linkCount = 10;
    [Tooltip("绳子直径 / Rope diameter")]
    public float diameter = 0.2f;
    [Tooltip("绳子长度。<= 0 时自动取两锚点距离 / Rope length. <= 0 means auto: distance between anchors")]
    public float length = 0f;

    [Header("Material / 材质")]
    [Tooltip("绳子材质。留空或 shader 缺失时自动生成兜底材质 / Rope material. Auto-generated fallback when empty or its shader is missing")]
    public Material material;
    [Tooltip("绳节截面材质。留空则复用 material / Section material. Reuses 'material' when empty")]
    public Material sectionMaterial;
    [Tooltip("兜底材质使用的贴图（未安装 Toony Colors Pro 时生效）/ Textures for the fallback material")]
    public Texture ropeTex;
    public Texture normalTex;

    [Header("Physics / 物理")]
    [Tooltip("单个绳节质量 / Mass per link")]
    public float linkMass = 0.1f;
    [Tooltip("关节求解迭代次数 / Joint solver iterations")]
    public int solverIterations = 30;
    [Tooltip("关节角限位（X/Y/Z 相同）/ Joint angular limit on X/Y/Z")]
    public float jointLimit = 90f;
    [Tooltip("绳节所在层名。该层不存在时回退到 Default / Layer for rope links; falls back to Default")]
    public string ropeLayerName = "Rope";

    [Header("Extensible / 可伸缩")]
    [Tooltip("开启后才能调用 ExtendBy() 平滑改长 / Required for smooth resizing via ExtendBy()")]
    public bool extensible = false;
    [Tooltip("可伸缩的总预算。必须在 Build() 之前给足 / Total extension budget. Must be set before Build()")]
    public float extensibleLength = 0f;

    [Header("Lifecycle / 生命周期")]
    [Tooltip("Start() 时自动生成绳子 / Build automatically in Start()")]
    public bool buildOnStart = true;

    private UltimateRope _rope;
    private bool _built;

    /// <summary>底层 UltimateRope 组件，用于访问本封装未覆盖的高级参数 / The underlying UltimateRope component.</summary>
    public UltimateRope RopeComponent { get { return _rope; } }

    /// <summary>是否已成功生成 / Whether the rope has been built.</summary>
    public bool IsBuilt { get { return _built; } }

    private void Start()
    {
        if (buildOnStart && !_built)
        {
            Build();
        }
    }

    /// <summary>
    /// 生成（或重新生成）绳子。锚点缺失、参数非法或 UltimateRope 生成失败时返回 false 并打印原因。
    /// Builds (or rebuilds) the rope. Returns false and logs the reason on failure.
    /// </summary>
    [ContextMenu("Build Rope / 生成绳子")]
    public bool Build()
    {
        if (startAnchor == null || endAnchor == null)
        {
            Debug.LogError("[Rope] startAnchor / endAnchor 未指定，无法生成绳子。/ Anchors are not assigned.", this);
            return false;
        }

        _rope = GetComponent<UltimateRope>();
        if (_rope == null)
        {
            _rope = gameObject.AddComponent<UltimateRope>();
        }

        // 绳根上的运动学刚体：对齐参考实现的既有配置。
        // Kinematic rigidbody on the rope root, mirroring the reference implementation's proven setup.
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;

        // 层必须在 Regenerate() 之前设好：UltimateRope 内部会用它给自身与绳节赋值。
        // The layer must be set before Regenerate(): UltimateRope reads it to tag itself and every link.
        _rope.RopeLayer = ResolveLayer();
        _rope.RopeType = UltimateRope.ERopeType.Procedural;
        _rope.RopeStart = startAnchor.gameObject;
        _rope.LinkAxis = UltimateRope.EAxis.Z;

        if (_rope.RopeNodes == null)
        {
            _rope.RopeNodes = new List<UltimateRope.RopeNode>();
        }
        if (_rope.RopeNodes.Count == 0)
        {
            _rope.RopeNodes.Add(new UltimateRope.RopeNode());
        }

        var node = _rope.RopeNodes[0];
        node.goNode = endAnchor.gameObject;
        node.fLength = ResolveLength();
        node.nNumLinks = Mathf.Max(1, linkCount);

        _rope.RopeDiameter = diameter;
        _rope.LinkMass = linkMass;
        _rope.LinkSolverIterationCount = solverIterations;
        _rope.LinkJointAngularXLimit = jointLimit;
        _rope.LinkJointAngularYLimit = jointLimit;
        _rope.LinkJointAngularZLimit = jointLimit;

        // 伸缩能力必须在生成时预埋（UltimateRope 会预留一批折叠绳节），运行时才能平滑伸缩。
        // Extensibility must be declared before generation: UltimateRope pre-allocates folded links.
        _rope.IsExtensible = extensible;
        _rope.ExtensibleLength = extensible ? Mathf.Max(0f, extensibleLength) : 0f;

        ApplyMaterial();

        if (!_rope.Regenerate())
        {
            // UltimateRope 不抛异常，失败原因放在 Status 字符串里。
            // UltimateRope never throws; the failure reason is stored in Status.
            Debug.LogError("[Rope] Regenerate 失败 / failed: " + _rope.Status, this);
            _built = false;
            return false;
        }

        _built = true;
        return true;
    }

    /// <summary>
    /// 改变绳子长度（重建式，无范围限制）。会重建全部绳节，可能有轻微跳变。
    /// Resizes by rebuilding every link. No range limit, but may visibly pop.
    /// </summary>
    public void SetLength(float newLength)
    {
        length = Mathf.Max(0.01f, newLength);

        if (!_built)
        {
            Build();
            return;
        }

        if (_rope == null || _rope.RopeNodes == null || _rope.RopeNodes.Count == 0)
        {
            return;
        }

        _rope.RopeNodes[0].fLength = length;

        if (!_rope.Regenerate())
        {
            Debug.LogError("[Rope] SetLength 失败 / failed: " + _rope.Status, this);
        }
    }

    /// <summary>
    /// 平滑地增减绳长。需在 Build() 之前开启 extensible 并给足 extensibleLength 预算；
    /// 超出预算时 UltimateRope 静默截断，请改用 SetLength()。
    /// Smoothly extends/shrinks the rope. Requires extensible + an extensibleLength budget set
    /// before Build(); beyond that budget UltimateRope silently clamps, so use SetLength() instead.
    /// </summary>
    public void ExtendBy(float delta)
    {
        if (!_built || _rope == null)
        {
            Debug.LogWarning("[Rope] 尚未生成绳子，无法伸缩。/ Rope has not been built yet.", this);
            return;
        }

        if (!extensible)
        {
            Debug.LogWarning("[Rope] extensible 未开启。请在 Build() 之前设置 extensible = true 与 extensibleLength 预算。/ Enable 'extensible' and give an 'extensibleLength' budget before Build().", this);
            return;
        }

        _rope.ExtendRope(UltimateRope.ERopeExtensionMode.LinearExtensionIncrement, delta);
    }

    /// <summary>
    /// 更换锚点并重新生成。
    /// Swaps the anchors and rebuilds.
    /// </summary>
    public void SetAnchors(Transform start, Transform end)
    {
        startAnchor = start;
        endAnchor = end;

        if (!_built)
        {
            return;
        }

        if (startAnchor == null || endAnchor == null)
        {
            Debug.LogError("[Rope] startAnchor / endAnchor 未指定，无法重新生成。/ Anchors are not assigned.", this);
            return;
        }

        _rope.RopeStart = startAnchor.gameObject;
        _rope.RopeNodes[0].goNode = endAnchor.gameObject;

        if (length <= 0f)
        {
            _rope.RopeNodes[0].fLength = Vector3.Distance(startAnchor.position, endAnchor.position);
        }

        if (!_rope.Regenerate())
        {
            Debug.LogError("[Rope] SetAnchors 失败 / failed: " + _rope.Status, this);
        }
    }

    private float ResolveLength()
    {
        if (length > 0f)
        {
            return length;
        }

        return Vector3.Distance(startAnchor.position, endAnchor.position);
    }

    private int ResolveLayer()
    {
        int layer = LayerMask.NameToLayer(ropeLayerName);
        if (layer < 0)
        {
            Debug.LogWarning(string.Format(
                "[Rope] 项目中没有名为 \"{0}\" 的层，绳节将落在 Default 层，可能与其他碰撞体互动。请先在 Tags and Layers 中添加该层。" +
                " Layer \"{0}\" does not exist; links fall back to Default and may collide unexpectedly.", ropeLayerName), this);
            layer = 0;
        }

        return layer;
    }

    private void ApplyMaterial()
    {
        // 最常见的情况：使用者指定了随 skill 附带的 Rope.mat，但目标项目没装 Toony Colors Pro，
        // 于是材质的 shader 引用解析失败（shader == null）。此处检测并回退。
        // Typical case: the bundled Rope.mat is assigned but Toony Colors Pro is absent, so its
        // shader reference fails to resolve. Detect that and fall back.
        if (material != null && material.shader == null)
        {
            Debug.LogWarning(
                "[Rope] 指定材质的 shader 缺失，通常是因为目标项目未安装 Toony Colors Pro。已改用代码生成的兜底材质。" +
                " The assigned material has a missing shader (Toony Colors Pro not installed); using a generated fallback.", this);
            material = null;
        }

        if (material == null)
        {
            material = CreateFallbackMaterial();
        }

        _rope.RopeMaterial = material;
        _rope.RopeSectionMaterial = sectionMaterial != null ? sectionMaterial : material;
    }

    private Material CreateFallbackMaterial()
    {
        Shader shader = Shader.Find("Toony Colors Pro 2/Hybrid Shader Outline");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            Debug.LogWarning("[Rope] 未找到任何可用 shader，绳子将使用 UltimateRope 的默认材质。/ No usable shader found.", this);
            return null;
        }

        var mat = new Material(shader);
        mat.name = "Rope_Fallback";

        if (mat.HasProperty("_Color"))
        {
            mat.color = new Color(0.82f, 0.72f, 0.55f);
        }

        if (ropeTex != null)
        {
            if (mat.HasProperty("_MainTex"))
            {
                mat.mainTexture = ropeTex;
            }
            else if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", ropeTex);
            }
        }

        if (normalTex != null)
        {
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalTex);
            }
            else if (mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", normalTex);
            }
        }

        return mat;
    }
}
