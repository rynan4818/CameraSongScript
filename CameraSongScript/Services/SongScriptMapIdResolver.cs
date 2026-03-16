using System;

namespace CameraSongScript.Services
{
    internal static class SongScriptMapIdResolver
    {
        public static string ResolveMapIdFromLevelId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || !Plugin.IsSongDetailsReady)
            {
                return null;
            }

            string hash = SongCore.Collections.hashForLevelID(levelId);
            if (string.IsNullOrEmpty(hash))
            {
                hash = ExtractHashFromLevelId(levelId);
            }

            if (string.IsNullOrEmpty(hash))
            {
                return null;
            }

            try
            {
                if (Plugin.SongDetailsInstance.songs.FindByHash(hash, out var song))
                {
                    return song.key;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"SongScriptMapIdResolver: SongDetailsCache lookup failed: {ex.Message}");
            }

            return null;
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
    }
}
