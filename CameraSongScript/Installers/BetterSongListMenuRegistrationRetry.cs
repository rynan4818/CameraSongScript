using Zenject;

namespace CameraSongScript.Installers
{
    internal sealed class BetterSongListMenuRegistrationRetry : IInitializable
    {
        public void Initialize()
        {
            Plugin.EnsureBetterSongListHelperLoaded();
        }
    }
}
