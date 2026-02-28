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
        ICameraToken GetTokenForCamera(string camName);
    }
}
