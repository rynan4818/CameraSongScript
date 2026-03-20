using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
using CameraSongScript.Utilities;
using UnityEngine;

namespace CameraSongScript.UI
{
    public partial class CameraSongScriptSettingsView
    {
        #region プレビュー設定

        private const float PreviewMiniatureScaleStep = 0.01f;
        private const float PreviewVisiblePositionStep = 0.1f;
        private const float PreviewPathLineWidthStep = 0.001f;
        private const float PreviewScreenScaleStep = 0.0001f;
        private const float PreviewScreenPositionYStep = 0.01f;
        private const float MinimumPreviewMiniatureScale = 0.01f;
        private const float MinimumPreviewPathLineWidth = 0.001f;
        private const float MinimumPreviewScreenScale = 0.0001f;

        [UIValue("can-preview")]
        public bool CanPreview => _previewController != null && _previewController.CanPreviewSelection;

        [UIValue("is-preview-visible")]
        public bool IsPreviewVisible => _previewController != null && _previewController.IsVisible;

        [UIValue("preview-position")]
        public float PreviewPosition
        {
            get => _previewController != null ? _previewController.CurrentTime : 0f;
            set
            {
                if (_suppressPreviewSeek || _previewController == null)
                    return;

                _previewController.Seek(value, _previewController.IsVisible);
                RefreshPreviewBindings();
            }
        }

        [UIValue("preview-status")]
        public string PreviewStatus
        {
            get
            {
                if (_previewController == null)
                    return UiLocalization.Get("preview-initializing");

                if (_previewController.IsVisible)
                {
                    string state = _previewController.IsPlaying
                        ? UiLocalization.Get("preview-state-playing")
                        : UiLocalization.Get("preview-state-stopped");
                    string displayName = string.IsNullOrEmpty(_previewController.LoadedScriptDisplayName)
                        ? "--"
                        : SongScriptDisplayLabelFormatter.Format(_previewController.LoadedScriptDisplayName);
                    return UiLocalization.Format(
                        "preview-active",
                        displayName,
                        FormatPreviewClock(_previewController.CurrentTime),
                        FormatPreviewClock(_previewController.Duration),
                        state,
                        _previewController.SpeedMultiplier);
                }

                return CanPreview ? UiLocalization.Get("preview-ready") : UiLocalization.Get("preview-no-script");
            }
        }

        [UIValue("label-preview-miniature-scale")]
        public string LabelPreviewMiniatureScale => UiLocalization.Get("label-preview-miniature-scale");

        [UIValue("button-preview-visual-settings-reset")]
        public string ButtonPreviewVisualSettingsReset => UiLocalization.Get("button-preview-visual-settings-reset");

        [UIValue("section-preview-visual-settings")]
        public string SectionPreviewVisualSettings => UiLocalization.Get("section-preview-visual-settings");

        [UIValue("label-preview-visible-position-x")]
        public string LabelPreviewVisiblePositionX => UiLocalization.Get("label-preview-visible-position-x");

        [UIValue("label-preview-visible-position-y")]
        public string LabelPreviewVisiblePositionY => UiLocalization.Get("label-preview-visible-position-y");

        [UIValue("label-preview-visible-position-z")]
        public string LabelPreviewVisiblePositionZ => UiLocalization.Get("label-preview-visible-position-z");

        [UIValue("label-preview-path-line-width")]
        public string LabelPreviewPathLineWidth => UiLocalization.Get("label-preview-path-line-width");

        [UIValue("label-preview-screen-scale")]
        public string LabelPreviewScreenScale => UiLocalization.Get("label-preview-screen-scale");

        [UIValue("label-preview-screen-position-y")]
        public string LabelPreviewScreenPositionY => UiLocalization.Get("label-preview-screen-position-y");

        [UIValue("preview-miniature-scale-value")]
        public string PreviewMiniatureScaleValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewMiniatureScale : 0f,
            "0.00");

