using System.IO;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(ColorConfig))]
    public class ColorConfigEditor : Editor
    {
        public const int ColorCount = 24;

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
        }

        [MenuItem("CrowdMatch/Create Color Config (24 种颜色)")]
        public static void CreateColorConfig()
        {
            const string dir = "Assets/CrowdMatch";
            EnsureFolder(dir);

            const string path = "Assets/CrowdMatch/ColorConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<ColorConfig>(path);
            if (config == null)
            {
                config = CreateInstance<ColorConfig>();
                AssetDatabase.CreateAsset(config, path);
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
