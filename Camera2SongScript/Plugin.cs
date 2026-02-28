using Camera2SongScript.Configuration;
using Camera2SongScript.Installers;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace Camera2SongScript
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        private Harmony _harmony;
        private const string HarmonyId = "com.github.camera2songscript";

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;

            SongScriptConfig.Instance = conf.Generated<SongScriptConfig>();
            Log.Info("Camera2SongScript initialized.");

            // Zenjectインストーラー登録
            zenjector.Install<Camera2SongScriptAppInstaller>(Location.App);
            zenjector.Install<Camera2SongScriptMenuInstaller>(Location.Menu);
            zenjector.Install<Camera2SongScriptPlayerInstaller>(Location.Player);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            // Harmonyパッチ適用
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Info("Camera2SongScript started.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
            _harmony?.UnpatchSelf();
        }
    }
}
