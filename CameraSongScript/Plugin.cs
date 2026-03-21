using System;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Installers;
using CameraSongScript.Interfaces;
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
        private static Version ExpectedAdapterVersion => typeof(Plugin).Assembly.GetName().Version;

        internal static IPALogger Log { get; private set; }
        internal static ICameraHelper CamHelper { get; private set; }
        internal static ICameraPlusHelper CamPlusHelper { get; private set; }
        internal static IHttpSiraStatusHelper HttpSiraStatusHelper { get; private set; }
        internal static IBetterSongListHelper BetterSongListHelper { get; private set; }
        internal static bool IsCamHelperReady => CamHelper != null && CamHelper.IsInitialized;
        internal static bool IsCamPlusHelperReady => CamPlusHelper != null && CamPlusHelper.IsInitialized;
        internal static bool IsHttpSiraStatusHelperReady => HttpSiraStatusHelper != null && HttpSiraStatusHelper.IsInitialized;
        internal static bool IsBetterSongListHelperReady => BetterSongListHelper != null && BetterSongListHelper.IsInitialized;
        internal static event Action SongDetailsCacheInitialized;

        internal static SongDetailsCache.SongDetails SongDetailsInstance { get; private set; }
        internal static bool IsSongDetailsReady => SongDetailsInstance != null;

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
            PluginAdapterManager.ClearAllUnsupportedAdapterVersionWarnings();

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
                CamHelper = PluginAdapterManager.TryCreateAdapterWithVersionCheck<ICameraHelper>(
                    "CameraSongScript.Cam2",
                    "CameraSongScript.Cam2.dll",
                    ExpectedAdapterVersion,
                    "CameraSongScript.Cam2.Camera2Helper");
                if (CamHelper == null)
                {
                    if (!PluginAdapterManager.HasUnsupportedAdapterVersionWarning("CameraSongScript.Cam2.dll"))
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

                CamPlusHelper = null;
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                CamPlusHelper = PluginAdapterManager.TryCreateAdapterWithVersionCheck<ICameraPlusHelper>(
                    "CameraSongScript.CamPlus",
                    "CameraSongScript.CamPlus.dll",
                    ExpectedAdapterVersion,
                    "CameraSongScript.CamPlus.CameraPlusHelper");
                if (CamPlusHelper == null)
                {
                    if (!PluginAdapterManager.HasUnsupportedAdapterVersionWarning("CameraSongScript.CamPlus.dll"))
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

                CamHelper = null;
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

            if (!PluginAdapterManager.IsAssemblyLoaded("HttpSiraStatus"))
            {
                return;
            }

            HttpSiraStatusHelper = PluginAdapterManager.TryCreateAdapterWithVersionCheck<IHttpSiraStatusHelper>(
                "CameraSongScript.HttpSiraStatus",
                "CameraSongScript.HttpSiraStatus.dll",
                ExpectedAdapterVersion,
                "CameraSongScript.HttpSiraStatus.HttpSiraStatusHelper");

            if (HttpSiraStatusHelper == null)
            {
                if (!PluginAdapterManager.HasUnsupportedAdapterVersionWarning("CameraSongScript.HttpSiraStatus.dll"))
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

            if (!PluginAdapterManager.IsAssemblyLoaded("BetterSongList"))
            {
                return;
            }

            BetterSongListHelper = PluginAdapterManager.TryCreateAdapterWithVersionCheck<IBetterSongListHelper>(
                "CameraSongScript.BetterSongList",
                "CameraSongScript.BetterSongList.dll",
                ExpectedAdapterVersion,
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
            return PluginAdapterManager.GetUnsupportedAdapterVersionWarningText();
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
                SongDetailsCacheInitialized?.Invoke();
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
