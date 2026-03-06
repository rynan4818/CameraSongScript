using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace CameraSongScript.Configuration
{
    internal class CameraSongScriptConfig
    {
        public static CameraSongScriptConfig Instance { get; set; }

        /// <summary>
        /// SongScript機能の有効/無効
        /// </summary>
        public virtual bool Enabled { get; set; } = true;

        /// <summary>
        /// AudioSync(曲同期)モード
        /// true: 曲のタイムラインに同期
        /// false: DateTime基準で進行
        /// </summary>
        public virtual bool UseAudioSync { get; set; } = true;

        /// <summary>
        /// スクリプトを適用するカメラ名リスト（カンマ区切り）
        /// 空の場合はすべてのアクティブカメラに適用
        /// </summary>
        public virtual string TargetCameras { get; set; } = "";

        /// <summary>
        /// ユーザー選択のスクリプトファイル名（ファイル名のみ、パスではない）
        /// 空文字列 = 自動選択（SongScript.json優先）
        /// </summary>
        public virtual string SelectedScriptFile { get; set; } = "";

        /// <summary>
        /// 自動的に切り替えるCamera2のCustom Scene名
        /// </summary>
        public virtual string CustomSceneToSwitch { get; set; } = "(Default)";

        /// <summary>
        /// カメラ全体に対するY座標オフセット（単位：cm）
        /// </summary>
        public virtual int CameraHeightOffsetCm { get; set; } = 0;

        /// <summary>
        /// true: 高さオフセットをスクリプト(譜面)ごとに個別保存する（デフォルト）
        /// false: 高さオフセットを共通の1つの設定として使用する（CameraSongScript.jsonに保存）
        /// </summary>
        public virtual bool UsePerScriptHeightOffset { get; set; } = true;

        /// <summary>
        /// hover-hint表示言語の強制設定
        /// "English" = 英語固定(デフォルト), "Japanese" = 日本語固定, "Auto" = ゲーム言語に追従
        /// </summary>
        public virtual string HoverHintLanguage { get; set; } = "Auto";

        /// <summary>
        /// hover-hintの表示ON/OFF
        /// true: hover-hintを表示する(デフォルト), false: hover-hintを非表示
        /// </summary>
        public virtual bool ShowHoverHints { get; set; } = true;

        /// <summary>
        /// ステータスインジケータパネルの表示ON/OFF
        /// </summary>
        public virtual bool ShowStatusPanel { get; set; } = true;

        /// <summary>
        /// ステータスインジケータパネルのプリセット位置インデックス（0=Left, 1=Right, 2=Bottom）
        /// </summary>
        public virtual int StatusPanelPosition { get; set; } = 0;

        /// <summary>
        /// BSIPAが設定ファイルを読み込むたびに呼び出される
        /// </summary>
        public virtual void OnReload()
        {
        }

        /// <summary>
        /// 設定が変更されたときに呼び出される
        /// </summary>
        public virtual void Changed()
        {
        }

        /// <summary>
        /// 値をコピー
        /// </summary>
        public virtual void CopyFrom(CameraSongScriptConfig other)
        {
            Enabled = other.Enabled;
            UseAudioSync = other.UseAudioSync;
            TargetCameras = other.TargetCameras;
            SelectedScriptFile = other.SelectedScriptFile;
            this.CustomSceneToSwitch = other.CustomSceneToSwitch;
            this.CameraHeightOffsetCm = other.CameraHeightOffsetCm;
            this.UsePerScriptHeightOffset = other.UsePerScriptHeightOffset;
            this.HoverHintLanguage = other.HoverHintLanguage;
            this.ShowHoverHints = other.ShowHoverHints;
            this.ShowStatusPanel = other.ShowStatusPanel;
            this.StatusPanelPosition = other.StatusPanelPosition;
        }

        /// <summary>
        /// TargetCamerasをパースして文字列配列で返す
        /// </summary>
        public string[] GetTargetCameraNames()
        {
            if (string.IsNullOrWhiteSpace(TargetCameras))
                return new string[0];
            var names = TargetCameras.Split(',');
            var result = new List<string>();
            foreach (var name in names)
            {
                var trimmed = name.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result.ToArray();
        }
    }
}