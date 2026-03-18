using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using CameraSongScript.Models;
using SongCore;
using Zenject;

namespace CameraSongScript.Services
{
    internal sealed class SongScriptMissingBeatmapDownloadService : IInitializable, IDisposable
    {
        private static readonly StringComparer MapIdComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly Uri BeatSaverBaseUri = new Uri("https://api.beatsaver.com/");

        private readonly object _stateLock = new object();
        private readonly BeatSaver _beatSaverClient;
        private readonly string _statusUserAgent;

        private HashSet<string> _missingMapIds = new HashSet<string>(MapIdComparer);
        private HashSet<string> _downloadableMapIds = new HashSet<string>(MapIdComparer);
        private HashSet<string> _unavailableOnBeatSaverMapIds = new HashSet<string>(MapIdComparer);
        private HashSet<string> _alreadyLoadedLatestHashMapIds = new HashSet<string>(MapIdComparer);
        private CancellationTokenSource _downloadCts;
        private bool _disposed;
        private bool _isDownloading;
        private int _downloadProgressCurrent;
        private int _downloadProgressTotal;

        public event Action StateChanged;

        public SongScriptMissingBeatmapDownloadService()
        {
            var version = typeof(Plugin).Assembly.GetName().Version ?? new Version(0, 0, 1, 0);
            string userAgent = string.Format(CultureInfo.InvariantCulture, "CameraSongScript/{0}", version);

            _beatSaverClient = new BeatSaver("CameraSongScript", version);
            _statusUserAgent = userAgent;
        }

        public bool IsSongDetailsReady => Plugin.IsSongDetailsReady;
        public bool IsSongScriptFolderReady => SongScriptFolderCache.IsReady;
        public bool IsSongScriptFolderScanning => SongScriptFolderCache.IsScanning;
        public bool AreSongsLoaded => Loader.AreSongsLoaded;
        public bool AreSongsLoading => Loader.AreSongsLoading;

        public bool IsDownloading
        {
            get
            {
                lock (_stateLock)
                {
                    return _isDownloading;
                }
            }
        }

        public int DownloadProgressCurrent
        {
            get
            {
                lock (_stateLock)
                {
                    return _downloadProgressCurrent;
                }
            }
        }

        public int DownloadProgressTotal
        {
            get
            {
                lock (_stateLock)
                {
                    return _downloadProgressTotal;
                }
            }
        }

        public int MissingMapIdCount
        {
            get
            {
                lock (_stateLock)
                {
                    return _missingMapIds.Count;
                }
            }
        }

        public int DownloadableMapIdCount
        {
            get
            {
                lock (_stateLock)
                {
                    return _downloadableMapIds.Count;
                }
            }
        }

        public int UnavailableOnBeatSaverCount
        {
            get
            {
                lock (_stateLock)
                {
                    return _unavailableOnBeatSaverMapIds.Count;
                }
            }
        }

        public int AlreadyLoadedLatestHashCount
        {
            get
            {
                lock (_stateLock)
                {
                    return _alreadyLoadedLatestHashMapIds.Count;
                }
            }
        }

        public bool IsDownloadAvailable
        {
            get
            {
                lock (_stateLock)
                {
                    return !_disposed &&
                        Plugin.IsSongDetailsReady &&
                        SongScriptFolderCache.IsReady &&
                        !SongScriptFolderCache.IsScanning &&
                        Loader.AreSongsLoaded &&
                        !Loader.AreSongsLoading &&
                        !_isDownloading &&
                        _downloadableMapIds.Count > 0;
                }
            }
        }

        public void Initialize()
        {
            SongScriptFolderCache.ScanStatusChanged += HandleExternalStateChanged;
            Plugin.SongDetailsCacheInitialized += HandleExternalStateChanged;
            Loader.LoadingStartedEvent += HandleLoadingStarted;
            Loader.SongsLoadedEvent += HandleSongsLoaded;

            RefreshState();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            SongScriptFolderCache.ScanStatusChanged -= HandleExternalStateChanged;
            Plugin.SongDetailsCacheInitialized -= HandleExternalStateChanged;
            Loader.LoadingStartedEvent -= HandleLoadingStarted;
            Loader.SongsLoadedEvent -= HandleSongsLoaded;

            lock (_stateLock)
            {
                if (_downloadCts != null)
                {
                    _downloadCts.Cancel();
                    _downloadCts.Dispose();
                    _downloadCts = null;
                }
            }
            _beatSaverClient.Dispose();
        }

        public void RefreshState()
        {
            if (_disposed)
            {
                return;
            }

            var songScriptMapIds = CollectSongScriptMapIds();
            var localMapIds = CollectInstalledBeatmapMapIds();
            var nextMissingMapIds = new HashSet<string>(songScriptMapIds, MapIdComparer);
            nextMissingMapIds.ExceptWith(localMapIds);

            lock (_stateLock)
            {
                _missingMapIds = nextMissingMapIds;
                _unavailableOnBeatSaverMapIds.IntersectWith(_missingMapIds);
                _alreadyLoadedLatestHashMapIds.IntersectWith(_missingMapIds);

                _downloadableMapIds = new HashSet<string>(_missingMapIds, MapIdComparer);
                _downloadableMapIds.ExceptWith(_unavailableOnBeatSaverMapIds);
                _downloadableMapIds.ExceptWith(_alreadyLoadedLatestHashMapIds);
            }

            NotifyStateChanged();
        }

