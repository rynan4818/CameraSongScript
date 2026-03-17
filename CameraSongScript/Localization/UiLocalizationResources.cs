using System.Collections.Generic;

namespace CameraSongScript.Localization
{
    internal static class UiLocalizationResources
    {
        public static Dictionary<string, Dictionary<string, string>> CreateTexts()
        {
            return new Dictionary<string, Dictionary<string, string>>
{
            ["label-camera-mod"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Camera Mod:",
                [HoverHintLocalization.Lang_JA] = "カメラMod:"
            },
            ["toggle-enabled"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Song Script Enabled",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプト"
            },
            ["label-script-file"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Script File",
                [HoverHintLocalization.Lang_JA] = "スクリプト"
            },
            ["label-meta-camera"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Camera:",
                [HoverHintLocalization.Lang_JA] = "カメラ:"
            },
            ["label-meta-song"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Song:",
                [HoverHintLocalization.Lang_JA] = "曲:"
            },
            ["label-meta-mapper"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Mapper:",
                [HoverHintLocalization.Lang_JA] = "譜面:"
            },
            ["label-meta-height"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Avatar Height:",
                [HoverHintLocalization.Lang_JA] = "アバター身長:"
            },
            ["label-height-offset"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Height Y Offset (cm)",
                [HoverHintLocalization.Lang_JA] = "高さYオフセット (cm)"
            },
            ["button-reset-offset"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Reset Offset",
                [HoverHintLocalization.Lang_JA] = "オフセットをリセット"
            },
            ["section-preview"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Preview ---",
                [HoverHintLocalization.Lang_JA] = "--- プレビュー ---"
            },
            ["section-camera2-settings"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Camera2 Settings ---",
                [HoverHintLocalization.Lang_JA] = "--- Camera2 設定 ---"
            },
            ["section-cameraplus-settings"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- CameraPlus Settings ---",
                [HoverHintLocalization.Lang_JA] = "--- CameraPlus 設定 ---"
            },
            ["button-preview-show-start"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show / Play(x1)",
                [HoverHintLocalization.Lang_JA] = "表示・再生(x1)"
            },
            ["button-preview-stop"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Stop",
                [HoverHintLocalization.Lang_JA] = "停止"
            },
            ["button-preview-clear"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Clear",
                [HoverHintLocalization.Lang_JA] = "消去"
            },
            ["label-preview-position"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Playback Position",
                [HoverHintLocalization.Lang_JA] = "再生位置"
            },
            ["label-preview-miniature-scale"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Miniature Scale",
                [HoverHintLocalization.Lang_JA] = "ミニチュアモデルのスケール"
            },
            ["section-preview-visual-settings"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Preview Visual Settings ---",
                [HoverHintLocalization.Lang_JA] = "--- プレビュー表示調整 ---"
            },
            ["button-preview-visual-settings-reset"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Reset Preview Visual Settings",
                [HoverHintLocalization.Lang_JA] = "プレビュー表示調整をデフォルトに戻す"
            },
            ["label-preview-visible-position-x"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview Position X",
                [HoverHintLocalization.Lang_JA] = "プレビューの表示位置 X"
            },
            ["label-preview-visible-position-y"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview Position Y",
                [HoverHintLocalization.Lang_JA] = "プレビューの表示位置 Y"
            },
            ["label-preview-visible-position-z"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview Position Z",
                [HoverHintLocalization.Lang_JA] = "プレビューの表示位置 Z"
            },
            ["label-preview-path-line-width"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview Path Line Width",
                [HoverHintLocalization.Lang_JA] = "経路線幅"
            },
            ["toggle-use-audio-sync"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Use Audio Sync",
                [HoverHintLocalization.Lang_JA] = "曲時間同期"
            },
            ["label-target-camera"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Target Camera",
                [HoverHintLocalization.Lang_JA] = "対象カメラ"
            },
            ["label-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Custom Scene",
                [HoverHintLocalization.Lang_JA] = "カスタムシーン"
            },
            ["button-add-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Add 'CameraSongScript' Custom Scene",
                [HoverHintLocalization.Lang_JA] = "'CameraSongScript' カスタムシーンを追加"
            },
            ["label-songscript-profile"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "SongScript Profile",
                [HoverHintLocalization.Lang_JA] = "曲専用プロファイル"
            },
            ["section-common-script"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Common Script ---",
                [HoverHintLocalization.Lang_JA] = "--- 汎用スクリプト ---"
            },
            ["toggle-fallback-to-common"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Fallback to Common",
                [HoverHintLocalization.Lang_JA] = "曲専用非対応時 汎用スクリプト使用"
            },
            ["toggle-force-common-script"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Force Common Script",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプトを強制使用"
            },
            ["label-common-script"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Common Script",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプト"
            },
            ["label-common-target-camera"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CS Target Camera",
                [HoverHintLocalization.Lang_JA] = "汎用対象カメラ"
            },
            ["label-common-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CS Custom Scene",
                [HoverHintLocalization.Lang_JA] = "汎用カスタムシーン"
            },
            ["label-common-profile"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CS Profile",
                [HoverHintLocalization.Lang_JA] = "汎用プロファイル"
            },
            ["section-status-panel"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Status Panel ---",
                [HoverHintLocalization.Lang_JA] = "--- ステータスパネル ---"
            },
            ["section-other"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Other ---",
                [HoverHintLocalization.Lang_JA] = "--- その他 ---"
            },
            ["toggle-show-status-panel"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show Status Panel",
                [HoverHintLocalization.Lang_JA] = "ステータスパネルを表示"
            },
            ["label-panel-position"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Panel Position",
                [HoverHintLocalization.Lang_JA] = "パネル位置"
            },
            ["button-rerun-songscript-caches"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Rebuild SongScript Caches",
                [HoverHintLocalization.Lang_JA] = "譜面/SongScripts キャッシュを再検索"
            },
            ["cache-refresh-status-beatmap"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Beatmaps: {0}",
                [HoverHintLocalization.Lang_JA] = "譜面フォルダ: {0}"
            },
            ["cache-refresh-status-songscripts"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "SongScripts: {0}",
                [HoverHintLocalization.Lang_JA] = "SongScripts: {0}"
            },
            ["cache-refresh-state-idle"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Idle",
                [HoverHintLocalization.Lang_JA] = "待機中"
            },
            ["cache-refresh-state-scanning"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Scanning",
                [HoverHintLocalization.Lang_JA] = "検索中"
            },
            ["cache-refresh-state-scanning-progress"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Scanning ({0}/{1})",
                [HoverHintLocalization.Lang_JA] = "検索中 ({0}/{1})"
            },
            ["cache-refresh-state-completed"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Completed",
                [HoverHintLocalization.Lang_JA] = "完了"
            },
            ["cache-refresh-state-failed"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Failed",
                [HoverHintLocalization.Lang_JA] = "失敗"
            },
            ["setting-per-script-height"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Per-Script Height Offset",
                [HoverHintLocalization.Lang_JA] = "スクリプト毎の高さオフセット"
            },
            ["setting-ui-language"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "UI Language",
                [HoverHintLocalization.Lang_JA] = "UI表示言語"
            },
            ["setting-show-hover-hints"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show Hover-Hints",
                [HoverHintLocalization.Lang_JA] = "ホーバーヒントを表示"
            },
            ["detected-none"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "None",
                [HoverHintLocalization.Lang_JA] = "なし"
            },
            ["song-status-common"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FFAA00>Common Script: {0}</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FFAA00>汎用スクリプト: {0}</color>"
            },
            ["song-status-common-with-count"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FFAA00>Common Script: {0}</color> <color=#AAAAAA>({1} SongScript available)</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FFAA00>汎用スクリプト: {0}</color> <color=#AAAAAA>(SongScript {1}件あり)</color>"
            },
            ["song-status-found"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#00FF00>{0} script(s) found - {1}</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#00FF00>{0}件のスクリプトを検出 - {1}</color>"
            },
            ["song-status-none"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#888888>No camera scripts</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#888888>カメラスクリプトなし</color>"
            },
            ["warning-camera2-unsupported"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FF5555>Warning: Unsupported in Camera2 - {0}</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FF5555>警告: Camera2未対応 - {0}</color>"
            },
            ["warning-adapter-version-unsupported"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FF5555>Warning: {0} version {1} is unsupported and will not be loaded. Allowed: {2}</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FF5555>警告: {0} のバージョン {1} は対象外のため読み込めません。対応: {2}</color>"
            },
            ["preview-initializing"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview: Initializing",
                [HoverHintLocalization.Lang_JA] = "プレビュー: 初期化中"
            },
            ["preview-state-playing"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Playing",
                [HoverHintLocalization.Lang_JA] = "再生中"
            },
            ["preview-state-stopped"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Stopped",
                [HoverHintLocalization.Lang_JA] = "停止中"
            },
            ["preview-active"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview: {0} | {1} / {2} | {3} | x{4}",
                [HoverHintLocalization.Lang_JA] = "プレビュー: {0} | {1} / {2} | {3} | x{4}"
            },
            ["preview-ready"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview: Ready",
                [HoverHintLocalization.Lang_JA] = "プレビュー: 準備完了"
            },
            ["preview-no-script"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Preview: No script",
                [HoverHintLocalization.Lang_JA] = "プレビュー: スクリプトなし"
            },
            ["panel-common"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FFAA00>CameraSongScript: COMMON</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FFAA00>CameraSongScript: 汎用</color>"
            },
            ["panel-none"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#888888>CameraSongScript: NONE</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#888888>CameraSongScript: なし</color>"
            },
            ["panel-off"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#888888>CameraSongScript: OFF</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#888888>CameraSongScript: 無効</color>"
            },
            ["panel-on"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#00FF00>CameraSongScript: ON</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#00FF00>CameraSongScript: 有効</color>"
            },
            ["panel-script-line"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#AAAAAA>Script:</color> {0}",
                [HoverHintLocalization.Lang_JA] = "<color=#AAAAAA>スクリプト:</color> {0}"
            },
            ["panel-author-song-line"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#AAAAAA>Author:</color> {0} <color=#AAAAAA>| Song:</color> {1}",
                [HoverHintLocalization.Lang_JA] = "<color=#AAAAAA>作者:</color> {0} <color=#AAAAAA>| 曲:</color> {1}"
            },
            ["panel-y-offset-line"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "<color=#FFFF00>Y Offset: {0}cm</color>",
                [HoverHintLocalization.Lang_JA] = "<color=#FFFF00>Yオフセット: {0}cm</color>"
            },
            ["status-panel-left-upper-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "LeftPanel UpperRight",
                [HoverHintLocalization.Lang_JA] = "左パネル上部右"
            },
            ["status-panel-left-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "LeftPanel UpperLeft",
                [HoverHintLocalization.Lang_JA] = "左パネル上部左"
            },
            ["status-panel-left-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "LeftPanel LowerRight",
                [HoverHintLocalization.Lang_JA] = "左パネル下部右"
            },
            ["status-panel-left-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "LeftPanel LowerLeft",
                [HoverHintLocalization.Lang_JA] = "左パネル下部左"
            },
            ["status-panel-center-upper-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CenterPanel UpperRight",
                [HoverHintLocalization.Lang_JA] = "中央パネル上部右"
            },
            ["status-panel-center-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CenterPanel UpperLeft",
                [HoverHintLocalization.Lang_JA] = "中央パネル上部左"
            },
            ["status-panel-center-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CenterPanel LowerRight",
                [HoverHintLocalization.Lang_JA] = "中央パネル下部右"
            },
            ["status-panel-center-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CenterPanel LowerLeft",
                [HoverHintLocalization.Lang_JA] = "中央パネル下部左"
            },
            ["status-panel-right-upper-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "RightPanel UpperRight",
                [HoverHintLocalization.Lang_JA] = "右パネル上部右"
            },
            ["status-panel-right-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "RightPanel UpperLeft",
                [HoverHintLocalization.Lang_JA] = "右パネル上部左"
            },
            ["status-panel-right-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "RightPanel LowerRight",
                [HoverHintLocalization.Lang_JA] = "右パネル下部右"
            },
            ["status-panel-right-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "RightPanel LowerLeft",
                [HoverHintLocalization.Lang_JA] = "右パネル下部左"
            }
        };
        }

