using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;
using Zenject;

namespace CameraSongScript.Services
{
    /// <summary>
    /// BetterSongList 用に譜面フォルダ内 SongScript の有無を索引化する。
    /// UserData/CameraSongScript/SongScripts は既存の SongScriptFolderCache を参照する。
    /// </summary>
    internal enum BeatmapSongScriptCacheScanState
    {
        Idle,
        Scanning,
        Completed,
        Failed
    }

    public class SongScriptBeatmapIndexService : IInitializable, IDisposable
    {
        private const int CacheVersion = 1;
        private const string CacheFormatVersion = "1.0";

        private sealed class BeatmapWorkItem
        {
            public string FolderPath { get; set; }
            public string Hash { get; set; }
        }

        private sealed class RuntimeState
        {
            public string Hash;
            public volatile bool ChartFolderScanComplete;
            public volatile bool HasChartFolderSongScript;
        }

        private sealed class CacheDocument
        {
            public int Version { get; set; } = CacheVersion;
            public string FormatVersion { get; set; } = CacheFormatVersion;
            public List<CacheEntry> Entries { get; set; } = new List<CacheEntry>();
        }

        private sealed class CacheEntry
        {
            public string FolderPath { get; set; }
            public string Hash { get; set; }
            public bool HasChartFolderSongScript { get; set; }
            public List<CachedJsonFileInfo> JsonFiles { get; set; } = new List<CachedJsonFileInfo>();
        }

        private sealed class CachedJsonFileInfo
        {
            public string RelativePath { get; set; }
            public long LastWriteUtcTicks { get; set; }
            public long Size { get; set; }
        }

        private static readonly string CacheFilePath =
            Path.Combine(UnityGame.UserDataPath, "CameraSongScript", "BeatmapSongScriptCache.json");

        private readonly object _cacheLock = new object();
        private readonly HashSet<string> _skipFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "info.dat",
            "BPMInfo.dat",
            "cinema-video.json"
        };

        private Dictionary<string, CacheEntry> _persistentCache =
            new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        private ConcurrentDictionary<string, RuntimeState> _statesByFolderPath =
            new ConcurrentDictionary<string, RuntimeState>(StringComparer.OrdinalIgnoreCase);

        private ConcurrentDictionary<string, bool> _songScriptsFolderResultsByLookupKey =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource _scanCts;
        private Task _scanTask = Task.CompletedTask;
        private volatile bool _pausedForGameplay;
        private volatile bool _disposed;
        private volatile bool _canFilter;
        private volatile bool _isScanning;
        private BeatmapSongScriptCacheScanState _scanState = BeatmapSongScriptCacheScanState.Idle;
        private int _processedBeatmapFolderCount;
        private int _totalBeatmapFolderCount;
        private string _lastScanErrorMessage = string.Empty;
        private long _scanGeneration;

        public static SongScriptBeatmapIndexService Instance { get; private set; }

        public bool CanFilter => _canFilter && !_disposed;
        public bool IsScanning => _isScanning;
        internal BeatmapSongScriptCacheScanState ScanState => _scanState;
        public int ProcessedBeatmapFolderCount => _processedBeatmapFolderCount;
        public int TotalBeatmapFolderCount => _totalBeatmapFolderCount;
        public string LastScanErrorMessage => _lastScanErrorMessage ?? string.Empty;

        public event Action ScanStatusChanged;

