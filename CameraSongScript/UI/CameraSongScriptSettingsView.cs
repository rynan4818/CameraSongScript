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
using CameraSongScript.Localization;
using UnityEngine;
using Zenject;
using UnityEngine.UI;

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

        private bool _needsRefresh = false;

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

        protected void OnEnable()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                RefreshLayout();
            }
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
                    // メインスレッド上でプロファイル名を再解決し、CameraPlusに同期する
                    // バックグラウンドスキャン完了時のSyncCameraPlusPath()は_currentSongKeyが
                    // 変わっている可能性があったため、ここで正しいキーで再解決する
                    CameraSongScriptDetector.ResolveProfileName();
                    CameraSongScriptDetector.SyncCameraPlusPath();

                    // プロファイルドロップダウンのUIを現在の曲の設定に更新する
                    NotifyPropertyChanged(nameof(SongSpecificProfile));

                    if (scriptFileDropdown != null)
                    {
                        scriptFileDropdown.values = ScriptFileOptions;
                        scriptFileDropdown.UpdateChoices();
                        scriptFileDropdown.ReceiveValue();
                    }
                    if (cameraHeightOffsetSlider != null)
                    {
                        cameraHeightOffsetSlider.ReceiveValue();
                    }

                    NotifyPropertyChanged(nameof(ScriptFileOptions));
                    NotifyPropertyChanged(nameof(SelectedScriptFile));
                    NotifyPropertyChanged(nameof(SongScriptStatus));

                    NotifyPropertyChanged(nameof(HasMetadata));
                    NotifyPropertyChanged(nameof(MetaAuthor));
                    NotifyPropertyChanged(nameof(MetaSong));
                    NotifyPropertyChanged(nameof(MetaMapper));
                    NotifyPropertyChanged(nameof(HasMetaHeight));
                    NotifyPropertyChanged(nameof(MetaHeight));
                    NotifyPropertyChanged(nameof(HasMetaDescription));
                    NotifyPropertyChanged(nameof(MetaDescription));
                    NotifyPropertyChanged(nameof(CameraHeightOffset));
                    NotifyPropertyChanged(nameof(IsOffsetInteractable));

                    RefreshLayout();
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
            set
            {
                CameraSongScriptConfig.Instance.Enabled = value;
                CameraSongScriptDetector.ReevaluateCommonScriptUsage();
                CameraSongScriptDetector.SyncCameraPlusPath();
                NotifyPropertyChanged(nameof(SongScriptStatus));
                NotifyPropertyChanged(nameof(IsOffsetInteractable));
                NotifyPropertyChanged(nameof(CameraHeightOffset));
                if (cameraHeightOffsetSlider != null) cameraHeightOffsetSlider.ReceiveValue();
                RefreshLayout();
                _statusView?.UpdateContent();
            }
        }

        #endregion

        #region スクリプトファイル選択

        [UIComponent("script-file-dropdown")]
        public DropDownListSetting scriptFileDropdown;

        [UIComponent("camera-height-offset")]
        public SliderSetting cameraHeightOffsetSlider;

        [UIComponent("settings-container")]
        public RectTransform settingsContainer;

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
                            CameraSongScriptDetector.SelectedScriptDisplayName != specificSettings.SelectedScriptFileName)
                        {
                            CameraSongScriptDetector.UpdateSelectedScript(specificSettings.SelectedScriptFileName);
                        }
                    }
                    return CameraSongScriptDetector.SelectedScriptDisplayName;
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
                    NotifyPropertyChanged(nameof(HasMetaHeight));
                    NotifyPropertyChanged(nameof(MetaHeight));
                    NotifyPropertyChanged(nameof(HasMetaDescription));
                    NotifyPropertyChanged(nameof(MetaDescription));
                    NotifyPropertyChanged(nameof(CameraHeightOffset));

                    RefreshLayout();
                    
                    if (cameraHeightOffsetSlider != null)
                    {
                        cameraHeightOffsetSlider.ReceiveValue();
                    }

                    _statusView?.UpdateContent();
                }
            }
        }

        private void RefreshLayout()
        {
            if (gameObject.activeInHierarchy)
            {
                if (isActiveAndEnabled)
                {
                    StartCoroutine(RefreshLayoutCoroutine());
                }
            }
            else
            {
                _needsRefresh = true;
            }
        }

        private System.Collections.IEnumerator RefreshLayoutCoroutine()
        {
            // BSMLの要素が有効/無効になった直後だと、Unityが推奨サイズ(preferredHeight)を
            // 正しく計算できていない場合があるため、複数フレーム待機する
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;

            if (settingsContainer != null)
            {
                // UI全体の更新状態を同期
                Canvas.ForceUpdateCanvases();

                // 自己のレイアウトを更新
                LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContainer);
                
                // 親（ContentSizeFitterを持っている可能性がある要素）のレイアウトも更新
                if (settingsContainer.parent is RectTransform parentTransform)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform);
                }
                
                // スクロールビュー全体の更新
                var scrollView = settingsContainer.GetComponentInParent<HMUI.ScrollView>();
                if (scrollView != null)
                {
                    scrollView.SetContentSize(settingsContainer.rect.height);
                    scrollView.RefreshButtons();
                }
            }
        }

        #endregion

        #region 高さオフセット

        /// <summary>
        /// 汎用スクリプトがランダム選択中かどうか（オフセットUIの有効/無効判定用）
        /// </summary>
        private bool IsCommonRandom =>
            CameraSongScriptDetector.IsUsingCommonScript &&
            CameraSongScriptConfig.Instance.SelectedCommonScript == "(Random)";

        /// <summary>
        /// オフセットスライダーとリセットボタンの操作可否
        /// ランダム汎用スクリプト時は操作不可
        /// </summary>
        [UIValue("is-offset-interactable")]
        public bool IsOffsetInteractable => !IsCommonRandom;

        [UIValue("camera-height-offset")]
        public int CameraHeightOffset
        {
            get
            {
                // ランダム汎用スクリプト時は常に0を返す（UI上で動かないようにする）
                if (IsCommonRandom) return 0;
                return CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            }
            set
            {
                // ランダム汎用スクリプト時は何もしない
                if (IsCommonRandom) return;

                int currentValue = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;

                if (currentValue != value)
                {
                    // 個別保存モードの場合: スクリプトハッシュ別に保存する
                    if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
                    {
                        if (CameraSongScriptDetector.IsUsingCommonScript)
                        {
                            // 汎用スクリプト非ランダム: ResolvedCommonScriptPathのハッシュで保存
                            string commonPath = CameraSongScriptDetector.ResolvedCommonScriptPath;
                            if (!string.IsNullOrEmpty(commonPath))
                            {
                                ScriptOffsetManager.UpdateOffsetForScript(commonPath, value);
                            }
                        }
                        else if (CameraSongScriptDetector.HasSongScript)
                        {
                            // 通常スクリプト: SelectedScriptPathのハッシュで保存
                            ScriptOffsetManager.UpdateOffsetForScript(CameraSongScriptDetector.SelectedScriptPath, value);
                        }
                    }
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = value;

                    if (CameraSongScriptDetector.HasSongScript)
                    {
                        CameraSongScriptDetector.UpdateEffectiveScriptPath();
                        CameraSongScriptDetector.SyncCameraPlusPath();
                    }
                    else if (CameraSongScriptDetector.IsUsingCommonScript)
                    {
                        // 汎用スクリプト: CameraPlusモードの場合はパス再同期
                        CameraSongScriptDetector.SyncCameraPlusPath();
                    }
                    _statusView?.UpdateContent();
                }
            }
        }

        [UIAction("reset-camera-height")]
        private void ResetCameraHeight()
        {
            if (IsCommonRandom) return;

            CameraHeightOffset = 0;
            NotifyPropertyChanged(nameof(CameraHeightOffset));

            if (cameraHeightOffsetSlider != null)
            {
                cameraHeightOffsetSlider.ReceiveValue();
            }
            _statusView?.UpdateContent();
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

        [UIValue("has-meta-height")]
        public bool HasMetaHeight
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                return meta != null && meta.avatarHeight > 0;
            }
        }

        [UIValue("meta-height")]
        public string MetaHeight
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                if (meta == null || meta.avatarHeight <= 0) return string.Empty;
                return $"{meta.avatarHeight:0.#} cm";
            }
        }

        [UIValue("has-meta-description")]
        public bool HasMetaDescription
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                return meta != null && !string.IsNullOrEmpty(meta.description);
            }
        }

        [UIValue("meta-description")]
        public string MetaDescription
        {
            get
            {
                var meta = CameraSongScriptDetector.CurrentMetadata;
                if (meta == null || string.IsNullOrEmpty(meta.description)) return string.Empty;
                return meta.description;
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
                var list = new List<object> { "(NoChange)", "(Delete)" };
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
                    {
                        if (!string.IsNullOrEmpty(profile))
                            list.Add(profile);
                    }
                }
                return list;
            }
        }

        [UIValue("song-specific-profile")]
        public object SongSpecificProfile
        {
            get
            {
                if (!Plugin.IsCamPlusHelperReady) return "(NoChange)";
                string profile = CameraSongScriptDetector.ResolvedProfileName;
                if (string.IsNullOrEmpty(profile)) return "(NoChange)";
                return profile;
            }
            set
            {
                if (!Plugin.IsCamPlusHelperReady) return;
                string profile = value as string ?? "(NoChange)";

                // グローバル設定として保存
                CameraSongScriptConfig.Instance.SongScriptProfile = profile;

                // 解決済みプロファイル名を直接更新
                CameraSongScriptDetector.SetResolvedProfileName(profile);

                // 即座に全体のパス・プロファイル状態を同期
                CameraSongScriptDetector.SyncCameraPlusPath();
            }
        }


        #endregion

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
            }
        }

        [UIComponent("common-script-dropdown")]
        public DropDownListSetting commonScriptDropdown;

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
                        list.Add(name);
                    }
                }
                if (list.Count == 0)
                    list.Add("(none)");
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
                        return selected;
                }
                return "(Random)";
            }
            set
            {
                string name = value as string;
                if (!string.IsNullOrEmpty(name) && name != "(none)")
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
                }
            }
        }

        // --- Camera2用: 汎用スクリプト専用設定 ---

        [UIValue("common-target-camera-options")]
        public List<object> CommonTargetCameraOptions
        {
            get
            {
                var list = new List<object> { "(Same as SongScript)" };
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
                return list;
            }
        }

        [UIValue("common-target-camera")]
        public object CommonTargetCamera
        {
            get
            {
                string cam = CameraSongScriptConfig.Instance.CommonScriptTargetCamera;
                return string.IsNullOrEmpty(cam) ? "(Same as SongScript)" : cam;
            }
            set
            {
                string cam = value as string;
                CameraSongScriptConfig.Instance.CommonScriptTargetCamera =
                    (cam == "(Same as SongScript)") ? string.Empty : (cam ?? string.Empty);
                _statusView?.UpdateContent();
            }
        }

        [UIValue("common-custom-scene-options")]
        public List<object> CommonCustomSceneOptions
        {
            get
            {
                var list = new List<object> { "(Same as SongScript)" };
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
                return list;
            }
        }

        [UIValue("common-custom-scene")]
        public object CommonCustomScene
        {
            get
            {
                string scene = CameraSongScriptConfig.Instance.CommonScriptCustomScene;
                return string.IsNullOrEmpty(scene) ? "(Same as SongScript)" : scene;
            }
            set
            {
                string scene = value as string;
                CameraSongScriptConfig.Instance.CommonScriptCustomScene =
                    (scene == "(Same as SongScript)") ? string.Empty : (scene ?? string.Empty);
                _statusView?.UpdateContent();
            }
        }

        // --- CameraPlus用: 汎用スクリプト専用設定 ---

        [UIValue("common-profile-options")]
        public List<object> CommonProfileOptions
        {
            get
            {
                var list = new List<object> { "(Same as SongScript)", "(NoChange)", "(Delete)" };
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
                    {
                        if (!string.IsNullOrEmpty(profile))
                            list.Add(profile);
                    }
                }
                return list;
            }
        }

        [UIValue("common-profile")]
        public object CommonProfile
        {
            get
            {
                string profile = CameraSongScriptConfig.Instance.CommonScriptProfile;
                if (string.IsNullOrEmpty(profile)) return "(Same as SongScript)";
                // 旧バージョンの "(Default)" 設定を "(Delete)" に変換
                if (profile == "(Default)") return "(Delete)";
                return profile;
            }
            set
            {
                string profile = value as string;
                if (profile == "(Same as SongScript)") profile = string.Empty;
                CameraSongScriptConfig.Instance.CommonScriptProfile = profile ?? string.Empty;
                CameraSongScriptDetector.SyncCameraPlusPath();
                _statusView?.UpdateContent();
            }
        }

        #endregion

        #region SongScript検出状態

        private static string AppendCamera2UnsupportedWarning(string statusText)
        {
            if (!CameraModDetector.IsCamera2 || !CameraSongScriptDetector.HasCurrentUnsupportedFeatures)
                return statusText;

            string warningText = $"<color=#FF5555>Warning: Unsupported in Camera2 - {CameraSongScriptDetector.CurrentUnsupportedFeatureSummary}</color>";
            return string.IsNullOrEmpty(statusText) ? warningText : $"{statusText}\n{warningText}";
        }

        [UIValue("song-script-status")]
        public string SongScriptStatus
        {
            get
            {
                int count = CameraSongScriptDetector.AvailableScriptFiles.Count;
                bool isCommon = CameraSongScriptDetector.IsUsingCommonScript;

                if (isCommon)
                {
                    string commonName = CameraSongScriptConfig.Instance.SelectedCommonScript;
                    if (count > 0)
                        return AppendCamera2UnsupportedWarning($"<color=#FFAA00>Common Script: {commonName}</color> <color=#AAAAAA>({count} SongScript available)</color>");
                    else
                        return AppendCamera2UnsupportedWarning($"<color=#FFAA00>Common Script: {commonName}</color>");
                }

                if (count > 0)
                {
                    string selected = CameraSongScriptDetector.HasSongScript
                        ? CameraSongScriptDetector.SelectedScriptDisplayName
                        : "?";
                    return AppendCamera2UnsupportedWarning($"<color=#00FF00>{count} script(s) found - {selected}</color>");
                }
                else
                {
                    return AppendCamera2UnsupportedWarning("<color=#888888>No camera scripts</color>");
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

        #region hover-hintローカライズ

        [UIValue("hint-enabled")]
        public string HintEnabled => HoverHintLocalization.Get("hint-enabled");

        [UIValue("hint-script-file")]
        public string HintScriptFile => HoverHintLocalization.Get("hint-script-file");

        [UIValue("hint-height-offset")]
        public string HintHeightOffset => HoverHintLocalization.Get("hint-height-offset");

        [UIValue("hint-height-reset")]
        public string HintHeightReset => HoverHintLocalization.Get("hint-height-reset");

        [UIValue("hint-audio-sync")]
        public string HintAudioSync => HoverHintLocalization.Get("hint-audio-sync");

        [UIValue("hint-target-camera")]
        public string HintTargetCamera => HoverHintLocalization.Get("hint-target-camera");

        [UIValue("hint-custom-scene")]
        public string HintCustomScene => HoverHintLocalization.Get("hint-custom-scene");

        [UIValue("hint-add-custom-scene")]
        public string HintAddCustomScene => HoverHintLocalization.Get("hint-add-custom-scene");

        [UIValue("hint-script-profile")]
        public string HintScriptProfile => HoverHintLocalization.Get("hint-script-profile");


        [UIValue("hint-show-status-panel")]
        public string HintShowStatusPanel => HoverHintLocalization.Get("hint-show-status-panel");

        [UIValue("hint-panel-position")]
        public string HintPanelPosition => HoverHintLocalization.Get("hint-panel-position");

        [UIValue("hint-common-fallback")]
        public string HintCommonFallback => HoverHintLocalization.Get("hint-common-fallback");

        [UIValue("hint-force-common")]
        public string HintForceCommon => HoverHintLocalization.Get("hint-force-common");

        [UIValue("hint-common-script-file")]
        public string HintCommonScriptFile => HoverHintLocalization.Get("hint-common-script-file");

        [UIValue("hint-common-target-camera")]
        public string HintCommonTargetCamera => HoverHintLocalization.Get("hint-common-target-camera");

        [UIValue("hint-common-custom-scene")]
        public string HintCommonCustomScene => HoverHintLocalization.Get("hint-common-custom-scene");

        [UIValue("hint-common-profile")]
        public string HintCommonProfile => HoverHintLocalization.Get("hint-common-profile");

        #endregion
    }
}
