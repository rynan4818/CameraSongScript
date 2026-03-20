using System.Collections.Generic;
using CameraSongScript.Configuration;
using PolyglotLanguage = BGLib.Polyglot.LocalizationLanguage;
using PolyglotLocalization = BGLib.Polyglot.Localization;

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
        private static readonly Dictionary<string, Dictionary<string, string>> _hints = HoverHintLocalizationResources.CreateHints();

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
                if (PolyglotLocalization.Instance.SelectedLanguage == PolyglotLanguage.Japanese)
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
