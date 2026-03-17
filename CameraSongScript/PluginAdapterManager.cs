using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CameraSongScript.Localization;

namespace CameraSongScript
{
    internal static class PluginAdapterManager
    {
        private sealed class AdapterVersionWarningInfo
        {
            internal AdapterVersionWarningInfo(string adapterFileName, string detectedVersionText, string supportedVersionsText)
            {
                AdapterFileName = adapterFileName;
                DetectedVersionText = detectedVersionText;
                SupportedVersionsText = supportedVersionsText;
            }

            internal string AdapterFileName { get; private set; }
            internal string DetectedVersionText { get; private set; }
            internal string SupportedVersionsText { get; private set; }
        }

        private static readonly string[] AdapterWarningOrder =
        {
            "CameraSongScript.HttpSiraStatus.dll",
            "CameraSongScript.BetterSongList.dll",
            "CameraSongScript.Cam2.dll",
            "CameraSongScript.CamPlus.dll"
        };

        private static readonly Dictionary<string, AdapterVersionWarningInfo> UnsupportedAdapterVersionWarnings =
            new Dictionary<string, AdapterVersionWarningInfo>(StringComparer.Ordinal);

        internal static event Action AdapterVersionWarningsChanged;

        internal static void ClearAllUnsupportedAdapterVersionWarnings()
        {
            if (UnsupportedAdapterVersionWarnings.Count == 0)
            {
                return;
            }

            UnsupportedAdapterVersionWarnings.Clear();
            NotifyAdapterVersionWarningsChanged();
        }

        internal static string GetUnsupportedAdapterVersionWarningText()
        {
            if (UnsupportedAdapterVersionWarnings.Count == 0)
            {
                return string.Empty;
            }

            List<string> warnings = new List<string>();
            for (int i = 0; i < AdapterWarningOrder.Length; i++)
            {
                string adapterFileName = AdapterWarningOrder[i];
                AdapterVersionWarningInfo warning;
                if (UnsupportedAdapterVersionWarnings.TryGetValue(adapterFileName, out warning))
                {
                    warnings.Add(UiLocalization.Format(
                        "warning-adapter-version-unsupported",
                        warning.AdapterFileName,
                        warning.DetectedVersionText,
                        warning.SupportedVersionsText));
                }
            }

            return string.Join("\n", warnings.ToArray());
        }

        internal static T TryCreateAdapterWithVersionCheck<T>(
            string assemblyName,
            string adapterFileName,
            IEnumerable<Version> supportedVersions,
            string typeName) where T : class
        {
            try
            {
                Assembly assembly = GetLoadedAssembly(assemblyName);
                Version detectedVersion = assembly != null ? assembly.GetName().Version : null;

                if (assembly == null)
                {
                    string adapterPath = ResolveAdapterDllPath(adapterFileName);
                    if (string.IsNullOrEmpty(adapterPath))
                    {
                        ClearUnsupportedAdapterVersionWarning(adapterFileName);
                        Plugin.Log.Error($"Adapter assembly '{adapterFileName}' was not found.");
                        return null;
                    }

                    if (!TryGetAssemblyVersion(adapterPath, out detectedVersion))
                    {
                        ClearUnsupportedAdapterVersionWarning(adapterFileName);
                        Plugin.Log.Error($"Failed to inspect version for adapter '{adapterFileName}'.");
                        return null;
                    }
                }

                if (!IsSupportedAdapterVersion(detectedVersion, supportedVersions))
                {
                    if (SetUnsupportedAdapterVersionWarning(
                        adapterFileName,
                        FormatVersion(detectedVersion),
                        FormatSupportedVersions(supportedVersions)))
                    {
                        Plugin.Log.Warn(
                            $"Adapter '{adapterFileName}' version {FormatVersion(detectedVersion)} is not supported. Allowed versions: {FormatSupportedVersions(supportedVersions)}.");
                    }

                    return null;
                }

                ClearUnsupportedAdapterVersionWarning(adapterFileName);
                return CreateAdapter<T>(assembly, assemblyName, typeName);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to prepare adapter '{typeName}': {ex.Message}");
                return null;
            }
        }

        private static T CreateAdapter<T>(Assembly assembly, string assemblyName, string typeName) where T : class
        {
            try
            {
                if (assembly == null)
                {
                    assembly = Assembly.Load(assemblyName);
                }

                Type type = assembly.GetType(typeName);
                if (type == null)
                {
                    Plugin.Log.Error($"Adapter type '{typeName}' not found in assembly '{assemblyName}'.");
                    return null;
                }

                return Activator.CreateInstance(type) as T;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to create adapter '{typeName}': {ex.Message}");
                return null;
            }
        }

