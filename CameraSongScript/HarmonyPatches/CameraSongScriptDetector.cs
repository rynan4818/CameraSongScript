using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Models;
using HarmonyLib;
using Newtonsoft.Json;

namespace CameraSongScript.HarmonyPatches
{
    /// <summary>
    /// 曲選択時にカメラスクリプト(.json)の存在を検出するHarmonyパッチ
    /// フォルダ内の全.jsonファイルをスキャンし、有効なMovementScript形式のものを候補として保持する
    /// ファイルI/Oは非同期で実行し、メインスレッドをブロックしない
    /// </summary>
    [HarmonyPatch(typeof(CustomPreviewBeatmapLevel), nameof(CustomPreviewBeatmapLevel.GetCoverImageAsync))]
    internal class CameraSongScriptDetector
    {
        private static string _latestSelectedSong = string.Empty;
        private static CancellationTokenSource _scanCts;
        private static readonly object _scanLock = new object();

        /// <summary>
        /// 現在選択中の曲フォルダのパス
        /// </summary>
        public static string CurrentLevelPath { get; private set; } = string.Empty;

        /// <summary>
        /// 現在選択中の曲フォルダにある有効なカメラスクリプトファイル名リスト（ファイル名のみ）
        /// </summary>
        public static List<string> AvailableScriptFiles { get; private set; } = new List<string>();

        /// <summary>
        /// 現在選択されているスクリプトのフルパス（存在しない場合はstring.Empty）
        /// </summary>
        public static string SelectedScriptPath { get; private set; } = string.Empty;

        /// <summary>
        /// 有効なカメラスクリプトが存在するかどうか
        /// </summary>
        public static bool HasSongScript => !string.IsNullOrEmpty(SelectedScriptPath);

        /// <summary>
        /// Beat Saberの既知ファイル名（スキップ対象）
        /// </summary>
        private static readonly HashSet<string> _skipFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "info.dat",
            "BPMInfo.dat",
            "cinema-video.json"
        };

        static void Postfix(CustomPreviewBeatmapLevel __instance)
        {
            if (__instance.customLevelPath != _latestSelectedSong)
            {
                _latestSelectedSong = __instance.customLevelPath;
                CurrentLevelPath = __instance.customLevelPath;
#if DEBUG
                Plugin.Log.Notice($"Selected CustomLevel Path:\n {__instance.customLevelPath}");
#endif
                // 前回のスキャンをキャンセルして新しいスキャンを開始
                StartScanAsync(__instance.customLevelPath);
            }
        }

        /// <summary>
        /// 非同期スキャンを開始する。前回のスキャンが実行中であればキャンセルする
        /// </summary>
        private static void StartScanAsync(string levelPath)
        {
            // 即座に状態をクリア（メインスレッド上で実行）
            // 共有リストをClear()せず新規リストを代入することで、他スレッドが旧リストを安全に参照し続けられる
            SelectedScriptPath = string.Empty;
            AvailableScriptFiles = new List<string>();

            CancellationToken ct;
            lock (_scanLock)
            {
                // 前回のスキャンをキャンセル
                _scanCts?.Cancel();
                _scanCts?.Dispose();
                _scanCts = new CancellationTokenSource();
                ct = _scanCts.Token;
            }

            Task.Run(() =>
            {
                try
                {
                    ScanForScriptFiles(levelPath, ct);
                }
                catch (OperationCanceledException)
                {
                    // 曲が素早く切り替えられた場合のキャンセルは正常系
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CameraSongScriptDetector: Async scan failed: {ex.Message}");
                }
            }, ct);
        }

        /// <summary>
        /// 指定フォルダ内の.jsonファイルをスキャンし、有効なカメラスクリプトを検出する（バックグラウンドスレッド実行）
        /// </summary>
        private static void ScanForScriptFiles(string levelPath, CancellationToken ct)
        {
            if (!Directory.Exists(levelPath))
                return;

            string[] jsonFiles;
            try
            {
                jsonFiles = Directory.GetFiles(levelPath, "*.json");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraSongScriptDetector: Failed to scan directory: {ex.Message}");
                return;
            }

            var validFiles = new List<string>();

            foreach (var filePath in jsonFiles)
            {
                ct.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(filePath);

                // Beat Saberの既知ファイルをスキップ
                if (_skipFileNames.Contains(fileName))
                    continue;

                // フォーマット検証
                if (IsValidMovementScript(filePath))
                {
                    validFiles.Add(fileName);
                }
            }

            ct.ThrowIfCancellationRequested();

            string selectedPath = SelectDefaultScript(validFiles, levelPath);

            if (validFiles.Count > 0)
                Plugin.Log.Info($"CameraSongScriptDetector: Found {validFiles.Count} valid script(s). Selected: {Path.GetFileName(selectedPath)}");

            ct.ThrowIfCancellationRequested();

            // 結果を一括で反映（SelectedScriptPathを先に代入し、AvailableScriptFilesの参照更新時に整合性を保つ）
            SelectedScriptPath = selectedPath;
            AvailableScriptFiles = validFiles;

            // CameraPlusモード時はパスを反映
            SyncCameraPlusPath();
        }

        /// <summary>
        /// 有効なスクリプトファイルリストからデフォルト選択を決定する
        /// 優先順位: Config指定ファイル > SongScript.json > リスト先頭ファイル
        /// </summary>
        private static string SelectDefaultScript(List<string> validFiles, string levelPath)
        {
            if (validFiles.Count == 0)
                return string.Empty;

            string configFileName = CameraSongScriptConfig.Instance.SelectedScriptFile;

            if (!string.IsNullOrEmpty(configFileName) && validFiles.Contains(configFileName))
                return Path.Combine(levelPath, configFileName);

            if (validFiles.Contains("SongScript.json"))
                return Path.Combine(levelPath, "SongScript.json");

            return Path.Combine(levelPath, validFiles[0]);
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
                Plugin.Log.Debug($"CameraSongScriptDetector: '{Path.GetFileName(filePath)}' is not a valid MovementScript: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 選択中のスクリプトファイルを変更する
        /// </summary>
        public static void UpdateSelectedScript(string fileName)
        {
            if (string.IsNullOrEmpty(CurrentLevelPath) || !AvailableScriptFiles.Contains(fileName))
                return;

            SelectedScriptPath = Path.Combine(CurrentLevelPath, fileName);
            Plugin.Log.Info($"CameraSongScriptDetector: Script selection changed to: {fileName}");

            // CameraPlusモード時はパスを反映
            SyncCameraPlusPath();

            // Configに記録
            CameraSongScriptConfig.Instance.SelectedScriptFile = fileName;
        }

        /// <summary>
        /// CameraPlusモード時にスクリプトパスを同期する
        /// </summary>
        private static void SyncCameraPlusPath()
        {
            if (CameraModDetector.IsCameraPlus && HasSongScript && Plugin.IsCamPlusHelperReady)
            {
                Plugin.CamPlusHelper.SetScriptPath(SelectedScriptPath);
            }
        }

        /// <summary>
        /// 状態をリセットする
        /// </summary>
        public static void Reset()
        {
            lock (_scanLock)
            {
                _scanCts?.Cancel();
                _scanCts?.Dispose();
                _scanCts = null;
            }
            _latestSelectedSong = string.Empty;
            CurrentLevelPath = string.Empty;
            SelectedScriptPath = string.Empty;
            AvailableScriptFiles = new List<string>();
        }
    }
}