        public void Initialize()
        {
            Instance = this;

            LoadPersistentCache();
            Plugin.EnsureBetterSongListHelperLoaded();

            SongCore.Loader.LoadingStartedEvent += HandleLoadingStarted;
            SongCore.Loader.SongsLoadedEvent += HandleSongsLoaded;

            if (SongCore.Loader.AreSongsLoaded && !SongCore.Loader.AreSongsLoading)
            {
                RefreshIndex();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _canFilter = false;

            SongCore.Loader.LoadingStartedEvent -= HandleLoadingStarted;
            SongCore.Loader.SongsLoadedEvent -= HandleSongsLoaded;

            Interlocked.Increment(ref _scanGeneration);
            CancelCurrentScan();
            SavePersistentCache();
            UpdateScanStatus(BeatmapSongScriptCacheScanState.Idle, 0, 0, string.Empty, _scanGeneration);

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        public void PauseForGameplay()
        {
            _pausedForGameplay = true;
        }

        public void ResumeFromGameplay()
        {
            _pausedForGameplay = false;
        }

        public bool HasAnySongScript(IPreviewBeatmapLevel level)
        {
            if (!CanFilter || !(level is CustomPreviewBeatmapLevel customLevel))
            {
                return false;
            }

            string folderPath = NormalizeFolderPath(customLevel.customLevelPath);
            if (!string.IsNullOrEmpty(folderPath) &&
                _statesByFolderPath.TryGetValue(folderPath, out var state) &&
                state.ChartFolderScanComplete &&
                state.HasChartFolderSongScript)
            {
                return true;
            }

            return HasSongScriptsFolderScript(level.levelID, customLevel.customLevelPath);
        }

        private void HandleLoadingStarted(SongCore.Loader _)
        {
            long generation = Interlocked.Increment(ref _scanGeneration);
            _canFilter = false;
            CancelCurrentScan();

            _statesByFolderPath = new ConcurrentDictionary<string, RuntimeState>(StringComparer.OrdinalIgnoreCase);
            _songScriptsFolderResultsByLookupKey =
                new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            UpdateScanStatus(BeatmapSongScriptCacheScanState.Idle, 0, 0, string.Empty, generation);
        }

        private void HandleSongsLoaded(SongCore.Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> __)
        {
            RefreshIndex();
        }

        public Task RefreshIndexAsync()
        {
            RefreshIndex();
            return _scanTask ?? Task.CompletedTask;
        }

        private void RefreshIndex()
        {
            if (_disposed)
            {
                return;
            }

            long generation = Interlocked.Increment(ref _scanGeneration);
            List<BeatmapWorkItem> workItems = CaptureLoadedBeatmaps();
            var nextStates = new ConcurrentDictionary<string, RuntimeState>(StringComparer.OrdinalIgnoreCase);

            foreach (BeatmapWorkItem item in workItems)
            {
                nextStates[item.FolderPath] = new RuntimeState
                {
                    Hash = item.Hash ?? string.Empty
                };
            }

            _statesByFolderPath = nextStates;
            _songScriptsFolderResultsByLookupKey =
                new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            _canFilter = true;
            UpdateScanStatus(BeatmapSongScriptCacheScanState.Scanning, 0, workItems.Count, string.Empty, generation);

            Plugin.Log.Info($"SongScriptBeatmapIndexService: Indexed {workItems.Count} beatmap folder(s) for background scanning.");

            StartScan(workItems, nextStates, generation);
        }

        private void StartScan(
            IReadOnlyList<BeatmapWorkItem> workItems,
            ConcurrentDictionary<string, RuntimeState> targetStates,
            long generation)
        {
            CancelCurrentScan();

            if (workItems.Count == 0)
            {
                if (TrimPersistentCache(Array.Empty<string>()))
                {
                    SavePersistentCache();
                }

                _scanTask = Task.CompletedTask;
                UpdateScanStatus(BeatmapSongScriptCacheScanState.Completed, 0, 0, string.Empty, generation);
                return;
            }

            _scanCts = new CancellationTokenSource();
            CancellationToken token = _scanCts.Token;
            _scanTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await ProcessWorkItemsAsync(workItems, targetStates, generation, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // 新しいスキャンに置き換わった場合のキャンセルは正常系
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warn($"SongScriptBeatmapIndexService: Background scan failed: {ex.Message}");
                        UpdateScanStatus(
                            BeatmapSongScriptCacheScanState.Failed,
                            _processedBeatmapFolderCount,
                            _totalBeatmapFolderCount,
                            ex.Message,
                            generation);
                    }
                },
                token);
        }

