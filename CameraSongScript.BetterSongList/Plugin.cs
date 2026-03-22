using IPA;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.BetterSongList
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("CameraSongScript.BetterSongList initialized.");

            var helper = new BetterSongListHelper(new SongScriptFilter(), new SongScriptSorter());
            if (helper.IsInitialized || helper.Initialize())
            {
                Log.Info("BetterSongList adapter initialized.");
                return;
            }

            Log.Warn("BetterSongList adapter initialization did not complete successfully.");
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
