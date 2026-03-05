using System.Collections.Generic;

namespace CameraSongScript.Interfaces
{
    /// <summary>
    /// Camera2のカメラ操作を抽象化するインターフェース
    /// CameraSongScript.Cam2プロジェクトで実装される
    /// </summary>
    public interface ICameraHelper
    {
        bool Initialize();
        bool IsInitialized { get; }
        IEnumerable<string> GetActiveCameras();
        IEnumerable<string> GetAvailableCameras();
        IEnumerable<string> CustomScenes { get; }
        void CreateOrUpdateCustomScene(string sceneName, IEnumerable<string> cameras);
        void PreGameSceneCurrentSetup(string customSceneName);
        void RestoreGameSceneSetup();
        void SwitchToCustomScene(string sceneName);
        void ShowNormalScene();
        ICameraToken GetTokenForCamera(string camName);
    }
}
