using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.HttpSiraStatus
{
    internal sealed class HttpSiraStatusBootstrap : IInitializable
    {
        private readonly IHttpSiraStatusHelper _helper;

        internal HttpSiraStatusBootstrap(IHttpSiraStatusHelper helper)
        {
            _helper = helper;
        }

        public void Initialize()
        {
            if (_helper.IsInitialized || _helper.Initialize())
            {
                Plugin.Log.Info("HttpSiraStatus adapter initialized.");
                return;
            }

            Plugin.Log.Warn("HttpSiraStatus adapter initialization did not complete successfully.");
        }
    }
}
