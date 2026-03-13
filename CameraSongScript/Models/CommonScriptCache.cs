using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;
using CameraSongScript.Localization;

namespace CameraSongScript.Models
{
    /// <summary>
    /// CommonScriptsフォルダ内の汎用スクリプト情報を保持するエントリ
    /// </summary>
    internal class CommonScriptEntry
    {
        /// <summary>UIドロップダウンに表示する名前（相対パス）</summary>
        public string DisplayName { get; set; }

        /// <summary>ディスク上のフルパス</summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// UserData/CameraSongScript/CommonScripts/ フォルダを再帰的にスキャンし、
    /// 汎用カメラスクリプトのリストをキャッシュするクラス。
    /// SongScriptが存在しない曲でのフォールバックや、強制使用で利用される。
    /// </summary>
    internal static class CommonScriptCache
    {
        private static readonly string CommonScriptFolderPath =
            Path.Combine(UnityGame.UserDataPath, "CameraSongScript", "CommonScripts");

        private static List<CommonScriptEntry> _scripts = new List<CommonScriptEntry>();
        private static readonly Random _random = new Random();

        /// <summary>キャッシュの準備が完了しているか</summary>
        public static bool IsReady { get; private set; }

        /// <summary>スキャン済みスクリプト一覧</summary>
        public static IReadOnlyList<CommonScriptEntry> Scripts => _scripts;

        /// <summary>
        /// 非同期でスキャンを開始する（Plugin.OnApplicationStartから呼ばれる）
        /// </summary>
        public static Task ScanAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    Scan();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CommonScriptCache: Scan failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// フォルダを再帰的にスキャンしてスクリプトリストを構築する
        /// </summary>
        private static void Scan()
        {
            var scripts = new List<CommonScriptEntry>();

            // フォルダが存在しなければ作成
            if (!Directory.Exists(CommonScriptFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(CommonScriptFolderPath);
                    Plugin.Log.Info($"CommonScriptCache: Created CommonScripts folder at: {CommonScriptFolderPath}");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CommonScriptCache: Failed to create CommonScripts folder: {ex.Message}");
                }
                _scripts = scripts;
                IsReady = true;
                return;
            }

            // .json ファイルをスキャン
            try
            {
                var jsonFiles = Directory.GetFiles(CommonScriptFolderPath, "*.json", SearchOption.AllDirectories);
                foreach (var filePath in jsonFiles)
                {
                    if (IsValidMovementScript(filePath))
                    {
                        string displayName = filePath.Length > CommonScriptFolderPath.Length
                            ? filePath.Substring(CommonScriptFolderPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            : Path.GetFileName(filePath);

                        scripts.Add(new CommonScriptEntry
                        {
                            DisplayName = displayName,
                            FilePath = filePath
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"CommonScriptCache: Failed to scan json files: {ex.Message}");
            }

            _scripts = scripts;
            IsReady = true;

            Plugin.Log.Info($"CommonScriptCache: Scan complete. {scripts.Count} common script(s) found.");
        }

        /// <summary>
        /// .jsonファイルが有効なMovementScript形式かどうかを検証する
        /// </summary>
        private static bool IsValidMovementScript(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                return parsed?.JsonMovements != null && parsed.JsonMovements.Length > 0;
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"CommonScriptCache: '{Path.GetFileName(filePath)}' is not a valid MovementScript: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// UI表示用の表示名リスト（先頭に UiLocalization.OptionRandom を含む）を返す
        /// </summary>
        public static List<string> GetDisplayNames()
        {
            var list = new List<string> { UiLocalization.OptionRandom };
            foreach (var entry in _scripts)
            {
                list.Add(entry.DisplayName);
            }
            return list;
        }

        /// <summary>
        /// 表示名からフルパスを解決する
        /// </summary>
        public static string GetPathByDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return null;

            foreach (var entry in _scripts)
            {
                if (entry.DisplayName == displayName)
                    return entry.FilePath;
            }
            return null;
        }

        /// <summary>
        /// ランダムにスクリプトを1つ選択して返す
        /// </summary>
        public static CommonScriptEntry GetRandom()
        {
            var scripts = _scripts;
            if (scripts.Count == 0)
                return null;

            int index = _random.Next(scripts.Count);
            return scripts[index];
        }
    }
}
