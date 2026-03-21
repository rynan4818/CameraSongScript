using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CameraSongScript.Localization;
using IPA.Loader;
using Version = System.Version;

namespace CameraSongScript
{
    internal static class PluginAdapterManager
    {
        private sealed class AdapterWarningSpec
        {
            internal AdapterWarningSpec(string pluginId, string adapterFileName)
            {
                PluginId = pluginId;
                AdapterFileName = adapterFileName;
            }

            internal string PluginId { get; private set; }
            internal string AdapterFileName { get; private set; }
        }

        private static readonly AdapterWarningSpec[] AdapterWarningOrder =
        {
            new AdapterWarningSpec("CameraSongScript.HttpSiraStatus", "CameraSongScript.HttpSiraStatus.dll"),
            new AdapterWarningSpec("CameraSongScript.BetterSongList", "CameraSongScript.BetterSongList.dll"),
            new AdapterWarningSpec("CameraSongScript.Cam2", "CameraSongScript.Cam2.dll"),
            new AdapterWarningSpec("CameraSongScript.CamPlus", "CameraSongScript.CamPlus.dll")
        };

        private static readonly Regex CoreDependencyMismatchPattern =
            new Regex("^Dependency 'CameraSongScript@.+?' not found$", RegexOptions.CultureInvariant);

        internal static event Action AdapterVersionWarningsChanged;

        internal static void ClearAllUnsupportedAdapterVersionWarnings()
        {
            NotifyAdapterVersionWarningsChanged();
        }

        internal static string GetUnsupportedAdapterVersionWarningText()
        {
            List<string> warnings = new List<string>();
            for (int i = 0; i < AdapterWarningOrder.Length; i++)
            {
                string warning = TryGetUnsupportedAdapterVersionWarningText(AdapterWarningOrder[i]);
                if (!string.IsNullOrEmpty(warning))
                {
                    warnings.Add(warning);
                }
            }

            return string.Join("\n", warnings.ToArray());
        }

        internal static bool HasUnsupportedAdapterVersionWarning(string adapterFileName)
        {
            return AdapterWarningOrder.Any(spec =>
                string.Equals(spec.AdapterFileName, adapterFileName, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(TryGetUnsupportedAdapterVersionWarningText(spec)));
        }

        internal static void NotifyAdapterStateChanged()
        {
            NotifyAdapterVersionWarningsChanged();
        }

        private static void NotifyAdapterVersionWarningsChanged()
        {
            Action handler = AdapterVersionWarningsChanged;
            if (handler != null)
            {
                handler();
            }
        }

        private static string TryGetUnsupportedAdapterVersionWarningText(AdapterWarningSpec spec)
        {
            if (PluginManager.GetPluginFromId(spec.PluginId) != null)
            {
                return string.Empty;
            }

            if (PluginManager.GetDisabledPluginFromId(spec.PluginId) != null)
            {
                return string.Empty;
            }

            foreach (var kvp in PluginManager.IgnoredPlugins)
            {
                if (!MatchesSpec(kvp.Key, spec))
                {
                    continue;
                }

                if (!IsCoreVersionMismatchReason(kvp.Value))
                {
                    return string.Empty;
                }

                return UiLocalization.Format(
                    "warning-adapter-version-unsupported",
                    spec.AdapterFileName,
                    GetAdapterAssemblyVersionText(kvp.Key),
                    FormatVersion(typeof(Plugin).Assembly.GetName().Version));
            }

            return string.Empty;
        }

        private static bool MatchesSpec(PluginMetadata metadata, AdapterWarningSpec spec)
        {
            if (metadata == null)
            {
                return false;
            }

            string metadataId = TryGetMetadataId(metadata);
            if (!string.IsNullOrEmpty(metadataId)
                && string.Equals(metadataId, spec.PluginId, StringComparison.Ordinal))
            {
                return true;
            }

            string adapterFileName = TryGetMetadataFileName(metadata);
            return !string.IsNullOrEmpty(adapterFileName)
                && string.Equals(adapterFileName, spec.AdapterFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCoreVersionMismatchReason(IgnoreReason reason)
        {
            return reason.Reason == Reason.Dependency
                && !string.IsNullOrEmpty(reason.ReasonText)
                && CoreDependencyMismatchPattern.IsMatch(reason.ReasonText);
        }

        private static string TryGetMetadataId(PluginMetadata metadata)
        {
            try
            {
                return metadata.Id;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TryGetMetadataFileName(PluginMetadata metadata)
        {
            try
            {
                return metadata.File?.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetAdapterAssemblyVersionText(PluginMetadata metadata)
        {
            string adapterPath = TryGetMetadataFilePath(metadata);
            if (string.IsNullOrEmpty(adapterPath) || !System.IO.File.Exists(adapterPath))
            {
                return "unknown";
            }

            try
            {
                return FormatVersion(AssemblyName.GetAssemblyName(adapterPath).Version);
            }
            catch
            {
                return "unknown";
            }
        }

        private static string TryGetMetadataFilePath(PluginMetadata metadata)
        {
            if (metadata == null)
            {
                return string.Empty;
            }

            try
            {
                return metadata.File?.FullName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FormatVersion(Version version)
        {
            if (version == null)
            {
                return "unknown";
            }

            int build = version.Build < 0 ? 0 : version.Build;
            int revision = version.Revision < 0 ? 0 : version.Revision;
            return new Version(version.Major, version.Minor, build, revision).ToString(4);
        }
    }
}
