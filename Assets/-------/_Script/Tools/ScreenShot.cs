/****************************************************
   功能：场景运行时截图
*****************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class ScreenShot : MonoBehaviour
{
    [SerializeField, Header("是否为单张图，false 则为多图")] private bool usingSingleScreenShot;
    [Space(10)]
    private bool isShooting = false;

#if UNITY_EDITOR
    public ScreenSizeAndBindEvent[] screenSizeAndBindEvents = new ScreenSizeAndBindEvent[4]
    {
            // new ScreenSizeAndBindEvent("-iPad",GameViewTools.GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,2048,2732), 
            // new ScreenSizeAndBindEvent("-iPhoneXs Max",GameViewTools.GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1242,2688),
            // new ScreenSizeAndBindEvent("-iPhone 8Plus",GameViewTools.GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1242,2208),
            // new ScreenSizeAndBindEvent("-Andriod",GameViewTools.GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1080,1920),
            new ScreenSizeAndBindEvent("-iPad",GameViewTools.GameViewSizeType.FixedResolution,2048,2732),
            new ScreenSizeAndBindEvent("-iPhoneXs Max",GameViewTools.GameViewSizeType.FixedResolution,1242,2688),
            new ScreenSizeAndBindEvent("-iPhone 8Plus",GameViewTools.GameViewSizeType.FixedResolution,1242,2208),
            new ScreenSizeAndBindEvent("-Andriod",GameViewTools.GameViewSizeType.FixedResolution,1080,1920),
    };
    public int count;
    private int curIndex;

#endif



    private void OnEnable()
    {

#if UNITY_EDITOR
        GameViewTools.Init();//初始化
        foreach (var screenInfo in screenSizeAndBindEvents)
        {
            //GameViewTools.AddCustomSize(screenInfo.gameViewSizeType,screenInfo.gameViewSizeGroupType,screenInfo.width,screenInfo.height,screenInfo.name);
            GameViewTools.AddCurCustomSize(screenInfo.gameViewSizeType, screenInfo.width, screenInfo.height, screenInfo.name);
        }
        count = screenSizeAndBindEvents.Length;
        isShooting = false;
#endif
    }

    public void ShowTxt(string txt)
    {
        Debug.Log("t---->" + txt);
    }


    private void OnDisable()
    {
#if UNITY_EDITOR
        GameViewTools.Remove();//移除
#endif
    }

#if UNITY_EDITOR
    private void UpdateSelectView()
    {
        if (!isShooting)
        {
            isShooting = true;
            StartCoroutine(ShootView(count, () => { isShooting = false; }));
        }
    }

    IEnumerator ShootView(int count, System.Action callback)
    {
        // var  wait = new Wa;
        Time.timeScale = 0;
        var wait2 = new WaitForEndOfFrame();
        int num = count;
        while (num > 0)
        {
            GameViewTools.Next();//更换窗口
            screenSizeAndBindEvents[num - 1].selectDo?.Invoke();
            yield return wait2;
            yield return wait2;
            Shoot();
            num--;
        }
        Time.timeScale = 1;
        callback?.Invoke();
    }

    //修改协程添加方案
#endif

    /// <summary>
    /// 截图代码
    /// </summary>
    private void Shoot()
    {
        string directoryName = Screen.width + "x" + Screen.height;
        string path = Application.dataPath.Replace("/Assets", "/" + Application.productName + "_Screenshot/" + directoryName);
        int fileCount;
        if (!System.IO.File.Exists(path))
            fileCount = System.IO.Directory.CreateDirectory(path).GetFiles().Length;
        else
            fileCount = new System.IO.DirectoryInfo(path).GetFiles().Length;
        string imageName = "Level_" + GameData.CurrentLevel + "_" + directoryName + ".png";
        ScreenCapture.CaptureScreenshot(path + "/" + imageName);
        Debug.Log("***Screenshot" + imageName + "***");
    }

    void Update()
    {
        if (isShooting) return;

        if (Input.GetMouseButtonDown(2))
        {
            if (usingSingleScreenShot)
            {
                Shoot();
            }
            else
            {
#if UNITY_EDITOR
                UpdateSelectView();
#endif
            }

        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 0;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            Time.timeScale = 1;
        }
    }
}
#if UNITY_EDITOR
[Serializable]
public class ScreenSizeAndBindEvent
{
    public string name;
    public GameViewTools.GameViewSizeType gameViewSizeType;
    // public GameViewSizeGroupType gameViewSizeGroupType;
    public int height;
    public int width;
    public UnityEvent selectDo;

    // public ScreenSizeAndBindEvent(string name, GameViewTools.GameViewSizeType gameViewSizeType,
    //     GameViewSizeGroupType gameViewSizeGroupType, int width, int height)
    // {
    //     this.name = name;
    //     this.gameViewSizeType = gameViewSizeType;
    //     this.gameViewSizeGroupType = gameViewSizeGroupType;
    //     this.height = height;
    //     this.width = width;
    // }
    public ScreenSizeAndBindEvent(string name, GameViewTools.GameViewSizeType gameViewSizeType, int width, int height)
    {
        this.name = name;
        this.gameViewSizeType = gameViewSizeType;
        this.height = height;
        this.width = width;
    }

    public void BindEvent(UnityAction action)
    {
        selectDo.AddListener(action);
    }

    public void UnBindEvent(UnityAction action)
    {
        selectDo.RemoveListener(action);
    }
}
#endif
