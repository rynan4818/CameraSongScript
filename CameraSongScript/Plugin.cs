using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Installers;
using CameraSongScript.Interfaces;
using CameraSongScript.Localization;
using CameraSongScript.Models;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
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

        private static readonly Version[] SupportedHttpSiraStatusAdapterVersions =
        {
            new Version(0, 0, 1, 0)
        };

        private static readonly Version[] SupportedBetterSongListAdapterVersions =
        {
            new Version(0, 0, 1, 0)
        };

        private static readonly Version[] SupportedCam2AdapterVersions =
        {
            new Version(0, 0, 1, 0)
        };

        private static readonly Version[] SupportedCamPlusAdapterVersions =
        {
            new Version(0, 0, 1, 0)
        };

        private static readonly string[] AdapterWarningOrder =
        {
            "CameraSongScript.HttpSiraStatus.dll",
            "CameraSongScript.BetterSongList.dll",
            "CameraSongScript.Cam2.dll",
            "CameraSongScript.CamPlus.dll"
        };

        private static readonly Dictionary<string, AdapterVersionWarningInfo> _unsupportedAdapterVersionWarnings =
            new Dictionary<string, AdapterVersionWarningInfo>(StringComparer.Ordinal);

        internal static IPALogger Log { get; private set; }
        internal static ICameraHelper CamHelper { get; private set; }
        internal static ICameraPlusHelper CamPlusHelper { get; private set; }
        internal static IHttpSiraStatusHelper HttpSiraStatusHelper { get; private set; }
        internal static IBetterSongListHelper BetterSongListHelper { get; private set; }
        internal static bool IsCamHelperReady => CamHelper != null && CamHelper.IsInitialized;
        internal static bool IsCamPlusHelperReady => CamPlusHelper != null && CamPlusHelper.IsInitialized;
        internal static bool IsHttpSiraStatusHelperReady => HttpSiraStatusHelper != null && HttpSiraStatusHelper.IsInitialized;
        internal static bool IsBetterSongListHelperReady => BetterSongListHelper != null && BetterSongListHelper.IsInitialized;

        internal static SongDetailsCache.SongDetails SongDetailsInstance { get; private set; }
        internal static bool IsSongDetailsReady => SongDetailsInstance != null;

        internal static event Action AdapterVersionWarningsChanged;

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Log = logger;

            CameraSongScriptConfig.Instance = conf.Generated<CameraSongScriptConfig>();
            Log.Info("CameraSongScript initialized.");

            // Zenjectインストーラー登録
            zenjector.Install<CameraSongScriptAppInstaller>(Location.App);
            zenjector.Install<CameraSongScriptPlayerInstaller>(Location.Player);
            zenjector.Install<CameraSongScriptMenuInstaller>(Location.Menu);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
            ClearAllUnsupportedAdapterVersionWarnings();

            // 1. カメラMod検出
            CameraModDetector.Detect();

            // 2. SongDetailsCacheの初期化（非同期、初回はデータダウンロードの可能性あり）
            _ = InitSongDetailsCacheAsync();

            // 3. SongScriptsフォルダのスキャン・キャッシュ構築（非同期）
            var songScriptScanTask = SongScriptFolderCache.ScanAsync();
            _ = ReevaluateSelectedLevelWhenReady(songScriptScanTask, "SongScripts");

            // 4. CommonScriptsフォルダのスキャン・キャッシュ構築（非同期）
            var commonScriptScanTask = CommonScriptCache.ScanAsync();
            _ = ReevaluateSelectedLevelWhenReady(commonScriptScanTask, "CommonScripts");

            // 5. アダプタ初期化（対応Modがインストールされている場合のみアダプタDLLをロード）
            if (CameraModDetector.IsCamera2)
            {
                CamHelper = TryCreateAdapterWithVersionCheck<ICameraHelper>(
                    "CameraSongScript.Cam2",
                    "CameraSongScript.Cam2.dll",
                    SupportedCam2AdapterVersions,
                    "CameraSongScript.Cam2.Camera2Helper");
                if (CamHelper == null)
                {
                    if (!HasUnsupportedAdapterVersionWarning("CameraSongScript.Cam2.dll"))
                    {
                        Log.Error("Camera2 adapter initialization failed.");
                    }

                    CamHelper = null;
                }
                else if (!CamHelper.Initialize())
                {
                    Log.Error("Camera2 adapter initialization failed.");
                    CamHelper = null;
                }
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                CamPlusHelper = TryCreateAdapterWithVersionCheck<ICameraPlusHelper>(
                    "CameraSongScript.CamPlus",
                    "CameraSongScript.CamPlus.dll",
                    SupportedCamPlusAdapterVersions,
                    "CameraSongScript.CamPlus.CameraPlusHelper");
                if (CamPlusHelper == null)
                {
                    if (!HasUnsupportedAdapterVersionWarning("CameraSongScript.CamPlus.dll"))
                    {
                        Log.Error("CameraPlus adapter initialization failed.");
                    }

                    CamPlusHelper = null;
                }
                else if (!CamPlusHelper.Initialize())
                {
                    Log.Error("CameraPlus adapter initialization failed.");
                    CamPlusHelper = null;
                }
            }

            EnsureHttpSiraStatusHelperLoaded();
            EnsureBetterSongListHelperLoaded();
            Log.Info("CameraSongScript started.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
        }

        internal static void EnsureHttpSiraStatusHelperLoaded()
        {
            if (IsHttpSiraStatusHelperReady)
            {
                return;
            }

            if (!IsAssemblyLoaded("HttpSiraStatus"))
            {
                return;
            }

            HttpSiraStatusHelper = TryCreateAdapterWithVersionCheck<IHttpSiraStatusHelper>(
                "CameraSongScript.HttpSiraStatus",
                "CameraSongScript.HttpSiraStatus.dll",
                SupportedHttpSiraStatusAdapterVersions,
                "CameraSongScript.HttpSiraStatus.HttpSiraStatusHelper");

            if (HttpSiraStatusHelper == null)
            {
                if (!HasUnsupportedAdapterVersionWarning("CameraSongScript.HttpSiraStatus.dll"))
                {
                    Log.Error("HttpSiraStatus adapter initialization failed.");
                }

                HttpSiraStatusHelper = null;
            }
            else if (!HttpSiraStatusHelper.Initialize())
            {
                Log.Error("HttpSiraStatus adapter initialization failed.");
                HttpSiraStatusHelper = null;
            }
        }

        internal static void EnsureBetterSongListHelperLoaded()
        {
            if (IsBetterSongListHelperReady)
            {
                return;
            }

            if (!IsAssemblyLoaded("BetterSongList"))
            {
                return;
            }

            BetterSongListHelper = TryCreateAdapterWithVersionCheck<IBetterSongListHelper>(
                "CameraSongScript.BetterSongList",
                "CameraSongScript.BetterSongList.dll",
                SupportedBetterSongListAdapterVersions,
                "CameraSongScript.BetterSongList.BetterSongListHelper");

            if (BetterSongListHelper == null)
            {
                return;
            }

            if (!BetterSongListHelper.Initialize())
            {
                Log.Warn("BetterSongList helper initialization did not complete successfully.");
                return;
            }

            Log.Info("BetterSongList helper initialized.");
        }

        internal static string GetUnsupportedAdapterVersionWarningText()
        {
            if (_unsupportedAdapterVersionWarnings.Count == 0)
            {
                return string.Empty;
            }

            List<string> warnings = new List<string>();
            for (int i = 0; i < AdapterWarningOrder.Length; i++)
            {
                string adapterFileName = AdapterWarningOrder[i];
                AdapterVersionWarningInfo warning;
                if (_unsupportedAdapterVersionWarnings.TryGetValue(adapterFileName, out warning))
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

        /// <summary>
        /// アダプタDLLからインターフェース実装を動的に生成する
        /// コアプロジェクトがアダプタDLLをコンパイル時に参照しないため循環依存を回避する
        /// </summary>
        private static T TryCreateAdapterWithVersionCheck<T>(
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
                        Log.Error($"Adapter assembly '{adapterFileName}' was not found.");
                        return null;
                    }

                    if (!TryGetAssemblyVersion(adapterPath, out detectedVersion))
                    {
                        ClearUnsupportedAdapterVersionWarning(adapterFileName);
                        Log.Error($"Failed to inspect version for adapter '{adapterFileName}'.");
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
                        Log.Warn(
                            $"Adapter '{adapterFileName}' version {FormatVersion(detectedVersion)} is not supported. Allowed versions: {FormatSupportedVersions(supportedVersions)}.");
                    }

                    return null;
                }

                ClearUnsupportedAdapterVersionWarning(adapterFileName);
                return CreateAdapter<T>(assembly, assemblyName, typeName);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to prepare adapter '{typeName}': {ex.Message}");
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

                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    Log.Error($"Adapter type '{typeName}' not found in assembly '{assemblyName}'.");
                    return null;
                }
                return Activator.CreateInstance(type) as T;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create adapter '{typeName}': {ex.Message}");
                return null;
            }
        }

        private static Assembly GetLoadedAssembly(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            return null;
        }

        private static bool IsAssemblyLoaded(string assemblyName)
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
                Log.Error($"Failed to inspect adapter assembly '{assemblyPath}': {ex.Message}");
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
            if (_unsupportedAdapterVersionWarnings.TryGetValue(adapterFileName, out existingWarning) &&
                string.Equals(existingWarning.DetectedVersionText, detectedVersionText, StringComparison.Ordinal) &&
                string.Equals(existingWarning.SupportedVersionsText, supportedVersionsText, StringComparison.Ordinal))
            {
                return false;
            }

            _unsupportedAdapterVersionWarnings[adapterFileName] = new AdapterVersionWarningInfo(
                adapterFileName,
                detectedVersionText,
                supportedVersionsText);

            NotifyAdapterVersionWarningsChanged();
            return true;
        }

        private static bool HasUnsupportedAdapterVersionWarning(string adapterFileName)
        {
            return _unsupportedAdapterVersionWarnings.ContainsKey(adapterFileName);
        }

        private static void ClearUnsupportedAdapterVersionWarning(string adapterFileName)
        {
            if (_unsupportedAdapterVersionWarnings.Remove(adapterFileName))
            {
                NotifyAdapterVersionWarningsChanged();
            }
        }

        private static void ClearAllUnsupportedAdapterVersionWarnings()
        {
            if (_unsupportedAdapterVersionWarnings.Count == 0)
            {
                return;
            }

            _unsupportedAdapterVersionWarnings.Clear();
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

        /// <summary>
        /// SongDetailsCacheを非同期で初期化する
        /// </summary>
        private static async Task InitSongDetailsCacheAsync()
        {
            try
            {
                SongDetailsInstance = await SongDetailsCache.SongDetails.Init();
                Log.Info("SongDetailsCache initialized successfully.");
                CameraSongScriptDetector.Instance?.ReevaluateCurrentLevel();
            }
            catch (Exception ex)
            {
                Log.Warn($"SongDetailsCache initialization failed: {ex.Message}");
            }
        }

        private static async Task ReevaluateSelectedLevelWhenReady(Task cacheScanTask, string cacheName)
        {
            try
            {
                await cacheScanTask;
                CameraSongScriptDetector.Instance?.ReevaluateCurrentLevel();
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to re-evaluate the selected level after {cacheName} scan: {ex.Message}");
            }
        }
    }
}