        public static Dictionary<string, Dictionary<string, string>> CreateOptionTexts()
        {
            return new Dictionary<string, Dictionary<string, string>>
{
            [UiLocalization.OptionEnglish] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "English",
                [HoverHintLocalization.Lang_JA] = "英語"
            },
            [UiLocalization.OptionJapanese] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Japanese",
                [HoverHintLocalization.Lang_JA] = "日本語"
            },
            [UiLocalization.OptionAuto] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Auto",
                [HoverHintLocalization.Lang_JA] = "自動"
            },
            [UiLocalization.OptionNone] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(none)",
                [HoverHintLocalization.Lang_JA] = "(なし)"
            },
            [UiLocalization.OptionAll] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(All)",
                [HoverHintLocalization.Lang_JA] = "(すべて)"
            },
            [UiLocalization.OptionDefault] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Default)",
                [HoverHintLocalization.Lang_JA] = "(デフォルト)"
            },
            [UiLocalization.OptionRandom] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Random)",
                [HoverHintLocalization.Lang_JA] = "(ランダム)"
            },
            [UiLocalization.OptionSameAsSongScript] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Same as SongScript)",
                [HoverHintLocalization.Lang_JA] = "(曲専用スクリプトと同じ)"
            },
            [UiLocalization.OptionNoChange] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(NoChange)",
                [HoverHintLocalization.Lang_JA] = "(変更しない)"
            },
            [UiLocalization.OptionDelete] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Delete)",
                [HoverHintLocalization.Lang_JA] = "(空にする)"
            }
        };
        }
    }
}
