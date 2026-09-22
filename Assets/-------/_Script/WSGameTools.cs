using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WSGameTools
{
    /// <summary>
    /// 将世界坐标转换为Ugui坐标
    /// </summary>
    /// <param name="worldPos">世界坐标</param>
    /// <returns></returns>
    public static Vector2 WorldPosToUgui(Vector3 worldPos, Camera camera)
    {
        Vector2 screenPoint = camera.WorldToScreenPoint(worldPos); //世界坐标转换为屏幕坐标
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        screenPoint -= screenSize / 2; //将屏幕坐标变换为以屏幕中心为原点
        Vector2 anchorPos = screenPoint / screenSize *
                            UIManager.Instance.GetComponent<RectTransform>().sizeDelta; //缩放得到UGUI坐标
        return anchorPos;
    }

    /// <summary>
    /// 根据名称查询第一个符合子物体组件（不包含返回 null）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="transform"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T LFirstOrDefault<T>(this Transform transform, string name, bool includeUnActive = false)
        where T : Component
    {
        return transform.GetComponentsInChildren<T>(includeUnActive).FirstOrDefault(s => s.name == name);
    }

    /// <summary>
    /// 转换位置坐标（用于在同一画不下，不同锚点预设的UI）
    /// </summary>
    /// <param name="selfTrans"></param>
    /// <param name="targetTrans"></param>
    /// <param name="canvas"></param>
    /// <returns></returns>
    public static Vector2 ConvertUGUIPosWithDiffAnchorPresets(Transform selfTrans, Transform targetTrans,
        UnityEngine.Canvas canvas = null)
    {
        if (canvas == null)
            canvas = UIManager.Instance.GetComponent<Canvas>();
        var uiElementTarget = targetTrans.GetComponent<RectTransform>();
        var uiElementSelf = selfTrans.GetComponent<RectTransform>();

        // 获取自身的屏幕坐标
        Vector2 selfScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, uiElementTarget.position);

        // 将屏幕坐标转换为UI元素B的局部坐标
        Vector2 targetPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElementSelf.parent as RectTransform, selfScreenPos,
            canvas.worldCamera, out targetPos);
        return targetPos;
    }

    /// <summary>
    /// 将双精度浮点数转为带有K、M、G的简化字符串，并保留两位小数
    /// </summary>
    /// <param name="data">双精度浮点数</param>
    /// <param name="decimalPlaces"></param>
    /// <returns>简化字符串</returns>
    public static string ConvertToKMGString(this float data, int decimalPlaces = 2)
    {
        string unit = "";
        var numK = 1000;
        if (Math.Abs(data) < numK)
        {
            unit = "";
            return Math.Round(data, decimalPlaces).ToString() + unit;
        }

        if (Math.Abs(data) < (Math.Pow(numK, 2)))
        {
            unit = "K";
            return Math.Round((data / numK), decimalPlaces).ToString() + unit; //kb
        }

        if (Math.Abs(data) < Math.Pow(numK, 3))
        {
            unit = "M";
            return Math.Round((data / Math.Pow(numK, 2)), decimalPlaces).ToString() + unit; //M
        }

        if (Math.Abs(data) < Math.Pow(numK, 4))
        {
            unit = "G";
            return Math.Round((data / Math.Pow(numK, 3)), decimalPlaces).ToString() + unit; //G
        }

        unit = "T";
        return Math.Round((data / Math.Pow(numK, 4)), decimalPlaces).ToString() + unit; //T
    }

    public static GameObject Show(this GameObject selfObj)
    {
        selfObj.SetActive(true);
        return selfObj;
    }

    public static T Show<T>(this T selfComponent) where T : Component
    {
        selfComponent.gameObject.Show();
        return selfComponent;
    }

    public static GameObject Hide(this GameObject selfObj)
    {
        selfObj.SetActive(false);
        return selfObj;
    }

    public static T Hide<T>(this T selfComponent) where T : Component
    {
        selfComponent.gameObject.Hide();
        return selfComponent;
    }

    /// <summary>
    /// 给指定Transform设置父类并将本地位置，本地旋转，本地缩放设置为0
    /// </summary>
    /// <param name="self">Transform自身</param>
    /// <param name="parent">目标父类</param>
    public static void LocalTranReset(this Transform self, Transform parent)
    {
        self.transform.SetParent(parent);
        self.transform.localPosition = Vector3.zero;
        self.transform.localRotation = Quaternion.identity;
        self.transform.localScale = Vector3.one;
    }

    public static string FormatTime(this double timeInSeconds)
    {
        int hours = Mathf.FloorToInt((float)(timeInSeconds / 3600));
        int minutes = Mathf.FloorToInt((float)((timeInSeconds - hours * 3600) / 60));
        int seconds = Mathf.FloorToInt((float)(timeInSeconds - hours * 3600 - minutes * 60));

        if (hours > 0)
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        else
            return string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }

    public static string FormatTime_Day(this double timeInSeconds)
    {
        int days = Mathf.FloorToInt((float)timeInSeconds / 86400);
        int hours = Mathf.FloorToInt((float)(timeInSeconds % 86400) / 3600);
        int minutes = Mathf.FloorToInt((float)((timeInSeconds % 3600) / 60));

        if (days > 0)
        {
            //TODO: 多语言支持
            // if (GameData.Lang != LangType.zh_CN)
            //     return $"{days}D{hours}H";
            // else
            return $"{days}天{hours}小时";
        }
        else
            return FormatTime(timeInSeconds);
    }

    public static Vector2 GetUITopLeftCorner(RectTransform uiElement)
    {
        Canvas canvas = UIManager.Instance.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("UI element is not under a canvas.");
            return Vector2.zero;
        }

        Vector3[] corners = new Vector3[4];
        uiElement.GetWorldCorners(corners);
// 将 UI 元素的四个角的世界坐标转换为屏幕坐标
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[i]);
        }

// 获取左上角横坐标和纵坐标
        float leftX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float topY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return new Vector2(leftX, topY);
    }

    public static Vector3 UIToWorldPosition(Canvas canvas, RectTransform uiElement, float cameraZ)
    {
        Camera cam = canvas.worldCamera;

        // 1. 获取UI元素的屏幕坐标
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, uiElement.position);

        Vector3 screenPos = new Vector3(screenPoint.x, screenPoint.y, cameraZ);

        // 3. 转换为世界坐标
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        return worldPos;
    }
    // 十六进制颜色转Color工具函数
    public static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        float r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float a = hex.Length >= 8 ? byte.Parse(hex.Substring(6,2), System.Globalization.NumberStyles.HexNumber) / 255f : 1f;
        return new Color(r,g,b,a);
    }
}