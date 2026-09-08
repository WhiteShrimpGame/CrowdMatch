using System.IO;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 编辑器文件面板的「上次路径」记忆工具。
    /// 遵循全局规则：凡是打开资源管理器（文件/目录选择面板）读取或保存文件，都要记忆上次操作的路径，
    /// 并在下次打开面板时复用。用 EditorPrefs 按 key 持久化目录（Unity 重启后仍保留）。
    /// </summary>
    public static class EditorPathMemory
    {
        /// <summary>读取上次使用的目录；key 无记录或目录不存在时回退到 fallback（默认 Assets）。</summary>
        public static string LoadDir(string key, string fallback = "Assets")
        {
            string dir = EditorPrefs.GetString(key, fallback);
            return string.IsNullOrEmpty(dir) || !Directory.Exists(dir) ? fallback : dir;
        }

        /// <summary>记录所选文件的目录，供下次打开文件面板时复用。</summary>
        public static void SaveDir(string key, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                EditorPrefs.SetString(key, dir);
        }
    }
}
