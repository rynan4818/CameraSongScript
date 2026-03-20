using System;
using System.IO;

namespace CameraSongScript.Services
{
    internal sealed class SongScriptLevelReference
    {
        public string MapId { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;

        public bool HasAnyValue => !string.IsNullOrEmpty(MapId) || !string.IsNullOrEmpty(Hash);
    }

    internal static class SongScriptMapIdResolver
    {
        public static SongScriptLevelReference ResolveLevelReferenceFromLevelId(string levelId, string levelPath = null)
        {
            var levelReference = new SongScriptLevelReference();
            if (string.IsNullOrEmpty(levelId))
            {
                levelReference.MapId = ExtractMapIdFromLevelPath(levelPath);
                return levelReference;
            }

            string hash = SongCore.Collections.GetCustomLevelHash(levelId);
            if (string.IsNullOrEmpty(hash))
            {
                hash = ExtractHashFromLevelId(levelId);
            }

            levelReference.Hash = NormalizeLookupKey(hash);

            if (!string.IsNullOrEmpty(hash) && Plugin.IsSongDetailsReady)
            {
                try
                {
                    if (Plugin.SongDetailsInstance.songs.FindByHash(hash, out var song))
                    {
                        levelReference.MapId = NormalizeLookupKey(song.key);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug($"SongScriptMapIdResolver: SongDetailsCache lookup failed: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(levelReference.MapId))
            {
                string fallbackMapId = ExtractMapIdFromLevelPath(levelPath);
                if (!string.IsNullOrEmpty(fallbackMapId))
                {
                    levelReference.MapId = fallbackMapId;
#if DEBUG
                    Plugin.Log.Debug(
                        $"SongScriptMapIdResolver: Resolved mapId '{fallbackMapId}' from beatmap folder name '{levelPath}'.");
#endif
                }
            }

            return levelReference;
        }

        public static string ResolveMapIdFromLevelId(string levelId, string levelPath = null)
        {
            return ResolveLevelReferenceFromLevelId(levelId, levelPath).MapId;
        }

        private static string ExtractHashFromLevelId(string levelId)
        {
            const string prefix = "custom_level_";
            if (!levelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            int hashStartIndex = prefix.Length;
            if (levelId.Length < hashStartIndex + 40)
            {
                return null;
            }

            return levelId.Substring(hashStartIndex, 40);
        }

        private static string NormalizeLookupKey(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.ToLowerInvariant();
        }

        private static string ExtractMapIdFromLevelPath(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath))
            {
                return string.Empty;
            }

            try
            {
                string trimmedPath = levelPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrEmpty(trimmedPath))
                {
                    return string.Empty;
                }

                string folderName = Path.GetFileName(trimmedPath);
                return ExtractLeadingMapIdCandidate(folderName);
            }
            catch
            {
                return string.Empty;
            }
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

        private static bool IsHexCharacter(char value)
        {
            return (value >= '0' && value <= '9') ||
                (value >= 'a' && value <= 'f') ||
                (value >= 'A' && value <= 'F');
        }
    }
}
