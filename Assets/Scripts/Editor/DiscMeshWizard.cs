using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 生成指定边数（圆周顶点数）与直径的圆片 mesh 资产。
    /// 法线朝 +Y、水平放置（可直接替换 Plane 做头部描边），正面从上方看为顺时针绕序。
    /// 菜单：CrowdMatch → 生成圆片 Mesh...
    /// </summary>
    public class DiscMeshWizard : ScriptableWizard
    {
        [Tooltip("圆周顶点数（边数），越大越圆滑，建议 24~64")]
        public int segments = 32;

        [Tooltip("圆片直径（世界单位）")]
        public float diameter = 1.2f;

        [MenuItem("CrowdMatch/生成圆片 Mesh...")]
        private static void CreateWizard()
        {
            DisplayWizard<DiscMeshWizard>("生成圆片 Mesh", "创建");
        }

        private void OnWizardUpdate()
        {
            isValid = segments >= 3 && diameter > 0f;
            helpString = "生成一个法线朝上的圆片，直接拖到 MeshFilter 使用。";
        }

        private void OnWizardCreate()
        {
            if (segments < 3 || diameter <= 0f)
                return;

            var mesh = BuildDisc(segments, diameter);

            string path = EditorUtility.SaveFilePanelInProject(
                "保存圆片 Mesh", mesh.name, "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path))
            {
                Object.DestroyImmediate(mesh);
                return;
            }

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = mesh;
            EditorGUIUtility.PingObject(mesh);
            Debug.Log("[DiscMeshWizard] 已生成圆片 mesh：" + path + "（" + segments + " 边，直径 " + diameter + "）");
        }

        /// <summary>构建圆片 mesh：中心 + 圆周 segments 个顶点，扇形三角化，法线朝 +Y。</summary>
        public static Mesh BuildDisc(int segments, float diameter)
        {
            float radius = diameter * 0.5f;

            var verts = new Vector3[segments + 1];
            var uvs = new Vector2[segments + 1];
            var normals = new Vector3[segments + 1];
            var tris = new int[segments * 3];

            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                verts[i + 1] = new Vector3(x, 0f, z);
                uvs[i + 1] = new Vector2(0.5f + x / diameter, 0.5f + z / diameter);
            }

            for (int i = 0; i < segments; i++)
            {
                int cur = i + 1;
                int next = i + 2;
                if (next >= verts.Length)
                    next = 1;   // 回绕到第一个圆周顶点

                // 从 +Y（上方）看正面为顺时针：中心 → 下一个 → 当前
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = next;
                tris[i * 3 + 2] = cur;
            }

            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;

            var mesh = new Mesh();
            mesh.name = "Disc_" + segments + "_" + diameter;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
