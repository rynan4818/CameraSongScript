using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Localization;
using CameraSongScript.Models;
using CameraSongScript.Services;
using CameraSongScript.Utilities;
using HMUI;
using IPA.Utilities;
using Newtonsoft.Json;

namespace CameraSongScript.Detectors
{
    /// <summary>
    /// 曲選択時にカメラスクリプト(.json)の存在を検出するクラス
    /// 譜面フォルダ内の.jsonファイルに加え、SongScriptsフォルダ内のスクリプトも
    /// metadata.mapId / metadata.hash によるマッチングで検出・統合する
    /// ファイルI/Oは非同期で実行し、メインスレッドをブロックしない
    /// </summary>
    internal partial class CameraSongScriptDetector
    {
        internal static CameraSongScriptDetector Instance { get; private set; }

        private string _latestSelectedSong = string.Empty;
        private CancellationTokenSource _scanCts;
        private readonly object _scanLock = new object();

        /// <summary>
        /// 表示名 → ScriptCandidate のマッピング（パス解決用）
        /// </summary>
        private Dictionary<string, ScriptCandidate> _candidateMap = new Dictionary<string, ScriptCandidate>();

        /// <summary>
        /// 現在選択中のlevelID（hash抽出用）
        /// </summary>
        private string _currentLevelId = string.Empty;

        /// <summary>
        /// スキャン完了時に発火するイベント（UIの更新通知用）
        /// スキャン結果の反映後、メインスレッド上で呼び出される。
        /// </summary>
        public event Action ScanCompleted;

        /// <summary>
        /// 現在選択中の曲フォルダのパス
        /// </summary>
        public string CurrentLevelPath { get; private set; } = string.Empty;

        /// <summary>
        /// 現在選択中の曲で利用可能なカメラスクリプトの表示名リスト
        /// 譜面フォルダ: ファイル名のみ / SongScriptsフォルダ: "[SS] filename" 形式
        /// </summary>
        public List<string> AvailableScriptFiles { get; private set; } = new List<string>();

        /// <summary>
        /// 現在選択されているスクリプトのフルパス（存在しない場合はstring.Empty）
        /// zipエントリの場合は展開済み一時ファイルのパスが入る
        /// </summary>
        public string SelectedScriptPath { get; private set; } = string.Empty;

        /// <summary>
        /// 現在選択されているスクリプトの表示名（AvailableScriptFilesの要素と一致する）
        /// </summary>
        public string SelectedScriptDisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// オフセット適用済みの一時スクリプトのフルパス
        /// CameraPlusにはこのパスが渡される
        /// </summary>
        public string EffectiveScriptPath { get; private set; } = string.Empty;

        /// <summary>
        /// 有効なカメラスクリプトが存在するかどうか
        /// </summary>
        public bool HasSongScript => !string.IsNullOrEmpty(SelectedScriptPath);

        /// <summary>
        /// 現在の曲で汎用スクリプトが使用されるかどうか
        /// </summary>
        public bool IsUsingCommonScript { get; private set; }

        /// <summary>
        /// 汎用スクリプトの解決済みファイルパス
        /// CameraPlusモードでは曲選択時に解決、Camera2モードではプレイ開始時に解決
        /// </summary>
        public string ResolvedCommonScriptPath { get; internal set; } = string.Empty;

        /// <summary>
        /// 汎用スクリプトの解決済み表示名（ステータス・ログ用）
        /// </summary>
        public string ResolvedCommonScriptDisplayName { get; internal set; } = string.Empty;

        /// <summary>
        /// 選択中のカメラスクリプトに含まれるメタデータ
        /// </summary>
        public MetadataElements CurrentMetadata { get; private set; }

        /// <summary>
        /// 現在解決済みの汎用スクリプトに含まれるメタデータ
        /// </summary>
        public MetadataElements ResolvedCommonMetadata { get; private set; }

        /// <summary>
        /// 現在選択中のSongScriptに含まれるCamera2非対応機能
        /// </summary>
        public bool SelectedScriptContainsCameraEffect { get; private set; }
        public bool SelectedScriptContainsWindowControl { get; private set; }

