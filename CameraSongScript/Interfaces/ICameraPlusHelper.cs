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

        /// <summary>
        /// 現在のプロファイルのsongSpecificScript=trueカメラのUseAudioSync設定を取得する
        /// </summary>
        bool GetUseAudioSync();

        /// <summary>
        /// 現在のプロファイルのsongSpecificScript=trueカメラのUseAudioSync設定を変更する
        /// </summary>
        void SetUseAudioSync(bool value);

        /// <summary>
        /// 全カメラの名前とsongSpecificScript設定の状態を表示用文字列で返す
        /// 例: "cameraplus: ON, second: OFF"
        /// </summary>
        string GetSongSpecificScriptStatus();

        /// <summary>
        /// 全てのカメラの名前のリストを返す
        /// </summary>
        IReadOnlyList<string> GetAllCameras();

        /// <summary>
        /// songSpecificScript=trueのカメラ名をカンマ区切りで返す
        /// </summary>
        string GetSongSpecificScriptCameras();

        /// <summary>
        /// 指定されたカメラ名（カンマ区切り）のsongSpecificScriptをtrueに、
        /// それ以外のカメラはfalseに設定して保存する
        /// </summary>
        void SetSongSpecificScriptCameras(string cameraNames);
    }
}
