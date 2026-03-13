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
                CameraSongScriptDetector.ReevaluateCommonScriptUsage();
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
                CameraSongScriptDetector.ReevaluateCommonScriptUsage();
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

        [UIValue("common-script-options")]
        public List<object> CommonScriptOptions
        {
            get
            {
                var list = new List<string>();
                if (CommonScriptCache.IsReady)
                {
                    foreach (var name in CommonScriptCache.GetDisplayNames())
                    {
                        list.Add(name);
                    }
                }
                if (list.Count == 0)
                    list.Add(UiLocalization.OptionNone);
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionRandom, UiLocalization.OptionNone);
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
                        return UiLocalization.GetOptionDisplay(selected, UiLocalization.OptionRandom);
                }
                return UiLocalization.GetOptionDisplay(UiLocalization.OptionRandom, UiLocalization.OptionRandom);
            }
            set
            {
                string name = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionRandom, UiLocalization.OptionNone);
                if (!string.IsNullOrEmpty(name) && name != UiLocalization.OptionNone)
                {
                    CameraSongScriptConfig.Instance.SelectedCommonScript = name;
                    CameraSongScriptDetector.ReevaluateCommonScriptUsage();
                    NotifyPropertyChanged(nameof(SongScriptStatus));

                    // 汎用スクリプト選択変更後のオフセット関連UI更新
                    NotifyPropertyChanged(nameof(IsOffsetInteractable));
                    NotifyPropertyChanged(nameof(CameraHeightOffset));
                    if (cameraHeightOffsetSlider != null)
                    {
                        cameraHeightOffsetSlider.ReceiveValue();
                    }

                    RefreshLayout();
                    _statusView?.UpdateContent();
                    HandlePreviewSelectionChanged();
                }
            }
        }

        // --- Camera2用: 汎用スクリプト専用設定 ---

        [UIValue("common-target-camera-options")]
        public List<object> CommonTargetCameraOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionSameAsSongScript };
                if (CameraModDetector.IsCamera2 && Plugin.IsCamHelperReady)
                {
                    try
                    {
                        var cams = Plugin.CamHelper.GetAvailableCameras()?.ToList() ?? new List<string>();
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
                if (CameraModDetector.IsCamera2 && Plugin.IsCamHelperReady)
                {
                    try
                    {
                        var scenes = Plugin.CamHelper.CustomScenes;
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
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
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
                CameraSongScriptDetector.SyncCameraPlusPath();
                _statusView?.UpdateContent();
            }
        }

        #endregion
    }
}