        private static Assembly GetLoadedAssembly(string assemblyName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            return null;
        }

        internal static bool IsAssemblyLoaded(string assemblyName)
        {
            return GetLoadedAssembly(assemblyName) != null;
        }

        private static string ResolveAdapterDllPath(string adapterFileName)
        {
            if (string.IsNullOrEmpty(adapterFileName))
            {
                return null;
            }

            List<string> candidatePaths = new List<string>();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                candidatePaths.Add(Path.Combine(baseDirectory, "Libs", adapterFileName));
                candidatePaths.Add(Path.Combine(baseDirectory, "Plugins", adapterFileName));
                candidatePaths.Add(Path.Combine(baseDirectory, adapterFileName));
            }

            string executingAssemblyLocation = typeof(Plugin).Assembly.Location;
            if (!string.IsNullOrEmpty(executingAssemblyLocation))
            {
                string pluginDirectory = Path.GetDirectoryName(executingAssemblyLocation);
                if (!string.IsNullOrEmpty(pluginDirectory))
                {
                    candidatePaths.Add(Path.Combine(pluginDirectory, adapterFileName));

                    DirectoryInfo gameDirectory = Directory.GetParent(pluginDirectory);
                    if (gameDirectory != null)
                    {
                        candidatePaths.Add(Path.Combine(gameDirectory.FullName, "Libs", adapterFileName));
                        candidatePaths.Add(Path.Combine(gameDirectory.FullName, "Plugins", adapterFileName));
                        candidatePaths.Add(Path.Combine(gameDirectory.FullName, adapterFileName));
                    }
                }
            }

            foreach (string candidatePath in candidatePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return null;
        }

        private static bool TryGetAssemblyVersion(string assemblyPath, out Version version)
        {
            version = null;

            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                version = assemblyName != null ? assemblyName.Version : null;
                return version != null;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to inspect adapter assembly '{assemblyPath}': {ex.Message}");
                return false;
            }
        }

        private static bool IsSupportedAdapterVersion(Version detectedVersion, IEnumerable<Version> supportedVersions)
        {
            Version normalizedDetectedVersion = NormalizeVersion(detectedVersion);
            if (normalizedDetectedVersion == null || supportedVersions == null)
            {
                return false;
            }

            foreach (Version supportedVersion in supportedVersions)
            {
                Version normalizedSupportedVersion = NormalizeVersion(supportedVersion);
                if (normalizedSupportedVersion != null && normalizedSupportedVersion.Equals(normalizedDetectedVersion))
                {
                    return true;
                }
            }

            return false;
        }

        private static Version NormalizeVersion(Version version)
        {
            if (version == null)
            {
                return null;
            }

            return new Version(
                version.Major < 0 ? 0 : version.Major,
                version.Minor < 0 ? 0 : version.Minor,
                version.Build < 0 ? 0 : version.Build,
                version.Revision < 0 ? 0 : version.Revision);
        }

        private static string FormatVersion(Version version)
        {
            Version normalizedVersion = NormalizeVersion(version);
            return normalizedVersion != null ? normalizedVersion.ToString(4) : "unknown";
        }

        private static string FormatSupportedVersions(IEnumerable<Version> versions)
        {
            if (versions == null)
            {
                return "none";
            }

            return string.Join(", ", versions.Select(FormatVersion).ToArray());
        }

        private static bool SetUnsupportedAdapterVersionWarning(
            string adapterFileName,
            string detectedVersionText,
            string supportedVersionsText)
        {
            AdapterVersionWarningInfo existingWarning;
            if (UnsupportedAdapterVersionWarnings.TryGetValue(adapterFileName, out existingWarning) &&
                string.Equals(existingWarning.DetectedVersionText, detectedVersionText, StringComparison.Ordinal) &&
                string.Equals(existingWarning.SupportedVersionsText, supportedVersionsText, StringComparison.Ordinal))
            {
                return false;
            }

            UnsupportedAdapterVersionWarnings[adapterFileName] = new AdapterVersionWarningInfo(
                adapterFileName,
                detectedVersionText,
                supportedVersionsText);

            NotifyAdapterVersionWarningsChanged();
            return true;
        }

        internal static bool HasUnsupportedAdapterVersionWarning(string adapterFileName)
        {
            return UnsupportedAdapterVersionWarnings.ContainsKey(adapterFileName);
        }

        private static void ClearUnsupportedAdapterVersionWarning(string adapterFileName)
        {
            if (UnsupportedAdapterVersionWarnings.Remove(adapterFileName))
            {
                NotifyAdapterVersionWarningsChanged();
            }
        }

        private static void NotifyAdapterVersionWarningsChanged()
        {
            Action handler = AdapterVersionWarningsChanged;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
