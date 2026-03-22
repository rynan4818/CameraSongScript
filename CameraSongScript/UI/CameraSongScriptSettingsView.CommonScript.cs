using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
using CameraSongScript.Models;
using CameraSongScript.Utilities;
using UnityEngine;

namespace CameraSongScript.UI
{
    public partial class CameraSongScriptSettingsView
    {
        #region 汎用スクリプト設定

        [UIValue("use-common-fallback")]
        public bool UseCommonFallback
        {
            get => CameraSongScriptConfig.Instance.UseCommonScriptAsFallback;
            set
            {
                CameraSongScriptConfig.Instance.UseCommonScriptAsFallback = value;
                _scriptDetector.ReevaluateCommonScriptUsage();
                NotifyPropertyChanged(nameof(SongScriptStatus));
                NotifyPropertyChanged(nameof(IsOffsetInteractable));
                NotifyPropertyChanged(nameof(CameraHeightOffset));
                if (cameraHeightOffsetSlider != null) cameraHeightOffsetSlider.ReceiveValue();
                RefreshLayout();
                _statusView?.UpdateContent();
                HandlePreviewSelectionChanged();
            }
        }

        [UIValue("force-common-script")]
        public bool ForceCommonScript
        {
            get => CameraSongScriptConfig.Instance.ForceCommonScript;
            set
            {
                CameraSongScriptConfig.Instance.ForceCommonScript = value;
                _scriptDetector.ReevaluateCommonScriptUsage();
                NotifyPropertyChanged(nameof(SongScriptStatus));
                NotifyPropertyChanged(nameof(IsOffsetInteractable));
                NotifyPropertyChanged(nameof(CameraHeightOffset));
                if (cameraHeightOffsetSlider != null) cameraHeightOffsetSlider.ReceiveValue();
                RefreshLayout();
                _statusView?.UpdateContent();
                HandlePreviewSelectionChanged();
            }
        }

        [UIComponent("common-script-dropdown")]
        public DropDownListSetting commonScriptDropdown;

        private sealed class CommonScriptDropdownOption
        {
            public CommonScriptDropdownOption(string canonicalName)
            {
                CanonicalName = canonicalName ?? string.Empty;
                DisplayLabel =
                    CanonicalName == UiLocalization.OptionRandom ||
                    CanonicalName == UiLocalization.OptionNone
                        ? UiLocalization.GetOptionDisplay(CanonicalName, UiLocalization.OptionRandom, UiLocalization.OptionNone)
                        : SongScriptDisplayLabelFormatter.Format(CanonicalName);
            }

            public string CanonicalName { get; }

            public string DisplayLabel { get; }

            public override string ToString()
            {
                return DisplayLabel;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                    return true;

                if (obj is CommonScriptDropdownOption option)
                    return string.Equals(CanonicalName, option.CanonicalName, StringComparison.Ordinal);

                if (obj is string value)
                    return string.Equals(CanonicalName, value, StringComparison.Ordinal);

                return false;
            }

            public override int GetHashCode()
            {
                return StringComparer.Ordinal.GetHashCode(CanonicalName);
            }
        }

        [UIValue("common-script-options")]
        public List<object> CommonScriptOptions
        {
            get
            {
                var list = new List<object>();
                if (CommonScriptCache.IsReady)
                {
                    foreach (var name in CommonScriptCache.GetDisplayNames())
                    {
                        list.Add(CreateCommonScriptDropdownOption(name));
                    }
                }
                if (list.Count == 0)
                    list.Add(CreateCommonScriptDropdownOption(UiLocalization.OptionNone));
                return list;
            }
        }

        [UIValue("selected-common-script")]
        public object SelectedCommonScript
        {
            get
            {
                string selected = CameraSongScriptConfig.Instance.SelectedCommonScript;
                if (!string.IsNullOrEmpty(selected) && CommonScriptCache.IsReady)
                {
                    var names = CommonScriptCache.GetDisplayNames();
                    if (names.Contains(selected))
                        return GetSelectedCommonScriptValue(selected);
                }
                return GetSelectedCommonScriptValue(CommonScriptCache.IsReady
                    ? UiLocalization.OptionRandom
                    : UiLocalization.OptionNone);
            }
            set
            {
                string name = GetCanonicalCommonScriptName(value);
                if (!string.IsNullOrEmpty(name) && name != UiLocalization.OptionNone)
                {
                    CameraSongScriptConfig.Instance.SelectedCommonScript = name;
                    _scriptDetector.ReevaluateCommonScriptUsage();
                    NotifyPropertyChanged(nameof(SongScriptStatus));
                    NotifyPropertyChanged(nameof(HintCommonScriptFile));

                    // 汎用スクリプト選択変更後のオフセット関連UI更新
                    NotifyPropertyChanged(nameof(IsOffsetInteractable));
                    NotifyPropertyChanged(nameof(CameraHeightOffset));
                    if (cameraHeightOffsetSlider != null)
                    {
                        cameraHeightOffsetSlider.ReceiveValue();
                    }

                    RefreshLayout();
                    EnsureCommonScriptDropdownTextPresentation();
                    _statusView?.UpdateContent();
                    HandlePreviewSelectionChanged();
                }
            }
        }

        private static CommonScriptDropdownOption CreateCommonScriptDropdownOption(string fileName)
        {
            return new CommonScriptDropdownOption(fileName);
        }