        public async Task DownloadMissingBeatmapsAsync()
        {
            List<string> mapIdsToDownload;
            CancellationTokenSource downloadCts;

            lock (_stateLock)
            {
                if (_disposed || _isDownloading || !IsDownloadAvailable)
                {
                    return;
                }

                _isDownloading = true;
                _downloadProgressCurrent = 0;
                _downloadProgressTotal = _downloadableMapIds.Count;

                if (_downloadCts != null)
                {
                    _downloadCts.Dispose();
                }

                _downloadCts = new CancellationTokenSource();
                downloadCts = _downloadCts;
                mapIdsToDownload = _downloadableMapIds
                    .OrderBy(mapId => mapId, MapIdComparer)
                    .ToList();
            }

            NotifyStateChanged();

            bool downloadedAny = false;

            try
            {
                foreach (string mapId in mapIdsToDownload)
                {
                    downloadCts.Token.ThrowIfCancellationRequested();

                    if (await TryDownloadBeatmapByMapIdAsync(mapId, downloadCts.Token).ConfigureAwait(false))
                    {
                        downloadedAny = true;
                    }

                    lock (_stateLock)
                    {
                        _downloadProgressCurrent++;
                    }

                    NotifyStateChanged();
                }

                if (downloadedAny)
                {
                    await RefreshSongsAndWaitAsync(downloadCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (_stateLock)
                {
                    _isDownloading = false;
                    _downloadProgressCurrent = 0;
                    _downloadProgressTotal = 0;

                    if (ReferenceEquals(_downloadCts, downloadCts))
                    {
                        _downloadCts.Dispose();
                        _downloadCts = null;
                    }
                }

                RefreshState();
            }
        }

        private void HandleExternalStateChanged()
        {
            RefreshState();
        }

        private void HandleLoadingStarted(Loader _)
        {
            RefreshState();
        }

        private void HandleSongsLoaded(Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> __)
        {
            RefreshState();
        }

        private async Task<bool> TryDownloadBeatmapByMapIdAsync(string mapId, CancellationToken token)
        {
            try
            {
                Beatmap song = await _beatSaverClient.Beatmap(mapId, token).ConfigureAwait(false);
                if (song == null)
                {
                    if (await IsBeatSaverMapMissingAsync(mapId, token).ConfigureAwait(false))
                    {
                        MarkUnavailableOnBeatSaver(mapId);
                    }
                    else
                    {
                        Plugin.Log.Warn(
                            $"SongScriptMissingBeatmapDownloadService: Skipping '{mapId}' because BeatSaver lookup did not succeed.");
                    }

                    return false;
                }

                BeatmapVersion latestVersion = song.LatestVersion;
                if (latestVersion == null || string.IsNullOrEmpty(latestVersion.Hash))
                {
                    Plugin.Log.Warn(
                        $"SongScriptMissingBeatmapDownloadService: BeatSaver map '{mapId}' does not have a downloadable latest version.");
                    return false;
                }

                if (Loader.GetLevelByHash(latestVersion.Hash) != null)
                {
                    MarkAlreadyLoadedLatestHash(mapId);
                    Plugin.Log.Info(
                        $"SongScriptMissingBeatmapDownloadService: Skipping '{mapId}' because latest hash '{latestVersion.Hash}' is already loaded.");
                    return false;
                }

                byte[] zip = await latestVersion.DownloadZIP(token).ConfigureAwait(false);
                if (zip == null || zip.Length == 0)
                {
                    Plugin.Log.Warn($"SongScriptMissingBeatmapDownloadService: BeatSaver returned an empty ZIP for '{mapId}'.");
                    return false;
                }

                string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                if (!Directory.Exists(customSongsPath))
                {
                    Directory.CreateDirectory(customSongsPath);
                }

                await ExtractZipAsync(zip, customSongsPath, BuildFolderName(song)).ConfigureAwait(false);
                Plugin.Log.Info($"SongScriptMissingBeatmapDownloadService: Downloaded missing beatmap '{mapId}'.");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptMissingBeatmapDownloadService: Failed to download '{mapId}': {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsBeatSaverMapMissingAsync(string mapId, CancellationToken token)
        {
            try
            {
                var request = WebRequest.CreateHttp(new Uri(BeatSaverBaseUri, "maps/id/" + mapId));
                request.Method = "GET";
                request.Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
                request.ReadWriteTimeout = request.Timeout;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.UserAgent = _statusUserAgent;

                using (token.Register(request.Abort))
                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    return response.StatusCode == HttpStatusCode.NotFound;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse response)
            {
                using (response)
                {
                    return response.StatusCode == HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptMissingBeatmapDownloadService: BeatSaver status check failed for '{mapId}': {ex.Message}");
                return false;
            }
        }

        private static async Task RefreshSongsAndWaitAsync(CancellationToken token)
        {
            if (Loader.Instance == null)
            {
                return;
            }

            var completionSource = new TaskCompletionSource<bool>();
            Action<Loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel>> songsLoadedHandler = null;
            songsLoadedHandler = delegate (Loader loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels)
            {
                Loader.SongsLoadedEvent -= songsLoadedHandler;
                completionSource.TrySetResult(true);
            };

            Loader.SongsLoadedEvent += songsLoadedHandler;

            try
            {
                Loader.Instance.RefreshSongs(false);

                using (token.Register(() => completionSource.TrySetCanceled()))
                {
                    await completionSource.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                Loader.SongsLoadedEvent -= songsLoadedHandler;
            }
        }

        private static HashSet<string> CollectSongScriptMapIds()
        {
            return new HashSet<string>(
                SongScriptFolderCache.GetIndexedMapIds()
                    .Where(mapId => !string.IsNullOrEmpty(mapId))
                    .Select(NormalizeMapId),
                MapIdComparer);
        }

        private static HashSet<string> CollectInstalledBeatmapMapIds()
        {
            var mapIds = new HashSet<string>(MapIdComparer);

            void AddFolderPath(string folderPath)
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    return;
                }

                string mapId = NormalizeMapId(SongScriptMapIdResolver.ResolveMapIdFromLevelId(null, folderPath));
                if (!string.IsNullOrEmpty(mapId))
                {
                    mapIds.Add(mapId);
                }
            }

            void AddLevel(CustomPreviewBeatmapLevel level)
            {
                if (level == null || string.IsNullOrEmpty(level.levelID))
                {
                    return;
                }

                string mapId = NormalizeMapId(SongScriptMapIdResolver.ResolveMapIdFromLevelId(level.levelID, level.customLevelPath));
                if (!string.IsNullOrEmpty(mapId))
                {
                    mapIds.Add(mapId);
                }
            }

            string customLevelsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
            if (!string.IsNullOrEmpty(customLevelsPath) && Directory.Exists(customLevelsPath))
            {
                try
                {
                    foreach (string folderPath in Directory.GetDirectories(customLevelsPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        AddFolderPath(folderPath);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug(
                        $"SongScriptMissingBeatmapDownloadService: Failed to enumerate custom level directories: {ex.Message}");
                }
            }

            foreach (CustomPreviewBeatmapLevel level in Loader.CustomLevels.Values)
            {
                AddLevel(level);
            }

            foreach (CustomPreviewBeatmapLevel level in Loader.CustomWIPLevels.Values)
            {
                AddLevel(level);
            }

            foreach (CustomPreviewBeatmapLevel level in Loader.CachedWIPLevels.Values)
            {
                AddLevel(level);
            }

            foreach (var separateSongFolder in Loader.SeperateSongFolders)
            {
                foreach (CustomPreviewBeatmapLevel level in separateSongFolder.Levels.Values)
                {
                    AddLevel(level);
                }
            }

            return mapIds;
        }

        private void MarkUnavailableOnBeatSaver(string mapId)
        {
            mapId = NormalizeMapId(mapId);

            lock (_stateLock)
            {
                _unavailableOnBeatSaverMapIds.Add(mapId);
                _downloadableMapIds.Remove(mapId);
            }
        }

        private void MarkAlreadyLoadedLatestHash(string mapId)
        {
            mapId = NormalizeMapId(mapId);

            lock (_stateLock)
            {
                _alreadyLoadedLatestHashMapIds.Add(mapId);
                _downloadableMapIds.Remove(mapId);
            }
        }

        private static string NormalizeMapId(string mapId)
        {
            return string.IsNullOrEmpty(mapId) ? string.Empty : mapId.ToLowerInvariant();
        }

        private static string BuildFolderName(Beatmap song)
        {
            string songName = song.Metadata != null ? song.Metadata.SongName ?? string.Empty : string.Empty;
            string levelAuthorName = song.Metadata != null ? song.Metadata.LevelAuthorName ?? string.Empty : string.Empty;
            string basePath = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1} - {2})",
                song.ID ?? string.Empty,
                songName,
                levelAuthorName);

            return string.Join(
                string.Empty,
                basePath.Split(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray()));
        }

        private static async Task ExtractZipAsync(byte[] zip, string customSongsPath, string songName, bool overwrite = false)
        {
            using (Stream zipStream = new MemoryStream(zip))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                string path = Path.Combine(customSongsPath, songName);

                if (!overwrite && Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})"))
                    {
                        pathNum++;
                    }

                    path += $" ({pathNum})";
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                await Task.Run(() =>
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name != entry.FullName)
                        {
                            continue;
                        }

                        string entryPath = Path.Combine(path, entry.Name);
                        if (overwrite || !File.Exists(entryPath))
                        {
                            entry.ExtractToFile(entryPath, overwrite);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private void NotifyStateChanged()
        {
            var handler = StateChanged;
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
                Plugin.Log.Warn($"SongScriptMissingBeatmapDownloadService: State listener failed: {ex.Message}");
            }
        }
    }
}
