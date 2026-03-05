using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using CameraSongScript.Configuration;
using CameraSongScript.HarmonyPatches;
using Zenject;

namespace CameraSongScript.UI
{
    /// <summary>
    /// BSML設定UI
    /// MenuInstallerでバインドされ、メニューシーンで表示
    /// Camera2/CameraPlus両対応
    /// </summary>
    [HotReload]
    public class CameraSongScriptSettingsView : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        public const string TabName = "CameraSongScript";
        public string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
        }

        public void Dispose()
        {
            GameplaySetup.instance?.RemoveTab(TabName);
        }

        #region 検出カメラMod表示

        [UIValue("detected-camera-mod")]
        public string DetectedCameraMod
        {
            get
            {
                switch (CameraModDetector.DetectedMod)
                {
                    case CameraModType.Camera2:
                        return "<color=#00FF00>Camera2</color>";
                    case CameraModType.CameraPlus:
                        return "<color=#00FF00>CameraPlus</color>";
                    default:
                        return "<color=#FF0000>None</color>";
                }
            }
        }

        #endregion

        #region SongScript有効/無効

        [UIValue("enabled")]
        public bool Enabled
        {
            get => CameraSongScriptConfig.Instance.Enabled;
            set => CameraSongScriptConfig.Instance.Enabled = value;
        }

        #endregion

        #region スクリプトファイル選択

        [UIValue("script-file-options")]
        public List<object> ScriptFileOptions
        {
            get
            {
                var list = new List<object>();
                foreach (var fileName in CameraSongScriptDetector.AvailableScriptFiles)
                {
                    list.Add(fileName);
                }
                if (list.Count == 0)
                    list.Add("(none)");
                return list;
            }
        }

        [UIValue("selected-script-file")]
        public object SelectedScriptFile
        {
            get
            {
                if (CameraSongScriptDetector.HasSongScript)
                    return Path.GetFileName(CameraSongScriptDetector.SelectedScriptPath);
                return "(none)";
            }
            set
            {
                string fileName = value as string;
                if (!string.IsNullOrEmpty(fileName) && fileName != "(none)")
                {
                    CameraSongScriptDetector.UpdateSelectedScript(fileName);
                    NotifyPropertyChanged(nameof(SongScriptStatus));
                }
            }
        }

        #endregion

        #region Camera2専用設定

        [UIValue("show-camera2-settings")]
        public bool ShowCamera2Settings => CameraModDetector.IsCamera2;

        [UIValue("custom-scene-options")]
        public List<object> CustomSceneOptions
        {
            get
            {
                var list = new List<object> { "(Default)" };
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
                return list;
            }
        }

        [UIComponent("custom-scene-dropdown")]
        public DropDownListSetting customSceneDropdown;

        private string _selectedCustomScene = CameraSongScriptConfig.Instance.CustomSceneToSwitch ?? "(Default)";

        [UIValue("selected-custom-scene")]
        public object SelectedCustomScene
        {
            get => _selectedCustomScene;
            set
            {
                string scene = value as string;
                _selectedCustomScene = scene ?? "(Default)";
                
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
                if (string.IsNullOrEmpty(targetCam) || targetCam == "(All)")
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
                customSceneDropdown?.UpdateChoices();

                // 既にCameraSongScriptが選択されている場合は、直ちに画面にも反映させる
                if (_selectedCustomScene == "CameraSongScript")
                {
                    Plugin.CamHelper.SwitchToCustomScene("CameraSongScript");
                }
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
                var list = new List<object> { "(All)" };
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
                return list;
            }
        }

        [UIValue("target-cameras")]
        public object TargetCameras
        {
            get
            {
                string cam = CameraSongScriptConfig.Instance.TargetCameras;
                return string.IsNullOrEmpty(cam) ? "(All)" : cam;
            }
            set
            {
                string cam = value as string;
                if (cam == "(All)") cam = string.Empty;
                CameraSongScriptConfig.Instance.TargetCameras = cam ?? string.Empty;
            }
        }

        #endregion

        #region CameraPlus専用設定

        [UIValue("show-cameraplus-settings")]
        public bool ShowCameraPlusSettings => CameraModDetector.IsCameraPlus;

        [UIValue("profile-options")]
        public List<object> ProfileOptions
        {
            get
            {
                var list = new List<object>();
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
                    {
                        list.Add(string.IsNullOrEmpty(profile) ? "(Default)" : profile);
                    }
                }
                if (list.Count == 0)
                    list.Add("(Default)");
                return list;
            }
        }

        [UIValue("song-specific-profile")]
        public object SongSpecificProfile
        {
            get
            {
                if (!Plugin.IsCamPlusHelperReady) return "(Default)";
                string profile = Plugin.CamPlusHelper.GetSongSpecificScriptProfile();
                return string.IsNullOrEmpty(profile) ? "(Default)" : profile;
            }
            set
            {
                if (!Plugin.IsCamPlusHelperReady) return;
                string profile = value as string;
                Plugin.CamPlusHelper.SetSongSpecificScriptProfile(profile == "(Default)" ? string.Empty : profile);
            }
        }

        [UIValue("cameraplus-use-audio-sync")]
        public bool CameraPlusUseAudioSync
        {
            get => Plugin.CamPlusHelper?.GetUseAudioSync() ?? true;
            set => Plugin.CamPlusHelper?.SetUseAudioSync(value);
        }

        #endregion

        #region SongScript検出状態

        [UIValue("song-script-status")]
        public string SongScriptStatus
        {
            get
            {
                int count = CameraSongScriptDetector.AvailableScriptFiles.Count;
                if (count > 0)
                {
                    string selected = CameraSongScriptDetector.HasSongScript
                        ? Path.GetFileName(CameraSongScriptDetector.SelectedScriptPath)
                        : "?";
                    return $"<color=#00FF00>{count} script(s) found - {selected}</color>";
                }
                else
                {
                    return "<color=#888888>No camera scripts</color>";
                }
            }
        }

        #endregion
    }
}