        private object GetSelectedCommonScriptValue(string fileName)
        {
            if (commonScriptDropdown?.values != null)
            {
                foreach (object value in commonScriptDropdown.values)
                {
                    if (string.Equals(GetCanonicalCommonScriptName(value), fileName, StringComparison.Ordinal))
                        return value;
                }
            }

            return CreateCommonScriptDropdownOption(fileName);
        }

        private static string GetCanonicalCommonScriptName(object value)
        {
            if (value is CommonScriptDropdownOption option)
                return option.CanonicalName;

            return UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionRandom, UiLocalization.OptionNone);
        }

        // --- Camera2用: 汎用スクリプト専用設定 ---

        [UIValue("common-target-camera-options")]
        public List<object> CommonTargetCameraOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionSameAsSongScript };
                if (CameraModDetector.IsCamera2 && _cameraHelper != null && _cameraHelper.IsInitialized)
                {
                    try
                    {
                        var cams = _cameraHelper.GetAvailableCameras()?.ToList() ?? new List<string>();
                        foreach (var cam in cams) list.Add(cam);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warn($"SettingsView: Failed to get available cameras for CS: {ex.Message}");
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionSameAsSongScript);
            }
        }

        [UIValue("common-target-camera")]
        public object CommonTargetCamera
        {
            get
            {
                string cam = CameraSongScriptConfig.Instance.CommonScriptTargetCamera;
                return UiLocalization.GetOptionDisplay(
                    string.IsNullOrEmpty(cam) ? UiLocalization.OptionSameAsSongScript : cam,
                    UiLocalization.OptionSameAsSongScript);
            }
            set
            {
                string cam = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionSameAsSongScript);
                CameraSongScriptConfig.Instance.CommonScriptTargetCamera =
                    (cam == UiLocalization.OptionSameAsSongScript) ? string.Empty : (cam ?? string.Empty);
                _statusView?.UpdateContent();
            }
        }

        [UIValue("common-custom-scene-options")]
        public List<object> CommonCustomSceneOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionSameAsSongScript };
                if (CameraModDetector.IsCamera2 && _cameraHelper != null && _cameraHelper.IsInitialized)
                {
                    try
                    {
                        var scenes = _cameraHelper.CustomScenes;
                        if (scenes != null)
                        {
                            foreach (var scene in scenes) list.Add(scene);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warn($"SettingsView: Failed to get custom scenes for CS: {ex.Message}");
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionSameAsSongScript);
            }
        }

        [UIValue("common-custom-scene")]
        public object CommonCustomScene
        {
            get
            {
                string scene = CameraSongScriptConfig.Instance.CommonScriptCustomScene;
                return UiLocalization.GetOptionDisplay(
                    string.IsNullOrEmpty(scene) ? UiLocalization.OptionSameAsSongScript : scene,
                    UiLocalization.OptionSameAsSongScript);
            }
            set
            {
                string scene = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionSameAsSongScript);
                CameraSongScriptConfig.Instance.CommonScriptCustomScene =
                    (scene == UiLocalization.OptionSameAsSongScript) ? string.Empty : (scene ?? string.Empty);
                _statusView?.UpdateContent();
            }
        }

        // --- CameraPlus用: 汎用スクリプト専用設定 ---

        [UIValue("common-profile-options")]
        public List<object> CommonProfileOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionSameAsSongScript, UiLocalization.OptionNoChange, UiLocalization.OptionDelete };
                if (_cameraPlusHelper != null && _cameraPlusHelper.IsInitialized)
                {
                    foreach (var profile in _cameraPlusHelper.GetProfileList())
                    {
                        if (!string.IsNullOrEmpty(profile))
                            list.Add(profile);
                    }
                }
                return UiLocalization.LocalizeOptions(
                    list,
                    UiLocalization.OptionSameAsSongScript,
                    UiLocalization.OptionNoChange,
                    UiLocalization.OptionDelete);
            }
        }

        [UIValue("common-profile")]
        public object CommonProfile
        {
            get
            {
                string profile = CameraSongScriptConfig.Instance.CommonScriptProfile;
                if (string.IsNullOrEmpty(profile)) return UiLocalization.GetOptionDisplay(
                    UiLocalization.OptionSameAsSongScript,
                    UiLocalization.OptionSameAsSongScript);
                // 旧バージョンの "(Default)" 設定を "(Delete)" に変換
                if (profile == UiLocalization.OptionDefault) return UiLocalization.GetOptionDisplay(
                    UiLocalization.OptionDelete,
                    UiLocalization.OptionDelete);
                return UiLocalization.GetOptionDisplay(
                    profile,
                    UiLocalization.OptionSameAsSongScript,
                    UiLocalization.OptionNoChange,
                    UiLocalization.OptionDelete);
            }
            set
            {
                string profile = UiLocalization.ToCanonicalOption(
                    value as string,
                    UiLocalization.OptionSameAsSongScript,
                    UiLocalization.OptionNoChange,
                    UiLocalization.OptionDelete);
                if (profile == UiLocalization.OptionSameAsSongScript) profile = string.Empty;
                CameraSongScriptConfig.Instance.CommonScriptProfile = profile ?? string.Empty;
                _scriptDetector.SyncCameraPlusPath();
                _statusView?.UpdateContent();
            }
        }

        #endregion
    }
}

