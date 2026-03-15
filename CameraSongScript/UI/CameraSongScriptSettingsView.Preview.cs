using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
using UnityEngine;

namespace CameraSongScript.UI
{
    public partial class CameraSongScriptSettingsView
    {
        #region プレビュー設定

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
                        : _previewController.LoadedScriptDisplayName;
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

        [UIAction("preview-show-start")]
        private void PreviewShowStart()
        {
            _previewController?.ShowAndStart();
            RefreshPreviewBindings();
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
            _previewController?.HandleSelectionChanged();
            _lastPreviewUiTime = float.NegativeInfinity;
            RefreshPreviewBindings();
        }

        private void HandlePreviewVisualChanged()
        {
            _previewController?.HandleVisualChange();
            _lastPreviewUiTime = float.NegativeInfinity;
            RefreshPreviewBindings();
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

        #endregion
    }
}
