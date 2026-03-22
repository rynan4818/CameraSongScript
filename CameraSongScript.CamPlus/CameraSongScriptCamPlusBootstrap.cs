using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.CamPlus
{
    internal sealed class CameraSongScriptCamPlusBootstrap : IInitializable
    {
        private readonly ICameraPlusHelper _helper;

        internal CameraSongScriptCamPlusBootstrap(ICameraPlusHelper helper)
        {
            _helper = helper;
        }

        public void Initialize()
        {
            if (_helper.IsInitialized || _helper.Initialize())
            {
                Plugin.Log.Info("CameraPlus adapter initialized.");
                return;
            }

            Plugin.Log.Error("CameraPlus adapter initialization failed.");
        }
    }
}
