using System;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Installers;
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
        internal static IPALogger Log { get; private set; }
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
            PluginAdapterManager.NotifyAdapterStateChanged();

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
            Log.Info("CameraSongScript started.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
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
