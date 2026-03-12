using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Inject]
        private CameraSongScriptPreviewController _previewController = null;

        private const float PreviewSliderStep = 0.05f;
        private bool _needsRefresh = false;
        private bool _suppressPreviewSeek = false;
        private float _lastPreviewUiTime = float.NegativeInfinity;
        private bool _lastPreviewVisible = false;
        private bool _lastPreviewPlaying = false;
        private int _lastPreviewSpeed = 1;

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
            CameraSongScriptDetector.ScanCompleted += OnScanCompleted;
            if (_previewController != null)
                _previewController.StateChanged += OnPreviewStateChanged;
            UiLocalization.LanguageChanged += OnLanguageChanged;
        }

        public void Dispose()
        {
            CameraSongScriptDetector.ScanCompleted -= OnScanCompleted;
            if (_previewController != null)
                _previewController.StateChanged -= OnPreviewStateChanged;
            UiLocalization.LanguageChanged -= OnLanguageChanged;
            GameplaySetup.instance?.RemoveTab(TabName);
        }

        protected void OnEnable()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                RefreshLayout();
            }

            RefreshPreviewBindings();
            RefreshLocalizedUi();
        }

        protected void Update()
        {
            var previewController = _previewController;
            if (!isActiveAndEnabled || previewController == null)
                return;

            float currentTime = previewController.CurrentTime;
            bool isVisible = previewController.IsVisible;
            bool isPlaying = previewController.IsPlaying;
            int speedMultiplier = previewController.SpeedMultiplier;
            bool stateChanged =
                isVisible != _lastPreviewVisible ||
                isPlaying != _lastPreviewPlaying ||
                speedMultiplier != _lastPreviewSpeed;

            if (isVisible || Mathf.Abs(currentTime - _lastPreviewUiTime) >= 0.001f || stateChanged)
            {
                _lastPreviewUiTime = currentTime;
                _lastPreviewVisible = isVisible;
                _lastPreviewPlaying = isPlaying;
                _lastPreviewSpeed = speedMultiplier;
                RefreshPreviewRuntimeUi();
            }
        }

        private void OnPreviewStateChanged()
        {
            if (_previewController == null)
                return;

            RefreshPreviewBindings();
        }

        private void OnLanguageChanged()
        {
            HMMainThreadDispatcher.instance?.Enqueue(() =>
            {
                try
                {
                    RefreshLocalizedUi();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to refresh localized UI: {ex.Message}");
                }
            });
        }

        private void RefreshLocalizedUi()
        {
            NotifyPropertyChanged(nameof(LabelCameraMod));
            NotifyPropertyChanged(nameof(ToggleEnabled));
            NotifyPropertyChanged(nameof(LabelScriptFile));
            NotifyPropertyChanged(nameof(LabelMetaCamera));
            NotifyPropertyChanged(nameof(LabelMetaSong));
            NotifyPropertyChanged(nameof(LabelMetaMapper));
            NotifyPropertyChanged(nameof(LabelMetaHeight));
            NotifyPropertyChanged(nameof(LabelHeightOffset));
            NotifyPropertyChanged(nameof(ButtonResetOffset));
            NotifyPropertyChanged(nameof(SectionPreview));
            NotifyPropertyChanged(nameof(ButtonPreviewShowStart));
            NotifyPropertyChanged(nameof(ButtonPreviewStop));
            NotifyPropertyChanged(nameof(ButtonPreviewClear));
            NotifyPropertyChanged(nameof(LabelPreviewPosition));
            NotifyPropertyChanged(nameof(ToggleUseAudioSync));
            NotifyPropertyChanged(nameof(LabelTargetCamera));
            NotifyPropertyChanged(nameof(LabelCustomScene));
            NotifyPropertyChanged(nameof(ButtonAddCustomScene));
            NotifyPropertyChanged(nameof(LabelSongScriptProfile));
            NotifyPropertyChanged(nameof(SectionCommonScript));
            NotifyPropertyChanged(nameof(ToggleFallbackToCommon));
            NotifyPropertyChanged(nameof(ToggleForceCommonScript));
            NotifyPropertyChanged(nameof(LabelCommonScript));
            NotifyPropertyChanged(nameof(LabelCommonTargetCamera));
            NotifyPropertyChanged(nameof(LabelCommonCustomScene));
            NotifyPropertyChanged(nameof(LabelCommonProfile));
            NotifyPropertyChanged(nameof(SectionStatusPanel));
            NotifyPropertyChanged(nameof(ToggleShowStatusPanel));
            NotifyPropertyChanged(nameof(LabelPanelPosition));
            NotifyPropertyChanged(nameof(DetectedCameraMod));
            NotifyPropertyChanged(nameof(ScriptFileOptions));
            NotifyPropertyChanged(nameof(SelectedScriptFile));
            NotifyPropertyChanged(nameof(CustomSceneOptions));
            NotifyPropertyChanged(nameof(SelectedCustomScene));
            NotifyPropertyChanged(nameof(TargetCameraOptions));
            NotifyPropertyChanged(nameof(TargetCameras));
            NotifyPropertyChanged(nameof(ProfileOptions));
            NotifyPropertyChanged(nameof(SongSpecificProfile));
            NotifyPropertyChanged(nameof(CommonScriptOptions));
            NotifyPropertyChanged(nameof(SelectedCommonScript));
            NotifyPropertyChanged(nameof(CommonTargetCameraOptions));
            NotifyPropertyChanged(nameof(CommonTargetCamera));
            NotifyPropertyChanged(nameof(CommonCustomSceneOptions));
            NotifyPropertyChanged(nameof(CommonCustomScene));
            NotifyPropertyChanged(nameof(CommonProfileOptions));
            NotifyPropertyChanged(nameof(CommonProfile));
            NotifyPropertyChanged(nameof(StatusPanelPositionOptions));
            NotifyPropertyChanged(nameof(StatusPanelPosition));
            NotifyPropertyChanged(nameof(SongScriptStatus));
            NotifyPropertyChanged(nameof(PreviewStatus));

            RefreshDropdown(scriptFileDropdown, ScriptFileOptions);
            RefreshDropdown(targetCameraDropdown, TargetCameraOptions);
            RefreshDropdown(customSceneDropdown, CustomSceneOptions);
            RefreshDropdown(songSpecificProfileDropdown, ProfileOptions);
            RefreshDropdown(commonScriptDropdown, CommonScriptOptions);
            RefreshDropdown(commonTargetCameraDropdown, CommonTargetCameraOptions);
            RefreshDropdown(commonCustomSceneDropdown, CommonCustomSceneOptions);
            RefreshDropdown(commonProfileDropdown, CommonProfileOptions);
            RefreshDropdown(statusPanelPositionDropdown, StatusPanelPositionOptions);

            RefreshHoverHintBindings();
            RefreshLayout();
            _statusView?.UpdateContent();
        }

        private static void RefreshDropdown(DropDownListSetting dropdown, List<object> options)
        {
            if (dropdown == null)
                return;

            dropdown.values = options;
            dropdown.UpdateChoices();
            dropdown.ReceiveValue();
        }

        private void RefreshHoverHintBindings()
        {
            NotifyPropertyChanged(nameof(HintEnabled));
            NotifyPropertyChanged(nameof(HintScriptFile));
            NotifyPropertyChanged(nameof(HintHeightOffset));
            NotifyPropertyChanged(nameof(HintHeightReset));
            NotifyPropertyChanged(nameof(HintAudioSync));
            NotifyPropertyChanged(nameof(HintTargetCamera));
            NotifyPropertyChanged(nameof(HintCustomScene));
            NotifyPropertyChanged(nameof(HintAddCustomScene));
            NotifyPropertyChanged(nameof(HintScriptProfile));
            NotifyPropertyChanged(nameof(HintShowStatusPanel));
            NotifyPropertyChanged(nameof(HintPanelPosition));
            NotifyPropertyChanged(nameof(HintCommonFallback));
            NotifyPropertyChanged(nameof(HintForceCommon));
            NotifyPropertyChanged(nameof(HintCommonScriptFile));
            NotifyPropertyChanged(nameof(HintCommonTargetCamera));
            NotifyPropertyChanged(nameof(HintCommonCustomScene));
            NotifyPropertyChanged(nameof(HintCommonProfile));
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
                    HandlePreviewSelectionChanged();

                    RefreshLayout();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to update script file UI: {ex.Message}");
                }
            });
        }

        #region UI文言ローカライズ

        [UIValue("label-camera-mod")]
        public string LabelCameraMod => UiLocalization.Get("label-camera-mod");

        [UIValue("toggle-enabled")]
        public string ToggleEnabled => UiLocalization.Get("toggle-enabled");

        [UIValue("label-script-file")]
        public string LabelScriptFile => UiLocalization.Get("label-script-file");

        [UIValue("label-meta-camera")]
        public string LabelMetaCamera => UiLocalization.Get("label-meta-camera");

        [UIValue("label-meta-song")]
        public string LabelMetaSong => UiLocalization.Get("label-meta-song");

        [UIValue("label-meta-mapper")]
        public string LabelMetaMapper => UiLocalization.Get("label-meta-mapper");

        [UIValue("label-meta-height")]
        public string LabelMetaHeight => UiLocalization.Get("label-meta-height");

        [UIValue("label-height-offset")]
        public string LabelHeightOffset => UiLocalization.Get("label-height-offset");

        [UIValue("button-reset-offset")]
        public string ButtonResetOffset => UiLocalization.Get("button-reset-offset");

        [UIValue("section-preview")]
        public string SectionPreview => UiLocalization.Get("section-preview");

        [UIValue("button-preview-show-start")]
        public string ButtonPreviewShowStart => UiLocalization.Get("button-preview-show-start");

        [UIValue("button-preview-stop")]
        public string ButtonPreviewStop => UiLocalization.Get("button-preview-stop");

        [UIValue("button-preview-clear")]
        public string ButtonPreviewClear => UiLocalization.Get("button-preview-clear");

        [UIValue("label-preview-position")]
        public string LabelPreviewPosition => UiLocalization.Get("label-preview-position");

        [UIValue("toggle-use-audio-sync")]
        public string ToggleUseAudioSync => UiLocalization.Get("toggle-use-audio-sync");

        [UIValue("label-target-camera")]
        public string LabelTargetCamera => UiLocalization.Get("label-target-camera");

        [UIValue("label-custom-scene")]
        public string LabelCustomScene => UiLocalization.Get("label-custom-scene");

        [UIValue("button-add-custom-scene")]
        public string ButtonAddCustomScene => UiLocalization.Get("button-add-custom-scene");

        [UIValue("label-songscript-profile")]
        public string LabelSongScriptProfile => UiLocalization.Get("label-songscript-profile");

        [UIValue("section-common-script")]
        public string SectionCommonScript => UiLocalization.Get("section-common-script");

        [UIValue("toggle-fallback-to-common")]
        public string ToggleFallbackToCommon => UiLocalization.Get("toggle-fallback-to-common");

        [UIValue("toggle-force-common-script")]
        public string ToggleForceCommonScript => UiLocalization.Get("toggle-force-common-script");

        [UIValue("label-common-script")]
        public string LabelCommonScript => UiLocalization.Get("label-common-script");

        [UIValue("label-common-target-camera")]
        public string LabelCommonTargetCamera => UiLocalization.Get("label-common-target-camera");

        [UIValue("label-common-custom-scene")]
        public string LabelCommonCustomScene => UiLocalization.Get("label-common-custom-scene");

        [UIValue("label-common-profile")]
        public string LabelCommonProfile => UiLocalization.Get("label-common-profile");

        [UIValue("section-status-panel")]
        public string SectionStatusPanel => UiLocalization.Get("section-status-panel");

        [UIValue("toggle-show-status-panel")]
        public string ToggleShowStatusPanel => UiLocalization.Get("toggle-show-status-panel");

        [UIValue("label-panel-position")]
        public string LabelPanelPosition => UiLocalization.Get("label-panel-position");

        #endregion

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
                        return string.Format(
                            CultureInfo.InvariantCulture,
                            "<color=#FF0000>{0}</color>",
                            UiLocalization.Get("detected-none"));
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
                HandlePreviewSelectionChanged();
            }
        }

        #endregion

        #region スクリプトファイル選択

        [UIComponent("script-file-dropdown")]
        public DropDownListSetting scriptFileDropdown;

        [UIComponent("target-camera-dropdown")]
        public DropDownListSetting targetCameraDropdown;

        [UIComponent("camera-height-offset")]
        public SliderSetting cameraHeightOffsetSlider;

        [UIComponent("settings-container")]
        public RectTransform settingsContainer;

        [UIComponent("preview-position-slider")]
        public SliderSetting previewPositionSlider;

        [UIComponent("song-specific-profile-dropdown")]
        public DropDownListSetting songSpecificProfileDropdown;

        [UIComponent("common-target-camera-dropdown")]
        public DropDownListSetting commonTargetCameraDropdown;

        [UIComponent("common-custom-scene-dropdown")]
        public DropDownListSetting commonCustomSceneDropdown;

        [UIComponent("common-profile-dropdown")]
        public DropDownListSetting commonProfileDropdown;

        [UIComponent("status-panel-position-dropdown")]
        public DropDownListSetting statusPanelPositionDropdown;

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
                    list.Add(UiLocalization.GetOptionDisplay(UiLocalization.OptionNone, UiLocalization.OptionNone));
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
                return UiLocalization.GetOptionDisplay(UiLocalization.OptionNone, UiLocalization.OptionNone);
            }
            set
            {
                string fileName = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNone);
                if (!string.IsNullOrEmpty(fileName) && fileName != UiLocalization.OptionNone)
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
                    HandlePreviewSelectionChanged();
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
            CameraSongScriptConfig.Instance.SelectedCommonScript == UiLocalization.OptionRandom;

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
                    HandlePreviewVisualChanged();
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
            HandlePreviewVisualChanged();
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

        #region CameraPlus専用設定

        [UIValue("show-cameraplus-settings")]
        public bool ShowCameraPlusSettings => CameraModDetector.IsCameraPlus;

        [UIValue("profile-options")]
        public List<object> ProfileOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionNoChange, UiLocalization.OptionDelete };
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
                    {
                        if (!string.IsNullOrEmpty(profile))
                            list.Add(profile);
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
            }
        }

        [UIValue("song-specific-profile")]
        public object SongSpecificProfile
        {
            get
            {
                if (!Plugin.IsCamPlusHelperReady) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                string profile = CameraSongScriptDetector.ResolvedProfileName;
                if (string.IsNullOrEmpty(profile)) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                return UiLocalization.GetOptionDisplay(profile, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
            }
            set
            {
                if (!Plugin.IsCamPlusHelperReady) return;
                string profile = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
                if (string.IsNullOrEmpty(profile))
                    profile = UiLocalization.OptionNoChange;

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

        #region SongScript検出状態

        private static string AppendCamera2UnsupportedWarning(string statusText)
        {
            if (!CameraModDetector.IsCamera2 || !CameraSongScriptDetector.HasCurrentUnsupportedFeatures)
                return statusText;

            string warningText = UiLocalization.Format(
                "warning-camera2-unsupported",
                CameraSongScriptDetector.CurrentUnsupportedFeatureSummary);
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
                    string commonName = UiLocalization.GetOptionDisplay(
                        CameraSongScriptConfig.Instance.SelectedCommonScript,
                        UiLocalization.OptionRandom);
                    if (count > 0)
                        return AppendCamera2UnsupportedWarning(UiLocalization.Format("song-status-common-with-count", commonName, count));
                    else
                        return AppendCamera2UnsupportedWarning(UiLocalization.Format("song-status-common", commonName));
                }

                if (count > 0)
                {
                    string selected = CameraSongScriptDetector.HasSongScript
                        ? CameraSongScriptDetector.SelectedScriptDisplayName
                        : "?";
                    return AppendCamera2UnsupportedWarning(UiLocalization.Format("song-status-found", count, selected));
                }
                else
                {
                    return AppendCamera2UnsupportedWarning(UiLocalization.Get("song-status-none"));
                }
            }
        }

        #endregion

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

                _previewController.Seek(value, true);
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
            _previewController?.Clear();
            _lastPreviewUiTime = float.NegativeInfinity;
            RefreshPreviewBindings();
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
            get => UiLocalization.GetStatusPanelPositionOptions();
        }

        [UIValue("status-panel-position")]
        public object StatusPanelPosition
        {
            get
            {
                int idx = CameraSongScriptConfig.Instance.StatusPanelPosition;
                return UiLocalization.GetStatusPanelPositionDisplayName(idx);
            }
            set
            {
                int index = UiLocalization.GetStatusPanelPositionIndex(value as string);
                CameraSongScriptConfig.Instance.StatusPanelPosition = index;
                _statusView?.SetPosition(index);
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
