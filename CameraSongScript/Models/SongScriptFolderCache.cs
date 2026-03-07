using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;

namespace CameraSongScript.Models
{
    /// <summary>
    /// SongScriptフォルダ内のスクリプト情報を保持するエントリ
    /// </summary>
    internal class SongScriptEntry
    {
        /// <summary>ディスク上のフルパス（.jsonまたは.zipファイル）</summary>
        public string FilePath { get; set; }

        /// <summary>zipエントリの場合のエントリ名（非zip時はnull）</summary>
        public string ZipEntryName { get; set; }

        /// <summary>metadata.mapId（小文字正規化済み）</summary>
        public string MapId { get; set; }

        /// <summary>表示用ファイル名</summary>
        public string FileName { get; set; }

        /// <summary>キャッシュ済みメタデータ</summary>
        public MetadataElements Metadata { get; set; }

        public bool IsZipEntry => !string.IsNullOrEmpty(ZipEntryName);
    }

    /// <summary>
    /// UserData/CameraSongScript/SongScript/ フォルダを再帰的にスキャンし、
    /// metadata.mapId をキーとしたインデックスをキャッシュするクラス。
    /// 起動時に一度だけスキャンし、以降は高速にmapIdで検索できる。
    /// </summary>
    internal static class SongScriptFolderCache
    {
        private static readonly string SongScriptFolderPath =
            Path.Combine(UnityGame.UserDataPath, "CameraSongScript", "SongScript");

        /// <summary>mapId（小文字） → エントリ一覧</summary>
        private static Dictionary<string, List<SongScriptEntry>> _mapIdIndex =
            new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);

        private static bool _isReady = false;

        /// <summary>キャッシュの準備が完了しているか</summary>
        public static bool IsReady => _isReady;

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
                    Plugin.Log.Error($"SongScriptFolderCache: Scan failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// フォルダを再帰的にスキャンしてインデックスを構築する
        /// </summary>
        private static void Scan()
        {
            var index = new Dictionary<string, List<SongScriptEntry>>(StringComparer.OrdinalIgnoreCase);

            // フォルダが存在しなければ作成
            if (!Directory.Exists(SongScriptFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(SongScriptFolderPath);
                    Plugin.Log.Info($"SongScriptFolderCache: Created SongScript folder at: {SongScriptFolderPath}");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"SongScriptFolderCache: Failed to create SongScript folder: {ex.Message}");
                }
                _mapIdIndex = index;
                _isReady = true;
                return;
            }

            // .json ファイルをスキャン
            try
            {
                var jsonFiles = Directory.GetFiles(SongScriptFolderPath, "*.json", SearchOption.AllDirectories);
                foreach (var filePath in jsonFiles)
                {
                    TryAddJsonFile(filePath, index);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to scan json files: {ex.Message}");
            }

            // .zip ファイルをスキャン
            try
            {
                var zipFiles = Directory.GetFiles(SongScriptFolderPath, "*.zip", SearchOption.AllDirectories);
                foreach (var zipPath in zipFiles)
                {
                    TryAddZipFile(zipPath, index);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScriptFolderCache: Failed to scan zip files: {ex.Message}");
            }

            _mapIdIndex = index;
            _isReady = true;

            int totalEntries = 0;
            foreach (var list in index.Values)
                totalEntries += list.Count;
            Plugin.Log.Info($"SongScriptFolderCache: Scan complete. {totalEntries} script(s) indexed across {index.Count} mapId(s).");

#if DEBUG
            if (index.Count > 0)
            {
                Plugin.Log.Debug($"SongScriptFolderCache: Indexed MapIds: {string.Join(", ", index.Keys)}");
            }
#endif
        }

        /// <summary>
        /// 単一の.jsonファイルを読み込み、有効ならインデックスに追加する
        /// </summary>
        private static void TryAddJsonFile(string filePath, Dictionary<string, List<SongScriptEntry>> index)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonConvert.DeserializeObject<MovementScriptJson>(json);

                if (parsed?.JsonMovements == null || parsed.JsonMovements.Length == 0)
                    return;

                if (parsed.metadata == null || string.IsNullOrEmpty(parsed.metadata.mapId))
                    return;

                string mapId = parsed.metadata.mapId.ToLowerInvariant();
                var entry = new SongScriptEntry
                {
                    FilePath = filePath,
                    ZipEntryName = null,
                    MapId = mapId,
                    FileName = filePath.Length > SongScriptFolderPath.Length 
                        ? filePath.Substring(SongScriptFolderPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        : Path.GetFileName(filePath),
                    Metadata = parsed.metadata
                };

                if (!index.TryGetValue(mapId, out var list))
                {
                    list = new List<SongScriptEntry>();
                    index[mapId] = list;
                }
                list.Add(entry);
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongScriptFolderCache: Skipping '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// .zipファイル内の各.jsonエントリを読み込み、有効ならインデックスに追加する
        /// </summary>
        private static void TryAddZipFile(string zipPath, Dictionary<string, List<SongScriptEntry>> index)
        {
            try
            {
                using (var zip = ZipFile.OpenRead(zipPath))
                {
                    foreach (var zipEntry in zip.Entries)
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

                            if (parsed?.JsonMovements == null || parsed.JsonMovements.Length == 0)
                                continue;

                            if (parsed.metadata == null || string.IsNullOrEmpty(parsed.metadata.mapId))
                                continue;

                            string mapId = parsed.metadata.mapId.ToLowerInvariant();
                            var entry = new SongScriptEntry
                            {
                                FilePath = zipPath,
                                ZipEntryName = zipEntry.FullName,
                                MapId = mapId,
                                FileName = Path.GetFileName(zipEntry.FullName),
                                Metadata = parsed.metadata
                            };

                            if (!index.TryGetValue(mapId, out var list))
                            {
                                list = new List<SongScriptEntry>();
                                index[mapId] = list;
                            }
                            list.Add(entry);
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
        }

        /// <summary>
        /// 指定mapIdに一致するエントリ一覧を返す
        /// </summary>
        public static List<SongScriptEntry> GetScriptsByMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return new List<SongScriptEntry>();

            if (_mapIdIndex.TryGetValue(mapId, out var list))
                return list;

            return new List<SongScriptEntry>();
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
    }
}
