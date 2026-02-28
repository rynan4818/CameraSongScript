using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
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
    internal class CameraSongScriptSettingsView : BSMLAutomaticViewController, IInitializable, IDisposable
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

        [UIValue("use-audio-sync")]
        public bool UseAudioSync
        {
            get => CameraSongScriptConfig.Instance.UseAudioSync;
            set => CameraSongScriptConfig.Instance.UseAudioSync = value;
        }

        [UIValue("target-cameras")]
        public string TargetCameras
        {
            get => CameraSongScriptConfig.Instance.TargetCameras;
            set => CameraSongScriptConfig.Instance.TargetCameras = value;
        }

        [UIValue("available-cameras")]
        public string AvailableCameras
        {
            get
            {
                if (!CameraModDetector.IsCamera2 || !Plugin.IsCamHelperReady)
                    return "";

                try
                {
                    var cams = Plugin.CamHelper.GetAvailableCameras()?.ToList() ?? new List<string>();
                    if (cams.Count == 0)
                        return "No cameras available";
                    return string.Join(", ", cams);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to get available cameras: {ex.Message}");
                    return "Camera2 not ready";
                }
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
                if (!Plugin.IsCamPlusHelperReady)
                    return "(Default)";
                string profile = Plugin.CamPlusHelper.GetSongSpecificScriptProfile();
                return string.IsNullOrEmpty(profile) ? "(Default)" : profile;
            }
            set
            {
                string profile = value as string;
                if (profile == "(Default)") profile = string.Empty;
                Plugin.CamPlusHelper?.SetSongSpecificScriptProfile(profile ?? string.Empty);
            }
        }

        [UIValue("cameraplus-use-audio-sync")]
        public bool CameraPlusUseAudioSync
        {
            get => Plugin.CamPlusHelper?.GetUseAudioSync() ?? true;
            set => Plugin.CamPlusHelper?.SetUseAudioSync(value);
        }

        [UIValue("song-specific-script-cameras")]
        public string SongSpecificScriptCameras
        {
            get => Plugin.CamPlusHelper?.GetSongSpecificScriptCameras() ?? string.Empty;
            set
            {
                Plugin.CamPlusHelper?.SetSongSpecificScriptCameras(value ?? string.Empty);
                NotifyPropertyChanged(nameof(SongSpecificScriptStatus));
            }
        }

        [UIValue("song-specific-script-status")]
        public string SongSpecificScriptStatus
        {
            get
            {
                if (!Plugin.IsCamPlusHelperReady)
                    return "CameraPlus not ready";

                try
                {
                    return Plugin.CamPlusHelper.GetSongSpecificScriptStatus();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to get CameraPlus status: {ex.Message}");
                    return "CameraPlus not ready";
                }
            }
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
