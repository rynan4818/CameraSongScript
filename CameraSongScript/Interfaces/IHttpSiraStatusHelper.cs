using CameraSongScript.Models;

namespace CameraSongScript.Interfaces
{
    /// <summary>
    /// HttpSiraStatusとの連携を抽象化するインターフェース
    /// CameraSongScript.HttpSiraStatusプロジェクトで実装される
    /// </summary>
    public interface IHttpSiraStatusHelper
    {
        bool Initialize();
        bool IsInitialized { get; }
        void SendStatusSnapshot(CameraSongScriptStatusSnapshot snapshot);
    }
}
