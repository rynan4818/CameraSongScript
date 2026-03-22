using IPA;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.BetterSongList
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            Log = logger;
            Log.Info("CameraSongScript.BetterSongList initialized.");

            zenjector.Install<Installers.BetterSongListAppInstaller>(Location.App);
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
