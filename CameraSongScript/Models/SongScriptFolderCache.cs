using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;

namespace CameraSongScript.Models
{
    /// <summary>
    /// SongScriptsフォルダ内のスクリプト情報を保持するエントリ
    /// </summary>
    internal class SongScriptEntry
    {
        /// <summary>ディスク上のフルパス（.jsonまたは.zipファイル）</summary>
        public string FilePath { get; set; }

        /// <summary>zipエントリの場合のエントリ名（非zip時はnull）</summary>
        public string ZipEntryName { get; set; }

        /// <summary>metadata.mapId（小文字正規化済み）</summary>
        public string MapId { get; set; }

        /// <summary>metadata.hash（小文字正規化済み）</summary>
        public string Hash { get; set; }

        /// <summary>表示用ファイル名</summary>
        public string FileName { get; set; }

        /// <summary>キャッシュ済みメタデータ</summary>
        public MetadataElements Metadata { get; set; }

        public bool IsZipEntry => !string.IsNullOrEmpty(ZipEntryName);
    }

    /// <summary>
    /// UserData/CameraSongScript/SongScripts/ フォルダを再帰的にスキャンし、
    /// metadata.mapId をキーとしたインデックスをキャッシュするクラス。
    /// 起動時に一度だけスキャンし、以降は高速にmapIdで検索できる。
    /// </summary>
    internal enum SongScriptFolderCacheScanState
    {
        Idle,
        Scanning,
        Completed,
        Failed
    }

    internal static class SongScriptFolderCache
    {
        private const int CacheVersion = 1;
        private const string CacheFormatVersion = "1.0";

        private sealed class SourceFileInfo
        {
            public string FullPath { get; set; }
            public string RelativePath { get; set; }
            public long LastWriteUtcTicks { get; set; }
            public long Size { get; set; }
            public bool IsZip { get; set; }
        }

        private sealed class CacheDocument
        {
            public int Version { get; set; } = CacheVersion;
            public string FormatVersion { get; set; } = CacheFormatVersion;
            public List<CacheSourceEntry> Entries { get; set; } = new List<CacheSourceEntry>();
        }

        private sealed class CacheSourceEntry
        {
            public string RelativePath { get; set; }
            public long LastWriteUtcTicks { get; set; }
            public long Size { get; set; }
            public bool IsZip { get; set; }
            public List<CacheSongScriptEntry> Scripts { get; set; } = new List<CacheSongScriptEntry>();
        }

        private sealed class CacheSongScriptEntry
        {
            public string ZipEntryName { get; set; }
            public string MapId { get; set; }
            public string Hash { get; set; }
            public string FileName { get; set; }
            public MetadataElements Metadata { get; set; }
        }

        private static readonly string SongScriptFolderPath =
            Path.Combine(UnityGame.UserDataPath, "CameraSongScript", "SongScripts");

        private static readonly string CacheFilePath =
            Path.Combine(UnityGame.UserDataPath, "CameraSongScript", "SongScriptsFolderCache.json");

        private static readonly object _scanLock = new object();
        private static Task _scanTask = Task.CompletedTask;

        /// <summary>mapId（小文字） → エントリ一覧</summary>
        private static Dictionary<string, List<SongScriptEntry>> _mapIdIndex =
            new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>hash（小文字） → エントリ一覧</summary>
        private static Dictionary<string, List<SongScriptEntry>> _hashIndex =
            new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);

        private static bool _isReady = false;
        private static volatile bool _isScanning = false;
        private static SongScriptFolderCacheScanState _scanState = SongScriptFolderCacheScanState.Idle;
        private static int _processedSourceCount = 0;
        private static int _totalSourceCount = 0;
        private static string _lastScanErrorMessage = string.Empty;

        /// <summary>キャッシュの準備が完了しているか</summary>
        public static bool IsReady => _isReady;

        /// <summary>現在スキャン中かどうか</summary>
        public static bool IsScanning => _isScanning;

