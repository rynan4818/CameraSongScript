using System.Collections.Generic;
using CameraSongScript.Configuration;

namespace CameraSongScript.Localization
{
    /// <summary>
    /// hover-hintのローカライズ管理クラス
    /// 全hover-hint文字列を一元管理し、設定に応じた言語の文字列を返す
    /// </summary>
    internal static class HoverHintLocalization
    {
        public const string Lang_EN = "en";
        public const string Lang_JA = "ja";

        /// <summary>
        /// hover-hintテキスト辞書
        /// Key: hint識別子, Value: { 言語コード: テキスト }
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> _hints
            = new Dictionary<string, Dictionary<string, string>>
        {
            // ========== CameraSongScriptSettingsView ==========
            ["hint-enabled"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Enable/Disable SongScript",
                [Lang_JA] = "SongScript機能の有効/無効"
            },
            ["hint-script-file"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Select camera script file to use",
                [Lang_JA] = "使用するカメラスクリプトファイルを選択"
            },
            ["hint-height-offset"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Offset all cameras up/down by specified cm",
                [Lang_JA] = "全カメラを指定cm分、上下にオフセットします"
            },
            ["hint-height-reset"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Reset offset to 0",
                [Lang_JA] = "オフセットを0にリセットします"
            },
            ["hint-audio-sync"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Sync script with song timeline",
                [Lang_JA] = "曲のタイムラインに同期してスクリプトを進行"
            },
            ["hint-target-camera"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Select target camera (All = all cameras)",
                [Lang_JA] = "適用対象のカメラ名を選択（Allなら全カメラ）"
            },
            ["hint-custom-scene"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Switch Camera2 Custom Scene",
                [Lang_JA] = "Camera2のCustom Sceneを切り替えます"
            },
            ["hint-add-custom-scene"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Register selected camera as 'CameraSongScript' custom scene in Camera2",
                [Lang_JA] = "上記の「Target Camera」に選ばれたカメラを「CameraSongScript」シーン名としてCamera2に登録・上書きします"
            },
            ["hint-script-profile"] = new Dictionary<string, string>
            {
                [Lang_EN] = "CameraPlus profile to use when song script is detected",
                [Lang_JA] = "曲スクリプト検出時に切り替えるCameraPlusプロファイル"
            },
            ["hint-cameraplus-audio-sync"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Sync script with song timeline (CameraPlus)",
                [Lang_JA] = "曲のタイムラインに同期してスクリプトを進行（CameraPlus）"
            },
            ["hint-show-status-panel"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Show status indicator panel outside tab",
                [Lang_JA] = "タブ外にステータスインジケータパネルを表示する"
            },
            ["hint-panel-position"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Select status panel position",
                [Lang_JA] = "ステータスパネルの表示位置を選択"
            },
            // ========== CameraSongScriptModSettingView ==========
            ["hint-per-script-height"] = new Dictionary<string, string>
            {
                [Lang_EN] = "ON: Save height offset per-script. OFF: Use single shared setting",
                [Lang_JA] = "ONの場合、高さオフセットをスクリプト毎に個別保存します。OFFの場合、共通の1つの設定として使用します"
            },
            ["hint-hover-hint-language"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Select hover-hint display language",
                [Lang_JA] = "hover-hintの表示言語を選択します"
            },
            ["hint-show-hover-hints"] = new Dictionary<string, string>
            {
                [Lang_EN] = "Show/hide hover-hints",
                [Lang_JA] = "hover-hintの表示/非表示を切り替えます"
            },
        };

        /// <summary>
        /// 現在の設定に基づいた実効言語コードを返す
        /// </summary>
        public static string GetEffectiveLanguage()
        {
            string setting = CameraSongScriptConfig.Instance.HoverHintLanguage;

            switch (setting)
            {
                case "Japanese":
                    return Lang_JA;

                case "Auto":
                    return DetectGameLanguage();

                case "English":
                default:
                    return Lang_EN;
            }
        }

        /// <summary>
        /// ゲーム本体の言語設定を検出する
        /// </summary>
        private static string DetectGameLanguage()
        {
            try
            {
                if (Polyglot.Localization.Instance.SelectedLanguage == Polyglot.Language.Japanese)
                {
                    return Lang_JA;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.Debug($"HoverHintLocalization: Failed to detect game language: {ex.Message}");
            }
            return Lang_EN;
        }

        /// <summary>
        /// 指定キーのhover-hintテキストを現在の設定言語で返す
        /// ShowHoverHints == false の場合は空文字列を返す
        /// </summary>
        public static string Get(string key)
        {
            if (!CameraSongScriptConfig.Instance.ShowHoverHints)
                return string.Empty;

            string lang = GetEffectiveLanguage();

            if (_hints.TryGetValue(key, out var langDict))
            {
                if (langDict.TryGetValue(lang, out var text))
                    return text;

                // フォールバック: 英語
                if (langDict.TryGetValue(Lang_EN, out var fallback))
                    return fallback;
            }

            return key;
        }
    }
}
