using System.Collections.Generic;
using Camera2.SDK;
using CameraSongScript.Interfaces;

namespace CameraSongScript.Cam2
{
    /// <summary>
    /// Camera2 SDKを直接参照するICameraHelper実装
    /// </summary>
    public class Camera2Helper : ICameraHelper
    {
        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            IsInitialized = true;
            return true;
        }

        public IEnumerable<string> GetActiveCameras()
        {
            return Cameras.active;
        }

        public IEnumerable<string> GetAvailableCameras()
        {
            return Cameras.available;
        }

        public ICameraToken GetTokenForCamera(string camName)
        {
            var token = OverrideToken.GetTokenForCamera(camName);
            return token != null ? new Camera2Token(token) : null;
        }
    }
}
