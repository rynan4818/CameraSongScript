using System;

namespace CameraSongScript.Models
{
    /// <summary>
    /// 譜面ごとに個別に保存する設定データを保持するクラス
    /// </summary>
    public class SongSpecificSettings
    {
        /// <summary>
        /// 選択されたスクリプトファイル名
        /// </summary>
        public string SelectedScriptFileName { get; set; }

}
}