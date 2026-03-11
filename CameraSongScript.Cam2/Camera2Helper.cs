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

        public IEnumerable<string> CustomScenes
        {
            get
            {
                // カメラアサインがないシーンも含めて、全てのカスタムシーン名を返す
                return Camera2.SDK.Scenes.customScenes.Keys;
            }
        }

        public void CreateOrUpdateCustomScene(string sceneName, IEnumerable<string> cameras)
        {
            var customScenes = Camera2.Managers.ScenesManager.settings.customScenes;
            if (!customScenes.ContainsKey(sceneName))
            {
                customScenes[sceneName] = new List<string>();
            }
            
            customScenes[sceneName].Clear();
            customScenes[sceneName].AddRange(cameras);

            Camera2.Managers.ScenesManager.settings.Save();
            Camera2.UI.SpaghettiUI.scenesSwitchUI.Update(-1, true);
        }

        private bool _backedUpAutoSwitch = false;
        private bool _hasBackup = false;

        public void PreGameSceneCurrentSetup(string customSceneName)
        {
            if (string.IsNullOrEmpty(customSceneName) || customSceneName == "(Default)")
                return;

            _backedUpAutoSwitch = Camera2.Managers.ScenesManager.settings.autoswitchFromCustom;
            _hasBackup = true;

            Camera2.Managers.ScenesManager.settings.autoswitchFromCustom = false;
            Camera2.SDK.Scenes.SwitchToCustomScene(customSceneName);
        }

        public void RestoreGameSceneSetup()
        {
            if (_hasBackup)
            {
                Camera2.Managers.ScenesManager.settings.autoswitchFromCustom = _backedUpAutoSwitch;
                _hasBackup = false;
                Camera2.SDK.Scenes.ShowNormalScene();
            }
        }

        public void SwitchToCustomScene(string sceneName)
        {
            Camera2.SDK.Scenes.SwitchToCustomScene(sceneName);
        }

        public void ShowNormalScene()
        {
            Camera2.SDK.Scenes.ShowNormalScene();
        }

        public ICameraToken GetTokenForCamera(string camName)
        {
            var token = OverrideToken.GetTokenForCamera(camName);
            return token != null ? new Camera2Token(token) : null;
        }

        public UnityEngine.Material GetPreviewMaterial()
        {
            if (Camera2.Plugin.Shader_VolumetricBlit != null)
            {
                return new UnityEngine.Material(Camera2.Plugin.Shader_VolumetricBlit);
            }
            return null;
        }
    }
}
