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
        #region Camera2専用設定

        [UIValue("show-camera2-settings")]
        public bool ShowCamera2Settings => CameraModDetector.IsCamera2;

        [UIValue("custom-scene-options")]
        public List<object> CustomSceneOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionDefault };
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
                        Plugin.Log.Warn($"SettingsView: Failed to get custom scenes: {ex.Message}");
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionDefault);
            }
        }

        [UIComponent("custom-scene-dropdown")]
        public DropDownListSetting customSceneDropdown;

        private string _selectedCustomScene = CameraSongScriptConfig.Instance.CustomSceneToSwitch ?? UiLocalization.OptionDefault;

        [UIValue("selected-custom-scene")]
        public object SelectedCustomScene
        {
            get => UiLocalization.GetOptionDisplay(
                string.IsNullOrEmpty(_selectedCustomScene) ? UiLocalization.OptionDefault : _selectedCustomScene,
                UiLocalization.OptionDefault);
            set
            {
                string scene = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionDefault);
                _selectedCustomScene = string.IsNullOrEmpty(scene) ? UiLocalization.OptionDefault : scene;
                
                // 設定に保存（実際の切り替えはゲームシーン開始時に自動で行われる）
                CameraSongScriptConfig.Instance.CustomSceneToSwitch = _selectedCustomScene;
            }
        }

        [UIAction("add-custom-scene")]
        public void AddCustomScene()
        {
            if (CameraModDetector.IsCamera2 && Plugin.IsCamHelperReady)
            {
                var targetCam = CameraSongScriptConfig.Instance.TargetCameras;
                IEnumerable<string> camerasToAdd;

                // All または 未指定の場合は有効なすべてのカメラを追加、そうでない場合は指定カメラのみ追加
                if (string.IsNullOrEmpty(targetCam) || targetCam == UiLocalization.OptionAll)
                {
                    camerasToAdd = Plugin.CamHelper.GetAvailableCameras();
                }
                else
                {
                    camerasToAdd = new List<string> { targetCam };
                }

                Plugin.CamHelper.CreateOrUpdateCustomScene("CameraSongScript", camerasToAdd);
                
                // ドロップダウンのリストとUIを更新
                NotifyPropertyChanged(nameof(CustomSceneOptions));
                NotifyPropertyChanged(nameof(SelectedCustomScene));
                RefreshDropdown(customSceneDropdown, CustomSceneOptions);
            }
        }

        [UIValue("use-audio-sync")]
        public bool UseAudioSync
        {
            get => CameraSongScriptConfig.Instance.UseAudioSync;
            set => CameraSongScriptConfig.Instance.UseAudioSync = value;
        }

        [UIValue("target-camera-options")]
        public List<object> TargetCameraOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionAll };
                if (CameraModDetector.IsCamera2 && Plugin.IsCamHelperReady)
                {
                    try
                    {
                        var cams = Plugin.CamHelper.GetAvailableCameras()?.ToList() ?? new List<string>();
                        foreach (var cam in cams) list.Add(cam);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warn($"SettingsView: Failed to get available cameras: {ex.Message}");
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionAll);
            }
        }

        [UIValue("target-cameras")]
        public object TargetCameras
        {
            get
            {
                string cam = CameraSongScriptConfig.Instance.TargetCameras;
                return UiLocalization.GetOptionDisplay(
                    string.IsNullOrEmpty(cam) ? UiLocalization.OptionAll : cam,
                    UiLocalization.OptionAll);
            }
            set
            {
                string cam = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionAll);
                if (cam == UiLocalization.OptionAll) cam = string.Empty;
                CameraSongScriptConfig.Instance.TargetCameras = cam ?? string.Empty;
            }
        }

        #endregion
    }
}