        /// <summary>現在のスキャン状態</summary>
        public static SongScriptFolderCacheScanState ScanState => _scanState;

        /// <summary>現在のスキャンで処理済みのソース数</summary>
        public static int ProcessedSourceCount => _processedSourceCount;

        /// <summary>現在のスキャン対象ソース総数</summary>
        public static int TotalSourceCount => _totalSourceCount;

        /// <summary>直近のスキャン失敗メッセージ</summary>
        public static string LastScanErrorMessage => _lastScanErrorMessage ?? string.Empty;

        /// <summary>スキャン状態更新通知</summary>
        public static event Action ScanStatusChanged;

        /// <summary>
        /// 非同期でスキャンを開始する（Plugin.OnApplicationStartから呼ばれる）
        /// </summary>
        public static Task ScanAsync()
        {
            lock (_scanLock)
            {
                if (_scanTask != null && !_scanTask.IsCompleted)
                {
                    return _scanTask;
                }

                UpdateScanStatus(SongScriptFolderCacheScanState.Scanning, 0, 0, string.Empty);
                _scanTask = Task.Run(() => RunScanCore());
                return _scanTask;
            }
        }

        private static void RunScanCore()
        {
            try
            {
                Scan();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"SongScriptFolderCache: Scan failed: {ex.Message}");
                UpdateScanStatus(
                    SongScriptFolderCacheScanState.Failed,
                    _processedSourceCount,
                    _totalSourceCount,
                    ex.Message);
            }
        }

        /// <summary>
        /// フォルダを再帰的にスキャンしてインデックスを構築する
        /// </summary>
        private static void Scan()
        {
            bool hadReadyIndex = _isReady;
            if (!hadReadyIndex)
            {
                _isReady = false;
            }

            EnsureSongScriptFolderExists();

            Dictionary<string, CacheSourceEntry> persistentCache = LoadPersistentCache();
            List<SourceFileInfo> currentSourceFiles = CaptureCurrentSourceFiles();
            var nextPersistentCache = new Dictionary<string, CacheSourceEntry>(StringComparer.OrdinalIgnoreCase);
            var mapIdIndex = new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);
            var hashIndex = new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);
            int totalSourceCount = currentSourceFiles.Count;

            int reusedSourceCount = 0;
            int scannedSourceCount = 0;
            int processedSourceCount = 0;

            UpdateScanStatus(SongScriptFolderCacheScanState.Scanning, 0, totalSourceCount, string.Empty);

            foreach (SourceFileInfo sourceFile in currentSourceFiles)
            {
                CacheSourceEntry cacheEntry;
                if (TryReusePersistentCacheEntry(persistentCache, sourceFile, out cacheEntry))
                {
                    reusedSourceCount++;
                }
                else
                {
                    cacheEntry = BuildCacheEntry(sourceFile);
                    scannedSourceCount++;
                }

                nextPersistentCache[sourceFile.RelativePath] = cacheEntry;
                AddCacheEntryToIndex(cacheEntry, sourceFile.FullPath, mapIdIndex, hashIndex);

                processedSourceCount++;
                UpdateScanStatus(
                    SongScriptFolderCacheScanState.Scanning,
                    processedSourceCount,
                    totalSourceCount,
                    string.Empty);
            }

            if (!PersistentCachesMatch(persistentCache, nextPersistentCache))
            {
                SavePersistentCache(nextPersistentCache);
            }

            _mapIdIndex = mapIdIndex;
            _hashIndex = hashIndex;
            _isReady = true;

            int totalEntries = 0;
            foreach (var sourceEntry in nextPersistentCache.Values)
            {
                totalEntries += sourceEntry?.Scripts?.Count ?? 0;
            }
            Plugin.Log.Info(
                $"SongScriptFolderCache: Scan complete. {totalEntries} script(s) indexed across {mapIdIndex.Count} mapId(s) and {hashIndex.Count} hash(es). " +
                $"Reused {reusedSourceCount} cached source(s), scanned {scannedSourceCount} source(s).");

