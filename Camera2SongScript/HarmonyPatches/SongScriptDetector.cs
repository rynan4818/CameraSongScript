using System.IO;
using HarmonyLib;

namespace Camera2SongScript.HarmonyPatches
{
    /// <summary>
    /// 曲選択時にSongScript.jsonの存在を検出するHarmonyパッチ
    /// CameraPlusのCustomPreviewBeatmapLevelPatchを参考に実装
    /// </summary>
    [HarmonyPatch(typeof(CustomPreviewBeatmapLevel), nameof(CustomPreviewBeatmapLevel.GetCoverImageAsync))]
    internal class SongScriptDetector
    {
        private static string _latestSelectedSong = string.Empty;

        /// <summary>
        /// 現在選択中の曲のSongScript.jsonパス（存在しない場合はstring.Empty）
        /// </summary>
        public static string SongScriptPath { get; private set; } = string.Empty;

        /// <summary>
        /// SongScript.jsonが存在するかどうか
        /// </summary>
        public static bool HasSongScript => !string.IsNullOrEmpty(SongScriptPath);

        static void Postfix(CustomPreviewBeatmapLevel __instance)
        {
            if (__instance.customLevelPath != _latestSelectedSong)
            {
                _latestSelectedSong = __instance.customLevelPath;
#if DEBUG
                Plugin.Log.Notice($"Selected CustomLevel Path:\n {__instance.customLevelPath}");
#endif
                string songScriptFile = Path.Combine(__instance.customLevelPath, "SongScript.json");
                if (File.Exists(songScriptFile))
                {
                    SongScriptPath = songScriptFile;
                    Plugin.Log.Info($"SongScript.json found: {songScriptFile}");
                }
                else
                {
                    SongScriptPath = string.Empty;
                }
            }
        }

        /// <summary>
        /// 状態をリセットする
        /// </summary>
        public static void Reset()
        {
            _latestSelectedSong = string.Empty;
            SongScriptPath = string.Empty;
        }
    }
}
