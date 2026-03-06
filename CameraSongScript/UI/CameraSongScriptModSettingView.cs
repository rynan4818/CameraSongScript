using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Localization;
using System;
using Zenject;

namespace CameraSongScript.UI
{
    public class CameraSongScriptModSettingView : IInitializable, IDisposable
    {
        private bool _disposedValue;
        public static readonly string _buttonName = "CameraSongScript";
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);
        public void Initialize()
        {
            BSMLSettings.instance.AddSettingsMenu(_buttonName, this.ResourceName, this);
        }
        public virtual void Dispose()
        {
            if (this._disposedValue)
                return;
            BSMLSettings.instance?.RemoveSettingsMenu(_buttonName);
            this._disposedValue = true;
        }

        [UIValue("UsePerScriptHeightOffset")]
        public bool UsePerScriptHeightOffset
        {
            get => CameraSongScriptConfig.Instance.UsePerScriptHeightOffset;
            set => CameraSongScriptConfig.Instance.UsePerScriptHeightOffset = value;
        }

        #region hover-hint言語設定

        [UIValue("hover-hint-language-options")]
        public List<object> HoverHintLanguageOptions => new List<object> { "English", "Japanese", "Auto" };

        [UIValue("hover-hint-language")]
        public object HoverHintLanguage
        {
            get => CameraSongScriptConfig.Instance.HoverHintLanguage ?? "English";
            set => CameraSongScriptConfig.Instance.HoverHintLanguage = value as string ?? "English";
        }

        #endregion

        #region hover-hint表示/非表示

        [UIValue("ShowHoverHints")]
        public bool ShowHoverHints
        {
            get => CameraSongScriptConfig.Instance.ShowHoverHints;
            set => CameraSongScriptConfig.Instance.ShowHoverHints = value;
        }

        #endregion

        #region hover-hintローカライズ

        [UIValue("hint-per-script-height")]
        public string HintPerScriptHeight => HoverHintLocalization.Get("hint-per-script-height");

        [UIValue("hint-hover-hint-language")]
        public string HintHoverHintLanguage => HoverHintLocalization.Get("hint-hover-hint-language");

        [UIValue("hint-show-hover-hints")]
        public string HintShowHoverHints => HoverHintLocalization.Get("hint-show-hover-hints");

        #endregion
    }
}
