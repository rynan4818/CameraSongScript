using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Models;
using Newtonsoft.Json;

namespace CameraSongScript.Detectors
{
    /// <summary>
    /// 曲選択時にカメラスクリプト(.json)の存在を検出するクラス
    /// フォルダ内の全.jsonファイルをスキャンし、有効なMovementScript形式のものを候補として保持する
    /// ファイルI/Oは非同期で実行し、メインスレッドをブロックしない
    /// </summary>
    internal class CameraSongScriptDetector
    {
        private static string _latestSelectedSong = string.Empty;
        private static CancellationTokenSource _scanCts;
        private static readonly object _scanLock = new object();

        /// <summary>
        /// スキャン完了時に発火するイベント（UIの更新通知用）
        /// 注意: バックグラウンドスレッドから呼ばれる可能性がある
        /// </summary>
        public static event Action ScanCompleted;

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
        /// オフセット適用済みの一時スクリプトのフルパス
        /// CameraPlusにはこのパスが渡される
        /// </summary>
        public static string EffectiveScriptPath { get; private set; } = string.Empty;

        /// <summary>
        /// 有効なカメラスクリプトが存在するかどうか
        /// </summary>
        public static bool HasSongScript => !string.IsNullOrEmpty(SelectedScriptPath);

        /// <summary>
        /// 選択中のカメラスクリプトに含まれるメタデータ
        /// </summary>
        public static MetadataElements CurrentMetadata { get; private set; }

        /// <summary>
        /// Beat Saberの既知ファイル名（スキップ対象）
        /// </summary>
        private static readonly HashSet<string> _skipFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "info.dat",
            "BPMInfo.dat",
            "cinema-video.json"
        };

        public static void ProcessLevel(IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                if (customLevel.customLevelPath != _latestSelectedSong)
                {
                    _latestSelectedSong = customLevel.customLevelPath;
                    CurrentLevelPath = customLevel.customLevelPath;
#if DEBUG
                    Plugin.Log.Notice($"Selected CustomLevel Path:\n {customLevel.customLevelPath}");
#endif
                    // 前回のスキャンをキャンセルして新しいスキャンを開始
                    StartScanAsync(customLevel.customLevelPath);
                }
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

                // Beat Saberের 既知ファイルをスキップ
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

            LoadMetadata(SelectedScriptPath);

            // 一時ファイルの再生成
            UpdateEffectiveScriptPath();

            // CameraPlusモード時はパスを反映
            SyncCameraPlusPath();

            // UIに通知
            ScanCompleted?.Invoke();
        }

        private static string SelectDefaultScript(List<string> validFiles, string levelPath)
        {
            if (validFiles.Count == 0)
                return string.Empty;

            string configFileName = CameraSongScriptConfig.Instance.SelectedScriptFile;

            // 1. Configに記録されているファイル名が存在すれば優先
            if (!string.IsNullOrEmpty(configFileName) && validFiles.Contains(configFileName))
                return Path.Combine(levelPath, configFileName);

            // 2. なければ "SongScript.json" を探す
            if (validFiles.Contains("SongScript.json"))
            {
                CameraSongScriptConfig.Instance.SelectedScriptFile = "SongScript.json";
                return Path.Combine(levelPath, "SongScript.json");
            }

            // 3. どちらもなければ先頭のファイルを選択
            string fallbackName = validFiles[0];
            CameraSongScriptConfig.Instance.SelectedScriptFile = fallbackName;
            return Path.Combine(levelPath, fallbackName);
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

            LoadMetadata(SelectedScriptPath);

            UpdateEffectiveScriptPath();
            SyncCameraPlusPath();

            // Configに記録
            CameraSongScriptConfig.Instance.SelectedScriptFile = fileName;
        }

        /// <summary>
        /// CameraPlusモード時にスクリプトパスを同期する
        /// </summary>
        public static void SyncCameraPlusPath()
        {
            if (CameraModDetector.IsCameraPlus && HasSongScript && Plugin.IsCamPlusHelperReady)
            {
                Plugin.CamPlusHelper.SetScriptPath(EffectiveScriptPath);
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
            EffectiveScriptPath = string.Empty;
            CurrentMetadata = null;
            AvailableScriptFiles = new List<string>();
        }

        private static void LoadMetadata(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                CurrentMetadata = null;
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                CurrentMetadata = parsed?.metadata;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"CameraSongScriptDetector: Failed to load metadata from '{filePath}': {ex.Message}");
                CurrentMetadata = null;
            }
        }

        /// <summary>
        /// オフセット指定がある場合に一時ファイル（Temp_OffsetScript.json）を生成する
        /// </summary>
        public static void UpdateEffectiveScriptPath()
        {
            if (string.IsNullOrEmpty(SelectedScriptPath) || !File.Exists(SelectedScriptPath))
            {
                EffectiveScriptPath = string.Empty;
                return;
            }

            int offsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            if (offsetCm == 0)
            {
                EffectiveScriptPath = SelectedScriptPath;
                return;
            }

            float offsetMeters = offsetCm / 100f;

            try
            {
                string json = File.ReadAllText(SelectedScriptPath);
                var movementScript = JsonConvert.DeserializeObject<MovementScriptJson>(json);

                if (movementScript?.JsonMovements != null)
                {
                    string sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    string sepCheck = (sep == "." ? "," : ".");

                    foreach (var movement in movementScript.JsonMovements)
                    {
                        if (movement.startPos != null && !string.IsNullOrEmpty(movement.startPos.y))
                        {
                            string yStr = movement.startPos.y.Contains(sepCheck) ? movement.startPos.y.Replace(sepCheck, sep) : movement.startPos.y;
                            if (float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float startY))
                            {
                                movement.startPos.y = (startY + offsetMeters).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }

                        if (movement.endPos != null && !string.IsNullOrEmpty(movement.endPos.y))
                        {
                            string yStr = movement.endPos.y.Contains(sepCheck) ? movement.endPos.y.Replace(sepCheck, sep) : movement.endPos.y;
                            if (float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float endY))
                            {
                                movement.endPos.y = (endY + offsetMeters).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    string tempDir = Path.Combine(Environment.CurrentDirectory, "UserData", "CameraSongScript");
                    if (!Directory.Exists(tempDir))
                        Directory.CreateDirectory(tempDir);

                    string tempFilePath = Path.Combine(tempDir, "Temp_OffsetScript.json");
                    
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    };
                    string modifiedJson = JsonConvert.SerializeObject(movementScript, settings);
                    File.WriteAllText(tempFilePath, modifiedJson);

                    EffectiveScriptPath = tempFilePath;
                    Plugin.Log.Info($"CameraSongScriptDetector: Generated temporary offset script ({offsetMeters}m) at {tempFilePath}");
                }
                else
                {
                    EffectiveScriptPath = SelectedScriptPath;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraSongScriptDetector: Failed to generate temporary offset script: {ex.Message}");
                // エラー時は元のスクリプトを使用
                EffectiveScriptPath = SelectedScriptPath;
            }
        }
    }
}
