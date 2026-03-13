using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CameraSongScript.Localization;
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
        /// [CameraPlus] SongScript使用時のプロファイル名
        /// "(NoChange)" = プロファイルを変更しない, "(Delete)" = プロファイルを空にする
        /// </summary>
        public virtual string SongScriptProfile { get; set; } = "(NoChange)";

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
        /// ステータスインジケータパネルのプリセット位置インデックス
        /// </summary>
        public virtual int StatusPanelPosition { get; set; } = 0;

        // --- ステータスパネル プリセット設定 (12種類) ---

        // Left 領域
        public virtual float PresetLeftUpperRightPosX { get; set; } = -3.0f;
        public virtual float PresetLeftUpperRightPosY { get; set; } = 2.8f;
        public virtual float PresetLeftUpperRightPosZ { get; set; } = 3.0f;
        public virtual float PresetLeftUpperRightRotX { get; set; } = 0.0f;
        public virtual float PresetLeftUpperRightRotY { get; set; } = -46.0f;
        public virtual float PresetLeftUpperRightRotZ { get; set; } = 0.0f;

        public virtual float PresetLeftUpperLeftPosX { get; set; } = -4.0f;
        public virtual float PresetLeftUpperLeftPosY { get; set; } = 3.0f;
        public virtual float PresetLeftUpperLeftPosZ { get; set; } = 1.5f;
        public virtual float PresetLeftUpperLeftRotX { get; set; } = 0.0f;
        public virtual float PresetLeftUpperLeftRotY { get; set; } = -70.0f;
        public virtual float PresetLeftUpperLeftRotZ { get; set; } = 0.0f;

        public virtual float PresetLeftLowerRightPosX { get; set; } = -2.6f;
        public virtual float PresetLeftLowerRightPosY { get; set; } = 0.1f;
        public virtual float PresetLeftLowerRightPosZ { get; set; } = 2.6f;
        public virtual float PresetLeftLowerRightRotX { get; set; } = 70.0f;
        public virtual float PresetLeftLowerRightRotY { get; set; } = -50.0f;
        public virtual float PresetLeftLowerRightRotZ { get; set; } = 0.0f;

        public virtual float PresetLeftLowerLeftPosX { get; set; } = -3.7f;
        public virtual float PresetLeftLowerLeftPosY { get; set; } = 0.3f;
        public virtual float PresetLeftLowerLeftPosZ { get; set; } = 1.0f;
        public virtual float PresetLeftLowerLeftRotX { get; set; } = 40.0f;
        public virtual float PresetLeftLowerLeftRotY { get; set; } = -76.0f;
        public virtual float PresetLeftLowerLeftRotZ { get; set; } = 0.0f;

        // Center 領域
        public virtual float PresetCenterUpperRightPosX { get; set; } = 1.8f;
        public virtual float PresetCenterUpperRightPosY { get; set; } = 2.9f;
        public virtual float PresetCenterUpperRightPosZ { get; set; } = 3.7f;
        public virtual float PresetCenterUpperRightRotX { get; set; } = 0.0f;
        public virtual float PresetCenterUpperRightRotY { get; set; } = 27.0f;
        public virtual float PresetCenterUpperRightRotZ { get; set; } = 0.0f;

        public virtual float PresetCenterUpperLeftPosX { get; set; } = -1.8f;
        public virtual float PresetCenterUpperLeftPosY { get; set; } = 3.0f;
        public virtual float PresetCenterUpperLeftPosZ { get; set; } = 3.7f;
        public virtual float PresetCenterUpperLeftRotX { get; set; } = 0.0f;
        public virtual float PresetCenterUpperLeftRotY { get; set; } = -27.0f;
        public virtual float PresetCenterUpperLeftRotZ { get; set; } = 0.0f;

        public virtual float PresetCenterLowerRightPosX { get; set; } = 1.6f;
        public virtual float PresetCenterLowerRightPosY { get; set; } = 0.15f;
        public virtual float PresetCenterLowerRightPosZ { get; set; } = 3.5f;
        public virtual float PresetCenterLowerRightRotX { get; set; } = 70.0f;
        public virtual float PresetCenterLowerRightRotY { get; set; } = 20.0f;
        public virtual float PresetCenterLowerRightRotZ { get; set; } = 0.0f;

        public virtual float PresetCenterLowerLeftPosX { get; set; } = -1.3f;
        public virtual float PresetCenterLowerLeftPosY { get; set; } = 0.2f;
        public virtual float PresetCenterLowerLeftPosZ { get; set; } = 3.2f;
        public virtual float PresetCenterLowerLeftRotX { get; set; } = 70.0f;
        public virtual float PresetCenterLowerLeftRotY { get; set; } = -20.0f;
        public virtual float PresetCenterLowerLeftRotZ { get; set; } = 0.0f;

        // Right 領域
        public virtual float PresetRightUpperRightPosX { get; set; } = 4.6f;
        public virtual float PresetRightUpperRightPosY { get; set; } = 3.4f;
        public virtual float PresetRightUpperRightPosZ { get; set; } = 0.0f;
        public virtual float PresetRightUpperRightRotX { get; set; } = 0.0f;
        public virtual float PresetRightUpperRightRotY { get; set; } = 86.0f;
        public virtual float PresetRightUpperRightRotZ { get; set; } = 0.0f;

        public virtual float PresetRightUpperLeftPosX { get; set; } = 2.5f;
        public virtual float PresetRightUpperLeftPosY { get; set; } = 3.9f;
        public virtual float PresetRightUpperLeftPosZ { get; set; } = 3.6f;
        public virtual float PresetRightUpperLeftRotX { get; set; } = 0.0f;
        public virtual float PresetRightUpperLeftRotY { get; set; } = 35.0f;
        public virtual float PresetRightUpperLeftRotZ { get; set; } = 0.0f;

        public virtual float PresetRightLowerRightPosX { get; set; } = 4.0f;
        public virtual float PresetRightLowerRightPosY { get; set; } = 0.2f;
        public virtual float PresetRightLowerRightPosZ { get; set; } = 0.4f;
        public virtual float PresetRightLowerRightRotX { get; set; } = 50.0f;
        public virtual float PresetRightLowerRightRotY { get; set; } = 76.0f;
        public virtual float PresetRightLowerRightRotZ { get; set; } = 0.0f;

        public virtual float PresetRightLowerLeftPosX { get; set; } = 2.6f;
        public virtual float PresetRightLowerLeftPosY { get; set; } = 0.2f;
        public virtual float PresetRightLowerLeftPosZ { get; set; } = 3.2f;
        public virtual float PresetRightLowerLeftRotX { get; set; } = 45.0f;
        public virtual float PresetRightLowerLeftRotY { get; set; } = 40.0f;
        public virtual float PresetRightLowerLeftRotZ { get; set; } = 0.0f;


        // --- ステータスパネル ビジュアルプロパティ ---

        /// <summary>ステータスパネルのフォントサイズ</summary>
        public virtual float StatusFontSize { get; set; } = 3.5f;
        /// <summary>ステータスパネルのキャンバス幅</summary>
        public virtual float StatusCanvasWidth { get; set; } = 100f;
        /// <summary>ステータスパネルのキャンバス高さ</summary>
        public virtual float StatusCanvasHeight { get; set; } = 10f;
        /// <summary>ステータスパネルのスケール（uniform）</summary>
        public virtual float StatusScale { get; set; } = 0.025f;

        // --- 汎用スクリプト（CommonScripts）設定 ---

        /// <summary>
        /// SongScriptが無い曲で汎用スクリプトを使用する
        /// </summary>
        public virtual bool UseCommonScriptAsFallback { get; set; } = false;

        /// <summary>
        /// SongScriptの有無やEnabled設定に関係なく汎用スクリプトを強制使用する
        /// </summary>
        public virtual bool ForceCommonScript { get; set; } = false;

        /// <summary>
        /// 選択中の汎用スクリプト表示名 (UiLocalization.OptionRandom またはファイル名)
        /// </summary>
        public virtual string SelectedCommonScript { get; set; } = UiLocalization.OptionRandom;

        /// <summary>
        /// [CameraPlus] 汎用スクリプト使用時のプロファイル（空文字列 = SongScript設定と同じ）
        /// </summary>
        public virtual string CommonScriptProfile { get; set; } = "";

        /// <summary>
        /// [Camera2] 汎用スクリプト使用時のターゲットカメラ（空文字列 = SongScript設定と同じ）
        /// </summary>
        public virtual string CommonScriptTargetCamera { get; set; } = "";

        /// <summary>
        /// [Camera2] 汎用スクリプト使用時のカスタムシーン（空文字列 = SongScript設定と同じ）
        /// </summary>
        public virtual string CommonScriptCustomScene { get; set; } = "";

        /// <summary>
        /// コンフィグがファイルからリロードされたときに発火するイベント。
        /// BSIPAのファイルシステム読み取りスレッドから呼ばれるため、
        /// ハンドラ側でメインスレッドへのディスパッチが必要。
        /// </summary>
        public static event Action ConfigReloaded;

        /// <summary>
        /// BSIPAが設定ファイルを読み込むたびに呼び出される
        /// </summary>
        public virtual void OnReload()
        {
            ConfigReloaded?.Invoke();
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
            this.SongScriptProfile = other.SongScriptProfile;
            this.CustomSceneToSwitch = other.CustomSceneToSwitch;
            this.CameraHeightOffsetCm = other.CameraHeightOffsetCm;
            this.UsePerScriptHeightOffset = other.UsePerScriptHeightOffset;
            this.HoverHintLanguage = other.HoverHintLanguage;
            this.ShowHoverHints = other.ShowHoverHints;
            this.ShowStatusPanel = other.ShowStatusPanel;
            this.StatusPanelPosition = other.StatusPanelPosition;

            this.PresetLeftUpperRightPosX = other.PresetLeftUpperRightPosX;
            this.PresetLeftUpperRightPosY = other.PresetLeftUpperRightPosY;
            this.PresetLeftUpperRightPosZ = other.PresetLeftUpperRightPosZ;
            this.PresetLeftUpperRightRotX = other.PresetLeftUpperRightRotX;
            this.PresetLeftUpperRightRotY = other.PresetLeftUpperRightRotY;
            this.PresetLeftUpperRightRotZ = other.PresetLeftUpperRightRotZ;

            this.PresetLeftUpperLeftPosX = other.PresetLeftUpperLeftPosX;
            this.PresetLeftUpperLeftPosY = other.PresetLeftUpperLeftPosY;
            this.PresetLeftUpperLeftPosZ = other.PresetLeftUpperLeftPosZ;
            this.PresetLeftUpperLeftRotX = other.PresetLeftUpperLeftRotX;
            this.PresetLeftUpperLeftRotY = other.PresetLeftUpperLeftRotY;
            this.PresetLeftUpperLeftRotZ = other.PresetLeftUpperLeftRotZ;

            this.PresetLeftLowerRightPosX = other.PresetLeftLowerRightPosX;
            this.PresetLeftLowerRightPosY = other.PresetLeftLowerRightPosY;
            this.PresetLeftLowerRightPosZ = other.PresetLeftLowerRightPosZ;
            this.PresetLeftLowerRightRotX = other.PresetLeftLowerRightRotX;
            this.PresetLeftLowerRightRotY = other.PresetLeftLowerRightRotY;
            this.PresetLeftLowerRightRotZ = other.PresetLeftLowerRightRotZ;

            this.PresetLeftLowerLeftPosX = other.PresetLeftLowerLeftPosX;
            this.PresetLeftLowerLeftPosY = other.PresetLeftLowerLeftPosY;
            this.PresetLeftLowerLeftPosZ = other.PresetLeftLowerLeftPosZ;
            this.PresetLeftLowerLeftRotX = other.PresetLeftLowerLeftRotX;
            this.PresetLeftLowerLeftRotY = other.PresetLeftLowerLeftRotY;
            this.PresetLeftLowerLeftRotZ = other.PresetLeftLowerLeftRotZ;

            this.PresetCenterUpperRightPosX = other.PresetCenterUpperRightPosX;
            this.PresetCenterUpperRightPosY = other.PresetCenterUpperRightPosY;
            this.PresetCenterUpperRightPosZ = other.PresetCenterUpperRightPosZ;
            this.PresetCenterUpperRightRotX = other.PresetCenterUpperRightRotX;
            this.PresetCenterUpperRightRotY = other.PresetCenterUpperRightRotY;
            this.PresetCenterUpperRightRotZ = other.PresetCenterUpperRightRotZ;

            this.PresetCenterUpperLeftPosX = other.PresetCenterUpperLeftPosX;
            this.PresetCenterUpperLeftPosY = other.PresetCenterUpperLeftPosY;
            this.PresetCenterUpperLeftPosZ = other.PresetCenterUpperLeftPosZ;
            this.PresetCenterUpperLeftRotX = other.PresetCenterUpperLeftRotX;
            this.PresetCenterUpperLeftRotY = other.PresetCenterUpperLeftRotY;
            this.PresetCenterUpperLeftRotZ = other.PresetCenterUpperLeftRotZ;

            this.PresetCenterLowerRightPosX = other.PresetCenterLowerRightPosX;
            this.PresetCenterLowerRightPosY = other.PresetCenterLowerRightPosY;
            this.PresetCenterLowerRightPosZ = other.PresetCenterLowerRightPosZ;
            this.PresetCenterLowerRightRotX = other.PresetCenterLowerRightRotX;
            this.PresetCenterLowerRightRotY = other.PresetCenterLowerRightRotY;
            this.PresetCenterLowerRightRotZ = other.PresetCenterLowerRightRotZ;

            this.PresetCenterLowerLeftPosX = other.PresetCenterLowerLeftPosX;
            this.PresetCenterLowerLeftPosY = other.PresetCenterLowerLeftPosY;
            this.PresetCenterLowerLeftPosZ = other.PresetCenterLowerLeftPosZ;
            this.PresetCenterLowerLeftRotX = other.PresetCenterLowerLeftRotX;
            this.PresetCenterLowerLeftRotY = other.PresetCenterLowerLeftRotY;
            this.PresetCenterLowerLeftRotZ = other.PresetCenterLowerLeftRotZ;

            this.PresetRightUpperRightPosX = other.PresetRightUpperRightPosX;
            this.PresetRightUpperRightPosY = other.PresetRightUpperRightPosY;
            this.PresetRightUpperRightPosZ = other.PresetRightUpperRightPosZ;
            this.PresetRightUpperRightRotX = other.PresetRightUpperRightRotX;
            this.PresetRightUpperRightRotY = other.PresetRightUpperRightRotY;
            this.PresetRightUpperRightRotZ = other.PresetRightUpperRightRotZ;

            this.PresetRightUpperLeftPosX = other.PresetRightUpperLeftPosX;
            this.PresetRightUpperLeftPosY = other.PresetRightUpperLeftPosY;
            this.PresetRightUpperLeftPosZ = other.PresetRightUpperLeftPosZ;
            this.PresetRightUpperLeftRotX = other.PresetRightUpperLeftRotX;
            this.PresetRightUpperLeftRotY = other.PresetRightUpperLeftRotY;
            this.PresetRightUpperLeftRotZ = other.PresetRightUpperLeftRotZ;

            this.PresetRightLowerRightPosX = other.PresetRightLowerRightPosX;
            this.PresetRightLowerRightPosY = other.PresetRightLowerRightPosY;
            this.PresetRightLowerRightPosZ = other.PresetRightLowerRightPosZ;
            this.PresetRightLowerRightRotX = other.PresetRightLowerRightRotX;
            this.PresetRightLowerRightRotY = other.PresetRightLowerRightRotY;
            this.PresetRightLowerRightRotZ = other.PresetRightLowerRightRotZ;

            this.PresetRightLowerLeftPosX = other.PresetRightLowerLeftPosX;
            this.PresetRightLowerLeftPosY = other.PresetRightLowerLeftPosY;
            this.PresetRightLowerLeftPosZ = other.PresetRightLowerLeftPosZ;
            this.PresetRightLowerLeftRotX = other.PresetRightLowerLeftRotX;
            this.PresetRightLowerLeftRotY = other.PresetRightLowerLeftRotY;
            this.PresetRightLowerLeftRotZ = other.PresetRightLowerLeftRotZ;

            this.StatusFontSize = other.StatusFontSize;
            this.StatusCanvasWidth = other.StatusCanvasWidth;
            this.StatusCanvasHeight = other.StatusCanvasHeight;
            this.StatusScale = other.StatusScale;

            this.UseCommonScriptAsFallback = other.UseCommonScriptAsFallback;
            this.ForceCommonScript = other.ForceCommonScript;
            this.SelectedCommonScript = other.SelectedCommonScript;
            this.CommonScriptProfile = other.CommonScriptProfile;
            this.CommonScriptTargetCamera = other.CommonScriptTargetCamera;
            this.CommonScriptCustomScene = other.CommonScriptCustomScene;
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
