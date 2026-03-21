using CameraSongScript.Interfaces;
using IPA;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.CamPlus
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        private ICameraPlusHelper _helper;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("CameraSongScript.CamPlus initialized.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            var helper = new CameraPlusHelper();
            if (!helper.Initialize())
            {
                Log.Error("CameraPlus adapter initialization failed.");
                return;
            }

            _helper = helper;
            global::CameraSongScript.AdapterRegistry.RegisterCameraPlusHelper(helper);
            Log.Info("CameraPlus adapter registered.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");

            if (_helper != null)
            {
                global::CameraSongScript.AdapterRegistry.UnregisterCameraPlusHelper(_helper);
                _helper = null;
            }
        }
    }
}
