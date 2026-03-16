using System;

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
        public static SongScriptLevelReference ResolveLevelReferenceFromLevelId(string levelId)
        {
            var levelReference = new SongScriptLevelReference();
            if (string.IsNullOrEmpty(levelId))
            {
                return levelReference;
            }

            string hash = SongCore.Collections.hashForLevelID(levelId);
            if (string.IsNullOrEmpty(hash))
            {
                hash = ExtractHashFromLevelId(levelId);
            }

            levelReference.Hash = NormalizeLookupKey(hash);

            if (string.IsNullOrEmpty(hash) || !Plugin.IsSongDetailsReady)
            {
                return levelReference;
            }

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

            return levelReference;
        }

        public static string ResolveMapIdFromLevelId(string levelId)
        {
            return ResolveLevelReferenceFromLevelId(levelId).MapId;
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
    }
}
