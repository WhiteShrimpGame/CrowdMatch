using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 音频裁剪：显示选中 AudioClip 的波形，拖拽选区，试听，把选区内的采样**另存**成一个新 WAV。
    ///
    /// 【只读源文件】本工具永远不覆盖源文件，只 `SaveFilePanel` 另存新 WAV —— 所以没有任何破坏性操作，
    /// 不需要两步确认。
    ///
    /// 【导出格式】16-bit PCM，**保持原 channels / frequency**，不重采样、不混音（重采样有它自己的音质问题，
    /// 且是另一个需求）。采样从选区取，跨声道交错照抄原样。
    ///
    /// 【最容易被忽略的坑】源音频若是压缩格式（Vorbis / MP3 / ADPCM），导出成 WAV 会**显著变大** ——
    /// WAV 是未压缩的。窗口里会常驻一条警告，并把「源文件多大 / 导出大约多大」算给你看。
    ///
    /// 【试听要反射 Unity 内部 API】编辑器里在非 Play 模式下试听，只有 `UnityEditor.AudioUtil` 能做，
    /// 而它是内部类。本文件是 `Assets/Scripts/Editor/` 下**第一处**反射 Unity 内部 API 的地方，
    /// 所以自己定了比现有先例更严的约定（见 <see cref="ResolveAudioUtil"/>）：解析一次并缓存、
    /// 拿不到就把按钮置灰 + 一条常驻说明 + 一条 warning，**绝不每次点击都抛异常炸 Console**。
    ///
    /// 【为什么试听要构造临时 AudioClip】`PlayPreviewClip` 的重载形态无法离线确定（IL 里只有方法名、没有参数表）。
    /// 播一个「只含选区」的临时 clip 从 0 开始，对**任何一种**重载都成立；若改成播原 clip 并传入起始帧，
    /// 就得赌某个特定重载存在。播放头位置因此要换算：原始帧号 = 选区起点 + 临时 clip 内的位置。
    /// </summary>
    public class AudioTrimWindow : EditorWindow
    {
        private const string Tag = "[AudioTrim]";

        /// <summary>「上次导出目录」EditorPrefs 键。</summary>
        private const string ExportPathKey = "CrowdMatch.AudioTrimWindow.LastExportPath";

        /// <summary>循环试听时，首遍从选区末尾往前这么多秒起播（不足则从选区开头起播）。</summary>
        private const float LoopLeadSeconds = 3f;

        /// <summary>波形区高度（像素）。</summary>
        private const float WaveHeight = 140f;

        /// <summary>试听期间的刷新节流：最多 30 fps，避免每个 editor update 都 Repaint。</summary>
        private const double PreviewRepaintInterval = 1.0 / 30.0;

        // ===== 源 =====
        private AudioClip _clip;
        private string _sourcePath;          // AssetDatabase 路径（可能为空 = 非工程资产）
        private long _sourceBytes;
        private float[] _samples;            // 交错 PCM：长度 = 帧数 × 声道数
        private int _frames;                 // = clip.samples（**每声道**帧数）
        private int _channels;
        private int _frequency;
        private string _loadError;           // 非空 = 读不出来，界面上显式报错（不画假波形）

        // ===== 波形缓存（失效条件：换 clip 或 宽度变了）=====
        private float[] _minPeaks, _maxPeaks;
        private int _peaksWidth = -1;
        private AudioClip _peaksClip;

        // ===== 选区（帧号，不是浮点数组下标）=====
        private int _selStart, _selEnd;
        private bool _dragging;
        private int _dragAnchor;

        // ===== 试听 =====
        private bool _previewing;
        private bool _paused;
        private bool _loopMode;              // 循环试听：播到选区末尾就回到选区起点重播
        private double _previewDeadline;     // 兜底自动停
        private double _previewStartedAt;
        private double _loopGraceUntil;      // 循环重播后的防抖宽限
        private double _nextRepaintAt;
        private bool _previewStartChecked;   // 起播后只做一次「真的响了吗」的检查
        private System.Type _audioUtil;
        private MethodInfo _miPlay, _miStop, _miPause, _miResume, _miIsPlaying, _miHasPreview;
        private MethodInfo _miSamplePosition, _miSecondsPosition;   // 前者优先：int 采样位置；后者是 float 秒
        private string _previewUnavailable;  // 非空 = 反射不可用，按钮置灰

        [MenuItem("CrowdMatch/更多工具/音频裁剪", false, MenuPriority.More + MenuPriority.Seg4)]
        private static void OpenWindow()
        {
            OpenFor(null);
        }

        /// <summary>打开并绑定指定 clip；传 null = 按当前选中自动绑定。</summary>
        public static void OpenFor(AudioClip clip)
        {
            var window = GetWindow<AudioTrimWindow>("音频裁剪");
            window.minSize = new Vector2(640f, 420f);
            if (clip != null)
                window.SetClip(clip);
            else
                window.BindFromSelection();
            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += BindFromSelection;
            ResolveAudioUtil();
            BindFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= BindFromSelection;
            StopPreview();   // 关窗别把试听留着一直响（循环试听尤其明显）
        }

        /// <summary>
        /// 再兜一次。原生侧的预览音频**不归托管对象管**，窗口没了它照样响，而且响起来之后
        /// 编辑器里再没有别的地方能停它 —— 所以关窗路径上多一道保险是值得的。
        /// 这里只停音频、不 <c>Repaint</c>（窗口正在销毁）。
        /// </summary>
        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreviewAudio();
        }

        // ===================== 绑定与读取 =====================

        /// <summary>跟随 Project / Hierarchy 选中；选中的不是 AudioClip 时**不动现有绑定**（不打扰）。</summary>
        private void BindFromSelection()
        {
            if (Selection.activeObject is AudioClip clip && clip != _clip)
                SetClip(clip);
        }

        private void SetClip(AudioClip clip)
        {
            StopPreview();

            _clip = clip;
            _samples = null;
            _loadError = null;
            _sourcePath = null;
            _sourceBytes = 0;
            _frames = 0;
            _channels = 0;
            _frequency = 0;
            _minPeaks = _maxPeaks = null;
            _peaksClip = null;
            _peaksWidth = -1;
            _selStart = _selEnd = 0;

            if (clip != null)
            {
                _sourcePath = AssetDatabase.GetAssetPath(clip);
                _sourceBytes = SourceFileBytes(_sourcePath);
                _frames = clip.samples;
                _channels = Mathf.Max(1, clip.channels);
                _frequency = Mathf.Max(1, clip.frequency);
                _samples = TryReadSamples(clip, out _loadError);
                _selStart = 0;
                _selEnd = _frames;
            }

            Repaint();
        }

        /// <summary>重新读一遍当前 clip（源文件被外部改过 / 改了 Load Type 之后用）。</summary>
        private void Refresh(bool showDialog)
        {
            if (_clip == null)
                return;
            SetClip(_clip);
            if (showDialog && _loadError != null)
                EditorUtility.DisplayDialog("音频裁剪", _loadError, "确定");
        }

        /// <summary>
        /// 读全部采样。先把「读不出来」变成一个**确定**的问题：Unloaded 时调公开 API
        /// <c>LoadAudioData()</c>，之后再查一次 <c>loadState</c>。仍然不是 Loaded 就如实报错。
        ///
        /// 不赌「Streaming 的 clip 一定读不出来」这条传闻 —— 本机 Unity 的 XML 文档对
        /// <c>GetData</c> 只有一句 "Fills an array with sample data from the clip."，没有任何 loadType 说明，
        /// 所以按实际 loadState 判断，而不是按 loadType 猜。
        /// </summary>
        private static float[] TryReadSamples(AudioClip clip, out string error)
        {
            error = null;

            if (clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                error = "读不出音频数据（loadState = " + clip.loadState + "）。\n\n" +
                        "请在 Inspector 里把 Load Type 改成 Decompress On Load（或勾上 Preload Audio Data），" +
                        "然后点本窗口的「刷新」。";
                return null;
            }

            long total = (long)clip.samples * clip.channels;
            if (total <= 0)
            {
                error = "该音频没有采样（samples = " + clip.samples + "）。";
                return null;
            }
            if (total > int.MaxValue)
            {
                error = "音频过大（" + total + " 个采样），超出单次可读取范围。";
                return null;
            }

            var data = new float[total];
            if (!clip.GetData(data, 0))
            {
                error = "AudioClip.GetData 返回 false，没有读到采样。";
                return null;
            }
            return data;
        }

        // ===================== 界面 =====================

        private void OnGUI()
        {
            DrawClipRow();

            if (_clip == null)
            {
                EditorGUILayout.HelpBox(
                    "未绑定音频：在 Project 窗口选中一个 AudioClip 即可自动绑定。",
                    MessageType.Info);
                return;
            }

            if (_loadError != null)
            {
                EditorGUILayout.HelpBox(_loadError, MessageType.Error);
                return;   // 读不出来就什么都不画 —— 画一条直线会让人误以为音频是静音的
            }

            DrawInfo();
            DrawWaveform();
            DrawSelectionRow();
            DrawButtonRow();
            DrawStatusLine(BuildStatusText());

            HandleDragEnd();   // 手势收尾放在最后统一判：拖到波形外松手时波形控件收不到 MouseUp
        }

        private void DrawClipRow()
        {
            EditorGUILayout.BeginHorizontal();

            var picked = (AudioClip)EditorGUILayout.ObjectField("音频", _clip, typeof(AudioClip), false);
            if (picked != _clip)
                SetClip(picked);

            if (GUILayout.Button("取当前选中", GUILayout.Width(90f)))
                BindFromSelection();
            if (GUILayout.Button("刷新", GUILayout.Width(60f)))
                Refresh(showDialog: true);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfo()
        {
            float seconds = _frequency > 0 ? _frames / (float)_frequency : 0f;
            EditorGUILayout.LabelField("时长",
                seconds.ToString("0.###") + " 秒　（" + _frames + " 帧　" + _frequency + " Hz　" + _channels + " 声道）");

            var importer = string.IsNullOrEmpty(_sourcePath) ? null : AssetImporter.GetAtPath(_sourcePath) as AudioImporter;
            if (importer == null)
            {
                if (string.IsNullOrEmpty(_sourcePath))
                    EditorGUILayout.LabelField("来源", "非工程资产（场景内嵌 / 运行期生成）—— 仍可裁剪导出");
                return;
            }

            var settings = importer.defaultSampleSettings;
            EditorGUILayout.LabelField("导入设置",
                settings.compressionFormat + "　" + settings.loadType + "　质量 " + settings.quality);

            // 压缩源 → 导出 WAV 会变大。这条提示的**文字长度固定**（只有格式名随 clip 变），
            // 数字那部分放到底部固定高的状态行里 —— 否则 HelpBox 换行会让下面所有控件的命中矩形跟着位移。
            if (settings.compressionFormat.ToString() != "PCM")
                EditorGUILayout.HelpBox(
                    "源音频是 " + settings.compressionFormat + " 压缩格式。导出为 WAV 是**未压缩**的 16-bit PCM，" +
                    "文件会比源大很多 —— 这是 WAV 的固有代价，不是工具算错了。具体数字见下方状态行。",
                    MessageType.Warning);
        }

        private void DrawWaveform()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, WaveHeight, GUILayout.ExpandWidth(true));
            HandleWaveformInput(rect);

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.17f));

            // 未选中的部分压暗 —— 亮的就是「会被保留」的那段
            float xStart = FrameToX(rect, _selStart);
            float xEnd = FrameToX(rect, _selEnd);
            var dim = new Color(0f, 0f, 0f, 0.45f);
            if (xStart > rect.x)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, xStart - rect.x, rect.height), dim);
            if (xEnd < rect.xMax)
                EditorGUI.DrawRect(new Rect(xEnd, rect.y, rect.xMax - xEnd, rect.height), dim);

            int widthPx = Mathf.Max(1, Mathf.FloorToInt(rect.width));
            EnsurePeaks(widthPx);

            float midY = rect.y + rect.height * 0.5f;
            EditorGUI.DrawRect(new Rect(rect.x, midY, rect.width, 1f), new Color(1f, 1f, 1f, 0.12f));

            var wave = new Color(0.45f, 0.85f, 0.55f, 1f);
            float half = rect.height * 0.5f;
            for (int x = 0; x < widthPx; x++)
            {
                float lo = _minPeaks[x], hi = _maxPeaks[x];
                float yTop = midY - hi * half;
                float yBottom = midY - lo * half;
                if (yBottom - yTop < 1f)
                    yBottom = yTop + 1f;
                EditorGUI.DrawRect(new Rect(rect.x + x, yTop, 1f, yBottom - yTop), wave);
            }

            // 选区两条边界
            var edge = new Color(1f, 0.85f, 0.2f);
            EditorGUI.DrawRect(new Rect(xStart - 1f, rect.y, 2f, rect.height), edge);
            EditorGUI.DrawRect(new Rect(xEnd - 1f, rect.y, 2f, rect.height), edge);

            // 播放头：位置就是原 clip 的采样号，直接映射（不再加选区起点）
            if (_previewing)
            {
                int pos = GetPreviewPosition();
                if (pos >= 0)
                {
                    float px = FrameToX(rect, pos);
                    EditorGUI.DrawRect(new Rect(px, rect.y, 1f, rect.height), new Color(1f, 0.3f, 0.3f));
                }
            }

            DrawOutline(rect, new Color(0f, 0f, 0f, 0.6f));
        }

        private void DrawSelectionRow()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("选区（帧）", GUILayout.Width(70f));
            int newStart = EditorGUILayout.IntField(_selStart, GUILayout.Width(100f));
            int newEnd = EditorGUILayout.IntField(_selEnd, GUILayout.Width(100f));
            if (newStart != _selStart || newEnd != _selEnd)
                SetSelection(newStart, newEnd);

            int len = _selEnd - _selStart;
            EditorGUILayout.LabelField(
                len > 0 ? len + " 帧　" + (len / (float)_frequency).ToString("0.###") + " 秒" : "（长度为 0）",
                GUILayout.Width(200f));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawButtonRow()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("全选", GUILayout.Width(60f)))
                SetSelection(0, _frames);

            bool canPreview = _previewUnavailable == null && _selEnd > _selStart;

            EditorGUI.BeginDisabledGroup(!canPreview || _paused);
            if (GUILayout.Button("试听选区", GUILayout.Width(80f)))
                StartPreview(loop: false);
            if (GUILayout.Button("循环试听", GUILayout.Width(80f)))
                StartPreview(loop: true);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canPreview || !_previewing);
            if (GUILayout.Button(_paused ? "继续" : "暂停", GUILayout.Width(60f)))
                TogglePause();
            if (GUILayout.Button("停止", GUILayout.Width(60f)))
                StopPreview();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(_selEnd <= _selStart);
            if (GUILayout.Button("另存为 WAV…", GUILayout.Width(130f)))
                ExportWav();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_previewUnavailable != null)
                EditorGUILayout.HelpBox(_previewUnavailable, MessageType.Info);
        }

        /// <summary>固定高度状态行 —— 内容随选区变，但**高度不变**，不会把上面的控件顶走。</summary>
        private void DrawStatusLine(string text)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.LabelField(rect, text);
        }

        private string BuildStatusText()
        {
            int len = _selEnd - _selStart;
            if (len <= 0)
                return "选区长度为 0：拖一下波形、或点「全选」。";

            long outBytes = (long)len * _channels * 2;   // 16-bit PCM
            string text = "导出约 " + Kb(outBytes) + " KB（16-bit PCM，" + _channels + " 声道，" + _frequency + " Hz）";
            if (_sourceBytes > 0)
                text += "　｜　源文件 " + Kb(_sourceBytes) + " KB";
            if (_previewing)
                text += _paused ? "　｜　试听已暂停" : (_loopMode ? "　｜　循环试听中…" : "　｜　试听中…");
            return text;
        }

        // ===================== 波形命中与选区 =====================

        private void HandleWaveformInput(Rect rect)
        {
            Event ev = Event.current;

            if (ev.type == EventType.MouseDown && ev.button == 0 && rect.Contains(ev.mousePosition))
            {
                StopPreview();
                _dragging = true;
                _dragAnchor = XToFrame(rect, ev.mousePosition.x);
                _selStart = _selEnd = _dragAnchor;
                ev.Use();
                Repaint();
            }
            else if (ev.type == EventType.MouseDrag && _dragging && rect.Contains(ev.mousePosition))
            {
                // **必须仍在波形内**才继续改选区。之前这里没判 rect，导致拖出去（比如拖到下面的
                // 数字输入框上、想手输采样号）时选区还在跟着动，把手动输入盖掉。
                // 拖出边界时选区停在最后一个「仍在波形内」的位置；拖回波形内可以接着拖。
                int frame = XToFrame(rect, ev.mousePosition.x);
                _selStart = Mathf.Min(_dragAnchor, frame);
                _selEnd = Mathf.Max(_dragAnchor, frame);
                ev.Use();
                Repaint();
            }
        }

        /// <summary>
        /// 手势收尾**必须在 OnGUI 末尾统一判** —— 拖到波形之外松手时，波形控件根本收不到 MouseUp，
        /// 只靠控件内判定会让 <c>_dragging</c> 永久卡在 true（项目里两个画布窗口都踩过这个坑）。
        /// 另外把鼠标移出窗口也当成收尾，免得「在窗口外松手」之后拖拽状态一直挂着。
        /// 单击（没有拖动）视作「恢复全选」，因为零长度选区什么也做不了。
        /// </summary>
        private void HandleDragEnd()
        {
            Event ev = Event.current;
            if (!_dragging || (ev.type != EventType.MouseUp && ev.type != EventType.MouseLeaveWindow))
                return;

            bool clicked = ev.type == EventType.MouseUp;
            _dragging = false;
            if (clicked && _selEnd <= _selStart)
            {
                _selStart = 0;
                _selEnd = _frames;
            }
            ev.Use();
            Repaint();
        }

        private void SetSelection(int a, int b)
        {
            a = Mathf.Clamp(a, 0, _frames);
            b = Mathf.Clamp(b, 0, _frames);
            _selStart = Mathf.Min(a, b);
            _selEnd = Mathf.Max(a, b);
            StopPreview();   // 选区变了，之前那段试听就没意义了
            Repaint();
        }

        private float FrameToX(Rect rect, int frame) =>
            rect.x + rect.width * Mathf.Clamp01(frame / (float)Mathf.Max(1, _frames));

        private int XToFrame(Rect rect, float x) =>
            Mathf.Clamp(Mathf.RoundToInt((x - rect.x) / Mathf.Max(1f, rect.width) * _frames), 0, _frames);

        /// <summary>按当前像素宽降采样成 min/max 峰值。**只在 clip 或宽度变化时重算** —— 每帧重扫几百万个采样会卡死。</summary>
        private void EnsurePeaks(int widthPx)
        {
            if (_samples == null || widthPx <= 0)
                return;
            if (_peaksClip == _clip && _peaksWidth == widthPx && _minPeaks != null)
                return;

            if (_minPeaks == null || _minPeaks.Length != widthPx)
            {
                _minPeaks = new float[widthPx];
                _maxPeaks = new float[widthPx];
            }

            int frames = _frames, ch = _channels;
            for (int x = 0; x < widthPx; x++)
            {
                int f0 = (int)((long)frames * x / widthPx);
                int f1 = (int)((long)frames * (x + 1) / widthPx);
                if (f1 <= f0)
                    f1 = f0 + 1;

                float lo = 1f, hi = -1f;
                for (int f = f0; f < f1 && f < frames; f++)
                {
                    int b = f * ch;
                    for (int c = 0; c < ch; c++)
                    {
                        float v = _samples[b + c];
                        if (v < lo) lo = v;
                        if (v > hi) hi = v;
                    }
                }
                if (lo > hi) { lo = 0f; hi = 0f; }   // 该列没有采样（帧数为 0）
                _minPeaks[x] = lo;
                _maxPeaks[x] = hi;
            }

            _peaksClip = _clip;
            _peaksWidth = widthPx;
        }

        private static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }

        // ===================== 试听（反射 UnityEditor.AudioUtil）=====================

        /// <summary>
        /// 解析一次并缓存 `UnityEditor.AudioUtil` 的成员。它和 <c>UnityEditor.Editor</c> 同在
        /// `UnityEditor.CoreModule.dll`（不是 `UnityEditor.dll`，那个只是 façade），所以
        /// <c>typeof(Editor).Assembly.GetType(...)</c> 能直接解析到，不必遍历 AppDomain。
        ///
        /// 拿不到就把试听按钮置灰 + 一条常驻说明 + 一条 warning，**只警告一次**；之后所有调用都走
        /// 同一条降级路径。热重载后（域重载会清掉 Type/MethodInfo）本方法在 `OnEnable` 里会重新跑一遍。
        /// </summary>
        private void ResolveAudioUtil()
        {
            _audioUtil = null;
            _miPlay = _miStop = _miPause = _miResume = null;
            _miIsPlaying = _miHasPreview = _miSamplePosition = _miSecondsPosition = null;
            _previewUnavailable = null;

            try
            {
                _audioUtil = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
                if (_audioUtil == null)
                {
                    SetPreviewUnavailable("解析不到 UnityEditor.AudioUtil。");
                    return;
                }

                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var m in _audioUtil.GetMethods(flags))
                {
                    var ps = m.GetParameters();

                    // 只认 (AudioClip clip, int startSample, bool loop) 这一种形态 —— 2021.3 的实测签名。
                    // 按**参数名**匹配而不是按个数：个数相同但含义可能完全不同的重载（比如
                    // (clip, startSample, endSample)）会让「把所有 int 填 0」变成「播 0 长度」= 静音。
                    if (m.Name == "PlayPreviewClip" && ps.Length == 3
                        && ps[0].ParameterType == typeof(AudioClip)
                        && ps[1].ParameterType == typeof(int)
                        && ps[2].ParameterType == typeof(bool))
                    {
                        _miPlay = m;
                    }
                    else if (ps.Length == 0 && m.Name == "StopAllPreviewClips") _miStop = m;
                    else if (ps.Length == 0 && m.Name == "PausePreviewClip") _miPause = m;
                    else if (ps.Length == 0 && m.Name == "ResumePreviewClip") _miResume = m;
                    // 采样位置用 GetPreviewClipSamplePosition（返回 int 采样号）；
                    // GetPreviewClipPosition 返回的是 **float 秒**，只能当兜底。
                    else if (ps.Length == 0 && m.Name == "GetPreviewClipSamplePosition") _miSamplePosition = m;
                    else if (ps.Length == 0 && m.Name == "GetPreviewClipPosition") _miSecondsPosition = m;
                    else if (ps.Length == 0 && m.Name == "IsPreviewClipPlaying") _miIsPlaying = m;
                    else if (ps.Length == 1 && m.Name == "HasPreview" && ps[0].ParameterType == typeof(AudioClip)) _miHasPreview = m;
                }

                if (_miPlay == null || _miStop == null)
                    SetPreviewUnavailable("UnityEditor.AudioUtil 缺少预期的 PlayPreviewClip(AudioClip,int,bool) / StopAllPreviewClips。");
            }
            catch (System.Exception e)
            {
                SetPreviewUnavailable("解析内部试听 API 抛异常：" + e.Message);
            }
        }

        private void SetPreviewUnavailable(string reason)
        {
            _previewUnavailable = "本 Unity 版本的内部试听 API 不可用（" + reason + "）。\n" +
                                  "仍可正常裁剪与导出；试听请用 Inspector 上 AudioClip 的播放按钮。";
            _miPlay = null;
            Debug.LogWarning(Tag + " " + _previewUnavailable);
        }

        /// <summary>
        /// 播**原 clip**，从选区起点开始，loop = false。
        ///
        /// 为什么不用「只含选区的临时 AudioClip」：初版这么做，结果**一点声音都没有**。
        /// 真正的原因是两条与临时 clip 无关的 bug（位置 API 取错、参数按个数填），但临时 clip 本身
        /// 还多担一层风险 —— **运行时 `AudioClip.Create` 出来的 clip 未必被编辑器的预览通道接受**
        /// （`AudioUtil.HasPreview` 就是为此存在的）。既然现在已经拿到确切的签名
        /// `PlayPreviewClip(AudioClip, int startSample, bool loop)`，就没有理由再绕这一圈：
        /// 原 clip 是正常导入资产，**Inspector 上那个播放按钮走的就是这套 API**，它响它就一定响。
        ///
        /// 代价：没有 endSample 参数，「只播选区」得自己在 <see cref="OnEditorUpdate"/> 里到点停。
        /// 好处：播放位置就是原 clip 的采样号，跟波形一一对应，不用再换算。
        /// </summary>
        /// <param name="loop">
        /// true = 循环试听：首次从「选区末尾往前 <see cref="LoopLeadSeconds"/> 秒」起播（尾巴通常才是要
        /// 反复听的），之后每次播到选区末尾就回到**选区起点**重播 —— 即循环的是整个选区，首遍只是换了个起播点。
        /// </param>
        private void StartPreview(bool loop)
        {
            if (_previewUnavailable != null || _clip == null || _selEnd <= _selStart)
                return;

            StopPreview();
            _loopMode = loop;

            int leadFrames = Mathf.RoundToInt(LoopLeadSeconds * _frequency);
            int start = loop ? Mathf.Max(_selStart, _selEnd - leadFrames) : _selStart;

            if (!PlayFrom(start))
            {
                StopPreview();
                return;
            }

            _previewing = true;
            _paused = false;
            _previewStartChecked = false;
            _previewStartedAt = EditorApplication.timeSinceStartup;
            // 循环模式不设自动停（只有「停止」或改选区才停）
            _previewDeadline = loop
                ? double.MaxValue
                : _previewStartedAt + (_selEnd - _selStart) / (double)_frequency + 0.5;
            _nextRepaintAt = 0;
            EditorApplication.update += OnEditorUpdate;
            Repaint();
        }

        /// <summary>**先停掉上一条预览**，再从指定采样帧起播；失败返回 false（此时已给出说明）。</summary>
        private bool PlayFrom(int frame)
        {
            StopPreviewAudio();   // 不先停 → 叠加（见 StopPreviewAudio 的说明）
            try
            {
                _miPlay.Invoke(null, BuildPlayArgs(_clip, frame));
                return true;
            }
            catch (System.Exception e)
            {
                SetPreviewUnavailable("调用 PlayPreviewClip 失败：" + e.Message);
                return false;
            }
        }

        /// <summary>按**参数名**构造实参（名不认时退回按类型），这样重载改了名也还能对上。</summary>
        private object[] BuildPlayArgs(AudioClip clip, int startSample)
        {
            var ps = _miPlay.GetParameters();
            var args = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                string name = ps[i].Name ?? "";
                var t = ps[i].ParameterType;

                if (t == typeof(AudioClip)) args[i] = clip;
                else if (t == typeof(bool)) args[i] = false;                       // loop
                else if (t == typeof(int)) args[i] = name.IndexOf("start", System.StringComparison.OrdinalIgnoreCase) >= 0
                                                        ? startSample : 0;
                else if (t == typeof(float)) args[i] = 0f;
                else args[i] = t.IsValueType ? System.Activator.CreateInstance(t) : null;
            }
            return args;
        }

        private void OnEditorUpdate()
        {
            if (!_previewing)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            bool playing = IsPreviewPlaying();
            int pos = GetPreviewPosition();

            // 起播后做一次「真的响了吗」的检查。原生侧起播有延迟，所以给 0.4 秒宽限；
            // 超时仍未响就**明确报出可查的事实**（而不是像初版那样静默无声、只能靠猜）。
            if (!_previewStartChecked && now - _previewStartedAt > 0.4)
            {
                _previewStartChecked = true;
                if (!playing && pos < 0)
                {
                    Debug.LogWarning(Tag + " 试听没有响起来：已调用 PlayPreviewClip，但 " +
                        "IsPreviewClipPlaying 仍为 false。\n  HasPreview=" + HasPreview(_clip) +
                        "　编辑器全局静音(EditorUtility.audioMasterMute)=" + EditorUtility.audioMasterMute +
                        "　（若为 True，取消 Game 视图工具栏的 Mute Audio）");
                    StopPreview();
                    return;
                }
            }

            // PlayPreviewClip 没有 endSample 参数，「只播选区」只能自己到点停。
            // 位置可用时**只信位置**：否则重播瞬间原生侧可能还没开始，`!playing` 会抖，
            // 那样就会连击重播。位置彻底拿不到时才退而用「不再播放」当信号，并加宽限防抖。
            bool reachedEnd = pos >= 0
                ? pos >= _selEnd
                : _previewStartChecked && !playing && !_paused && now >= _loopGraceUntil;

            if (reachedEnd)
            {
                if (_loopMode && PlayFrom(_selStart))
                {
                    _loopGraceUntil = now + 0.2;   // 回到选区起点重播
                    _nextRepaintAt = 0;
                    Repaint();
                    return;
                }

                StopPreview();
                return;
            }

            if (_previewStartChecked && !playing && !_paused && now >= _loopGraceUntil)
            {
                StopPreview();   // 自然播完
                return;
            }

            if (now >= _previewDeadline)
            {
                StopPreview();   // 兜底
                return;
            }

            if (now >= _nextRepaintAt)
            {
                _nextRepaintAt = now + PreviewRepaintInterval;
                Repaint();
            }
        }

        private bool IsPreviewPlaying()
        {
            if (_miIsPlaying == null)
                return false;
            try { return _miIsPlaying.Invoke(null, null) is bool playing && playing; }
            catch (System.Exception) { return false; }
        }

        private bool HasPreview(AudioClip clip)
        {
            if (_miHasPreview == null || clip == null)
                return false;
            try { return _miHasPreview.Invoke(null, new object[] { clip }) is bool has && has; }
            catch (System.Exception) { return false; }
        }

        private void TogglePause()
        {
            if (!_previewing)
                return;
            var mi = _paused ? _miResume : _miPause;
            if (mi == null)
                return;
            try { mi.Invoke(null, null); }
            catch (System.Exception) { /* 暂停失败不值得打断用户：试听本身还在响 */ }
            _paused = !_paused;
            Repaint();
        }

        private void StopPreview()
        {
            EditorApplication.update -= OnEditorUpdate;
            _previewing = false;
            _paused = false;
            _loopMode = false;
            _loopGraceUntil = 0;
            StopPreviewAudio();
            Repaint();
        }

        /// <summary>
        /// 只停掉正在响的预览音频，不碰窗口状态。
        ///
        /// **每次起播前都必须先调这个**：`PlayPreviewClip` 不会替你停掉上一条 —— 它只是又起一个。
        /// 循环试听最初就是漏了这一步（播到选区末尾直接又 Play 一次），而上一条此时还在继续播到
        /// clip 末尾，于是每绕一圈叠一条音轨，听感就是「多个声音叠加」。
        ///
        /// 单独抽出来是因为**循环重播**也要用它，而那条路径不能走 <see cref="StopPreview"/>
        /// （那会把 <c>_loopMode</c> 一起清掉，循环就断了）。
        /// </summary>
        private void StopPreviewAudio()
        {
            if (_miStop == null)
                return;
            try { _miStop.Invoke(null, null); }
            catch (System.Exception) { /* 已经在停了，忽略 */ }
        }

        /// <summary>
        /// 当前试听位置（**原 clip 的采样号**，所以跟波形一一对应，不用换算）。拿不到返回 -1。
        ///
        /// 优先 `GetPreviewClipSamplePosition()`（int 采样号）。兜底的 `GetPreviewClipPosition()` 返回的是
        /// **float 秒** —— 初版把它当成 int 匹配（`is int`），装箱的 float 永远匹配不上，于是每次都返回 -1、
        /// 播放头从来没画出来过。这里按 float 处理再乘采样率。
        /// </summary>
        private int GetPreviewPosition()
        {
            try
            {
                if (_miSamplePosition != null && _miSamplePosition.Invoke(null, null) is int samples)
                    return samples;
                if (_miSecondsPosition != null && _miSecondsPosition.Invoke(null, null) is float seconds)
                    return Mathf.RoundToInt(seconds * _frequency);
            }
            catch (System.Exception)
            {
                // 位置读不到不影响出声，静默返回 -1
            }
            return -1;
        }

        // ===================== 导出 =====================

        private void ExportWav()
        {
            int len = _selEnd - _selStart;
            if (len <= 0 || _samples == null)
                return;

            string baseName = string.IsNullOrEmpty(_sourcePath)
                ? (_clip != null ? _clip.name : "audio")
                : Path.GetFileNameWithoutExtension(_sourcePath);

            string defaultDir = string.IsNullOrEmpty(_sourcePath)
                ? "Assets"
                : Path.GetDirectoryName(Path.Combine(ProjectRoot(), _sourcePath));

            string path = EditorUtility.SaveFilePanel("另存为 WAV",
                EditorPathMemory.LoadDir(ExportPathKey, defaultDir), baseName + "_trim.wav", "wav");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ExportPathKey, path);

            byte[] bytes = BuildWav(_selStart, len, _channels, _frequency);
            if (bytes == null)
            {
                EditorUtility.DisplayDialog("音频裁剪",
                    "选区太长（" + Kb((long)len * _channels * 2) + " KB），超出本工具一次写出的上限（约 2 GB）。\n" +
                    "请缩小选区。", "确定");
                return;
            }

            File.WriteAllBytes(path, bytes);
            if (path.Replace('\\', '/').Contains("/Assets/"))
                AssetDatabase.Refresh();

            Debug.Log(Tag + " 已导出 " + len + " 帧（" + (len / (float)_frequency).ToString("0.###") +
                      " 秒）→ " + path);
        }

        /// <summary>把选区的采样写成标准 WAV（16-bit PCM，保持原声道数与采样率）。超上限返回 null。</summary>
        private byte[] BuildWav(int startFrame, int frameCount, int channels, int frequency)
        {
            long sampleCount = (long)frameCount * channels;
            long dataBytes = sampleCount * 2;
            // 上限取 .NET 单数组上限（约 2 GB），**不是** WAV 格式的 4 GB —— 因为要先把 PCM 攒进一个 byte[]。
            // 实际到不了（2 GB ≈ 6 小时立体声），但既然写了守卫就得写对：写成 4 GB 会在这里放过、
            // 然后在 new byte[] 上抛异常。
            if (dataBytes > int.MaxValue - 64)
                return null;

            int channels16 = channels;
            int byteRate = frequency * channels16 * 2;
            int blockAlign = channels16 * 2;

            var pcm = new byte[dataBytes];
            for (long i = 0; i < sampleCount; i++)
            {
                long srcIndex = (long)startFrame * channels + i;
                if (srcIndex < 0 || srcIndex >= _samples.Length)
                    break;
                short s = (short)Mathf.Clamp(Mathf.Round(_samples[srcIndex] * 32767f), -32768f, 32767f);
                pcm[i * 2] = (byte)(s & 0xFF);            // 显式小端，不依赖 BitConverter 的平台字节序
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }

            using (var ms = new MemoryStream(pcm.Length + 44))
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write((uint)(36 + dataBytes));
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16u);                    // fmt chunk 长度
                w.Write((ushort)1);              // PCM
                w.Write((ushort)channels16);
                w.Write((uint)frequency);
                w.Write((uint)byteRate);
                w.Write((ushort)blockAlign);
                w.Write((ushort)16);             // bits per sample
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write((uint)dataBytes);
                w.Write(pcm);
                w.Flush();
                return ms.ToArray();
            }
        }

        // ===================== 杂项 =====================

        private static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);

        private static long SourceFileBytes(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return 0;
            var file = new FileInfo(Path.Combine(ProjectRoot(), assetPath));
            return file.Exists ? file.Length : 0L;
        }

        private static string Kb(long bytes) => (bytes / 1024.0).ToString("0.0");
    }
}
