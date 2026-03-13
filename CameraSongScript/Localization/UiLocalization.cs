using System;
using System.Collections.Generic;
using System.Globalization;
using CameraSongScript.Configuration;

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

        private static readonly Dictionary<string, Dictionary<string, string>> _texts = UiLocalizationResources.CreateTexts();

        private static readonly Dictionary<string, Dictionary<string, string>> _optionTexts = UiLocalizationResources.CreateOptionTexts();

        private static readonly string[] _statusPanelPresetKeys = StatusPanelPresetCatalog.GetLocalizationKeys();
        private static readonly string[] _statusPanelLegacyNames = StatusPanelPresetCatalog.GetLegacyNames();

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

