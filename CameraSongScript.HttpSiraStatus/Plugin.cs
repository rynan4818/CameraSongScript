using CameraSongScript.Interfaces;
using IPA;
using IPALogger = IPA.Logging.Logger;

namespace CameraSongScript.HttpSiraStatus
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }

        private IHttpSiraStatusHelper _helper;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("CameraSongScript.HttpSiraStatus initialized.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");

            var helper = new HttpSiraStatusHelper();
            _helper = helper;
            global::CameraSongScript.AdapterRegistry.RegisterHttpSiraStatusHelper(helper);

            if (helper.Initialize())
            {
                Log.Info("HttpSiraStatus adapter registered.");
            }
            else
            {
                Log.Warn("HttpSiraStatus adapter registered but initialization will be retried later.");
            }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");

            if (_helper != null)
            {
                global::CameraSongScript.AdapterRegistry.UnregisterHttpSiraStatusHelper(_helper);
                _helper = null;
            }
        }
    }
}