        /// <summary>
        /// 現在解決済みの汎用スクリプトに含まれるCamera2非対応機能
        /// </summary>
        public bool ResolvedCommonScriptContainsCameraEffect { get; private set; }
        public bool ResolvedCommonScriptContainsWindowControl { get; private set; }

        /// <summary>
        /// 現在の実効スクリプトに含まれるCamera2非対応機能
        /// </summary>
        public bool CurrentScriptContainsCameraEffect =>
            IsUsingCommonScript ? ResolvedCommonScriptContainsCameraEffect : SelectedScriptContainsCameraEffect;

        public bool CurrentScriptContainsWindowControl =>
            IsUsingCommonScript ? ResolvedCommonScriptContainsWindowControl : SelectedScriptContainsWindowControl;

        public bool HasCurrentUnsupportedFeatures =>
            CurrentScriptContainsCameraEffect || CurrentScriptContainsWindowControl;

        public string CurrentUnsupportedFeatureSummary
        {
            get
            {
                var unsupportedFeatures = new List<string>(2);
                if (CurrentScriptContainsCameraEffect)
                    unsupportedFeatures.Add("CameraEffect");
                if (CurrentScriptContainsWindowControl)
                    unsupportedFeatures.Add("WindowControl");
                return string.Join(", ", unsupportedFeatures);
            }
        }

        /// <summary>
        /// CameraPlusに設定するプロファイル名の解決済みキャッシュ
        /// SyncCameraPlusPath()はSongSettingsManagerに直接アクセスせず、この値を使用する
        /// これにより、バックグラウンドスレッドからのSyncCameraPlusPath()呼び出し時に
        /// _currentSongKeyが別の曲/難易度に変わっていても正しいプロファイルが適用される
        /// </summary>
        public string ResolvedProfileName { get; private set; } = string.Empty;

        /// <summary>
        /// Beat Saberの既知ファイル名（スキップ対象）
        /// </summary>
        private readonly HashSet<string> _skipFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "info.dat",
            "BPMInfo.dat",
            "cinema-video.json"
        };

        public CameraSongScriptDetector()
        {
            Instance = this;
        }

