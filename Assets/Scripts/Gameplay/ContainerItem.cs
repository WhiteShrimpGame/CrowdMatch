using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 容器单位：配置颜色 ID 与容量，挂载一个 UI Text 显示剩余容量。
    /// 由 ContainerGroup 生成；gridX / gridZ 记录网格坐标。
    /// </summary>
    public class ContainerItem : MonoBehaviour
    {
        [Tooltip("容器接受的颜色 ID")]
        public int colorId;

        [Tooltip("总容量（可容纳的 PixelItem 数量）")]
        public int capacity = 1;

        [Tooltip("显示容量的 UI Text，留空自动从子物体查找")]
        public Text capacityText;

        [Tooltip("网格列坐标（横，X 方向）")]
        public int gridX;

        [Tooltip("网格行坐标（纵，Z 方向），0 为最前排，越大越靠后")]
        public int gridZ;

        [Header("小车出库轴（可选）")]
        [Tooltip("前轴（空子物体，出车时的驱动轴）")]
        public Transform frontAxle;

        [Tooltip("后轴（空子物体，倒车时的驱动轴）")]
        public Transform rearAxle;

        [Tooltip("倒车缩放轴（空子物体，倒车/出车时置于轴与车体之间，X 缩放用于惯性夸张）")]
        public Transform reverseScaleAxle;

        [Tooltip("侧翻自转轴（空子物体，最深层节点，位于缩放轴与车体之间，绕前进轴旋转做惯性侧翻）")]
        public Transform rollAxle;

        [Tooltip("弹性缩放轴（空子物体，侧翻归 0 后单独应用的 XZ 放大/Y 缩小弹性缩放 pivot，可选）")]
        public Transform elasticScaleAxle;

        [Tooltip("盖子（可选，前排打开时直接隐藏；后排在前方全部找全匹配对象时播放 DisappearWithPop 消失动画）")]
        public Transform lidTransform;

        /// <summary>盖子是否已打开（隐藏）。运行时赋值，不序列化。</summary>
        [System.NonSerialized] public bool lidOpened;

        /// <summary>是否正在补位移动（Row 间 lerp）中。移动中禁止匹配与出库，避免与补位动画冲突。</summary>
        [System.NonSerialized] public bool isRefilling;

        /// <summary>所属 ContainerGroup（运行时赋值，不序列化）</summary>
        [System.NonSerialized] public ContainerGroup group;

        [Header("材质替换")]
        [Tooltip("按配置替换容器上指定 Renderer 的材质；留空则回退到现有逻辑（用 colorId 给首个 Renderer 上色）")]
        public List<MaterialReplacement> materialReplacements = new List<MaterialReplacement>();

        [System.Serializable]
        public class MaterialReplacement
        {
            [Tooltip("需要替换材质的 Renderer（指向本预制体上的 Renderer，实例化后自动重映射到实例）")]
            public Renderer renderer;

            [Tooltip("要替换的材质槽位下标（Renderer.materials 数组的 index），颜色仍按 colorId 取")]
            public int materialSlotIndex;
        }

        private Renderer _renderer;
        private int _remaining;

        /// <summary>剩余容量</summary>
        public int Remaining => _remaining;

        public bool IsEmpty => _remaining <= 0;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _remaining = capacity;
            if (capacityText == null)
                capacityText = GetComponentInChildren<Text>();
            ApplyMaterial();
            UpdateText();
        }

        /// <summary>设置容量（编辑器与运行时都可用），并刷新显示</summary>
        public void SetCapacity(int cap)
        {
            capacity = cap;
            _remaining = cap;
            UpdateText();
        }

        /// <summary>消耗 1 点容量，返回是否耗尽</summary>
        public bool Consume()
        {
            _remaining--;
            UpdateText();
            return _remaining <= 0;
        }

        public void UpdateText()
        {
            if (capacityText == null)
                capacityText = GetComponentInChildren<Text>();
            if (capacityText != null)
                capacityText.text = _remaining.ToString();
        }

        /// <summary>直接隐藏盖子（初始就在第一排的小车使用）。</summary>
        public void HideLid()
        {
            lidOpened = true;
            if (lidTransform != null)
                lidTransform.gameObject.SetActive(false);
        }

        /// <summary>播放开盖动画（后排小车满足「前方全部找全匹配对象」时使用），幂等：只播放一次。</summary>
        public void OpenLid()
        {
            if (lidOpened)
                return;
            lidOpened = true;
            if (lidTransform == null || !lidTransform.gameObject.activeSelf)
                return;
            lidTransform.DisappearWithPop(() =>
            {
                if (lidTransform != null)
                    lidTransform.gameObject.SetActive(false);
            });
        }

        /// <summary>按 colorId 应用材质，config 为空时从 GameManager 获取；随后按 materialReplacements 替换指定 Renderer 的指定材质槽位（颜色仍用 colorId）。</summary>
        public void ApplyMaterial(ColorConfig config = null)
        {
            if (config == null)
            {
                if (GameManager.Instance != null)
                    config = GameManager.Instance.colorConfig;
            }
            if (config == null)
                return;

            // 现有逻辑：colorId → 首个 Renderer 的材质
            var mat = config.GetMaterial(colorId);
            if (mat == null)
                return;

            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            if (_renderer != null)
                _renderer.sharedMaterial = mat;

            // 按配置替换指定 Renderer 的指定材质槽位（颜色仍用 colorId）
            if (materialReplacements == null)
                return;
            foreach (var rep in materialReplacements)
            {
                if (rep == null || rep.renderer == null)
                    continue;
                ApplyToMaterialSlot(rep.renderer, rep.materialSlotIndex, mat);
            }
        }

        /// <summary>把 renderer 的 materials 数组里 index 下标处替换为 mat（越界则忽略）。</summary>
        private static void ApplyToMaterialSlot(Renderer renderer, int index, Material mat)
        {
            var mats = renderer.sharedMaterials;
            if (mats == null || index < 0 || index >= mats.Length)
                return;
            mats[index] = mat;
            renderer.sharedMaterials = mats;
        }
    }
}
