using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.UI
{
    internal sealed class BetterSongListMenuRegistrationRetry : IInitializable
    {
        private readonly IBetterSongListHelper _betterSongListHelper;

        internal BetterSongListMenuRegistrationRetry([InjectOptional] IBetterSongListHelper betterSongListHelper)
        {
            _betterSongListHelper = betterSongListHelper;
        }

        public void Initialize()
        {
            if (_betterSongListHelper == null || _betterSongListHelper.IsInitialized)
            {
                return;
            }

            if (!_betterSongListHelper.Initialize())
            {
                Plugin.Log.Warn("BetterSongList helper initialization did not complete successfully.");
            }
        }
    }
}
