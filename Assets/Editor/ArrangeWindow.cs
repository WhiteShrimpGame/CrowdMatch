using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ArrangeWindow : EditorWindow
{
    // 拆分双间距，独立调节
    private float spacingX = 1.2f;
    private float spacingZ = 1.2f;
    private int columnCount = 10;

    [MenuItem("工具/排列工具/打开排列面板")]
    private static void OpenWindow()
    {
        ArrangeWindow window = GetWindow<ArrangeWindow>("模型排列面板");
        window.minSize = new Vector2(300, 260);
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        spacingX = EditorGUILayout.FloatField("X轴横向间距：", spacingX);
        spacingZ = EditorGUILayout.FloatField("Z轴纵向间距：", spacingZ);
        columnCount = EditorGUILayout.IntField("矩阵每行数量：", columnCount);

        GUILayout.Space(12);
        if (GUILayout.Button("横向X轴 排成一排", GUILayout.Height(30))) ArrangeX();
        GUILayout.Space(3);
        if (GUILayout.Button("纵向Z轴 排成一排", GUILayout.Height(30))) ArrangeZ();
        GUILayout.Space(3);
        if (GUILayout.Button("二维网格矩阵排列", GUILayout.Height(30))) ArrangeGrid();

        GUILayout.Space(6);
        // B方案按钮：打乱交换已有点位（不新增坐标，不会重叠）
        if (GUILayout.Button("随机打乱(交换点位不重叠)", GUILayout.Height(30))) RandomShuffleSwapPoint();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("使用：多选模型→设置X/Z间距→点击按钮排列，Ctrl+Z可撤销\n随机打乱：复用现有所有点位，物体互相交换位置，不会重叠", MessageType.Info);
    }

    // X单行排列，使用X间距
    private void ArrangeX()
    {
        GameObject[] objs = Selection.gameObjects;
        if (objs.Length < 2)
        {
            EditorUtility.DisplayDialog("提醒", "至少选中2个物体", "确定");
            return;
        }
        Vector3 startPos = objs[0].transform.position;
        for (int i = 0; i < objs.Length; i++)
        {
            Undo.RecordObject(objs[i].transform, "X轴排列");
            objs[i].transform.position = startPos + new Vector3(i * spacingX, 0, 0);
        }
    }

    // Z单行排列，使用Z间距
    private void ArrangeZ()
    {
        GameObject[] objs = Selection.gameObjects;
        if (objs.Length < 2)
        {
            EditorUtility.DisplayDialog("提醒", "至少选中2个物体", "确定");
            return;
        }
        Vector3 startPos = objs[0].transform.position;
        for (int i = 0; i < objs.Length; i++)
        {
            Undo.RecordObject(objs[i].transform, "Z轴排列");
            objs[i].transform.position = startPos + new Vector3(0, 0, i * spacingZ);
        }
    }

    // 矩阵：X方向用X间距，Z方向用Z间距
    private void ArrangeGrid()
    {
        GameObject[] objs = Selection.gameObjects;
        if (objs.Length < 2 || columnCount < 1)
        {
            EditorUtility.DisplayDialog("提醒", "选中物体≥2，列数≥1", "确定");
            return;
        }
        Vector3 startPos = objs[0].transform.position;
        for (int i = 0; i < objs.Length; i++)
        {
            Undo.RecordObject(objs[i].transform, "矩阵排列");
            int xIndex = i % columnCount;
            int zIndex = i / columnCount;
            objs[i].transform.position = startPos + new Vector3(xIndex * spacingX, 0, zIndex * spacingZ);
        }
    }

    /// <summary>
    /// B方案：收集现有全部点位，洗牌后分配给物体，交换位置，点位不变，无重叠
    /// </summary>
    private void RandomShuffleSwapPoint()
    {
        GameObject[] objs = Selection.gameObjects;
        if (objs.Length < 2)
        {
            EditorUtility.DisplayDialog("提醒", "至少选中2个物体", "确定");
            return;
        }

        //1.记录所有物体当前的坐标点
        List<Vector3> pointList = new List<Vector3>();
        foreach (var go in objs)
        {
            pointList.Add(go.transform.position);
            Undo.RecordObject(go.transform, "随机交换点位");
        }

        //2.洗牌算法 Fisher‑Yates 打乱点位列表
        for (int i = pointList.Count - 1; i > 0; i--)
        {
            int randIdx = Random.Range(0, i + 1);
            Vector3 temp = pointList[i];
            pointList[i] = pointList[randIdx];
            pointList[randIdx] = temp;
        }

        //3.把打乱后的点位重新赋值给物体
        for (int i = 0; i < objs.Length; i++)
        {
            objs[i].transform.position = pointList[i];
        }
    }
}
