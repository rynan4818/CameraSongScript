using System;
using System.Threading.Tasks;
using CameraSongScript.Configuration;
using CameraSongScript.Installers;
using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        internal static ICameraHelper CamHelper { get; private set; }
        internal static ICameraPlusHelper CamPlusHelper { get; private set; }
        internal static bool IsCamHelperReady => CamHelper != null && CamHelper.IsInitialized;
        internal static bool IsCamPlusHelperReady => CamPlusHelper != null && CamPlusHelper.IsInitialized;

        internal static SongDetailsCache.SongDetails SongDetailsInstance { get; private set; }
        internal static bool IsSongDetailsReady => SongDetailsInstance != null;

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Log = logger;

            CameraSongScriptConfig.Instance = conf.Generated<CameraSongScriptConfig>();
            Log.Info("CameraSongScript initialized.");

            // Zenjectインストーラー登録
            zenjector.Install<CameraSongScriptPlayerInstaller>(Location.Player);
            zenjector.Install<CameraSongScriptMenuInstaller>(Location.Menu);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            // 1. カメラMod検出
            CameraModDetector.Detect();

            // 2. SongDetailsCacheの初期化（非同期、初回はデータダウンロードの可能性あり）
            _ = InitSongDetailsCacheAsync();

            // 3. SongScriptフォルダのスキャン・キャッシュ構築（非同期）
            _ = SongScriptFolderCache.ScanAsync();

            // 4. アダプタ初期化（対応Modがインストールされている場合のみアダプタDLLをロード）
            if (CameraModDetector.IsCamera2)
            {
                CamHelper = CreateAdapter<ICameraHelper>("CameraSongScript.Cam2", "CameraSongScript.Cam2.Camera2Helper");
                if (CamHelper == null || !CamHelper.Initialize())
                {
                    Log.Error("Camera2 adapter initialization failed.");
                    CamHelper = null;
                }
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                CamPlusHelper = CreateAdapter<ICameraPlusHelper>("CameraSongScript.CamPlus", "CameraSongScript.CamPlus.CameraPlusHelper");
                if (CamPlusHelper == null || !CamPlusHelper.Initialize())
                {
                    Log.Error("CameraPlus adapter initialization failed.");
                    CamPlusHelper = null;
                }
            }

            Log.Info("CameraSongScript started.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
        }

        /// <summary>
        /// アダプタDLLからインターフェース実装を動的に生成する
        /// コアプロジェクトがアダプタDLLをコンパイル時に参照しないため循環依存を回避する
        /// </summary>
        private static T CreateAdapter<T>(string assemblyName, string typeName) where T : class
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
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

        /// <summary>
        /// SongDetailsCacheを非同期で初期化する
        /// </summary>
        private static async Task InitSongDetailsCacheAsync()
        {
            try
            {
                SongDetailsInstance = await SongDetailsCache.SongDetails.Init();
                Log.Info("SongDetailsCache initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Warn($"SongDetailsCache initialization failed: {ex.Message}");
            }
        }
    }
}
