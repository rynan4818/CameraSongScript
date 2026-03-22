using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.Cam2
{
    internal sealed class CameraSongScriptCam2Bootstrap : IInitializable
    {
        private readonly ICameraHelper _helper;

        internal CameraSongScriptCam2Bootstrap(ICameraHelper helper)
        {
            _helper = helper;
        }

        public void Initialize()
        {
            if (_helper.IsInitialized || _helper.Initialize())
            {
                Plugin.Log.Info("Camera2 adapter initialized.");
                return;
            }

            Plugin.Log.Error("Camera2 adapter initialization failed.");
        }
    }
}
