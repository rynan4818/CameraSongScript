using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Models;
using UnityEngine;
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

        [Inject]
        private CameraSongScriptStatusView _statusView = null;

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
            CameraSongScriptDetector.ScanCompleted += OnScanCompleted;
        }

        public void Dispose()
        {
            CameraSongScriptDetector.ScanCompleted -= OnScanCompleted;
            GameplaySetup.instance?.RemoveTab(TabName);
        }

        /// <summary>
        /// スキャン完了時コールバック（バックグラウンドスレッドから呼ばれるためメインスレッドへディスパッチ）
        /// </summary>
        private void OnScanCompleted()
        {
            HMMainThreadDispatcher.instance?.Enqueue(() =>
            {
                try
                {
                    if (scriptFileDropdown != null)
                    {
                        scriptFileDropdown.values = ScriptFileOptions;
                        scriptFileDropdown.UpdateChoices();
                        scriptFileDropdown.ReceiveValue();
                    }
                    
                    NotifyPropertyChanged(nameof(ScriptFileOptions));
                    NotifyPropertyChanged(nameof(SelectedScriptFile));
                    NotifyPropertyChanged(nameof(SongScriptStatus));
                    
                    NotifyPropertyChanged(nameof(HasMetadata));
                    NotifyPropertyChanged(nameof(MetaAuthor));
                    NotifyPropertyChanged(nameof(MetaSong));
                    NotifyPropertyChanged(nameof(MetaMapper));
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to update script file UI: {ex.Message}");
                }
            });
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

        [UIComponent("script-file-dropdown")]
        public DropDownListSetting scriptFileDropdown;

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
                {
                    var specificSettings = SongSettingsManager.GetCurrentSettings();
                    if (specificSettings != null && !string.IsNullOrEmpty(specificSettings.SelectedScriptFileName))
                    {
                        // 譜面個別設定があれば、それに合わせて内部選択も更新しておく
                        if (CameraSongScriptDetector.AvailableScriptFiles.Contains(specificSettings.SelectedScriptFileName) && 
                            Path.GetFileName(CameraSongScriptDetector.SelectedScriptPath) != specificSettings.SelectedScriptFileName)
                        {
                            CameraSongScriptDetector.UpdateSelectedScript(specificSettings.SelectedScriptFileName);
                        }
                    }
                    return Path.GetFileName(CameraSongScriptDetector.SelectedScriptPath);
                }
                return "(none)";
            }
            set
            {
                string fileName = value as string;
                if (!string.IsNullOrEmpty(fileName) && fileName != "(none)")
                {
                    CameraSongScriptDetector.UpdateSelectedScript(fileName);
                    SongSettingsManager.UpdateCurrentScriptFileName(fileName);

                    NotifyPropertyChanged(nameof(SongScriptStatus));
                    NotifyPropertyChanged(nameof(HasMetadata));
                    NotifyPropertyChanged(nameof(MetaAuthor));
                    NotifyPropertyChanged(nameof(MetaSong));
                    NotifyPropertyChanged(nameof(MetaMapper));
                }
            }
        }

        #endregion

        #region 高さオフセット

        [UIValue("camera-height-offset")]
        public int CameraHeightOffset
        {
            get
            {
                var specificSettings = SongSettingsManager.GetCurrentSettings();
                if (specificSettings != null && specificSettings.CameraHeightOffsetCm.HasValue)
                {
                    // 譜面個別設定が優先
                    return specificSettings.CameraHeightOffsetCm.Value;
                }
                return CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            }
            set
            {
                // UI表示用の値の取得ロジック（上のget）と同じロジックで現在の値を判定
                int currentValue = CameraHeightOffset;
                
                if (currentValue != value)
                {
                    // 変更された場合は、個別設定として保存するとともに、グローバル設定にも反映して全体挙動を同期させる
                    SongSettingsManager.UpdateCurrentHeightOffset(value);
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = value;

                    // UIでオフセットが変更されたら直ちに一時ファイルを再生成し、CameraPlusに適用する
                    if (CameraSongScriptDetector.HasSongScript)
                    {
                        CameraSongScriptDetector.UpdateEffectiveScriptPath();
                        CameraSongScriptDetector.SyncCameraPlusPath();
                    }
                }
            }
        }

        #endregion

        #region メタデータ表示

        [UIValue("has-metadata")]
        public bool HasMetadata => CameraSongScriptDetector.CurrentMetadata != null;

        [UIValue("meta-author")]
        public string MetaAuthor
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                if (meta == null) return string.Empty;
                return string.IsNullOrEmpty(meta.cameraScriptAuthorName) ? "--" : meta.cameraScriptAuthorName;
            }
        }

        [UIValue("meta-song")]
        public string MetaSong
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                if (meta == null) return string.Empty;
                string song = meta.songName ?? "";
                string sub = meta.songSubName ?? "";
                return string.IsNullOrEmpty(sub) ? song : $"{song} {sub}";
            }
        }

        [UIValue("meta-mapper")]
        public string MetaMapper
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                if (meta == null) return string.Empty;
                return string.IsNullOrEmpty(meta.levelAuthorName) ? "--" : meta.levelAuthorName;
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

        #region ステータスパネル設定

        [UIValue("show-status-panel")]
        public bool ShowStatusPanel
        {
            get => CameraSongScriptConfig.Instance.ShowStatusPanel;
            set
            {
                CameraSongScriptConfig.Instance.ShowStatusPanel = value;
                _statusView?.SetVisible(value);
            }
        }

        [UIValue("status-panel-position-options")]
        public List<object> StatusPanelPositionOptions
        {
            get
            {
                var list = new List<object>();
                foreach (var name in CameraSongScriptStatusView.GetPresetNames())
                    list.Add(name);
                return list;
            }
        }

        [UIValue("status-panel-position")]
        public object StatusPanelPosition
        {
            get
            {
                var names = CameraSongScriptStatusView.GetPresetNames();
                int idx = CameraSongScriptConfig.Instance.StatusPanelPosition;
                if (idx >= 0 && idx < names.Length)
                    return names[idx];
                return names[0];
            }
            set
            {
                string selected = value as string;
                var names = CameraSongScriptStatusView.GetPresetNames();
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == selected)
                    {
                        CameraSongScriptConfig.Instance.StatusPanelPosition = i;
                        _statusView?.SetPosition(i);
                        break;
                    }
                }
            }
        }

        #endregion
    }
}
