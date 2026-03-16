using Zenject;

namespace CameraSongScript.UI
{
    internal sealed class BetterSongListMenuRegistrationRetry : IInitializable
    {
        public void Initialize()
        {
            Plugin.EnsureBetterSongListHelperLoaded();
        }
    }
}
