using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.BetterSongList
{
    internal sealed class BetterSongListBootstrap : IInitializable
    {
        private readonly IBetterSongListHelper _helper;

        internal BetterSongListBootstrap(IBetterSongListHelper helper)
        {
            _helper = helper;
        }

        public void Initialize()
        {
            if (_helper.IsInitialized || _helper.Initialize())
            {
                Plugin.Log.Info("BetterSongList adapter initialized.");
                return;
            }

            Plugin.Log.Warn("BetterSongList adapter initialization did not complete successfully.");
        }
    }
}
