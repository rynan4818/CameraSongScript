using System;
using System.IO;
using CameraSongScript.Configuration;
using IPA.Utilities;

namespace CameraSongScript.Utilities
{
    internal static class ScriptFolderPathResolver
    {
        private const string DefaultCommonScriptsFolderPath = @"UserData\CameraSongScript\CommonScripts";
        private const string DefaultSongScriptsFolderPath = @"UserData\CameraSongScript\SongScripts";

        public static string GetCommonScriptsFolderPath()
        {
            return ResolveConfiguredPath(
                CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.CommonScriptsFolderPath : null,
                DefaultCommonScriptsFolderPath);
        }

        public static string GetSongScriptsFolderPath()
        {
            return ResolveConfiguredPath(
                CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.SongScriptsFolderPath : null,
                DefaultSongScriptsFolderPath);
        }

        private static string ResolveConfiguredPath(string configuredPath, string defaultRelativePath)
        {
            string candidatePath = string.IsNullOrWhiteSpace(configuredPath)
                ? defaultRelativePath
                : configuredPath.Trim();

            candidatePath = Environment.ExpandEnvironmentVariables(candidatePath);

            if (!Path.IsPathRooted(candidatePath))
            {
                candidatePath = Path.Combine(GetGameRootPath(), candidatePath);
            }

            return NormalizePath(candidatePath);
        }

        private static string GetGameRootPath()
        {
            string userDataPath = NormalizePath(UnityGame.UserDataPath);
            if (string.IsNullOrEmpty(userDataPath))
            {
                return Directory.GetCurrentDirectory();
            }

            string gameRootPath = Path.GetDirectoryName(userDataPath);
            return string.IsNullOrEmpty(gameRootPath) ? userDataPath : gameRootPath;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }
    }
}