        public void ProcessLevel(IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                if (customLevel.customLevelPath != _latestSelectedSong)
                {
                    _latestSelectedSong = customLevel.customLevelPath;
#if DEBUG
                    Plugin.Log.Notice($"Selected CustomLevel Path:\n {customLevel.customLevelPath}");
#endif
                    RequestScan(customLevel.customLevelPath, customLevel.levelID);
                }
            }
        }

        public void ReevaluateCurrentLevel()
        {
            if (string.IsNullOrEmpty(CurrentLevelPath) || string.IsNullOrEmpty(_currentLevelId))
                return;

            RequestScan(CurrentLevelPath, _currentLevelId);
        }

        /// <summary>
        /// 非同期スキャンを開始する。前回のスキャンが実行中であればキャンセルする
        /// </summary>
        private void StartScanAsync(string levelPath, string levelId)
        {
            // 即座に状態をクリア（メインスレッド上で実行）
            // 共有リストをClear()せず新規リストを代入することで、他スレッドが旧リストを安全に参照し続けられる
            SelectedScriptPath = string.Empty;
            SelectedScriptDisplayName = string.Empty;
            AvailableScriptFiles = new List<string>();
            _candidateMap = new Dictionary<string, ScriptCandidate>();
            CurrentMetadata = null;
            ResolvedCommonMetadata = null;
            SelectedScriptContainsCameraEffect = false;
            SelectedScriptContainsWindowControl = false;
            IsUsingCommonScript = false;
            ResolvedCommonScriptPath = string.Empty;
            ResolvedCommonScriptDisplayName = string.Empty;
            ResolvedCommonScriptContainsCameraEffect = false;
            ResolvedCommonScriptContainsWindowControl = false;

            // CameraPlusモード: 前の曲のPending pathをクリアし、スキャン完了前にゲーム開始された場合に
            // 前の曲のスクリプトが適用されるのを防ぐ
            SyncCameraPlusPath();

            CancellationToken ct;
            lock (_scanLock)
            {
                // 前回のスキャンをキャンセル
                _scanCts?.Cancel();
                _scanCts?.Dispose();
                _scanCts = new CancellationTokenSource();
                ct = _scanCts.Token;
            }

            string configuredSelectedScript = CameraSongScriptConfig.Instance.SelectedScriptFile;
            Task.Run(() =>
            {
                try
                {
                    ScanResult scanResult = CollectScanResult(levelPath, levelId, configuredSelectedScript, ct);
                    if (scanResult == null || ct.IsCancellationRequested)
                        return;

                    ApplyScanResultOnMainThread(levelPath, levelId, scanResult, ct);
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
        /// 譜面フォルダとSongScriptsフォルダの両方をスキャンし、有効なカメラスクリプトを検出する（バックグラウンドスレッド実行）
        /// </summary>
        private void ScanForScriptFiles(string levelPath, string levelId, CancellationToken ct)
        {
            // --- Phase 1: 譜面フォルダスキャン ---
            var chartCandidates = new List<ScriptCandidate>();

            if (Directory.Exists(levelPath))
            {
                string[] jsonFiles;
                try
                {
                    jsonFiles = Directory.GetFiles(levelPath, "*.json");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CameraSongScriptDetector: Failed to scan directory: {ex.Message}");
                    jsonFiles = new string[0];
                }

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
                        chartCandidates.Add(new ScriptCandidate
                        {
                            DisplayName = fileName,
                            FilePath = filePath,
                            Source = ScriptSource.ChartFolder
                        });
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            // --- Phase 2: SongScriptsフォルダからmapId/hashマッチング ---
            var ssCandidates = new List<ScriptCandidate>();
            SongScriptLevelReference levelReference = ResolveSongScriptLevelReference(levelId);
#if DEBUG
            Plugin.Log.Debug(
                $"CameraSongScriptDetector: Level selected. Resolved mapId/hash from LevelId '{levelId}': " +
                $"{levelReference.MapId ?? "(null)"}/{levelReference.Hash ?? "(null)"}");
#endif

            if (levelReference.HasAnyValue && SongScriptFolderCache.IsReady)
            {
                var entries = SongScriptFolderCache.GetScriptsByLevelReference(levelReference.MapId, levelReference.Hash);
                foreach (var entry in entries)
                {
                    ct.ThrowIfCancellationRequested();

                    string displayName = FormatSongScriptDisplayName(entry);

                    // 重複する表示名を避ける
                    if (chartCandidates.Any(c => c.DisplayName == displayName) ||
                        ssCandidates.Any(c => c.DisplayName == displayName))
                        continue;

                    ssCandidates.Add(new ScriptCandidate
                    {
                        DisplayName = displayName,
                        FilePath = entry.FilePath,
                        ZipEntryName = entry.ZipEntryName,
                        Source = ScriptSource.SongScriptFolder,
                        Metadata = entry.Metadata
                    });
                }
            }

            ct.ThrowIfCancellationRequested();

            // --- Phase 3: 結果のマージ ---
            var allCandidates = new List<ScriptCandidate>();
            allCandidates.AddRange(chartCandidates);
            allCandidates.AddRange(ssCandidates);

            var newCandidateMap = new Dictionary<string, ScriptCandidate>();
            var displayNames = new List<string>();
            foreach (var c in allCandidates)
            {
                newCandidateMap[c.DisplayName] = c;
                displayNames.Add(c.DisplayName);
            }

            ct.ThrowIfCancellationRequested();

            string selectedDisplayName;
            string selectedPath = SelectDefaultScript(displayNames, newCandidateMap, out selectedDisplayName);

            int chartCount = chartCandidates.Count;
            int ssCount = ssCandidates.Count;
            int totalCount = chartCount + ssCount;
            if (totalCount > 0)
            {
                string logName = string.IsNullOrEmpty(selectedDisplayName) ? "?" : selectedDisplayName;
                if (ssCount > 0)
                    Plugin.Log.Info($"CameraSongScriptDetector: Found {totalCount} valid script(s) ({chartCount} chart + {ssCount} SS). Selected: {logName}");
                else
                    Plugin.Log.Info($"CameraSongScriptDetector: Found {totalCount} valid script(s). Selected: {logName}");
            }

            ct.ThrowIfCancellationRequested();

            _candidateMap = newCandidateMap;
            SelectedScriptPath = selectedPath;
            SelectedScriptDisplayName = selectedDisplayName ?? string.Empty;
            AvailableScriptFiles = displayNames;

            if (!string.IsNullOrEmpty(SelectedScriptPath))
            {
                // 個別保存モードの場合のみ、スクリプトハッシュから保存済みオフセットを復元する
                if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
                {
                    int savedOffset = ScriptOffsetManager.GetOffsetForScript(SelectedScriptPath);
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = savedOffset;
                }
                // 共通モードの場合はCameraSongScriptConfig.Instance.CameraHeightOffsetCmをそのまま使用
            }
            else
            {
                if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
                {
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = 0;
                }
            }

            LoadSelectedScriptInfo(SelectedScriptPath);

            // 一時ファイルの再生成
            UpdateEffectiveScriptPath();

            // --- Phase 4: 汎用スクリプト判定 ---
            DetermineCommonScriptUsage(allCandidates.Count);

            // CameraPlusモード時はパスを反映
            SyncCameraPlusPath();

            // UIに通知
            ScanCompleted?.Invoke();
        }

        /// <summary>
        /// デフォルトスクリプトを選択する（候補マップを使用してパス解決）
        /// </summary>
        private string SelectDefaultScript(List<string> validFiles, Dictionary<string, ScriptCandidate> candidateMap, out string selectedDisplayName)
        {
            if (validFiles.Count == 0)
            {
                selectedDisplayName = string.Empty;
                return string.Empty;
            }

            string configFileName = CameraSongScriptConfig.Instance.SelectedScriptFile;

            // 1. Configに記録されている表示名が存在すれば優先
            if (!string.IsNullOrEmpty(configFileName) && validFiles.Contains(configFileName) && candidateMap.ContainsKey(configFileName))
            {
                selectedDisplayName = configFileName;
                return ResolveScriptPath(candidateMap[configFileName]);
            }

            // 2. なければ "SongScript.json" を探す（譜面フォルダのデフォルト名）
            if (validFiles.Contains("SongScript.json") && candidateMap.ContainsKey("SongScript.json"))
            {
                CameraSongScriptConfig.Instance.SelectedScriptFile = "SongScript.json";
                selectedDisplayName = "SongScript.json";
                return ResolveScriptPath(candidateMap["SongScript.json"]);
            }

            // 3. どちらもなければ先頭のファイルを選択
            string fallbackName = validFiles[0];
            CameraSongScriptConfig.Instance.SelectedScriptFile = fallbackName;
            selectedDisplayName = fallbackName;
            return ResolveScriptPath(candidateMap[fallbackName]);
        }

        /// <summary>
        /// ScriptCandidateからFile.ReadAllText等で使えるファイルパスに解決する
        /// zipエントリの場合は一時ファイルに展開する
        /// </summary>
        private string ResolveScriptPath(ScriptCandidate candidate)
        {
            if (candidate.Source == ScriptSource.ChartFolder)
            {
                return candidate.FilePath;
            }

            // SongScriptsフォルダ: raw jsonファイル
            if (!candidate.IsZipEntry)
            {
                return candidate.FilePath;
            }

            // SongScriptsフォルダ: zipエントリ → 一時ファイルに展開
            return ExtractZipEntryToTemp(candidate.FilePath, candidate.ZipEntryName);
        }

        /// <summary>
        /// zipエントリを一時ファイルに展開し、そのパスを返す
        /// </summary>
        private string ExtractZipEntryToTemp(string zipPath, string entryName)
        {
            string tempPath = GetTempScriptPath("Temp_ZipScript", zipPath, entryName);

            using (var zip = ZipFile.OpenRead(zipPath))
            {
                var entry = zip.GetEntry(entryName);
                if (entry == null)
                    throw new FileNotFoundException($"Zip entry '{entryName}' not found in '{zipPath}'");

                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    File.WriteAllText(tempPath, content);
                }
            }

            Plugin.Log.Info($"CameraSongScriptDetector: Extracted zip entry '{entryName}' to temp file.");
            return tempPath;
        }

        private string GetTempScriptPath(string prefix, params string[] identityParts)
        {
            string tempDir = Path.Combine(UnityGame.UserDataPath, "CameraSongScript");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            string identity = string.Join("|", identityParts.Select(part => part ?? string.Empty));
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(identity));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return Path.Combine(tempDir, $"{prefix}_{sb}.json");
            }
        }

        /// <summary>
        /// levelIDからSongScripts照合用のmapId/hashを解決する
        /// </summary>
        private SongScriptLevelReference ResolveSongScriptLevelReference(string levelId)
        {
            SongScriptLevelReference levelReference = SongScriptMapIdResolver.ResolveLevelReferenceFromLevelId(levelId);
#if DEBUG
            if (levelReference.HasAnyValue)
            {
                Plugin.Log.Notice(
                    $"CameraSongScriptDetector: Resolved mapId/hash '{levelReference.MapId}'/'{levelReference.Hash}' from LevelId '{levelId}'");
            }
#endif
            return levelReference;
        }

        /// <summary>
        /// SongScriptsフォルダのエントリからUI表示名を生成する
        /// </summary>
        private string FormatSongScriptDisplayName(SongScriptEntry entry)
        {
            if (entry.IsZipEntry)
            {
                string zipName = Path.GetFileNameWithoutExtension(entry.FilePath);
                return $"[SS] {zipName}/{entry.ZipEntryName}";
            }
            return $"[SS] {entry.FileName}";
        }

        /// <summary>
        /// .jsonファイルが有効なMovementScript形式かどうかを検証する
        /// </summary>
        private bool IsValidMovementScript(string filePath)
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
        /// 選択中のスクリプトファイルを変更する（表示名で指定）
        /// </summary>
        public void UpdateSelectedScript(string displayName)
        {
            if (!AvailableScriptFiles.Contains(displayName))
                return;

            if (!_candidateMap.TryGetValue(displayName, out var candidate))
                return;

            try
            {
                SelectedScriptPath = ResolveScriptPath(candidate);
                SelectedScriptDisplayName = displayName;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraSongScriptDetector: Failed to resolve script path for '{displayName}': {ex.Message}");
                SelectedScriptPath = string.Empty;
                SelectedScriptDisplayName = string.Empty;
                return;
            }

            Plugin.Log.Info($"CameraSongScriptDetector: Script selection changed to: {displayName}");

            LoadSelectedScriptInfo(SelectedScriptPath);

            // 個別保存モードの場合のみ、スクリプト変更時にハッシュから保存済みオフセットを復元する
            if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
            {
                int savedOffset = ScriptOffsetManager.GetOffsetForScript(SelectedScriptPath);
                CameraSongScriptConfig.Instance.CameraHeightOffsetCm = savedOffset;
            }
            // 共通モードの場合はCameraSongScriptConfig.Instance.CameraHeightOffsetCmをそのまま使用

            UpdateEffectiveScriptPath();
            SyncCameraPlusPath();

            // Configに記録（表示名を保存）
            CameraSongScriptConfig.Instance.SelectedScriptFile = displayName;
        }

        /// <summary>
        /// CameraPlusモード時にスクリプトパスを同期する
        /// Enabled=falseの場合は空パスを設定し、CameraPlusにスクリプトが存在しないものとして扱わせる
        /// 汎用スクリプト使用時はForceCommonScript=ONならEnabled無視で適用
        /// プロファイル名はResolvedProfileNameを使用し、SongSettingsManagerへの直接アクセスを避ける
        /// </summary>
        public void SyncCameraPlusPath()
        {
            if (CameraModDetector.IsCameraPlus && Plugin.IsCamPlusHelperReady)
            {
                string profileToSet = string.Empty;

                // 1. スクリプトパスの決定と同期
                if (IsUsingCommonScript && !string.IsNullOrEmpty(ResolvedCommonScriptPath))
                {
                    string commonEffectivePath = GenerateCommonOffsetScript(ResolvedCommonScriptPath);
                    Plugin.CamPlusHelper.SetScriptPath(commonEffectivePath);

                    // 汎用スクリプト用プロファイル
                    profileToSet = CameraSongScriptConfig.Instance.CommonScriptProfile;
                    // "Same as SongScript" (空文字) の場合は、後続の通常スクリプト判定に任せるためここでは何もしない
                }
                else if (HasSongScript && CameraSongScriptConfig.Instance.Enabled)
                {
                    Plugin.CamPlusHelper.SetScriptPath(EffectiveScriptPath);
                }
                else
                {
                    Plugin.CamPlusHelper.SetScriptPath(string.Empty);
                }

                // 2. プロファイル名の決定
                // 汎用スクリプト側で指定がない、または汎用スクリプト未使用時はResolvedProfileNameを使用
                if (string.IsNullOrEmpty(profileToSet))
                {
                    profileToSet = ResolvedProfileName;
                }

                // "(NoChange)" または未設定: プロファイルを変更しない（CameraPlusの現在の設定を維持）
                if (!string.IsNullOrEmpty(profileToSet) && profileToSet != "(NoChange)")
                {
                    // "(Delete)": プロファイルを空にする
                    if (profileToSet == "(Delete)")
                    {
                        profileToSet = string.Empty;
                    }

                    Plugin.CamPlusHelper.SetSongSpecificScriptProfile(profileToSet);
                }
            }
        }

        /// <summary>
        /// グローバル設定からプロファイル名を解決してResolvedProfileNameにキャッシュする
        /// </summary>
        public void ResolveProfileName()
        {
            ResolvedProfileName = CameraSongScriptConfig.Instance.SongScriptProfile ?? string.Empty;
        }

        /// <summary>
        /// プロファイル名を直接設定する（UI操作時やキー未使用の場合に使用）
        /// </summary>
        public void SetResolvedProfileName(string profileName)
        {
            ResolvedProfileName = profileName ?? string.Empty;
        }

        /// <summary>
        /// 汎用スクリプトの使用を再判定する（UI設定変更時に呼ばれる）
        /// 現在のAvailableScriptFilesの数をsongScriptCountとして使用
        /// </summary>
        public void ReevaluateCommonScriptUsage()
        {
            int songScriptCount = AvailableScriptFiles?.Count ?? 0;
            DetermineCommonScriptUsage(songScriptCount);
            SyncCameraPlusPath();
        }

        /// <summary>
        /// 汎用スクリプトの使用を判定し、CameraPlusモードの場合はパスを即時解決する
        /// </summary>
        private void DetermineCommonScriptUsage(int songScriptCount)
        {
            var config = CameraSongScriptConfig.Instance;
            IsUsingCommonScript = false;
            ResolvedCommonScriptPath = string.Empty;
            ResolvedCommonScriptDisplayName = string.Empty;
            ResolvedCommonMetadata = null;

            if (!CommonScriptCache.IsReady || CommonScriptCache.Scripts.Count == 0)
                return;

            // ForceCommonScript=ON → SongScriptの有無やEnabled設定に関係なく強制使用
            if (config.ForceCommonScript)
            {
                IsUsingCommonScript = true;
            }
            // SongScript無し && Fallback=ON && Enabled=ON
            else if (songScriptCount == 0 && config.UseCommonScriptAsFallback && config.Enabled)
            {
                IsUsingCommonScript = true;
            }

            if (!IsUsingCommonScript)
                return;

            // CameraPlusモードでは曲選択時にパスを解決する（プレイ終了時に再抽選される）
            // Camera2モードでは指定スクリプトのみ即時解決（ランダムはプレイ開始時に解決）
            if (CameraModDetector.IsCameraPlus || config.SelectedCommonScript != UiLocalization.OptionRandom)
            {
                ResolveAndSetCommonScriptPath();
            }
            // Camera2 + Random の場合は空のまま（プレイ開始時に解決）

            if (IsUsingCommonScript)
            {
                // 汎用スクリプトのオフセット復元
                if (config.UsePerScriptHeightOffset)
                {
                    if (!string.IsNullOrEmpty(ResolvedCommonScriptPath))
                    {
                        // パス確定済み（非ランダム、またはCameraPlusランダム解決済み）: ハッシュ対応表から復元
                        int savedOffset = ScriptOffsetManager.GetOffsetForScript(ResolvedCommonScriptPath);
                        config.CameraHeightOffsetCm = savedOffset;
                    }
                    else
                    {
                        // パス未確定（Camera2ランダム等）: プレイ開始時まで対象スクリプト不明なのでUI上は0にする
                        config.CameraHeightOffsetCm = 0;
                    }
                }

                string displayInfo = config.SelectedCommonScript == UiLocalization.OptionRandom
                    ? UiLocalization.OptionRandom
                    : ResolvedCommonScriptDisplayName;
                Plugin.Log.Info($"CameraSongScriptDetector: Common script will be used: {displayInfo}");
            }
        }

        /// <summary>
        /// 汎用スクリプトのパスを解決する（ランダムまたは指定ファイル）
        /// CameraPlusモード: 曲選択時に呼ばれる
        /// Camera2モード: プレイ開始時にControllerから呼ばれる
        /// </summary>
        public void ResolveAndSetCommonScriptPath()
        {
            var config = CameraSongScriptConfig.Instance;
            ResolvedCommonScriptPath = string.Empty;
            ResolvedCommonScriptDisplayName = string.Empty;
            ResolvedCommonMetadata = null;
            ResolvedCommonScriptContainsCameraEffect = false;
            ResolvedCommonScriptContainsWindowControl = false;

            if (config.SelectedCommonScript == UiLocalization.OptionRandom)
            {
                var entry = CommonScriptCache.GetRandom();
                if (entry != null)
                {
                    ResolvedCommonScriptPath = entry.FilePath;
                    ResolvedCommonScriptDisplayName = entry.DisplayName;
                    Plugin.Log.Info($"CameraSongScriptDetector: Random common script selected: {entry.DisplayName}");
                }
            }
            else
            {
                string path = CommonScriptCache.GetPathByDisplayName(config.SelectedCommonScript);
                if (!string.IsNullOrEmpty(path))
                {
                    ResolvedCommonScriptPath = path;
                    ResolvedCommonScriptDisplayName = config.SelectedCommonScript;
                }
            }

            LoadResolvedCommonScriptCompatibility(ResolvedCommonScriptPath);
        }

        public string GetSelectedScriptFileName()
        {
            if (!string.IsNullOrEmpty(SelectedScriptDisplayName) &&
                _candidateMap.TryGetValue(SelectedScriptDisplayName, out var candidate))
            {
                return GetScriptFileName(candidate);
            }

            return string.IsNullOrEmpty(SelectedScriptPath) ? string.Empty : Path.GetFileName(SelectedScriptPath);
        }

        public string GetResolvedCommonScriptFileName()
        {
            return string.IsNullOrEmpty(ResolvedCommonScriptPath)
                ? string.Empty
                : Path.GetFileName(ResolvedCommonScriptPath);
        }

        /// <summary>
        /// 状態をリセットする
        /// </summary>
        public void Reset()
        {
            lock (_scanLock)
            {
                _scanCts?.Cancel();
                _scanCts?.Dispose();
                _scanCts = null;
            }
            _latestSelectedSong = string.Empty;
            _currentLevelId = string.Empty;
            CurrentLevelPath = string.Empty;
            SelectedScriptPath = string.Empty;
            SelectedScriptDisplayName = string.Empty;
            EffectiveScriptPath = string.Empty;
            CurrentMetadata = null;
            ResolvedCommonMetadata = null;
            SelectedScriptContainsCameraEffect = false;
            SelectedScriptContainsWindowControl = false;
            AvailableScriptFiles = new List<string>();
            _candidateMap = new Dictionary<string, ScriptCandidate>();
            IsUsingCommonScript = false;
            ResolvedCommonScriptPath = string.Empty;
            ResolvedCommonScriptDisplayName = string.Empty;
            ResolvedCommonScriptContainsCameraEffect = false;
            ResolvedCommonScriptContainsWindowControl = false;
            ResolvedProfileName = string.Empty;
        }

        private void LoadSelectedScriptInfo(string filePath)
        {
            CurrentMetadata = null;
            SelectedScriptContainsCameraEffect = false;
            SelectedScriptContainsWindowControl = false;

            if (!TryLoadScriptInfo(filePath, "selected script", out var parsed))
                return;

            CurrentMetadata = parsed.metadata;
            PopulateUnsupportedFeatureFlags(parsed, out var containsCameraEffect, out var containsWindowControl);
            SelectedScriptContainsCameraEffect = containsCameraEffect;
            SelectedScriptContainsWindowControl = containsWindowControl;
        }

        private void LoadResolvedCommonScriptCompatibility(string filePath)
        {
            ResolvedCommonMetadata = null;
            ResolvedCommonScriptContainsCameraEffect = false;
            ResolvedCommonScriptContainsWindowControl = false;

            if (!TryLoadScriptInfo(filePath, "common script", out var parsed))
                return;

            ResolvedCommonMetadata = parsed.metadata;
            PopulateUnsupportedFeatureFlags(parsed, out var containsCameraEffect, out var containsWindowControl);
            ResolvedCommonScriptContainsCameraEffect = containsCameraEffect;
            ResolvedCommonScriptContainsWindowControl = containsWindowControl;
        }

        private static string GetScriptFileName(ScriptCandidate candidate)
        {
            if (candidate == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(candidate.ZipEntryName))
                return Path.GetFileName(candidate.ZipEntryName);

            return string.IsNullOrEmpty(candidate.FilePath) ? string.Empty : Path.GetFileName(candidate.FilePath);
        }

        private bool TryLoadScriptInfo(string filePath, string scriptLabel, out MovementScriptJson parsed)
        {
            parsed = null;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                return parsed != null;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"CameraSongScriptDetector: Failed to load {scriptLabel} info from '{filePath}': {ex.Message}");
                return false;
            }
        }

        private void PopulateUnsupportedFeatureFlags(
            MovementScriptJson parsed,
            out bool containsCameraEffect,
            out bool containsWindowControl)
        {
            containsCameraEffect = false;
            containsWindowControl = false;

            if (parsed?.JsonMovements == null)
                return;

            foreach (var movement in parsed.JsonMovements)
            {
                if (!containsCameraEffect && movement?.cameraEffect != null)
                    containsCameraEffect = true;

                if (!containsWindowControl && movement?.windowControl != null)
                    containsWindowControl = true;

                if (containsCameraEffect && containsWindowControl)
                    return;
            }
        }

        /// <summary>
        /// オフセット指定がある場合に一時ファイル（Temp_OffsetScript.json）を生成する
        /// </summary>
        public void UpdateEffectiveScriptPath()
        {
            if (string.IsNullOrEmpty(SelectedScriptPath) || !File.Exists(SelectedScriptPath))
            {
                EffectiveScriptPath = string.Empty;
                return;
            }

            EffectiveScriptPath = GenerateOffsetScript(
                SelectedScriptPath,
                "Temp_OffsetScript",
                "offset script");
        }

        /// <summary>
        /// 汎用スクリプト用: オフセットがある場合に一時ファイルを生成し、そのパスを返す。
        /// オフセットが0またはCameraPlusモードでない場合は元のパスをそのまま返す。
        /// </summary>
        private string GenerateCommonOffsetScript(string commonScriptPath)
        {
            return GenerateOffsetScript(
                commonScriptPath,
                "Temp_CommonOffsetScript",
                "common offset script");
        }

        private string GenerateOffsetScript(string sourcePath, string tempPrefix, string scriptLabel)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return sourcePath;

            int offsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            if (offsetCm == 0 || !CameraModDetector.IsCameraPlus)
                return sourcePath;

            float offsetMeters = offsetCm / 100f;

            try
            {
                string json = File.ReadAllText(sourcePath);
                var movementScript = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                if (movementScript?.JsonMovements == null)
                    return sourcePath;

                foreach (var movement in movementScript.JsonMovements)
                {
                    if (movement.startPos != null &&
                        !string.IsNullOrEmpty(movement.startPos.y) &&
                        NumericStringParser.TryParse(movement.startPos.y, out float startY))
                    {
                        movement.startPos.y = (startY + offsetMeters).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (movement.endPos != null &&
                        !string.IsNullOrEmpty(movement.endPos.y) &&
                        NumericStringParser.TryParse(movement.endPos.y, out float endY))
                    {
                        movement.endPos.y = (endY + offsetMeters).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                string tempFilePath = GetTempScriptPath(tempPrefix, sourcePath, offsetCm.ToString());
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                string modifiedJson = JsonConvert.SerializeObject(movementScript, settings);
                File.WriteAllText(tempFilePath, modifiedJson);
                Plugin.Log.Info($"CameraSongScriptDetector: Generated temporary {scriptLabel} ({offsetMeters}m) at {tempFilePath}");
                return tempFilePath;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraSongScriptDetector: Failed to generate temporary {scriptLabel}: {ex.Message}");
                return sourcePath;
            }
        }
}



}
