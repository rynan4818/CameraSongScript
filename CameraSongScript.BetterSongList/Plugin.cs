using CameraSongScript.Interfaces;
using IPA;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.BetterSongList
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        private IBetterSongListHelper _helper;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("CameraSongScript.BetterSongList initialized.");

            var helper = new BetterSongListHelper();
            _helper = helper;
            global::CameraSongScript.AdapterRegistry.RegisterBetterSongListHelper(helper);

            if (helper.Initialize())
            {
                Log.Info("BetterSongList adapter registered.");
            }
            else
            {
                Log.Warn("BetterSongList adapter registration did not complete successfully.");
            }
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

            if (_helper != null)
            {
                global::CameraSongScript.AdapterRegistry.UnregisterBetterSongListHelper(_helper);
                _helper = null;
            }
        }
    }
}
