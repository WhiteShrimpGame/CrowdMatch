using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 生成 ElevatorItem 默认预制体：根节点挂 ElevatorItem 组件，视觉子节点按
    /// Frame（4 横梁）/ Door（左右门板）/ HoleMask（挖洞蒙版）/ Pit（5 面竖井）组织，
    /// 并自动配置 ElevatorItem 上的视觉引用字段（frameRoot/doorLeft/doorRight/holeMask/pitRoot）。
    /// 材质会保存为 prefab 同目录下 ElevatorItem_Materials/ 内的 .mat 资产（避免运行时材质丢失），
    /// 美术后续只需替换各子节点的模型与材质。
    /// </summary>
    public static class ElevatorItemPrefabCreator
    {
        [MenuItem("CrowdMatch/更多工具/生成升降台预制体", false, MenuPriority.More + MenuPriority.Seg2)]
        public static void CreateElevatorPrefab()
        {
            string dir = AssetDatabase.IsValidFolder("Assets/Prefabs") ? "Assets/Prefabs" : "Assets";

            string path = EditorUtility.SaveFilePanelInProject(
                "保存升降台预制体", "ElevatorItem", "prefab", "选择保存位置", dir);
            if (string.IsNullOrEmpty(path))
                return;

            // 材质子文件夹（与 prefab 同目录；目录不存在时回退到 Assets）
            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                folder = "Assets";
            string matDir = folder + "/ElevatorItem_Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
                AssetDatabase.CreateFolder(folder, "ElevatorItem_Materials");

            var root = new GameObject("ElevatorItem");
            var elev = root.AddComponent<ElevatorItem>();

            // 默认按 3×3 区域初始化（col 0..2 × row 0..2），供编辑器预览一个正确摆放的升降台。
            // 运行时 BuildVisual 会按实际区域与 PixelGroup 尺寸重新计算。
            elev.colMin = 0;
            elev.rowMin = 0;
            elev.colMax = 2;
            elev.rowMax = 2;

            // 先保存材质为 .mat 资产（有 asset path，prefab 才能正确引用）
            Material frameMat = CreateColorMaterialAsset(matDir, "Elevator_Frame", elev.frameColor);
            Material doorMat = CreateColorMaterialAsset(matDir, "Elevator_Door", elev.doorColor);
            Material pitMat = CreateColorMaterialAsset(matDir, "Elevator_Pit", elev.pitColor);
            Material holeMat = CreateHoleMaskMaterialAsset(matDir, "Elevator_HoleMask");

            // 外框（4 横梁，容器名 Frame，子节点名与 ConfigureFrame 的 Find 一致）
            var frame = new GameObject("Frame").transform;
            frame.SetParent(root.transform, false);
            elev.frameRoot = frame;
            CreateBox(frame, "FrameFront", frameMat);
            CreateBox(frame, "FrameBack", frameMat);
            CreateBox(frame, "FrameLeft", frameMat);
            CreateBox(frame, "FrameRight", frameMat);

            // 门板（左右两片，直接挂在根节点下）
            elev.doorLeft = CreateQuad(root.transform, "DoorLeft", doorMat);
            elev.doorRight = CreateQuad(root.transform, "DoorRight", doorMat);

            // 挖洞蒙版
            elev.holeMask = CreateHoleMask(root.transform, "HoleMask", holeMat);

            // 竖井（5 面，容器名 Pit，子节点名与 ConfigurePit 的 Find 一致）
            var pit = new GameObject("Pit");
            pit.transform.SetParent(root.transform, false);
            elev.pitRoot = pit;
            CreateBox(pit.transform, "PitFloor", pitMat);
            CreateBox(pit.transform, "PitFront", pitMat);
            CreateBox(pit.transform, "PitBack", pitMat);
            CreateBox(pit.transform, "PitLeft", pitMat);
            CreateBox(pit.transform, "PitRight", pitMat);

            // 水平（x/z）按 3×3 区域初始化；垂直（y）用下方默认布局，运行时不再重算
            elev.ConfigureVisualOnly(null);
            ApplyDefaultVerticalLayout(elev);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            if (prefab == null)
            {
                Debug.LogError("[ElevatorItemPrefabCreator] 预制体保存失败：" + path);
                return;
            }

            Selection.activeObject = prefab;
            Debug.Log("[ElevatorItemPrefabCreator] 已生成升降台预制体：" + path +
                "。请把它拖到 PixelGroup 的 elevatorPrefab 字段（或通过菜单「创建（场景视图 · 选中 Pixel）▸ 升降台」时自动使用）。", prefab);
        }

        /// <summary>按 unitSize=1 的默认值摆好各视觉子节点的 y 位置与 y 尺寸；运行时 Configure 不再重算 y。</summary>
        private static void ApplyDefaultVerticalLayout(ElevatorItem elev)
        {
            float ground = -0.5f;   // unitSize=1 的地面高度（像素底部）
            float depth = 1.5f;     // unitSize=1 的竖井深度
            float frameH = 0.12f;   // 横梁 y 厚度
            float pitT = 0.06f;     // 竖井壁厚

            Transform frame = elev.frameRoot;
            SetVertical(frame != null ? frame.Find("FrameFront") : null, ground + frameH * 0.5f, frameH);
            SetVertical(frame != null ? frame.Find("FrameBack") : null, ground + frameH * 0.5f, frameH);
            SetVertical(frame != null ? frame.Find("FrameLeft") : null, ground + frameH * 0.5f, frameH);
            SetVertical(frame != null ? frame.Find("FrameRight") : null, ground + frameH * 0.5f, frameH);

            SetVertical(elev.doorLeft, ground - 0.01f, 1f);
            SetVertical(elev.doorRight, ground - 0.01f, 1f);

            if (elev.holeMask != null)
                SetVertical(elev.holeMask.transform, ground + 0.02f, 1f);

            Transform pit = elev.pitRoot != null ? elev.pitRoot.transform : null;
            SetVertical(pit != null ? pit.Find("PitFloor") : null, ground - depth, pitT);
            SetVertical(pit != null ? pit.Find("PitFront") : null, ground - depth * 0.5f, depth);
            SetVertical(pit != null ? pit.Find("PitBack") : null, ground - depth * 0.5f, depth);
            SetVertical(pit != null ? pit.Find("PitLeft") : null, ground - depth * 0.5f, depth);
            SetVertical(pit != null ? pit.Find("PitRight") : null, ground - depth * 0.5f, depth);
        }

        private static void SetVertical(Transform t, float y, float scaleY)
        {
            if (t == null) return;
            Vector3 p = t.localPosition;
            p.y = y;
            t.localPosition = p;
            Vector3 s = t.localScale;
            s.y = scaleY;
            t.localScale = s;
        }

        private static GameObject CreateBox(Transform parent, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static Transform CreateQuad(Transform parent, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        private static Renderer CreateHoleMask(Transform parent, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            return renderer;
        }

        /// <summary>创建（或覆盖）一个颜色材质 .mat 资产；Standard shader 不可用时退回内置 Default-Diffuse。</summary>
        private static Material CreateColorMaterialAsset(string dir, string name, Color color)
        {
            string p = dir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(p) != null)
                AssetDatabase.DeleteAsset(p);

            Material mat;
            var shader = Shader.Find("Standard");
            if (shader != null)
                mat = new Material(shader);
            else
                mat = new Material(AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, p);
            return mat;
        }

        /// <summary>创建（或覆盖）HoleMask 挖洞蒙版材质 .mat 资产；找不到 shader 时返回 null。</summary>
        private static Material CreateHoleMaskMaterialAsset(string dir, string name)
        {
            string p = dir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(p) != null)
                AssetDatabase.DeleteAsset(p);

            var shader = Shader.Find("CrowdMatch/HoleMask");
            if (shader == null)
            {
                Debug.LogWarning("[ElevatorItemPrefabCreator] 未找到 CrowdMatch/HoleMask shader，HoleMask 材质未创建（运行时 BuildVisual 会重试）。");
                return null;
            }
            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, p);
            return mat;
        }

        /// <summary>从场景里的 Block_BG 拷贝外观（纹理/颜色/裁剪阈值），生成一个配好的 GroundHole 材质球资产。</summary>
        [MenuItem("CrowdMatch/更多工具/生成挖洞地面材质球（从 Block_BG 拷贝外观）", false, MenuPriority.More + MenuPriority.Seg2 + 1)]
        public static void CreateGroundHoleMaterialFromBG()
        {
            Renderer bg = null;
            foreach (var r in Object.FindObjectsOfType<Renderer>())
            {
                if (r != null && r.name == "Block_BG") { bg = r; break; }
            }
            if (bg == null)
            {
                EditorUtility.DisplayDialog("生成挖洞地面材质球", "场景里找不到名为 Block_BG 的 Renderer。", "确定");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "保存挖洞地面材质球", "GroundHole_BG", "mat", "选择保存位置", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            var shader = Shader.Find("CrowdMatch/GroundHole");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("生成挖洞地面材质球", "未找到 CrowdMatch/GroundHole shader。", "确定");
                return;
            }

            var src = bg.sharedMaterial;
            var mat = new Material(shader);
            mat.name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (src != null)
            {
                if (src.HasProperty("_MainTex"))
                {
                    mat.mainTexture = src.GetTexture("_MainTex");
                    mat.mainTextureScale = src.mainTextureScale;
                    mat.mainTextureOffset = src.mainTextureOffset;
                }
                if (src.HasProperty("_Color"))
                    mat.color = src.GetColor("_Color");
                if (src.HasProperty("_Cutoff"))
                    mat.SetFloat("_Cutoff", src.GetFloat("_Cutoff"));
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(path);
            EditorUtility.DisplayDialog("生成挖洞地面材质球", "已生成 " + path + "。请把它拖到 ElevatorItem 的 groundHoleMaterial 字段（或 prefab 上）。", "确定");
        }
    }
}