        private void CancelCurrentScan()
        {
            var cts = _scanCts;
            if (cts == null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch
            {
                // ignored
            }

            cts.Dispose();
            _scanCts = null;
        }

        private async Task ProcessWorkItemsAsync(
            IReadOnlyList<BeatmapWorkItem> workItems,
            ConcurrentDictionary<string, RuntimeState> targetStates,
            long generation,
            CancellationToken token)
        {
            int changedEntriesSinceSave = 0;
            int processedBeatmapFolderCount = 0;

            foreach (BeatmapWorkItem item in workItems)
            {
                ThrowIfScanStopped(generation, token);
                await WaitWhilePausedAsync(generation, token).ConfigureAwait(false);

                IReadOnlyList<CachedJsonFileInfo> currentFiles = CaptureCurrentJsonFiles(item.FolderPath);

                bool hasChartFolderSongScript;
                if (!TryReusePersistentCache(item, currentFiles, out hasChartFolderSongScript))
                {
                    hasChartFolderSongScript = ScanChartFolder(item.FolderPath, currentFiles);
                }

                ThrowIfScanStopped(generation, token);

                if (targetStates.TryGetValue(item.FolderPath, out var state))
                {
                    state.Hash = item.Hash ?? string.Empty;
                    state.HasChartFolderSongScript = hasChartFolderSongScript;
                    state.ChartFolderScanComplete = true;
                }

                if (UpdatePersistentCache(item, currentFiles, hasChartFolderSongScript))
                {
                    changedEntriesSinceSave++;
                    if (changedEntriesSinceSave >= 25)
                    {
                        SavePersistentCache();
                        changedEntriesSinceSave = 0;
                    }
                }

                processedBeatmapFolderCount++;
                UpdateScanStatus(
                    BeatmapSongScriptCacheScanState.Scanning,
                    processedBeatmapFolderCount,
                    workItems.Count,
                    string.Empty,
                    generation);
            }

            ThrowIfScanStopped(generation, token);

            if (TrimPersistentCache(workItems.Select(item => item.FolderPath)))
            {
                changedEntriesSinceSave++;
            }

            if (changedEntriesSinceSave > 0)
            {
                SavePersistentCache();
            }

            UpdateScanStatus(
                BeatmapSongScriptCacheScanState.Completed,
                workItems.Count,
                workItems.Count,
                string.Empty,
                generation);
            Plugin.Log.Info("SongScriptBeatmapIndexService: Chart folder background scan completed.");
        }

        private async Task WaitWhilePausedAsync(long generation, CancellationToken token)
        {
            while (_pausedForGameplay && !token.IsCancellationRequested)
            {
                ThrowIfScanStopped(generation, token);
                await Task.Delay(250, token).ConfigureAwait(false);
            }
        }

        private List<BeatmapWorkItem> CaptureLoadedBeatmaps()
        {
            var workItems = new Dictionary<string, BeatmapWorkItem>(StringComparer.OrdinalIgnoreCase);

            void AddLevel(CustomPreviewBeatmapLevel level)
            {
                if (level == null || string.IsNullOrEmpty(level.customLevelPath))
                {
                    return;
                }

                string folderPath = NormalizeFolderPath(level.customLevelPath);
                if (string.IsNullOrEmpty(folderPath) || workItems.ContainsKey(folderPath))
                {
                    return;
                }

                string hash = SongCore.Collections.hashForLevelID(level.levelID);
                if (string.IsNullOrEmpty(hash))
                {
                    try
                    {
                        hash = SongCore.Utilities.Hashing.GetCustomLevelHash(level);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Debug($"SongScriptBeatmapIndexService: Failed to resolve hash for '{folderPath}': {ex.Message}");
                        hash = string.Empty;
                    }
                }

                workItems[folderPath] = new BeatmapWorkItem
                {
                    FolderPath = folderPath,
                    Hash = hash ?? string.Empty
                };
            }

            foreach (CustomPreviewBeatmapLevel level in SongCore.Loader.CustomLevels.Values)
            {
                AddLevel(level);
            }

            foreach (CustomPreviewBeatmapLevel level in SongCore.Loader.CustomWIPLevels.Values)
            {
                AddLevel(level);
            }

            foreach (CustomPreviewBeatmapLevel level in SongCore.Loader.CachedWIPLevels.Values)
            {
                AddLevel(level);
            }

            foreach (var separateSongFolder in SongCore.Loader.SeperateSongFolders)
            {
                foreach (CustomPreviewBeatmapLevel level in separateSongFolder.Levels.Values)
                {
                    AddLevel(level);
                }
            }

            return workItems.Values.ToList();
        }

        private IReadOnlyList<CachedJsonFileInfo> CaptureCurrentJsonFiles(string folderPath)
        {
            var files = new List<CachedJsonFileInfo>();

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return files;
            }

            string[] jsonPaths;
            try
            {
                jsonPaths = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongScriptBeatmapIndexService: Failed to enumerate json files in '{folderPath}': {ex.Message}");
                return files;
            }

            foreach (string jsonPath in jsonPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(jsonPath);
                if (_skipFileNames.Contains(fileName))
                {
                    continue;
                }

                try
                {
                    var fileInfo = new FileInfo(jsonPath);
                    files.Add(new CachedJsonFileInfo
                    {
                        RelativePath = fileName,
                        LastWriteUtcTicks = fileInfo.LastWriteTimeUtc.Ticks,
                        Size = fileInfo.Length
                    });
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug($"SongScriptBeatmapIndexService: Failed to inspect '{jsonPath}': {ex.Message}");
                }
            }

            return files;
        }

