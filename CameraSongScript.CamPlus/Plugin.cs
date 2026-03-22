using CameraSongScript.CamPlus.Installers;
using HarmonyLib;
using IPA;
using SiraUtil.Zenject;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.CamPlus
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        private const string HarmonyId = "com.github.rynan4818.CameraSongScript.CamPlus";
        private Harmony _harmony;

        [Init]
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            Log = logger;
            Log.Info("CameraSongScript.CamPlus initialized.");

            zenjector.Install<CameraSongScriptCamPlusAppInstaller>(Location.App);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
            _harmony?.UnpatchSelf();
            _harmony = null;
        }
    }
}
