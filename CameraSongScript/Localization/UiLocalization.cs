using System;
using System.Collections.Generic;
using System.Globalization;

namespace CameraSongScript.Localization
{
    /// <summary>
    /// hover-hintを含むUI表示文言全体のローカライズ管理クラス
    /// </summary>
    internal static class UiLocalization
    {
        public const string OptionEnglish = "English";
        public const string OptionJapanese = "Japanese";
        public const string OptionAuto = "Auto";

        public const string OptionNone = "(none)";
        public const string OptionAll = "(All)";
        public const string OptionDefault = "(Default)";
        public const string OptionRandom = "(Random)";
        public const string OptionSameAsSongScript = "(Same as SongScript)";
        public const string OptionNoChange = "(NoChange)";
        public const string OptionDelete = "(Delete)";

        private static readonly Dictionary<string, Dictionary<string, string>> _texts
            = new Dictionary<string, Dictionary<string, string>>
        {
            ["label-camera-mod"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Camera Mod:",
                [HoverHintLocalization.Lang_JA] = "カメラMod:"
            },
            ["toggle-enabled"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Enabled",
                [HoverHintLocalization.Lang_JA] = "有効"
            },
            ["label-script-file"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Script File",
                [HoverHintLocalization.Lang_JA] = "スクリプトファイル"
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
                [HoverHintLocalization.Lang_JA] = "譜面作者:"
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
            ["button-preview-show-start"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Show / Start",
                [HoverHintLocalization.Lang_JA] = "表示・開始"
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
            ["toggle-use-audio-sync"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Use Audio Sync",
                [HoverHintLocalization.Lang_JA] = "音声同期を使用"
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
                [HoverHintLocalization.Lang_JA] = "SongScriptプロファイル"
            },
            ["section-common-script"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "--- Common Script ---",
                [HoverHintLocalization.Lang_JA] = "--- 汎用スクリプト ---"
            },
            ["toggle-fallback-to-common"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Fallback to Common",
                [HoverHintLocalization.Lang_JA] = "汎用スクリプトにフォールバック"
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
            ["setting-per-script-height"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Per-Script Height Offset",
                [HoverHintLocalization.Lang_JA] = "スクリプト別高さオフセット"
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
                [HoverHintLocalization.Lang_EN] = "Left / Upper / Right",
                [HoverHintLocalization.Lang_JA] = "左 / 上 / 右"
            },
            ["status-panel-left-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Left / Upper / Left",
                [HoverHintLocalization.Lang_JA] = "左 / 上 / 左"
            },
            ["status-panel-left-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Left / Lower / Right",
                [HoverHintLocalization.Lang_JA] = "左 / 下 / 右"
            },
            ["status-panel-left-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Left / Lower / Left",
                [HoverHintLocalization.Lang_JA] = "左 / 下 / 左"
            },
            ["status-panel-center-upper-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Center / Upper / Right",
                [HoverHintLocalization.Lang_JA] = "中央 / 上 / 右"
            },
            ["status-panel-center-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Center / Upper / Left",
                [HoverHintLocalization.Lang_JA] = "中央 / 上 / 左"
            },
            ["status-panel-center-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Center / Lower / Right",
                [HoverHintLocalization.Lang_JA] = "中央 / 下 / 右"
            },
            ["status-panel-center-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Center / Lower / Left",
                [HoverHintLocalization.Lang_JA] = "中央 / 下 / 左"
            },
            ["status-panel-right-upper-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Right / Upper / Right",
                [HoverHintLocalization.Lang_JA] = "右 / 上 / 右"
            },
            ["status-panel-right-upper-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Right / Upper / Left",
                [HoverHintLocalization.Lang_JA] = "右 / 上 / 左"
            },
            ["status-panel-right-lower-right"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Right / Lower / Right",
                [HoverHintLocalization.Lang_JA] = "右 / 下 / 右"
            },
            ["status-panel-right-lower-left"] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Right / Lower / Left",
                [HoverHintLocalization.Lang_JA] = "右 / 下 / 左"
            }
        };

        private static readonly Dictionary<string, Dictionary<string, string>> _optionTexts
            = new Dictionary<string, Dictionary<string, string>>
        {
            [OptionEnglish] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "English",
                [HoverHintLocalization.Lang_JA] = "英語"
            },
            [OptionJapanese] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Japanese",
                [HoverHintLocalization.Lang_JA] = "日本語"
            },
            [OptionAuto] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "Auto",
                [HoverHintLocalization.Lang_JA] = "自動"
            },
            [OptionNone] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(none)",
                [HoverHintLocalization.Lang_JA] = "(なし)"
            },
            [OptionAll] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(All)",
                [HoverHintLocalization.Lang_JA] = "(すべて)"
            },
            [OptionDefault] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Default)",
                [HoverHintLocalization.Lang_JA] = "(デフォルト)"
            },
            [OptionRandom] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Random)",
                [HoverHintLocalization.Lang_JA] = "(ランダム)"
            },
            [OptionSameAsSongScript] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Same as SongScript)",
                [HoverHintLocalization.Lang_JA] = "(SongScriptと同じ)"
            },
            [OptionNoChange] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(NoChange)",
                [HoverHintLocalization.Lang_JA] = "(変更しない)"
            },
            [OptionDelete] = new Dictionary<string, string>
            {
                [HoverHintLocalization.Lang_EN] = "(Delete)",
                [HoverHintLocalization.Lang_JA] = "(空にする)"
            }
        };

        private static readonly string[] _statusPanelPresetKeys = new string[]
        {
            "status-panel-left-upper-right",
            "status-panel-left-upper-left",
            "status-panel-left-lower-right",
            "status-panel-left-lower-left",
            "status-panel-center-upper-right",
            "status-panel-center-upper-left",
            "status-panel-center-lower-right",
            "status-panel-center-lower-left",
            "status-panel-right-upper-right",
            "status-panel-right-upper-left",
            "status-panel-right-lower-right",
            "status-panel-right-lower-left"
        };

        private static readonly string[] _statusPanelLegacyNames = new string[]
        {
            "LeftUpperRight",
            "LeftUpperLeft",
            "LeftLowerRight",
            "LeftLowerLeft",
            "CenterUpperRight",
            "CenterUpperLeft",
            "CenterLowerRight",
            "CenterLowerLeft",
            "RightUpperRight",
            "RightUpperLeft",
            "RightLowerRight",
            "RightLowerLeft"
        };

        public static event Action LanguageChanged;

        /// <summary>
        /// UI言語が変更されたことを通知する
        /// </summary>
        public static void NotifyLanguageChanged()
        {
            var handler = LanguageChanged;
            if (handler != null)
                handler();
        }

        /// <summary>
        /// 指定キーのUI文字列を現在言語で返す
        /// </summary>
        public static string Get(string key)
        {
            return GetLocalizedValue(_texts, key, key);
        }

        /// <summary>
        /// 書式付きUI文字列を現在言語で返す
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }

        /// <summary>
        /// 内部で保持する英語ベースの選択値を表示用文字列に変換する
        /// </summary>
        public static string GetOptionDisplay(string canonicalValue, params string[] localizableValues)
        {
            if (string.IsNullOrEmpty(canonicalValue))
                return canonicalValue;

            if (localizableValues == null || localizableValues.Length == 0)
                return canonicalValue;

            for (int i = 0; i < localizableValues.Length; i++)
            {
                if (string.Equals(canonicalValue, localizableValues[i], StringComparison.Ordinal))
                    return GetLocalizedValue(_optionTexts, canonicalValue, canonicalValue);
            }

            return canonicalValue;
        }

        /// <summary>
        /// 表示用文字列を内部保持用の英語ベース選択値へ戻す
        /// </summary>
        public static string ToCanonicalOption(string displayValue, params string[] localizableValues)
        {
            if (string.IsNullOrEmpty(displayValue))
                return displayValue;

            if (localizableValues == null || localizableValues.Length == 0)
                return displayValue;

            for (int i = 0; i < localizableValues.Length; i++)
            {
                string canonicalValue = localizableValues[i];
                if (string.Equals(displayValue, canonicalValue, StringComparison.Ordinal))
                    return canonicalValue;

                if (string.Equals(displayValue, GetOptionDisplay(canonicalValue, canonicalValue), StringComparison.Ordinal))
                    return canonicalValue;

                if (string.Equals(displayValue, GetLocalizedValue(_optionTexts, canonicalValue, canonicalValue, HoverHintLocalization.Lang_EN), StringComparison.Ordinal))
                    return canonicalValue;

                if (string.Equals(displayValue, GetLocalizedValue(_optionTexts, canonicalValue, canonicalValue, HoverHintLocalization.Lang_JA), StringComparison.Ordinal))
                    return canonicalValue;
            }

            return displayValue;
        }

        /// <summary>
        /// 選択肢一覧を現在言語でローカライズする
        /// </summary>
        public static List<object> LocalizeOptions(IEnumerable<string> canonicalValues, params string[] localizableValues)
        {
            var list = new List<object>();
            if (canonicalValues == null)
                return list;

            foreach (var canonicalValue in canonicalValues)
            {
                list.Add(GetOptionDisplay(canonicalValue, localizableValues));
            }

            return list;
        }

        /// <summary>
        /// ステータスパネル位置の表示名一覧を返す
        /// </summary>
        public static List<object> GetStatusPanelPositionOptions()
        {
            var list = new List<object>();
            for (int i = 0; i < _statusPanelPresetKeys.Length; i++)
            {
                list.Add(Get(_statusPanelPresetKeys[i]));
            }
            return list;
        }

        /// <summary>
        /// ステータスパネル位置インデックスの表示名を返す
        /// </summary>
        public static string GetStatusPanelPositionDisplayName(int index)
        {
            if (index < 0 || index >= _statusPanelPresetKeys.Length)
                index = 0;

            return Get(_statusPanelPresetKeys[index]);
        }

        /// <summary>
        /// 表示名からステータスパネル位置インデックスを解決する
        /// </summary>
        public static int GetStatusPanelPositionIndex(string displayValue)
        {
            if (string.IsNullOrEmpty(displayValue))
                return 0;

            for (int i = 0; i < _statusPanelPresetKeys.Length; i++)
            {
                if (string.Equals(displayValue, Get(_statusPanelPresetKeys[i]), StringComparison.Ordinal) ||
                    string.Equals(displayValue, _statusPanelLegacyNames[i], StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        private static string GetLocalizedValue(
            Dictionary<string, Dictionary<string, string>> dictionary,
            string key,
            string fallback)
        {
            return GetLocalizedValue(dictionary, key, fallback, HoverHintLocalization.GetEffectiveLanguage());
        }

        private static string GetLocalizedValue(
            Dictionary<string, Dictionary<string, string>> dictionary,
            string key,
            string fallback,
            string language)
        {
            Dictionary<string, string> valueByLanguage;
            if (dictionary.TryGetValue(key, out valueByLanguage))
            {
                string localizedValue;
                if (valueByLanguage.TryGetValue(language, out localizedValue))
                    return localizedValue;

                if (valueByLanguage.TryGetValue(HoverHintLocalization.Lang_EN, out localizedValue))
                    return localizedValue;
            }

            return fallback;
        }
    }
}