        private bool TryReusePersistentCache(
            BeatmapWorkItem item,
            IReadOnlyList<CachedJsonFileInfo> currentFiles,
            out bool hasChartFolderSongScript)
        {
            lock (_cacheLock)
            {
                if (_persistentCache.TryGetValue(item.FolderPath, out var entry) &&
                    string.Equals(entry.Hash ?? string.Empty, item.Hash ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                    JsonFileListsMatch(entry.JsonFiles, currentFiles))
                {
                    hasChartFolderSongScript = entry.HasChartFolderSongScript;
                    return true;
                }
            }

            hasChartFolderSongScript = false;
            return false;
        }

        private bool ScanChartFolder(string folderPath, IReadOnlyList<CachedJsonFileInfo> currentFiles)
        {
            foreach (CachedJsonFileInfo fileInfo in currentFiles)
            {
                string filePath = Path.Combine(folderPath, fileInfo.RelativePath);
                if (ContainsSongScriptElement(filePath))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsSongScriptElement(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonConvert.DeserializeObject<Models.MovementScriptJson>(json);
                return parsed?.JsonMovements != null && parsed.JsonMovements.Length > 0;
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongScriptBeatmapIndexService: '{Path.GetFileName(filePath)}' is not a valid SongScript: {ex.Message}");
                return false;
            }
        }

        private bool UpdatePersistentCache(
            BeatmapWorkItem item,
            IReadOnlyList<CachedJsonFileInfo> currentFiles,
            bool hasChartFolderSongScript)
        {
            lock (_cacheLock)
            {
                if (_persistentCache.TryGetValue(item.FolderPath, out var existing) &&
                    string.Equals(existing.Hash ?? string.Empty, item.Hash ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                    existing.HasChartFolderSongScript == hasChartFolderSongScript &&
                    JsonFileListsMatch(existing.JsonFiles, currentFiles))
                {
                    return false;
                }

                _persistentCache[item.FolderPath] = new CacheEntry
                {
                    FolderPath = item.FolderPath,
                    Hash = item.Hash ?? string.Empty,
                    HasChartFolderSongScript = hasChartFolderSongScript,
                    JsonFiles = CloneJsonFiles(currentFiles)
                };

                return true;
            }
        }

        private bool TrimPersistentCache(IEnumerable<string> activeFolderPaths)
        {
            var activeSet = new HashSet<string>(activeFolderPaths, StringComparer.OrdinalIgnoreCase);
            bool removedAny = false;

            lock (_cacheLock)
            {
                foreach (string folderPath in _persistentCache.Keys.ToArray())
                {
                    if (!activeSet.Contains(folderPath))
                    {
                        removedAny = _persistentCache.Remove(folderPath) || removedAny;
                    }
                }
            }

            return removedAny;
        }

        private bool HasSongScriptsFolderScript(string levelId, string levelPath)
        {
            if ((string.IsNullOrEmpty(levelId) && string.IsNullOrEmpty(levelPath)) || !Models.SongScriptFolderCache.IsReady)
            {
                return false;
            }

            if (!Plugin.IsSongDetailsReady)
            {
                return ResolveSongScriptsFolderScriptCore(levelId, levelPath);
            }

            string lookupCacheKey = CreateSongScriptsFolderLookupCacheKey(levelId, levelPath);
            return _songScriptsFolderResultsByLookupKey.GetOrAdd(
                lookupCacheKey,
                _ => ResolveSongScriptsFolderScriptCore(levelId, levelPath));
        }

        private bool ResolveSongScriptsFolderScriptCore(string levelId, string levelPath)
        {
            SongScriptLevelReference levelReference = SongScriptMapIdResolver.ResolveLevelReferenceFromLevelId(levelId, levelPath);
            if (!levelReference.HasAnyValue)
            {
                return false;
            }

            return Models.SongScriptFolderCache.GetScriptsByLevelReference(levelReference.MapId, levelReference.Hash).Count > 0;
        }

        private static string CreateSongScriptsFolderLookupCacheKey(string levelId, string levelPath)
        {
            return $"{levelId ?? string.Empty}|{NormalizeFolderPath(levelPath)}";
        }

        private void LoadPersistentCache()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    return;
                }

                string json = File.ReadAllText(CacheFilePath);
                var document = JsonConvert.DeserializeObject<CacheDocument>(json);
                if (document == null)
                {
                    return;
                }

                if (document.Version != CacheVersion)
                {
                    Plugin.Log.Info(
                        $"SongScriptBeatmapIndexService: Ignoring cache file with unsupported version {document.Version}. Expected {CacheVersion}.");
                    return;
                }

                if (document.Entries == null)
                {
                    return;
                }

                lock (_cacheLock)
                {
                    _persistentCache = document.Entries
                        .Where(entry => !string.IsNullOrEmpty(entry?.FolderPath))
                        .ToDictionary(
                            entry => NormalizeFolderPath(entry.FolderPath),
                            entry => new CacheEntry
                            {
                                FolderPath = NormalizeFolderPath(entry.FolderPath),
                                Hash = entry.Hash ?? string.Empty,
                                HasChartFolderSongScript = entry.HasChartFolderSongScript,
                                JsonFiles = CloneJsonFiles(entry.JsonFiles)
                            },
                            StringComparer.OrdinalIgnoreCase);
                }

                Plugin.Log.Info($"SongScriptBeatmapIndexService: Loaded {_persistentCache.Count} cached beatmap entry(s).");
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptBeatmapIndexService: Failed to load cache: {ex.Message}");
            }
        }