        [UIValue("preview-visible-position-x-value")]
        public string PreviewVisiblePositionXValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewVisiblePositionX : 0f,
            "0.0");

        [UIValue("preview-visible-position-y-value")]
        public string PreviewVisiblePositionYValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewVisiblePositionY : 0f,
            "0.0");

        [UIValue("preview-visible-position-z-value")]
        public string PreviewVisiblePositionZValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewVisiblePositionZ : 0f,
            "0.0");

        [UIValue("preview-path-line-width-value")]
        public string PreviewPathLineWidthValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewPathLineWidth : 0f,
            "0.000");

        [UIValue("preview-screen-scale-value")]
        public string PreviewScreenScaleValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewScreenScale : 0f,
            "0.0000");

        [UIValue("preview-screen-position-y-value")]
        public string PreviewScreenPositionYValue => FormatPreviewConfigValue(
            CameraSongScriptConfig.Instance != null ? CameraSongScriptConfig.Instance.PreviewScreenPositionY : 0f,
            "0.00");

        [UIAction("preview-show-start")]
        private void PreviewShowStart()
        {
            _previewController?.ShowAndStart();
            RefreshPreviewBindings();
        }

        [UIAction("preview-miniature-scale-decrease")]
        private void PreviewMiniatureScaleDecrease()
        {
            AdjustPreviewMiniatureScale(-PreviewMiniatureScaleStep);
        }

        [UIAction("preview-miniature-scale-increase")]
        private void PreviewMiniatureScaleIncrease()
        {
            AdjustPreviewMiniatureScale(PreviewMiniatureScaleStep);
        }

        [UIAction("preview-visible-position-x-decrease")]
        private void PreviewVisiblePositionXDecrease()
        {
            AdjustPreviewVisiblePositionX(-PreviewVisiblePositionStep);
        }

        [UIAction("preview-visible-position-x-increase")]
        private void PreviewVisiblePositionXIncrease()
        {
            AdjustPreviewVisiblePositionX(PreviewVisiblePositionStep);
        }

        [UIAction("preview-visible-position-y-decrease")]
        private void PreviewVisiblePositionYDecrease()
        {
            AdjustPreviewVisiblePositionY(-PreviewVisiblePositionStep);
        }

        [UIAction("preview-visible-position-y-increase")]
        private void PreviewVisiblePositionYIncrease()
        {
            AdjustPreviewVisiblePositionY(PreviewVisiblePositionStep);
        }

        [UIAction("preview-visible-position-z-decrease")]
        private void PreviewVisiblePositionZDecrease()
        {
            AdjustPreviewVisiblePositionZ(-PreviewVisiblePositionStep);
        }

        [UIAction("preview-visible-position-z-increase")]
        private void PreviewVisiblePositionZIncrease()
        {
            AdjustPreviewVisiblePositionZ(PreviewVisiblePositionStep);
        }

        [UIAction("preview-path-line-width-decrease")]
        private void PreviewPathLineWidthDecrease()
        {
            AdjustPreviewPathLineWidth(-PreviewPathLineWidthStep);
        }

        [UIAction("preview-path-line-width-increase")]
        private void PreviewPathLineWidthIncrease()
        {
            AdjustPreviewPathLineWidth(PreviewPathLineWidthStep);
        }

        [UIAction("preview-screen-scale-decrease")]
        private void PreviewScreenScaleDecrease()
        {
            AdjustPreviewScreenScale(-PreviewScreenScaleStep);
        }

        [UIAction("preview-screen-scale-increase")]
        private void PreviewScreenScaleIncrease()
        {
            AdjustPreviewScreenScale(PreviewScreenScaleStep);
        }

        [UIAction("preview-screen-position-y-decrease")]
        private void PreviewScreenPositionYDecrease()
        {
            AdjustPreviewScreenPositionY(-PreviewScreenPositionYStep);
        }

        [UIAction("preview-screen-position-y-increase")]
        private void PreviewScreenPositionYIncrease()
        {
            AdjustPreviewScreenPositionY(PreviewScreenPositionYStep);
        }

        [UIAction("reset-preview-visual-settings")]
        private void ResetPreviewVisualSettings()
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            var defaults = new CameraSongScriptConfig();
            bool changed = false;

            changed |= !Mathf.Approximately(config.PreviewMiniatureScale, defaults.PreviewMiniatureScale);
            changed |= !Mathf.Approximately(config.PreviewVisiblePositionX, defaults.PreviewVisiblePositionX);
            changed |= !Mathf.Approximately(config.PreviewVisiblePositionY, defaults.PreviewVisiblePositionY);
            changed |= !Mathf.Approximately(config.PreviewVisiblePositionZ, defaults.PreviewVisiblePositionZ);
            changed |= !Mathf.Approximately(config.PreviewPathLineWidth, defaults.PreviewPathLineWidth);
            changed |= !Mathf.Approximately(config.PreviewScreenScale, defaults.PreviewScreenScale);
            changed |= !Mathf.Approximately(config.PreviewScreenPositionY, defaults.PreviewScreenPositionY);

            config.PreviewMiniatureScale = defaults.PreviewMiniatureScale;
            config.PreviewVisiblePositionX = defaults.PreviewVisiblePositionX;
            config.PreviewVisiblePositionY = defaults.PreviewVisiblePositionY;
            config.PreviewVisiblePositionZ = defaults.PreviewVisiblePositionZ;
            config.PreviewPathLineWidth = defaults.PreviewPathLineWidth;
            config.PreviewScreenScale = defaults.PreviewScreenScale;
            config.PreviewScreenPositionY = defaults.PreviewScreenPositionY;

            NotifyPropertyChanged(nameof(PreviewMiniatureScaleValue));
            NotifyPropertyChanged(nameof(PreviewVisiblePositionXValue));
            NotifyPropertyChanged(nameof(PreviewVisiblePositionYValue));
            NotifyPropertyChanged(nameof(PreviewVisiblePositionZValue));
            NotifyPropertyChanged(nameof(PreviewPathLineWidthValue));
            NotifyPropertyChanged(nameof(PreviewScreenScaleValue));
            NotifyPropertyChanged(nameof(PreviewScreenPositionYValue));

            if (changed)
                HandlePreviewVisualChanged();
        }

        [UIAction("preview-stop")]
        private void PreviewStop()
        {
            _previewController?.Stop();
            RefreshPreviewBindings();
        }

        [UIAction("preview-clear")]
        private void PreviewClear()
        {
            _suppressPreviewSeek = true;
            try
            {
                _previewController?.Clear();
                _lastPreviewUiTime = float.NegativeInfinity;
                RefreshPreviewBindings();
            }
            finally
            {
                _suppressPreviewSeek = false;
            }
        }

        [UIAction("preview-speed-x2")]
        private void PreviewSpeedX2()
        {
            _previewController?.StartAtSpeed(2);
            RefreshPreviewBindings();
        }

        [UIAction("preview-speed-x4")]
        private void PreviewSpeedX4()
        {
            _previewController?.StartAtSpeed(4);
            RefreshPreviewBindings();
        }

        [UIAction("preview-speed-x8")]
        private void PreviewSpeedX8()
        {
            _previewController?.StartAtSpeed(8);
            RefreshPreviewBindings();
        }

        [UIAction("preview-speed-x16")]
        private void PreviewSpeedX16()
        {
            _previewController?.StartAtSpeed(16);
            RefreshPreviewBindings();
        }

        [UIAction("format-preview-time")]
        private string FormatPreviewSliderValue(float value)
        {
            return FormatPreviewClock(value);
        }

        private void HandlePreviewSelectionChanged()
        {
            _suppressPreviewSeek = true;
            try
            {
                _previewController?.HandleSelectionChanged();
                _lastPreviewUiTime = float.NegativeInfinity;
                RefreshPreviewBindings();
            }
            finally
            {
                _suppressPreviewSeek = false;
            }
        }

        private void HandlePreviewVisualChanged()
        {
            _previewController?.HandleVisualChange();
            _lastPreviewUiTime = float.NegativeInfinity;
            RefreshPreviewBindings();
        }

        private void AdjustPreviewMiniatureScale(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = Mathf.Max(
                MinimumPreviewMiniatureScale,
                RoundToDecimals(config.PreviewMiniatureScale + delta, 2));
            if (Mathf.Approximately(config.PreviewMiniatureScale, newValue))
                return;

            config.PreviewMiniatureScale = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewMiniatureScaleValue));
        }

        private void AdjustPreviewVisiblePositionX(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = RoundToDecimals(config.PreviewVisiblePositionX + delta, 1);
            if (Mathf.Approximately(config.PreviewVisiblePositionX, newValue))
                return;

            config.PreviewVisiblePositionX = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewVisiblePositionXValue));
        }

        private void AdjustPreviewVisiblePositionY(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = RoundToDecimals(config.PreviewVisiblePositionY + delta, 1);
            if (Mathf.Approximately(config.PreviewVisiblePositionY, newValue))
                return;

            config.PreviewVisiblePositionY = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewVisiblePositionYValue));
        }

        private void AdjustPreviewVisiblePositionZ(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = RoundToDecimals(config.PreviewVisiblePositionZ + delta, 1);
            if (Mathf.Approximately(config.PreviewVisiblePositionZ, newValue))
                return;

            config.PreviewVisiblePositionZ = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewVisiblePositionZValue));
        }

        private void AdjustPreviewPathLineWidth(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = Mathf.Max(
                MinimumPreviewPathLineWidth,
                RoundToDecimals(config.PreviewPathLineWidth + delta, 3));
            if (Mathf.Approximately(config.PreviewPathLineWidth, newValue))
                return;

            config.PreviewPathLineWidth = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewPathLineWidthValue));
        }

        private void AdjustPreviewScreenScale(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = Mathf.Max(
                MinimumPreviewScreenScale,
                RoundToDecimals(config.PreviewScreenScale + delta, 4));
            if (Mathf.Approximately(config.PreviewScreenScale, newValue))
                return;

            config.PreviewScreenScale = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewScreenScaleValue));
        }

        private void AdjustPreviewScreenPositionY(float delta)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            float newValue = RoundToDecimals(config.PreviewScreenPositionY + delta, 2);
            if (Mathf.Approximately(config.PreviewScreenPositionY, newValue))
                return;

            config.PreviewScreenPositionY = newValue;
            RefreshPreviewVisualSettingUi(nameof(PreviewScreenPositionYValue));
        }

        private void RefreshPreviewBindings()
        {
            if (_previewController != null)
            {
                _lastPreviewUiTime = _previewController.CurrentTime;
                _lastPreviewVisible = _previewController.IsVisible;
                _lastPreviewPlaying = _previewController.IsPlaying;
                _lastPreviewSpeed = _previewController.SpeedMultiplier;
            }

            NotifyPropertyChanged(nameof(CanPreview));
            NotifyPropertyChanged(nameof(IsPreviewVisible));
            NotifyPropertyChanged(nameof(PreviewPosition));
            NotifyPropertyChanged(nameof(PreviewStatus));
            SyncPreviewSlider();
        }

        private void RefreshPreviewVisualSettingUi(params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                NotifyPropertyChanged(propertyName);
            }

            HandlePreviewVisualChanged();
        }

        private void RefreshPreviewRuntimeUi()
        {
            NotifyPropertyChanged(nameof(PreviewPosition));
            NotifyPropertyChanged(nameof(PreviewStatus));
            SyncPreviewSlider();
        }

        private void SyncPreviewSlider()
        {
            if (previewPositionSlider == null || previewPositionSlider.slider == null)
                return;

            float maxValue = Mathf.Max(_previewController != null ? _previewController.Duration : 0f, PreviewSliderStep);
            previewPositionSlider.slider.minValue = 0f;
            previewPositionSlider.slider.maxValue = maxValue;
            previewPositionSlider.increments = PreviewSliderStep;
            previewPositionSlider.slider.numberOfSteps = Mathf.Max(2, Mathf.RoundToInt(maxValue / PreviewSliderStep) + 1);
            previewPositionSlider.interactable = CanPreview;

            _suppressPreviewSeek = true;
            previewPositionSlider.Value = Mathf.Clamp(_previewController != null ? _previewController.CurrentTime : 0f, 0f, maxValue);
            _suppressPreviewSeek = false;
        }

        private static string FormatPreviewClock(float seconds)
        {
            if (seconds < 0f)
                seconds = 0f;

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}.{2:00}",
                (int)time.TotalMinutes,
                time.Seconds,
                time.Milliseconds / 10);
        }

        private static float RoundToDecimals(float value, int decimals)
        {
            return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }

        private static string FormatPreviewConfigValue(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
