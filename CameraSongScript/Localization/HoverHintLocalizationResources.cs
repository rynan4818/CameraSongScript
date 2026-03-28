using System.Collections.Generic;

namespace CameraSongScript.Localization
{
    internal static class HoverHintLocalizationResources
    {
        public static Dictionary<string, Dictionary<string, string>> CreateHints()
        {
            return new Dictionary<string, Dictionary<string, string>>
{
            // ========== CameraSongScriptSettingsView ==========
            ["hint-enabled"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Enable/Disable SongScript",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプト機能の有効/無効"
            },
            ["hint-script-file"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Select camera script to use",
                [HoverHintLocalization.Lang_JA] = "使用するカメラスクリプトを選択"
            },
            ["hint-height-offset"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Offset script cameras up/down by specified cm",
                [HoverHintLocalization.Lang_JA] = "スクリプトのカメラを上下にオフセット"
            },
            ["hint-height-reset"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Reset offset to 0",
                [HoverHintLocalization.Lang_JA] = "オフセットを0にリセット"
            },
            ["hint-audio-sync"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Sync script with song timeline",
                [HoverHintLocalization.Lang_JA] = "曲の進行に同期してスクリプトを進行"
            },
            ["hint-target-camera"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Select the camera name to play the script (All = all cameras)",
                [HoverHintLocalization.Lang_JA] = "スクリプト再生するカメラ名を選択（すべて = 全カメラ）"
            },
            ["hint-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Custom Scene to Switch to When SongScript is Detected",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプト検出時に切り替えるカスタムシーン"
            },
            ["hint-add-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Register and update the single camera selected as the 'Target Camera' as the 'CameraSongScript' custom scene name for Camera2",
                [HoverHintLocalization.Lang_JA] = "上記の「対象カメラ」に選ばれた単独のカメラを、カスタムシーンの「CameraSongScript」としてCamera2に登録・更新"
            },
            ["hint-refresh-camera2-lists"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Reload the latest Camera2 camera and custom scene lists into all Camera2 dropdowns",
                [HoverHintLocalization.Lang_JA] = "Camera2の最新カメラ一覧とカスタムシーン一覧を、関連するすべてのドロップダウンで更新"
            },
            ["hint-script-profile"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "CameraPlus profile to use when SongScript is detected",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプト検出時に切り替えるCameraPlusプロファイル"
            },
            ["hint-refresh-cameraplus-profiles"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Reload the latest CameraPlus profile list into both profile dropdowns",
                [HoverHintLocalization.Lang_JA] = "CameraPlusの最新プロファイル一覧を両方のドロップダウンで更新"
            },
            ["hint-show-status-panel"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show status indicator panel outside tab",
                [HoverHintLocalization.Lang_JA] = "タブ外にステータスパネルを表示"
            },
            ["hint-shorten-status-panel-script-path"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Use the same shortened script label style as the green status display in the status panel",
                [HoverHintLocalization.Lang_JA] = "ステータスパネルのスクリプト表示を、緑のステータス表示と同じ短縮表記にする"
            },
            ["hint-panel-position"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Select status panel position",
                [HoverHintLocalization.Lang_JA] = "ステータスパネルの表示位置を選択"
            },
            ["hint-rerun-songscript-caches"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Run the startup beatmap and SongScripts folder cache scan again",
                [HoverHintLocalization.Lang_JA] = "起動時に実行している譜面フォルダとSongScriptsフォルダの検索・キャッシュ更新を再実行"
            },
            ["hint-download-missing-beatmaps"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Download beatmaps referenced by SongScripts folder that are not installed yet",
                [HoverHintLocalization.Lang_JA] = "SongScriptsフォルダで参照されていて未取得の譜面をBeatSaverから取得"
            },
            // ========== CameraSongScriptModSettingView ==========
            ["hint-per-script-height"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "ON: Save height offset per-script. OFF: Use single shared setting",
                [HoverHintLocalization.Lang_JA] = "ONの場合、高さオフセットをスクリプト毎に個別保存。OFFの場合、共通の1つの設定として使用"
            },
            ["hint-hover-hint-language"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Select UI display language",
                [HoverHintLocalization.Lang_JA] = "UI表示言語を選択"
            },
            ["hint-show-hover-hints"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show hover-hints",
                [HoverHintLocalization.Lang_JA] = "ホバーヒントの表示"
            },
            // ========== Common Script ==========
            ["hint-common-fallback"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Use a common script when no SongScript is found",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプトが無い曲で汎用スクリプトを使用"
            },
            ["hint-force-common"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Force common script regardless of SongScript or Enable setting",
                [HoverHintLocalization.Lang_JA] = "曲専用スクリプトに関係なく汎用スクリプトを強制使用"
            },
            ["hint-common-script-file"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Select common script file (Random = random each play)",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプトファイルを選択（ランダム = プレイごとにランダム）"
            },
            ["hint-common-target-camera"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Camera for common script (Same = use SongScript setting)",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプト用カメラ"
            },
            ["hint-common-custom-scene"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Custom scene for common script (Same = use SongScript setting)",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプト用カスタムシーン"
            },
            ["hint-common-profile"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Profile for common script (Same = use SongScript setting)",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプト用プロファイル"
            },
        };
        }
    }
}
