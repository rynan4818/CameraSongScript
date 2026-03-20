using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CameraSongScript.Services
{
    internal static class SongCoreBeatmapLevelAccessor
    {
        public static IEnumerable<BeatmapLevel> GetCustomBeatmapLevels()
        {
            var beatmapLevelsModel = SongCore.Loader.BeatmapLevelsModelSO;
            if (beatmapLevelsModel == null)
            {
                return Enumerable.Empty<BeatmapLevel>();
            }

            return beatmapLevelsModel
                ._customLevelsRepository?.beatmapLevelPacks
                .Where(pack => pack is SongCore.OverrideClasses.SongCoreCustomBeatmapLevelPack)
                .SelectMany(pack => pack.AllBeatmapLevels()) ??
                Enumerable.Empty<BeatmapLevel>();
        }

        public static string GetLevelFolderPath(BeatmapLevel level)
        {
            if (level == null || string.IsNullOrEmpty(level.levelID))
            {
                return string.Empty;
            }

            var customLevelLoader = SongCore.Loader.CustomLevelLoader;
            if (customLevelLoader != null &&
                customLevelLoader._loadedBeatmapSaveData != null &&
                customLevelLoader._loadedBeatmapSaveData.TryGetValue(level.levelID, out var loadedSaveData) &&
                !string.IsNullOrEmpty(loadedSaveData.customLevelFolderInfo.folderPath))
            {
                return NormalizeFolderPath(loadedSaveData.customLevelFolderInfo.folderPath);
            }

            if (level.previewMediaData is FileSystemPreviewMediaData previewMediaData)
            {
                string folderPath = NormalizeFolderPath(Path.GetDirectoryName(previewMediaData._coverSpritePath));
                if (!string.IsNullOrEmpty(folderPath))
                {
                    return folderPath;
                }

                folderPath = NormalizeFolderPath(Path.GetDirectoryName(previewMediaData._previewAudioClipPath));
                if (!string.IsNullOrEmpty(folderPath))
                {
                    return folderPath;
                }
            }

            return string.Empty;
        }

        public static string GetLevelHash(BeatmapLevel level)
        {
            if (level == null || string.IsNullOrEmpty(level.levelID))
            {
                return string.Empty;
            }

            try
            {
                return SongCore.Collections.GetCustomLevelHash(level.levelID) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongCoreBeatmapLevelAccessor: Failed to resolve hash for '{level.levelID}': {ex.Message}");
                return string.Empty;
            }
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
    }
}
