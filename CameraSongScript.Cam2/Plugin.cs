using CameraSongScript.Interfaces;
using IPA;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.Cam2
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        private ICameraHelper _helper;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("CameraSongScript.Cam2 initialized.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            var helper = new Camera2Helper();
            if (!helper.Initialize())
            {
                Log.Error("Camera2 adapter initialization failed.");
                return;
            }

            _helper = helper;
            global::CameraSongScript.AdapterRegistry.RegisterCameraHelper(helper);
            Log.Info("Camera2 adapter registered.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");

            if (_helper != null)
            {
                global::CameraSongScript.AdapterRegistry.UnregisterCameraHelper(_helper);
                _helper = null;
            }
        }
    }
}