#if DEBUG
            if (mapIdIndex.Count > 0)
            {
                Plugin.Log.Debug($"SongScriptFolderCache: Indexed MapIds: {string.Join(", ", mapIdIndex.Keys)}");
            }

            if (hashIndex.Count > 0)
            {
                Plugin.Log.Debug($"SongScriptFolderCache: Indexed Hashes: {string.Join(", ", hashIndex.Keys)}");
            }
#endif

            UpdateScanStatus(
                SongScriptFolderCacheScanState.Completed,
                totalSourceCount,
                totalSourceCount,
                string.Empty);
        }

        /// <summary>
        /// SongScriptsフォルダが存在しなければ作成する
        /// </summary>
        private static void EnsureSongScriptFolderExists()
        {
            if (Directory.Exists(SongScriptFolderPath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(SongScriptFolderPath);
                Plugin.Log.Info($"SongScriptFolderCache: Created SongScript folder at: {SongScriptFolderPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"SongScriptFolderCache: Failed to create SongScript folder: {ex.Message}");
            }
        }

        private static List<SourceFileInfo> CaptureCurrentSourceFiles()
        {
            var sourceFiles = new List<SourceFileInfo>();

            AppendSourceFiles("*.json", false, sourceFiles);
            AppendSourceFiles("*.zip", true, sourceFiles);

            return sourceFiles
                .OrderBy(sourceFile => sourceFile.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AppendSourceFiles(string searchPattern, bool isZip, ICollection<SourceFileInfo> sourceFiles)
        {
            string[] paths;
            try
            {
                paths = Directory.GetFiles(SongScriptFolderPath, searchPattern, SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to scan {searchPattern} files: {ex.Message}");
                return;
            }

            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    sourceFiles.Add(new SourceFileInfo
                    {
                        FullPath = path,
                        RelativePath = GetRelativeSourcePath(path),
                        LastWriteUtcTicks = fileInfo.LastWriteTimeUtc.Ticks,
                        Size = fileInfo.Length,
                        IsZip = isZip
                    });
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug($"SongScriptFolderCache: Failed to inspect '{path}': {ex.Message}");
                }
            }
        }

        private static bool TryReusePersistentCacheEntry(
            IReadOnlyDictionary<string, CacheSourceEntry> persistentCache,
            SourceFileInfo sourceFile,
            out CacheSourceEntry cacheEntry)
        {
            if (persistentCache != null &&
                persistentCache.TryGetValue(sourceFile.RelativePath, out cacheEntry) &&
                CacheSourceEntryMatches(cacheEntry, sourceFile))
            {
                return true;
            }

            cacheEntry = null;
            return false;
        }

        private static CacheSourceEntry BuildCacheEntry(SourceFileInfo sourceFile)
        {
            return new CacheSourceEntry
            {
                RelativePath = sourceFile.RelativePath,
                LastWriteUtcTicks = sourceFile.LastWriteUtcTicks,
                Size = sourceFile.Size,
                IsZip = sourceFile.IsZip,
                Scripts = sourceFile.IsZip
                    ? ScanZipFile(sourceFile.FullPath, sourceFile.RelativePath)
                    : ScanJsonFile(sourceFile.FullPath, sourceFile.RelativePath)
            };
        }

        private static List<CacheSongScriptEntry> ScanJsonFile(string filePath, string relativePath)
        {
            var scripts = new List<CacheSongScriptEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                if (!TryCreateCacheScriptEntry(parsed, relativePath, relativePath, null, out var script))
                {
                    return scripts;
                }

                scripts.Add(script);
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongScriptFolderCache: Skipping '{filePath}': {ex.Message}");
            }

            return scripts;
        }

        private static List<CacheSongScriptEntry> ScanZipFile(string zipPath, string relativePath)
        {
            var scripts = new List<CacheSongScriptEntry>();

            try
            {
                using (var zip = ZipFile.OpenRead(zipPath))
                {
                    foreach (var zipEntry in zip.Entries.OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!zipEntry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (zipEntry.Length == 0)
                            continue;

                        try
                        {
                            string json;
                            using (var stream = zipEntry.Open())
                            using (var reader = new StreamReader(stream))
                            {
                                json = reader.ReadToEnd();
                            }

                            var parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);
                            if (!TryCreateCacheScriptEntry(
                                parsed,
                                Path.GetFileName(zipEntry.FullName),
                                relativePath,
                                zipEntry.FullName,
                                out var script))
                                continue;

                            scripts.Add(script);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Debug($"SongScriptFolderCache: Skipping zip entry '{zipEntry.FullName}' in '{zipPath}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to read zip '{zipPath}': {ex.Message}");
            }

            return scripts;
        }

        private static bool TryCreateCacheScriptEntry(
            MovementScriptJson parsed,
            string fileName,
            string sourceRelativePath,
            string zipEntryName,
            out CacheSongScriptEntry script)
        {
            script = null;

            if (parsed?.JsonMovements == null || parsed.JsonMovements.Length == 0)
                return false;

            string mapId = NormalizeMapId(parsed.metadata?.mapId);
            string hash = NormalizeHash(parsed.metadata?.hash);
            string pathMapId = ResolvePathMapId(fileName, sourceRelativePath, zipEntryName);
            if (string.IsNullOrEmpty(mapId) && string.IsNullOrEmpty(hash) && string.IsNullOrEmpty(pathMapId))
                return false;

            script = new CacheSongScriptEntry
            {
                ZipEntryName = zipEntryName,
                MapId = mapId,
                Hash = hash,
                FileName = fileName ?? string.Empty,
                Metadata = parsed.metadata
            };

            return true;
        }

        private static void AddCacheEntryToIndex(
            CacheSourceEntry cacheEntry,
            string sourceFilePath,
            Dictionary<string, List<SongScriptEntry>> mapIdIndex,
            Dictionary<string, List<SongScriptEntry>> hashIndex)
        {
            if (cacheEntry?.Scripts == null)
            {
                return;
            }

            foreach (CacheSongScriptEntry cachedScript in cacheEntry.Scripts)
            {
                SongScriptEntry entry = CreateRuntimeSongScriptEntry(cacheEntry, cachedScript, sourceFilePath);
                if (entry == null)
                {
                    continue;
                }

                foreach (string mapId in GetMapIdLookupKeys(cacheEntry, cachedScript, entry))
                {
                    AddEntryToIndex(mapIdIndex, mapId, entry);
                }

                AddEntryToIndex(hashIndex, entry.Hash, entry);
            }
        }

        private static SongScriptEntry CreateRuntimeSongScriptEntry(
            CacheSourceEntry cacheEntry,
            CacheSongScriptEntry cachedScript,
            string sourceFilePath)
        {
            if (cachedScript == null || string.IsNullOrEmpty(sourceFilePath))
            {
                return null;
            }

            string mapId = NormalizeMapId(cachedScript.MapId);
            string hash = NormalizeHash(cachedScript.Hash);
            string pathMapId = ResolvePathMapId(cachedScript.FileName, cacheEntry?.RelativePath, cachedScript.ZipEntryName);
            if (string.IsNullOrEmpty(mapId) && string.IsNullOrEmpty(hash) && string.IsNullOrEmpty(pathMapId))
            {
                return null;
            }

            return new SongScriptEntry
            {
                FilePath = sourceFilePath,
                ZipEntryName = cacheEntry != null && cacheEntry.IsZip ? cachedScript.ZipEntryName : null,
                MapId = mapId,
                Hash = hash,
                FileName = cachedScript.FileName ?? string.Empty,
                Metadata = cachedScript.Metadata
            };
        }

        private static IEnumerable<string> GetMapIdLookupKeys(
            CacheSourceEntry cacheEntry,
            CacheSongScriptEntry cachedScript,
            SongScriptEntry entry)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddLookupKey(keys, entry?.MapId);
            AddLookupKey(keys, ResolvePathMapId(cachedScript?.FileName, cacheEntry?.RelativePath, cachedScript?.ZipEntryName));

            return keys;
        }

        private static void AddLookupKey(ICollection<string> keys, string key)
        {
            if (keys == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            keys.Add(key);
        }

        private static void AddEntryToIndex(
            Dictionary<string, List<SongScriptEntry>> index,
            string key,
            SongScriptEntry entry)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!index.TryGetValue(key, out var list))
            {
                list = new List<SongScriptEntry>();
                index[key] = list;
            }

            list.Add(entry);
        }

        private static Dictionary<string, CacheSourceEntry> LoadPersistentCache()
        {
            var cache = new Dictionary<string, CacheSourceEntry>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    return cache;
                }

                string json = File.ReadAllText(CacheFilePath);
                var document = JsonConvert.DeserializeObject<CacheDocument>(json);
                if (document == null)
                {
                    return cache;
                }

                if (document.Version != CacheVersion)
                {
                    Plugin.Log.Info(
                        $"SongScriptFolderCache: Ignoring cache file with unsupported version {document.Version}. Expected {CacheVersion}.");
                    return cache;
                }

                if (document.Entries == null)
                {
                    return cache;
                }

                foreach (CacheSourceEntry entry in document.Entries)
                {
                    string relativePath = NormalizeRelativePath(entry?.RelativePath);
                    if (string.IsNullOrEmpty(relativePath))
                    {
                        continue;
                    }

                    cache[relativePath] = CloneCacheSourceEntry(entry, relativePath);
                }

                Plugin.Log.Info($"SongScriptFolderCache: Loaded {cache.Count} cached source file(s).");
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to load cache: {ex.Message}");
            }

            return cache;
        }

        private static void SavePersistentCache(IReadOnlyDictionary<string, CacheSourceEntry> cache)
        {
            var snapshot = new CacheDocument
            {
                Entries = cache.Values
                    .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .Select(CloneCacheSourceEntry)
                    .ToList()
            };

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
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to save cache: {ex.Message}");
            }
        }

        private static CacheSourceEntry CloneCacheSourceEntry(CacheSourceEntry source)
        {
            return CloneCacheSourceEntry(source, NormalizeRelativePath(source?.RelativePath));
        }

        private static CacheSourceEntry CloneCacheSourceEntry(CacheSourceEntry source, string relativePath)
        {
            return new CacheSourceEntry
            {
                RelativePath = relativePath ?? string.Empty,
                LastWriteUtcTicks = source?.LastWriteUtcTicks ?? 0,
                Size = source?.Size ?? 0,
                IsZip = source != null && source.IsZip,
                Scripts = CloneCacheSongScriptEntries(source?.Scripts)
            };
        }

        private static List<CacheSongScriptEntry> CloneCacheSongScriptEntries(IEnumerable<CacheSongScriptEntry> source)
        {
            if (source == null)
            {
                return new List<CacheSongScriptEntry>();
            }

            return source
                .Select(entry => new CacheSongScriptEntry
                {
                    ZipEntryName = entry?.ZipEntryName,
                    MapId = NormalizeMapId(entry?.MapId),
                    Hash = NormalizeHash(entry?.Hash),
                    FileName = entry?.FileName ?? string.Empty,
                    Metadata = entry?.Metadata
                })
                .ToList();
        }

        private static bool PersistentCachesMatch(
            IReadOnlyDictionary<string, CacheSourceEntry> left,
            IReadOnlyDictionary<string, CacheSourceEntry> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            foreach (var pair in left)
            {
                if (!right.TryGetValue(pair.Key, out var rightEntry))
                {
                    return false;
                }

                if (!CacheSourceEntriesEqual(pair.Value, rightEntry))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CacheSourceEntryMatches(CacheSourceEntry cacheEntry, SourceFileInfo sourceFile)
        {
            return cacheEntry != null &&
                sourceFile != null &&
                cacheEntry.IsZip == sourceFile.IsZip &&
                cacheEntry.LastWriteUtcTicks == sourceFile.LastWriteUtcTicks &&
                cacheEntry.Size == sourceFile.Size;
        }

        private static bool CacheSourceEntriesEqual(CacheSourceEntry left, CacheSourceEntry right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase) &&
                left.LastWriteUtcTicks == right.LastWriteUtcTicks &&
                left.Size == right.Size &&
                left.IsZip == right.IsZip &&
                CacheSongScriptEntriesEqual(left.Scripts, right.Scripts);
        }

        private static bool CacheSongScriptEntriesEqual(
            IReadOnlyList<CacheSongScriptEntry> left,
            IReadOnlyList<CacheSongScriptEntry> right)
        {
            if (left == null)
            {
                return right == null || right.Count == 0;
            }

            if (right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                CacheSongScriptEntry leftEntry = left[index];
                CacheSongScriptEntry rightEntry = right[index];

                if (!string.Equals(leftEntry?.ZipEntryName, rightEntry?.ZipEntryName, StringComparison.Ordinal) ||
                    !string.Equals(leftEntry?.MapId, rightEntry?.MapId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftEntry?.Hash, rightEntry?.Hash, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftEntry?.FileName, rightEntry?.FileName, StringComparison.Ordinal) ||
                    !MetadataEquals(leftEntry?.Metadata, rightEntry?.Metadata))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MetadataEquals(MetadataElements left, MetadataElements right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.cameraScriptAuthorName, right.cameraScriptAuthorName, StringComparison.Ordinal) &&
                string.Equals(left.songName, right.songName, StringComparison.Ordinal) &&
                string.Equals(left.songSubName, right.songSubName, StringComparison.Ordinal) &&
                string.Equals(left.songAuthorName, right.songAuthorName, StringComparison.Ordinal) &&
                string.Equals(left.levelAuthorName, right.levelAuthorName, StringComparison.Ordinal) &&
                string.Equals(NormalizeMapId(left.mapId), NormalizeMapId(right.mapId), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeHash(left.hash), NormalizeHash(right.hash), StringComparison.OrdinalIgnoreCase) &&
                left.bpm == right.bpm &&
                left.duration == right.duration &&
                left.avatarHeight == right.avatarHeight &&
                string.Equals(left.description, right.description, StringComparison.Ordinal);
        }

        private static string GetRelativeSourcePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            if (fullPath.Length > SongScriptFolderPath.Length)
            {
                return NormalizeRelativePath(
                    fullPath.Substring(SongScriptFolderPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            return NormalizeRelativePath(Path.GetFileName(fullPath));
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }

            return relativePath
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
        }

        private static string ResolvePathMapId(string fileName, string sourceRelativePath, string zipEntryName)
        {
            string fileMapId = ExtractFileMapId(fileName);
            if (!string.IsNullOrEmpty(fileMapId))
            {
                return fileMapId;
            }

            string zipEntryFolderMapId = ExtractFolderMapId(zipEntryName);
            if (!string.IsNullOrEmpty(zipEntryFolderMapId))
            {
                return zipEntryFolderMapId;
            }

            return ExtractFolderMapId(sourceRelativePath);
        }

        private static string ExtractFileMapId(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            return ExtractLeadingMapIdCandidate(Path.GetFileNameWithoutExtension(fileName));
        }

        private static string ExtractFolderMapId(string path)
        {
            string folderName = GetContainingFolderName(path);
            if (string.IsNullOrEmpty(folderName))
            {
                return string.Empty;
            }

            return ExtractLeadingMapIdCandidate(folderName);
        }

        private static string GetContainingFolderName(string path)
        {
            string normalizedPath = NormalizeRelativePath(path);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return string.Empty;
            }

            string directoryPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return string.Empty;
            }

            return Path.GetFileName(directoryPath) ?? string.Empty;
        }

        private static bool IsHexCharacter(char value)
        {
            return (value >= '0' && value <= '9') ||
                (value >= 'a' && value <= 'f') ||
                (value >= 'A' && value <= 'F');
        }

        private static string ExtractLeadingMapIdCandidate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int length = 0;
            while (length < value.Length && IsHexCharacter(value[length]))
            {
                length++;
            }

            if (length == 0 || length > 6)
            {
                return string.Empty;
            }

            return value.Substring(0, length).ToLowerInvariant();
        }

        private static string NormalizeMapId(string mapId)
        {
            return string.IsNullOrEmpty(mapId) ? string.Empty : mapId.ToLowerInvariant();
        }

        /// <summary>
        /// hashを小文字に正規化する
        /// </summary>
        private static string NormalizeHash(string hash)
        {
            return string.IsNullOrEmpty(hash) ? string.Empty : hash.ToLowerInvariant();
        }

        /// <summary>
        /// 指定mapIdに一致するエントリ一覧を返す
        /// </summary>
        public static List<SongScriptEntry> GetScriptsByMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return new List<SongScriptEntry>();

            mapId = NormalizeMapId(mapId);
            if (_mapIdIndex.TryGetValue(mapId, out var list))
                return list;

            return new List<SongScriptEntry>();
        }

        /// <summary>
        /// 指定hashに一致するエントリ一覧を返す
        /// </summary>
        public static List<SongScriptEntry> GetScriptsByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return new List<SongScriptEntry>();

            hash = NormalizeHash(hash);
            if (_hashIndex.TryGetValue(hash, out var list))
                return list;

            return new List<SongScriptEntry>();
        }

        /// <summary>
        /// 指定mapIdまたはhashに一致するエントリ一覧を返す
        /// 同一スクリプトが両方の条件で一致した場合でも結果は1件にまとめる
        /// </summary>
        public static List<SongScriptEntry> GetScriptsByLevelReference(string mapId, string hash)
        {
            var results = new List<SongScriptEntry>();
            var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddEntries(results, seenEntries, GetScriptsByMapId(mapId));
            AddEntries(results, seenEntries, GetScriptsByHash(hash));

            return results;
        }

        private static void AddEntries(
            ICollection<SongScriptEntry> target,
            ISet<string> seenEntries,
            IEnumerable<SongScriptEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (SongScriptEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                string identity = $"{entry.FilePath}|{entry.ZipEntryName ?? string.Empty}";
                if (seenEntries.Add(identity))
                {
                    target.Add(entry);
                }
            }
        }

        /// <summary>
        /// SongScriptEntryからJSON文字列を読み出す（raw jsonファイルまたはzipエントリ対応）
        /// </summary>
        public static string ReadJsonContent(SongScriptEntry entry)
        {
            if (!entry.IsZipEntry)
            {
                return File.ReadAllText(entry.FilePath);
            }

            using (var zip = ZipFile.OpenRead(entry.FilePath))
            {
                var zipEntry = zip.GetEntry(entry.ZipEntryName);
                if (zipEntry == null)
                    throw new FileNotFoundException($"Zip entry '{entry.ZipEntryName}' not found in '{entry.FilePath}'");

                using (var stream = zipEntry.Open())
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void UpdateScanStatus(
            SongScriptFolderCacheScanState state,
            int processedSourceCount,
            int totalSourceCount,
            string errorMessage)
        {
            _scanState = state;
            _isScanning = state == SongScriptFolderCacheScanState.Scanning;
            _processedSourceCount = processedSourceCount < 0 ? 0 : processedSourceCount;
            _totalSourceCount = totalSourceCount < 0 ? 0 : totalSourceCount;
            _lastScanErrorMessage = errorMessage ?? string.Empty;
            NotifyScanStatusChanged();
        }

        private static void NotifyScanStatusChanged()
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
                Plugin.Log.Warn($"SongScriptFolderCache: Scan status listener failed: {ex.Message}");
            }
        }
    }
}
