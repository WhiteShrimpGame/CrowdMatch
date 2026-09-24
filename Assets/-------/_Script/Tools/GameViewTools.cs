using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class GameViewTools
{
    private static object gameViewSizesInstance; // GameViewSizes的引用

    private static MethodInfo getGroup; // 方法引用 public GameViewSizeGroup GetGroup(GameViewSizeGroupType gameViewSizeGroupType)

    private static MethodInfo addCustomSize; //添加方法
    private static MethodInfo removeCustomSize; //添加方法
    private static MethodInfo getCustomCount; //引用获取当前自定义数量
    private static MethodInfo getTotakCount; //获取所以的数量
    private static MethodInfo getBuiltinCount; //获取基础数量

    public static GameViewSizeGroupType gameViewSizeGroupType;

    static bool isInit = false;


    //将数据添加到自定义的位置
    private static int screenIndex = 0;
    private static int minIndex;//
    private static int maxIndex;
    private static bool isChange = false;

    static void InitiaLized()
    {
        var gvSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(gvSize);
        var instacnceProp = singleType.GetProperty("instance");
        getGroup = gvSize.GetMethod("GetGroup");

        gameViewSizesInstance = instacnceProp.GetValue(null, null); //获取单例

        /* -GameViewSizes- 方法*/
        getCustomCount = getGroup.ReturnType.GetMethod("GetCustomCount");
        getTotakCount = getGroup.ReturnType.GetMethod("GetTotalCount");
        getBuiltinCount = getGroup.ReturnType.GetMethod("GetBuiltinCount");
        addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
        removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
    }


    public enum GameViewSizeType
    {
        AspectRatio,
        FixedResolution
    }

#if UNITY_EDITOR
    [MenuItem("Tools2/GameViewSize/Init")]
    public static void Init()
    {
        if (isChange)
        {
            Remove();
            isChange = false;
        }

        gameViewSizeGroupType = GetCurrGameViewSizeGroupType();
        InitiaLized();

        //var gameViewSizes = getGroup.Invoke(gameViewSizesInstance, new object[] {(int)GameViewSizeGroupType.Android});
        var gameViewSizes = getGroup.Invoke(gameViewSizesInstance, new object[] { (int)GetCurrGameViewSizeGroupType() });
        var totakCount = getTotakCount.Invoke(gameViewSizes, null);

        screenIndex = maxIndex = minIndex = (int)totakCount;
        isInit = true;
    }
#endif

#if UNITY_EDITOR
    [MenuItem("Tools2/GameViewSize/AddAll")]
    public static void Add()
    {
        //将添加的数据
        //AddCustomSize(GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,2048,2732,"-iPad");
        AddCurCustomSize(GameViewSizeType.FixedResolution, 2048, 2732, "-iPad");

        // AddCustomSize(GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1242,2688,"-iPhoneXs Max");
        AddCurCustomSize(GameViewSizeType.FixedResolution, 1242, 2688, "-iPhoneXs Max");

        //AddCustomSize(GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1242,2208,"-iPhone 8Plus");
        AddCurCustomSize(GameViewSizeType.FixedResolution, 1242, 2208, "-iPhone 8Plus");

        // AddCustomSize(GameViewSizeType.FixedResolution,GameViewSizeGroupType.Android,1080,1920,"-Androdi");
        AddCurCustomSize(GameViewSizeType.FixedResolution, 1080, 1920, "-Androdi");
        //四个尺寸
        isChange = true;
        Debug.Log("-->>>" + maxIndex);
    }
#endif


#if UNITY_EDITOR
    [MenuItem("Tools2/GameViewSize/RemoveAll ")]
    public static void Remove()
    {
        while (maxIndex - minIndex > 0)
        {
            if (screenIndex == maxIndex)
            {
                screenIndex = maxIndex - 1;
                SelcetGameView(screenIndex);//
            }
            //RemoveCustomSize(GameViewSizeGroupType.Android, maxIndex);
            RemoveCurCustomSize(maxIndex);
        }
        isChange = false;
    }
#endif


#if UNITY_EDITOR
    [MenuItem("Tools2/GameViewSize/NextLoop  %&N")]
    public static void Next()
    {
        screenIndex++;
        if (screenIndex > maxIndex)
        {
            screenIndex = minIndex;
        }
        SelcetGameView(screenIndex);
    }
#endif

    private static int index = 0;

#if UNITY_EDITOR

    //获取当前平台，这里需要进行
    public static GameViewSizeGroupType GetCurrGameViewSizeGroupType()
    {
        if (!isInit)
        {
            InitiaLized();
        }
        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var gvWnd = EditorWindow.GetWindow(gvType); //获取窗口
        var gvSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");

        var getCrrentSizeGroupType =
            gvSize.GetField("s_GameViewSizeGroupType", BindingFlags.Static | BindingFlags.NonPublic);

        //Debug.Log(getCrrentSizeGroupType.GetValue(null));

        return (GameViewSizeGroupType)getCrrentSizeGroupType.GetValue(null);
    }

#endif


    public static void AddCurCustomSize(GameViewSizeType viewSizeType,
        int width, int height, string text)
    {
        if (!isInit) Init();
        AddCustomSize(viewSizeType, gameViewSizeGroupType, width, height, text);//添加
    }

    public static void RemoveCurCustomSize(int index)
    {
        if (!isInit) Init();
        RemoveCustomSize(gameViewSizeGroupType, index);
    }


#if UNITY_EDITOR

    //1.增加窗口
    static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType gameViewSizeGroupType,
        int width, int height, string text)
    {
        //获取类GameViewSize
        var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");

        //找到 构造方法 public GameViewSize(GameViewSizeType type, int width, int height, string baseText)
        var ctor = gvsType.GetConstructor(new Type[]
        {
                typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType"),
                typeof(int),
                typeof(int),
                typeof(string)
        });

        //找到enum 类型 GameViewSizeType
        var newGvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");

        int enumGvsType = 0;

        // //获取enum中的类型
        if (viewSizeType == GameViewSizeType.AspectRatio)
        {
            var aspectRatio = newGvsType.GetField("AspectRatio", BindingFlags.Static | BindingFlags.Public);
            //newGvsType =aspectRatio.GetType() ; //typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType.AspectRatio");
            enumGvsType = (int)aspectRatio.GetValue(null);
        }
        else
        {
            var fixedResolution = newGvsType.GetField("FixedResolution", BindingFlags.Static | BindingFlags.Public);
            // newGvsType = fixedResolution.GetType(); //typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType.FixedResolution");
            enumGvsType = (int)fixedResolution.GetValue(null);
        }

        var newSize = ctor.Invoke(new object[] { enumGvsType, width, height, text });


        // //执行 添加到 group 中 GameViewSizeType m_SizeType;
        var sizetype = gvsType.GetField("m_SizeType", BindingFlags.NonPublic | BindingFlags.Instance);

        //获取 GameViewSizeGroup //指定了平台 gameViewSizeGroupType
        var gameViewSizes = getGroup.Invoke(gameViewSizesInstance, new object[] { (int)gameViewSizeGroupType });

        addCustomSize.Invoke(gameViewSizes, new object[] { newSize });

        //获取
        var totakCount = getTotakCount.Invoke(gameViewSizes, null);//没有参数

        maxIndex = (int)totakCount - 1;
    }



    //2.删除增加的窗口
    static void RemoveCustomSize(GameViewSizeGroupType gameViewSizeType, int index = -1)
    {
        //bool isLastIndex = index < 0 ? true : false;

        //获取 GameViewSizes ->  GetGroup(GameViewSizeGroupType gameViewSizeGroupType)
        //-> GameViewSizeGroup ->
        //        -> GetTotalCount() -> int 所有的数量
        //        -> GetCustomCount() ->int  用户自定的数量
        var gameViewSizes = getGroup.Invoke(gameViewSizesInstance, new object[] { gameViewSizeType });

        var totakCount = getTotakCount.Invoke(gameViewSizes, null);//没有参数

        var customCount = getCustomCount.Invoke(gameViewSizes, null); //无参

        //Debug.Log(customCount + "-->" + totakCount);
        //var builinCount = getBuiltinCount.Invoke(gameViewSizes, null); //无参

        // int removeIndex = isLastIndex ? (int) totakCount : index + (int)builinCount;
        //int removeIndex = isLastIndex ? (int) totakCount : index;
        //移除
        removeCustomSize.Invoke(gameViewSizes, new object[] { (int)totakCount - 1 });
        maxIndex = (int)totakCount - 1;

    }
#endif

    //3.设置指定的窗口
    public static void SelcetGameView(int index)
    {
        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var gvWnd = EditorWindow.GetWindow(gvType); //获取窗口

        var sizeSelcetionCallBack = gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        sizeSelcetionCallBack?.Invoke(gvWnd, new object[] { index, null });
    }
}
#endif
