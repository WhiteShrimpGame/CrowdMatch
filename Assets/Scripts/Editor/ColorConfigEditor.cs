using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(ColorConfig))]
    public class ColorConfigEditor : Editor
    {
        public const int ColorCount = 24;
        private const string DefaultAssetPath = "Assets/CrowdMatch/ColorConfig.asset";

        public override void OnInspectorGUI()
        {
            var config = (ColorConfig)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("生成 / 刷新 24 种颜色材质"))
            {
                GenerateMaterials(config);
            }

            if (GUILayout.Button("从材质主色生成字色与描边色"))
            {
                GenerateTextColors(config);
            }
        }

        [MenuItem("CrowdMatch/Create Color Config (24 种颜色)")]
        public static void CreateColorConfig()
        {
            const string dir = "Assets/CrowdMatch";
            EnsureFolder(dir);

            var config = AssetDatabase.LoadAssetAtPath<ColorConfig>(DefaultAssetPath);
            if (config == null)
            {
                config = CreateInstance<ColorConfig>();
                AssetDatabase.CreateAsset(config, DefaultAssetPath);
            }

            GenerateMaterials(config);
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        /// <summary>生成 / 刷新 24 种视觉上差异尽可能大的颜色材质并赋给 config</summary>
        public static void GenerateMaterials(ColorConfig config)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("未找到 Standard 着色器（内置渲染管线可用）。");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(config);
            string baseDir = (string.IsNullOrEmpty(assetPath) ? "Assets/CrowdMatch" : Path.GetDirectoryName(assetPath)).Replace('\\', '/');
            string matDir = baseDir + "/Materials";
            EnsureFolder(matDir);

            Color[] palette = GenerateDistinctColors();

            var materials = new Material[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                Color color = palette[i];

                string matPath = matDir + "/Color_" + i.ToString("00") + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }

                mat.name = "Color_" + i.ToString("00");
                mat.color = color;
                // 关闭高光 / 金属，让颜色更纯粹
                mat.SetFloat("_Glossiness", 0f);
                mat.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(mat);

                materials[i] = mat;
            }

            config.materials = materials;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 从 materials 的主色生成字色与描边色：字色 = 材质主色；
        /// 按 RGB 权重（感知亮度）判断亮暗，亮色描边取原色 1/3，暗色描边取 255-(255-c)/5。
        /// </summary>
        [MenuItem("CrowdMatch/从材质主色生成字色与描边色")]
        public static void GenerateTextColorsFromMaterials()
        {
            var config = Selection.activeObject as ColorConfig;
            if (config == null)
                config = AssetDatabase.LoadAssetAtPath<ColorConfig>(DefaultAssetPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("生成字色与描边色", "未找到 ColorConfig，请先选中 ColorConfig 资产或先创建。", "确定");
                return;
            }

            GenerateTextColors(config);
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        /// <summary>从 config.materials 的主色生成字色/描边色，写入 textColors / textOutlineColors。</summary>
        public static void GenerateTextColors(ColorConfig config)
        {
            if (config.materials == null || config.materials.Length == 0)
            {
                Debug.LogWarning("[ColorConfig] materials 为空，无法生成字色/描边色。");
                return;
            }

            if (config.textColors == null)
                config.textColors = new List<Color>();
            if (config.textOutlineColors == null)
                config.textOutlineColors = new List<Color>();
            config.textColors.Clear();
            config.textOutlineColors.Clear();

            for (int i = 0; i < config.materials.Length; i++)
            {
                var mat = config.materials[i];
                Color c = mat != null ? mat.color : Color.white;
                config.textColors.Add(c);                       // 字色 = 材质主色
                config.textOutlineColors.Add(ComputeOutlineColor(c));
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[ColorConfig] 已从材质主色生成 " + config.textColors.Count + " 组字色/描边色。");
        }

        /// <summary>按 RGB 权重（0.299/0.587/0.114）判断亮暗并计算描边色。</summary>
        private static Color ComputeOutlineColor(Color c)
        {
            float luminance = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            if (luminance > 0.5f)
            {
                // 亮色：描边取原色 1/2（更暗）
                return new Color(c.r / 2f, c.g / 2f, c.b / 2f, 1f);
            }
            // 暗色：描边取 255-(255-c)/6（更亮）
            return new Color(
                1f - (1f - c.r) / 6f,
                1f - (1f - c.g) / 6f,
                1f - (1f - c.b) / 6f,
                1f);
        }

        /// <summary>
        /// 按指定结构生成 24 种颜色：0 饱和黑白灰 3 色、中饱和暗色 6 色、
        /// 中饱和亮色 6 色、高饱和 9 色。色相按人眼敏感度采样——
        /// 红色附近更密集、蓝绿附近更稀疏（见 WarpHue）。
        /// </summary>
        public static Color[] GenerateDistinctColors()
        {
            var colors = new Color[24];
            int idx = 0;

            // 1) 0 饱和：黑 / 灰 / 白
            colors[idx++] = Color.HSVToRGB(0f, 0f, 0.0f);   // 黑
            colors[idx++] = Color.HSVToRGB(0f, 0f, 0.5f);   // 灰
            colors[idx++] = Color.HSVToRGB(0f, 0f, 1.0f);   // 白

            // 2) 中饱和暗色 6 色：色相错开半格，低明度
            const int midCount = 6;
            for (int i = 0; i < midCount; i++)
                colors[idx++] = Color.HSVToRGB(WarpHue((i + 0.5f) / midCount), 0.55f, 0.35f);

            // 3) 中饱和亮色 6 色：与暗色同色相，高明度
            for (int i = 0; i < midCount; i++)
                colors[idx++] = Color.HSVToRGB(WarpHue((i + 0.5f) / midCount), 0.55f, 0.80f);

            // 4) 高饱和 9 色：色相按敏感度分布
            const int highCount = 9;
            for (int i = 0; i < highCount; i++)
                colors[idx++] = Color.HSVToRGB(WarpHue(i / (float)highCount), 0.90f, 0.90f);

            return colors;
        }

        // 把均匀参数 u(0~1) 映射为色相 u'：红色（0°/360°）附近更密集，蓝绿（约 180°）附近更稀疏。
        // 公式 u' = u - A*sin(2*PI*u)：u=0/1（红）处斜率小（密集），u=0.5（蓝绿）处斜率大（稀疏）。
        // A 需 < 1/(2*PI) ≈ 0.159 以保证单调递增。
        private static float WarpHue(float u)
        {
            const float A = 0.08f; // 疏密强度，越大红色越密集
            return u - A * Mathf.Sin(2f * Mathf.PI * u);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
