using System;
using System.Collections.Generic;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Localization;
using Zenject;

namespace CameraSongScript.UI
{
    public class CameraSongScriptModSettingView : IInitializable, IDisposable, INotifyPropertyChanged
    {
        private bool _disposedValue;

        public static readonly string _buttonName = "CameraSongScript";

        public event PropertyChangedEventHandler PropertyChanged;

        public string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        [UIComponent("ui-language-dropdown")]
        public DropDownListSetting UiLanguageDropdown;

        public void Initialize()
        {
            BSMLSettings.instance.AddSettingsMenu(_buttonName, ResourceName, this);
            UiLocalization.LanguageChanged += OnLanguageChanged;
        }

        public virtual void Dispose()
        {
            if (_disposedValue)
                return;

            UiLocalization.LanguageChanged -= OnLanguageChanged;
            BSMLSettings.instance?.RemoveSettingsMenu(_buttonName);
            _disposedValue = true;
        }

        [UIValue("UsePerScriptHeightOffset")]
        public bool UsePerScriptHeightOffset
        {
            get => CameraSongScriptConfig.Instance.UsePerScriptHeightOffset;
            set => CameraSongScriptConfig.Instance.UsePerScriptHeightOffset = value;
        }

        #region hover-hint言語設定

        [UIValue("hover-hint-language-options")]
        public List<object> HoverHintLanguageOptions => UiLocalization.LocalizeOptions(
            new[] { UiLocalization.OptionEnglish, UiLocalization.OptionJapanese, UiLocalization.OptionAuto },
            UiLocalization.OptionEnglish,
            UiLocalization.OptionJapanese,
            UiLocalization.OptionAuto);

        [UIValue("hover-hint-language")]
        public object HoverHintLanguage
        {
            get => UiLocalization.GetOptionDisplay(
                CameraSongScriptConfig.Instance.HoverHintLanguage ?? UiLocalization.OptionAuto,
                UiLocalization.OptionEnglish,
                UiLocalization.OptionJapanese,
                UiLocalization.OptionAuto);
            set
            {
                string selected = UiLocalization.ToCanonicalOption(
                    value as string,
                    UiLocalization.OptionEnglish,
                    UiLocalization.OptionJapanese,
                    UiLocalization.OptionAuto);
                if (selected != UiLocalization.OptionEnglish &&
                    selected != UiLocalization.OptionJapanese &&
                    selected != UiLocalization.OptionAuto)
                {
                    selected = UiLocalization.OptionAuto;
                }

                if (CameraSongScriptConfig.Instance.HoverHintLanguage == selected)
                    return;

                CameraSongScriptConfig.Instance.HoverHintLanguage = selected;
                UiLocalization.NotifyLanguageChanged();
            }
        }

        #endregion

        #region hover-hint表示/非表示

        [UIValue("ShowHoverHints")]
        public bool ShowHoverHints
        {
            get => CameraSongScriptConfig.Instance.ShowHoverHints;
            set
            {
                if (CameraSongScriptConfig.Instance.ShowHoverHints == value)
                    return;

                CameraSongScriptConfig.Instance.ShowHoverHints = value;
                NotifyHoverHintBindingsChanged();
            }
        }

        #endregion

        #region UI文言ローカライズ

        [UIValue("setting-per-script-height")]
        public string SettingPerScriptHeight => UiLocalization.Get("setting-per-script-height");

        [UIValue("setting-ui-language")]
        public string SettingUiLanguage => UiLocalization.Get("setting-ui-language");

        [UIValue("setting-show-hover-hints")]
        public string SettingShowHoverHints => UiLocalization.Get("setting-show-hover-hints");

        #endregion

        #region hover-hintローカライズ

        [UIValue("hint-per-script-height")]
        public string HintPerScriptHeight => HoverHintLocalization.Get("hint-per-script-height");

        [UIValue("hint-hover-hint-language")]
        public string HintHoverHintLanguage => HoverHintLocalization.Get("hint-hover-hint-language");

        [UIValue("hint-show-hover-hints")]
        public string HintShowHoverHints => HoverHintLocalization.Get("hint-show-hover-hints");

        #endregion

        private void OnLanguageChanged()
        {
            NotifyPropertyChanged(nameof(HoverHintLanguageOptions));
            NotifyPropertyChanged(nameof(HoverHintLanguage));
            NotifyPropertyChanged(nameof(SettingPerScriptHeight));
            NotifyPropertyChanged(nameof(SettingUiLanguage));
            NotifyPropertyChanged(nameof(SettingShowHoverHints));
            NotifyHoverHintBindingsChanged();
            RefreshUiLanguageDropdown();
        }

        private void RefreshUiLanguageDropdown()
        {
            if (UiLanguageDropdown == null)
                return;

            UiLanguageDropdown.values = HoverHintLanguageOptions;
            UiLanguageDropdown.UpdateChoices();
            UiLanguageDropdown.ReceiveValue();
        }

        private void NotifyHoverHintBindingsChanged()
        {
            NotifyPropertyChanged(nameof(HintPerScriptHeight));
            NotifyPropertyChanged(nameof(HintHoverHintLanguage));
            NotifyPropertyChanged(nameof(HintShowHoverHints));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
