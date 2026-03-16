using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Models;
using CameraSongScript.Localization;
using CameraSongScript.Services;
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
    public partial class CameraSongScriptSettingsView : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        public const string TabName = "Camera Song Script";
        public string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        private CameraSongScriptStatusView _statusView;

        private CameraSongScriptPreviewController _previewController;

        private CameraSongScriptDetector _scriptDetector;

        private SongScriptBeatmapIndexService _beatmapIndexService;

        private const float PreviewSliderStep = 0.05f;
        private bool _needsRefresh = false;
        private bool _suppressPreviewSeek = false;
        private float _lastPreviewUiTime = float.NegativeInfinity;
        private bool _lastPreviewVisible = false;
        private bool _lastPreviewPlaying = false;
        private int _lastPreviewSpeed = 1;
        private bool _cacheScanStatusRefreshQueued = false;

        [Inject]
        internal void Constractor(
            CameraSongScriptStatusView statusView,
            CameraSongScriptPreviewController previewController,
            CameraSongScriptDetector scriptDetector,
            SongScriptBeatmapIndexService beatmapIndexService)
        {
            _statusView = statusView;
            _previewController = previewController;
            _scriptDetector = scriptDetector;
            _beatmapIndexService = beatmapIndexService;
        }

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
            _scriptDetector.ScanCompleted += OnScanCompleted;
            SongScriptFolderCache.ScanStatusChanged += OnSongScriptFolderCacheStatusChanged;
            if (_beatmapIndexService != null)
                _beatmapIndexService.ScanStatusChanged += OnBeatmapSongScriptCacheStatusChanged;
            if (_previewController != null)
                _previewController.StateChanged += OnPreviewStateChanged;
            UiLocalization.LanguageChanged += OnLanguageChanged;
        }

        public void Dispose()
        {
            _scriptDetector.ScanCompleted -= OnScanCompleted;
            SongScriptFolderCache.ScanStatusChanged -= OnSongScriptFolderCacheStatusChanged;
            if (_beatmapIndexService != null)
                _beatmapIndexService.ScanStatusChanged -= OnBeatmapSongScriptCacheStatusChanged;
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
            EnqueueCacheScanStatusRefresh();
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

        private void OnSongScriptFolderCacheStatusChanged()
        {
            EnqueueCacheScanStatusRefresh();
        }

        private void OnBeatmapSongScriptCacheStatusChanged()
        {
            EnqueueCacheScanStatusRefresh();
        }

        private void EnqueueCacheScanStatusRefresh()
        {
            if (_cacheScanStatusRefreshQueued)
            {
                return;
            }

            _cacheScanStatusRefreshQueued = true;

            void Refresh()
            {
                _cacheScanStatusRefreshQueued = false;

                try
                {
                    NotifyPropertyChanged(nameof(SongScriptCacheRefreshStatus));
                    NotifyPropertyChanged(nameof(IsSongScriptCacheRefreshAvailable));
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to refresh cache scan UI: {ex.Message}");
                }
            }

            if (HMMainThreadDispatcher.instance != null)
            {
                HMMainThreadDispatcher.instance.Enqueue(Refresh);
            }
            else
            {
                Refresh();
            }
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
            NotifyPropertyChanged(nameof(SectionCameraModSettings));
            NotifyPropertyChanged(nameof(ButtonPreviewShowStart));
            NotifyPropertyChanged(nameof(ButtonPreviewStop));
            NotifyPropertyChanged(nameof(ButtonPreviewClear));
            NotifyPropertyChanged(nameof(LabelPreviewPosition));
            NotifyPropertyChanged(nameof(ButtonPreviewVisualSettingsReset));
            NotifyPropertyChanged(nameof(SectionPreviewVisualSettings));
            NotifyPropertyChanged(nameof(LabelPreviewMiniatureScale));
            NotifyPropertyChanged(nameof(LabelPreviewVisiblePositionX));
            NotifyPropertyChanged(nameof(LabelPreviewVisiblePositionY));
            NotifyPropertyChanged(nameof(LabelPreviewVisiblePositionZ));
            NotifyPropertyChanged(nameof(LabelPreviewPathLineWidth));
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
            NotifyPropertyChanged(nameof(SectionOther));
            NotifyPropertyChanged(nameof(ToggleShowStatusPanel));
            NotifyPropertyChanged(nameof(LabelPanelPosition));
            NotifyPropertyChanged(nameof(ButtonRerunSongScriptCaches));
            NotifyPropertyChanged(nameof(DetectedCameraMod));
            NotifyPropertyChanged(nameof(ScriptFileOptions));
            NotifyPropertyChanged(nameof(SelectedScriptFile));
            NotifyPropertyChanged(nameof(CustomSceneOptions));
            NotifyPropertyChanged(nameof(SelectedCustomScene));
            NotifyPropertyChanged(nameof(TargetCameraOptions));
            NotifyPropertyChanged(nameof(TargetCameras));
            NotifyPropertyChanged(nameof(ProfileOptions));
            NotifyPropertyChanged(nameof(SongScriptProfile));
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
            NotifyPropertyChanged(nameof(SongScriptCacheRefreshStatus));
            NotifyPropertyChanged(nameof(IsSongScriptCacheRefreshAvailable));

            RefreshDropdown(scriptFileDropdown, ScriptFileOptions);
            RefreshDropdown(targetCameraDropdown, TargetCameraOptions);
            RefreshDropdown(customSceneDropdown, CustomSceneOptions);
            RefreshDropdown(songScriptProfileDropdown, ProfileOptions);
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
            NotifyPropertyChanged(nameof(HintRerunSongScriptCaches));
        }

        /// <summary>
        /// スキャン完了時コールバック（UI更新を安全に行うためメインスレッドへディスパッチ）
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
                    _scriptDetector.ResolveProfileName();
                    _scriptDetector.SyncCameraPlusPath();

                    // プロファイルドロップダウンのUIを現在の曲の設定に更新する
                    NotifyPropertyChanged(nameof(SongScriptProfile));

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

        [UIValue("section-camera-mod-settings")]
        public string SectionCameraModSettings
        {
            get
            {
                if (CameraModDetector.IsCamera2)
                    return UiLocalization.Get("section-camera2-settings");

                if (CameraModDetector.IsCameraPlus)
                    return UiLocalization.Get("section-cameraplus-settings");

                return string.Empty;
            }
        }

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

        [UIValue("section-other")]
        public string SectionOther => UiLocalization.Get("section-other");

        [UIValue("toggle-show-status-panel")]
        public string ToggleShowStatusPanel => UiLocalization.Get("toggle-show-status-panel");

        [UIValue("label-panel-position")]
        public string LabelPanelPosition => UiLocalization.Get("label-panel-position");

        [UIValue("button-rerun-songscript-caches")]
        public string ButtonRerunSongScriptCaches => UiLocalization.Get("button-rerun-songscript-caches");

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

        [UIValue("show-camera-mod-settings-section")]
        public bool ShowCameraModSettingsSection => CameraModDetector.IsCamera2 || CameraModDetector.IsCameraPlus;

        #endregion

        #region SongScript有効/無効

        [UIValue("enabled")]
        public bool Enabled
        {
            get => CameraSongScriptConfig.Instance.Enabled;
            set
            {
                CameraSongScriptConfig.Instance.Enabled = value;
                _scriptDetector.ReevaluateCommonScriptUsage();
                _scriptDetector.SyncCameraPlusPath();
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

        [UIComponent("song-script-profile-dropdown")]
        public DropDownListSetting songScriptProfileDropdown;

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
                foreach (var fileName in _scriptDetector.AvailableScriptFiles)
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
                if (_scriptDetector.HasSongScript)
                {
                    var specificSettings = SongSettingsManager.GetCurrentSettings();
                    if (specificSettings != null && !string.IsNullOrEmpty(specificSettings.SelectedScriptFileName))
                    {
                        // 譜面個別設定があれば、それに合わせて内部選択も更新しておく
                        if (_scriptDetector.AvailableScriptFiles.Contains(specificSettings.SelectedScriptFileName) &&
                            _scriptDetector.SelectedScriptDisplayName != specificSettings.SelectedScriptFileName)
                        {
                            _scriptDetector.UpdateSelectedScript(specificSettings.SelectedScriptFileName);
                        }
                    }
                    return _scriptDetector.SelectedScriptDisplayName;
                }
                return UiLocalization.GetOptionDisplay(UiLocalization.OptionNone, UiLocalization.OptionNone);
            }
            set
            {
                string fileName = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNone);
                if (!string.IsNullOrEmpty(fileName) && fileName != UiLocalization.OptionNone)
                {
                    _scriptDetector.UpdateSelectedScript(fileName);
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
            _scriptDetector.IsUsingCommonScript &&
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
                        if (_scriptDetector.IsUsingCommonScript)
                        {
                            // 汎用スクリプト非ランダム: ResolvedCommonScriptPathのハッシュで保存
                            string commonPath = _scriptDetector.ResolvedCommonScriptPath;
                            if (!string.IsNullOrEmpty(commonPath))
                            {
                                ScriptOffsetManager.UpdateOffsetForScript(commonPath, value);
                            }
                        }
                        else if (_scriptDetector.HasSongScript)
                        {
                            // 通常スクリプト: SelectedScriptPathのハッシュで保存
                            ScriptOffsetManager.UpdateOffsetForScript(_scriptDetector.SelectedScriptPath, value);
                        }
                    }
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = value;

                    if (_scriptDetector.HasSongScript)
                    {
                        _scriptDetector.UpdateEffectiveScriptPath();
                        _scriptDetector.SyncCameraPlusPath();
                    }
                    else if (_scriptDetector.IsUsingCommonScript)
                    {
                        // 汎用スクリプト: CameraPlusモードの場合はパス再同期
                        _scriptDetector.SyncCameraPlusPath();
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
        public bool HasMetadata => _scriptDetector.CurrentMetadata != null;

        [UIValue("meta-author")]
        public string MetaAuthor
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
                if (meta == null) return string.Empty;
                return string.IsNullOrEmpty(meta.cameraScriptAuthorName) ? "--" : meta.cameraScriptAuthorName;
            }
        }

        [UIValue("meta-song")]
        public string MetaSong
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
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
                var meta = _scriptDetector.CurrentMetadata;
                if (meta == null) return string.Empty;
                return string.IsNullOrEmpty(meta.levelAuthorName) ? "--" : meta.levelAuthorName;
            }
        }

        [UIValue("has-meta-height")]
        public bool HasMetaHeight
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
                return meta != null && meta.avatarHeight.HasValue && meta.avatarHeight.Value > 0;
            }
        }

        [UIValue("meta-height")]
        public string MetaHeight
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
                if (meta == null || !meta.avatarHeight.HasValue || meta.avatarHeight.Value <= 0) return string.Empty;
                return $"{meta.avatarHeight.Value:0.#} cm";
            }
        }

        [UIValue("has-meta-description")]
        public bool HasMetaDescription
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
                return meta != null && !string.IsNullOrEmpty(meta.description);
            }
        }

        [UIValue("meta-description")]
        public string MetaDescription
        {
            get
            {
                var meta = _scriptDetector.CurrentMetadata;
                if (meta == null || string.IsNullOrEmpty(meta.description)) return string.Empty;
                return meta.description;
            }
        }

        #endregion




        #region SongScript検出状態

        private string AppendCamera2UnsupportedWarning(string statusText)
        {
            if (!CameraModDetector.IsCamera2 || !_scriptDetector.HasCurrentUnsupportedFeatures)
                return statusText;

            string warningText = UiLocalization.Format(
                "warning-camera2-unsupported",
                _scriptDetector.CurrentUnsupportedFeatureSummary);
            return string.IsNullOrEmpty(statusText) ? warningText : $"{statusText}\n{warningText}";
        }

        [UIValue("song-script-status")]
        public string SongScriptStatus
        {
            get
            {
                int count = _scriptDetector.AvailableScriptFiles.Count;
                bool isCommon = _scriptDetector.IsUsingCommonScript;

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
                    string selected = _scriptDetector.HasSongScript
                        ? _scriptDetector.SelectedScriptDisplayName
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


        #region ステータスパネル設定

        [UIValue("show-status-panel")]
        public bool ShowStatusPanel
        {
            get => CameraSongScriptConfig.Instance.ShowStatusPanel;
            set
            {
                CameraSongScriptConfig.Instance.ShowStatusPanel = value;
                _statusView?.UpdateContent();
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

        [UIValue("songscript-cache-refresh-status")]
        public string SongScriptCacheRefreshStatus
        {
            get
            {
                string beatmapStatus = UiLocalization.Format(
                    "cache-refresh-status-beatmap",
                    GetBeatmapSongScriptCacheStatusText());
                string songScriptsStatus = UiLocalization.Format(
                    "cache-refresh-status-songscripts",
                    GetSongScriptFolderCacheStatusText());
                return string.Format(CultureInfo.InvariantCulture, "{0} | {1}", beatmapStatus, songScriptsStatus);
            }
        }

        [UIValue("is-songscript-cache-refresh-available")]
        public bool IsSongScriptCacheRefreshAvailable =>
            !SongScriptFolderCache.IsScanning &&
            (_beatmapIndexService == null || !_beatmapIndexService.IsScanning);

        [UIAction("rerun-songscript-caches")]
        private void RerunSongScriptCaches()
        {
            if (!IsSongScriptCacheRefreshAvailable)
            {
                return;
            }

            Task songScriptFolderScanTask = SongScriptFolderCache.ScanAsync();
            if (_beatmapIndexService != null)
            {
                _ = _beatmapIndexService.RefreshIndexAsync();
            }
            EnqueueCacheScanStatusRefresh();
            _ = ReevaluateCurrentLevelWhenSongScriptFolderCacheReadyAsync(songScriptFolderScanTask);
        }

        private async Task ReevaluateCurrentLevelWhenSongScriptFolderCacheReadyAsync(Task scanTask)
        {
            try
            {
                await scanTask.ConfigureAwait(false);
                _scriptDetector.ReevaluateCurrentLevel();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SettingsView: Failed to re-evaluate current level after SongScripts cache refresh: {ex.Message}");
            }
        }

        private string GetBeatmapSongScriptCacheStatusText()
        {
            if (_beatmapIndexService == null)
            {
                return UiLocalization.Get("cache-refresh-state-idle");
            }

            return GetCacheScanStateText(
                GetCacheScanStateKey(_beatmapIndexService.ScanState),
                _beatmapIndexService.ProcessedBeatmapFolderCount,
                _beatmapIndexService.TotalBeatmapFolderCount);
        }

        private string GetSongScriptFolderCacheStatusText()
        {
            return GetCacheScanStateText(
                GetCacheScanStateKey(SongScriptFolderCache.ScanState),
                SongScriptFolderCache.ProcessedSourceCount,
                SongScriptFolderCache.TotalSourceCount);
        }

        private string GetCacheScanStateText(string stateKey, int processedCount, int totalCount)
        {
            if (string.Equals(stateKey, "cache-refresh-state-scanning", StringComparison.Ordinal) && totalCount > 0)
            {
                return UiLocalization.Format(
                    "cache-refresh-state-scanning-progress",
                    processedCount < 0 ? 0 : processedCount,
                    totalCount < 0 ? 0 : totalCount);
            }

            return UiLocalization.Get(stateKey);
        }

        private static string GetCacheScanStateKey(BeatmapSongScriptCacheScanState state)
        {
            switch (state)
            {
                case BeatmapSongScriptCacheScanState.Scanning:
                    return "cache-refresh-state-scanning";
                case BeatmapSongScriptCacheScanState.Completed:
                    return "cache-refresh-state-completed";
                case BeatmapSongScriptCacheScanState.Failed:
                    return "cache-refresh-state-failed";
                default:
                    return "cache-refresh-state-idle";
            }
        }

        private static string GetCacheScanStateKey(SongScriptFolderCacheScanState state)
        {
            switch (state)
            {
                case SongScriptFolderCacheScanState.Scanning:
                    return "cache-refresh-state-scanning";
                case SongScriptFolderCacheScanState.Completed:
                    return "cache-refresh-state-completed";
                case SongScriptFolderCacheScanState.Failed:
                    return "cache-refresh-state-failed";
                default:
                    return "cache-refresh-state-idle";
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

        [UIValue("hint-rerun-songscript-caches")]
        public string HintRerunSongScriptCaches => HoverHintLocalization.Get("hint-rerun-songscript-caches");

        #endregion
    }
}


