using IPA;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.Cam2
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            Log = logger;
            Log.Info("CameraSongScript.Cam2 initialized.");

            zenjector.Install<Installers.CameraSongScriptCam2AppInstaller>(Location.App);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
        }
    }
}
