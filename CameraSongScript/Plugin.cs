using CameraSongScript.Configuration;
using CameraSongScript.Helpers;
using CameraSongScript.Installers;
using HarmonyLib;
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
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static Camera2ReflectionHelper Cam2Helper { get; private set; }

        private Harmony _harmony;
        private const string HarmonyId = "com.github.camerasongscript";

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;

            CameraSongScriptConfig.Instance = conf.Generated<CameraSongScriptConfig>();
            Log.Info("CameraSongScript initialized.");

            // Zenjectインストーラー登録
            zenjector.Install<CameraSongScriptMenuInstaller>(Location.Menu);
            zenjector.Install<CameraSongScriptPlayerInstaller>(Location.Player);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            // 1. カメラMod検出
            CameraModDetector.Detect();

            // 2. ヘルパー初期化
            if (CameraModDetector.IsCamera2)
            {
                Cam2Helper = new Camera2ReflectionHelper();
                if (!Cam2Helper.Initialize())
                {
                    Log.Error("Camera2ReflectionHelper initialization failed.");
                    Cam2Helper = null;
                }
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                if (!CameraPlusHarmonyHelper.Initialize())
                {
                    Log.Error("CameraPlusHarmonyHelper initialization failed.");
                }
            }

            // 3. Harmonyパッチ適用
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Info("CameraSongScript started.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
            _harmony?.UnpatchSelf();
        }
    }
}