        private void SavePersistentCache()
        {
            CacheDocument snapshot;

            lock (_cacheLock)
            {
                snapshot = new CacheDocument
                {
                    Entries = _persistentCache.Values
                        .OrderBy(entry => entry.FolderPath, StringComparer.OrdinalIgnoreCase)
                        .Select(entry => new CacheEntry
                        {
                            FolderPath = entry.FolderPath,
                            Hash = entry.Hash ?? string.Empty,
                            HasChartFolderSongScript = entry.HasChartFolderSongScript,
                            JsonFiles = CloneJsonFiles(entry.JsonFiles)
                        })
                        .ToList()
                };
            }

            try
            {
                string cacheDirectory = Path.GetDirectoryName(CacheFilePath);
                if (!string.IsNullOrEmpty(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                string tempPath = CacheFilePath + ".tmp";
                string json = JsonConvert.SerializeObject(snapshot, Formatting.None);
                File.WriteAllText(tempPath, json);

                if (File.Exists(CacheFilePath))
                {
                    File.Delete(CacheFilePath);
                }

                File.Move(tempPath, CacheFilePath);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptBeatmapIndexService: Failed to save cache: {ex.Message}");
            }
        }

        private static List<CachedJsonFileInfo> CloneJsonFiles(IEnumerable<CachedJsonFileInfo> source)
        {
            if (source == null)
            {
                return new List<CachedJsonFileInfo>();
            }

            return source
                .Select(file => new CachedJsonFileInfo
                {
                    RelativePath = file.RelativePath ?? string.Empty,
                    LastWriteUtcTicks = file.LastWriteUtcTicks,
                    Size = file.Size
                })
                .ToList();
        }

        private static bool JsonFileListsMatch(
            IReadOnlyList<CachedJsonFileInfo> cachedFiles,
            IReadOnlyList<CachedJsonFileInfo> currentFiles)
        {
            if (cachedFiles == null)
            {
                return currentFiles == null || currentFiles.Count == 0;
            }

            if (currentFiles == null || cachedFiles.Count != currentFiles.Count)
            {
                return false;
            }

            for (int index = 0; index < cachedFiles.Count; index++)
            {
                CachedJsonFileInfo cached = cachedFiles[index];
                CachedJsonFileInfo current = currentFiles[index];

                if (!string.Equals(cached.RelativePath, current.RelativePath, StringComparison.OrdinalIgnoreCase) ||
                    cached.LastWriteUtcTicks != current.LastWriteUtcTicks ||
                    cached.Size != current.Size)
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(folderPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private void ThrowIfScanStopped(long generation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (generation != Interlocked.Read(ref _scanGeneration))
            {
                throw new OperationCanceledException(token);
            }
        }

        private void UpdateScanStatus(
            BeatmapSongScriptCacheScanState state,
            int processedBeatmapFolderCount,
            int totalBeatmapFolderCount,
            string errorMessage,
            long generation)
        {
            if (generation != Interlocked.Read(ref _scanGeneration))
            {
                return;
            }

            _scanState = state;
            _isScanning = state == BeatmapSongScriptCacheScanState.Scanning;
            _processedBeatmapFolderCount = processedBeatmapFolderCount < 0 ? 0 : processedBeatmapFolderCount;
            _totalBeatmapFolderCount = totalBeatmapFolderCount < 0 ? 0 : totalBeatmapFolderCount;
            _lastScanErrorMessage = errorMessage ?? string.Empty;
            NotifyScanStatusChanged();
        }

        private void NotifyScanStatusChanged()
        {
            var handler = ScanStatusChanged;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptBeatmapIndexService: Scan status listener failed: {ex.Message}");
            }
        }
    }
}
