using System.Collections.Generic;

namespace CameraSongScript.Interfaces
{
    /// <summary>
    /// CameraPlusとの連携を抽象化するインターフェース
    /// CameraSongScript.CamPlusプロジェクトで実装される
    /// </summary>
    public interface ICameraPlusHelper
    {
        bool Initialize();
        bool IsInitialized { get; }
        void SetScriptPath(string fullPath);
        string GetCurrentPath();

        /// <summary>
        /// 利用可能なプロファイル名リストを取得する（先頭は空文字列=デフォルト）
        /// </summary>
        IReadOnlyList<string> GetProfileList();


        /// <summary>
        /// 曲固有スクリプト検出時に切り替えるプロファイル名を取得する
        /// </summary>
        string GetSongSpecificScriptProfile();

        /// <summary>
        /// 曲固有スクリプト検出時に切り替えるプロファイル名を設定する
        /// </summary>
        void SetSongSpecificScriptProfile(string profileName);
    }
}
