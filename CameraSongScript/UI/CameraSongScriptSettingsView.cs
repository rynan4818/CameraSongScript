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
using HMUI;
using IPA.Utilities;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Models;
using CameraSongScript.Localization;
using CameraSongScript.Services;
using CameraSongScript.Utilities;
using TMPro;
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

        private SongScriptMissingBeatmapDownloadService _missingBeatmapDownloadService;

        private const float PreviewSliderStep = 0.05f;
        private const float StatusPanelPositionStep = 0.05f;
        private const float StatusPanelRotationStep = 1f;
        private bool _needsRefresh = false;
        private bool _suppressPreviewSeek = false;
        private float _lastPreviewUiTime = float.NegativeInfinity;
        private bool _lastPreviewVisible = false;
        private bool _lastPreviewPlaying = false;
        private int _lastPreviewSpeed = 1;
        private bool _cacheScanStatusRefreshQueued = false;
        private bool _missingBeatmapStatusRefreshQueued = false;
        private Coroutine _refreshLayoutCoroutine;
        private Coroutine _scriptFileDropdownTextRefreshCoroutine;
        private Coroutine _commonScriptDropdownTextRefreshCoroutine;
        private Button _scriptFileDropdownButton;
        private Button _commonScriptDropdownButton;

        [Inject]
        internal void Constractor(
            CameraSongScriptStatusView statusView,
            CameraSongScriptPreviewController previewController,
            CameraSongScriptDetector scriptDetector,
            SongScriptBeatmapIndexService beatmapIndexService,
            SongScriptMissingBeatmapDownloadService missingBeatmapDownloadService)
        {
            _statusView = statusView;
            _previewController = previewController;
            _scriptDetector = scriptDetector;
            _beatmapIndexService = beatmapIndexService;
            _missingBeatmapDownloadService = missingBeatmapDownloadService;
        }

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
            _scriptDetector.ScanCompleted += OnScanCompleted;
            SongScriptFolderCache.ScanStatusChanged += OnSongScriptFolderCacheStatusChanged;
            if (_beatmapIndexService != null)
                _beatmapIndexService.ScanStatusChanged += OnBeatmapSongScriptCacheStatusChanged;
            if (_missingBeatmapDownloadService != null)
                _missingBeatmapDownloadService.StateChanged += OnMissingBeatmapDownloadServiceStateChanged;
            if (_previewController != null)
                _previewController.StateChanged += OnPreviewStateChanged;
            PluginAdapterManager.AdapterVersionWarningsChanged += OnAdapterVersionWarningsChanged;
            UiLocalization.LanguageChanged += OnLanguageChanged;
        }

        public void Dispose()
        {
            _scriptDetector.ScanCompleted -= OnScanCompleted;
            SongScriptFolderCache.ScanStatusChanged -= OnSongScriptFolderCacheStatusChanged;
            if (_beatmapIndexService != null)
                _beatmapIndexService.ScanStatusChanged -= OnBeatmapSongScriptCacheStatusChanged;
            if (_missingBeatmapDownloadService != null)
                _missingBeatmapDownloadService.StateChanged -= OnMissingBeatmapDownloadServiceStateChanged;
            if (_previewController != null)
                _previewController.StateChanged -= OnPreviewStateChanged;
            PluginAdapterManager.AdapterVersionWarningsChanged -= OnAdapterVersionWarningsChanged;
            UiLocalization.LanguageChanged -= OnLanguageChanged;
            DetachScriptFileDropdownButtonHandler();
            DetachCommonScriptDropdownButtonHandler();
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
            if (_missingBeatmapDownloadService != null)
            {
                _missingBeatmapDownloadService.RefreshState();
            }
            EnqueueMissingBeatmapStatusRefresh();
        }

        protected void OnDisable()
        {
            CancelPendingLayoutRefresh();
            CancelPendingScriptFileDropdownTextRefresh();
            CancelPendingCommonScriptDropdownTextRefresh();
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

        private void OnAdapterVersionWarningsChanged()
        {
            void Refresh()
            {
                try
                {
                    NotifyPropertyChanged(nameof(SongScriptStatus));
                    RefreshLayout();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to apply adapter warning change: {ex.Message}");
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

        private void OnSongScriptFolderCacheStatusChanged()
        {
            EnqueueCacheScanStatusRefresh();
        }

        private void OnBeatmapSongScriptCacheStatusChanged()
        {
            EnqueueCacheScanStatusRefresh();
        }

        private void OnMissingBeatmapDownloadServiceStateChanged()
        {
            EnqueueMissingBeatmapStatusRefresh();
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

        private void EnqueueMissingBeatmapStatusRefresh()
        {
            if (_missingBeatmapStatusRefreshQueued)
            {
                return;
            }

            _missingBeatmapStatusRefreshQueued = true;

            void Refresh()
            {
                _missingBeatmapStatusRefreshQueued = false;

                try
                {
                    NotifyPropertyChanged(nameof(MissingBeatmapDownloadStatus));
                    NotifyPropertyChanged(nameof(IsMissingBeatmapDownloadAvailable));
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SettingsView: Failed to refresh missing beatmap download UI: {ex.Message}");
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

        private void RefreshMetadataBindings()
        {
            NotifyPropertyChanged(nameof(HasMetadata));
            NotifyPropertyChanged(nameof(MetaAuthor));
            NotifyPropertyChanged(nameof(MetaSong));
            NotifyPropertyChanged(nameof(MetaMapper));
            NotifyPropertyChanged(nameof(HasMetaHeight));
            NotifyPropertyChanged(nameof(MetaHeight));
            NotifyPropertyChanged(nameof(HasMetaDescription));
            NotifyPropertyChanged(nameof(MetaDescription));
        }

        private void RefreshSelectedScriptUi(bool refreshScriptOptions, bool refreshOffsetInteractable)
        {
            if (refreshScriptOptions)
            {
                NotifyPropertyChanged(nameof(ScriptFileOptions));
                NotifyPropertyChanged(nameof(SelectedScriptFile));
            }

            NotifyPropertyChanged(nameof(SongScriptStatus));
            NotifyPropertyChanged(nameof(HintScriptFile));
            RefreshMetadataBindings();
            NotifyPropertyChanged(nameof(CameraHeightOffset));

            if (refreshOffsetInteractable)
            {
                NotifyPropertyChanged(nameof(IsOffsetInteractable));
            }

            RefreshLayout();

            if (cameraHeightOffsetSlider != null)
            {
                cameraHeightOffsetSlider.ReceiveValue();
            }

            EnsureScriptFileDropdownTextPresentation();
            _statusView?.UpdateContent();
            HandlePreviewSelectionChanged();
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
            NotifyPropertyChanged(nameof(SectionBeatmapScriptManagement));
            NotifyPropertyChanged(nameof(ToggleShowStatusPanel));
            NotifyPropertyChanged(nameof(LabelPanelPosition));
            NotifyPropertyChanged(nameof(LabelPanelAdjustPosition));
            NotifyPropertyChanged(nameof(LabelPanelAdjustRotation));
            NotifyPropertyChanged(nameof(ButtonStatusPanelTransformReset));
            NotifyPropertyChanged(nameof(StatusPanelTransformSummary));
            NotifyPropertyChanged(nameof(ButtonRerunSongScriptCaches));
            NotifyPropertyChanged(nameof(ButtonDownloadMissingBeatmaps));
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
            NotifyPropertyChanged(nameof(MissingBeatmapDownloadStatus));
            NotifyPropertyChanged(nameof(IsMissingBeatmapDownloadAvailable));

            RefreshDropdown(scriptFileDropdown, ScriptFileOptions);
            RefreshDropdown(targetCameraDropdown, TargetCameraOptions);
            RefreshDropdown(customSceneDropdown, CustomSceneOptions);
            RefreshDropdown(songScriptProfileDropdown, ProfileOptions);
            RefreshDropdown(commonScriptDropdown, CommonScriptOptions);
            RefreshDropdown(commonTargetCameraDropdown, CommonTargetCameraOptions);
            RefreshDropdown(commonCustomSceneDropdown, CommonCustomSceneOptions);
            RefreshDropdown(commonProfileDropdown, CommonProfileOptions);
            RefreshDropdown(statusPanelPositionDropdown, StatusPanelPositionOptions);

            EnsureScriptFileDropdownTextPresentation();
            EnsureCommonScriptDropdownTextPresentation();
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

        private void EnsureScriptFileDropdownTextPresentation()
        {
            if (scriptFileDropdown?.dropdown == null)
                return;

            ApplyDropdownTextPresentation(scriptFileDropdown);

            Button button = scriptFileDropdown.dropdown.GetField<Button, DropdownWithTableView>("_button");
            if (button == null || ReferenceEquals(button, _scriptFileDropdownButton))
                return;

            DetachScriptFileDropdownButtonHandler();
            _scriptFileDropdownButton = button;
            _scriptFileDropdownButton.onClick.AddListener(HandleScriptFileDropdownButtonClicked);
        }

        private void DetachScriptFileDropdownButtonHandler()
        {
            if (_scriptFileDropdownButton != null)
            {
                _scriptFileDropdownButton.onClick.RemoveListener(HandleScriptFileDropdownButtonClicked);
                _scriptFileDropdownButton = null;
            }
        }

        private void HandleScriptFileDropdownButtonClicked()
        {
            CancelPendingScriptFileDropdownTextRefresh();
            _scriptFileDropdownTextRefreshCoroutine = StartCoroutine(RefreshScriptFileDropdownTextPresentationCoroutine());
        }

        private System.Collections.IEnumerator RefreshScriptFileDropdownTextPresentationCoroutine()
        {
            yield return null;
            yield return null;
            ApplyDropdownTextPresentation(scriptFileDropdown);
            _scriptFileDropdownTextRefreshCoroutine = null;
        }

        private void CancelPendingScriptFileDropdownTextRefresh()
        {
            if (_scriptFileDropdownTextRefreshCoroutine == null)
                return;

            StopCoroutine(_scriptFileDropdownTextRefreshCoroutine);
            _scriptFileDropdownTextRefreshCoroutine = null;
        }

        private void EnsureCommonScriptDropdownTextPresentation()
        {
            if (commonScriptDropdown?.dropdown == null)
                return;

            ApplyDropdownTextPresentation(commonScriptDropdown);

            Button button = commonScriptDropdown.dropdown.GetField<Button, DropdownWithTableView>("_button");
            if (button == null || ReferenceEquals(button, _commonScriptDropdownButton))
                return;

            DetachCommonScriptDropdownButtonHandler();
            _commonScriptDropdownButton = button;
            _commonScriptDropdownButton.onClick.AddListener(HandleCommonScriptDropdownButtonClicked);
        }

        private void DetachCommonScriptDropdownButtonHandler()
        {
            if (_commonScriptDropdownButton != null)
            {
                _commonScriptDropdownButton.onClick.RemoveListener(HandleCommonScriptDropdownButtonClicked);
                _commonScriptDropdownButton = null;
            }
        }

        private void HandleCommonScriptDropdownButtonClicked()
        {
            CancelPendingCommonScriptDropdownTextRefresh();
            _commonScriptDropdownTextRefreshCoroutine = StartCoroutine(RefreshCommonScriptDropdownTextPresentationCoroutine());
        }

        private System.Collections.IEnumerator RefreshCommonScriptDropdownTextPresentationCoroutine()
        {
            yield return null;
            yield return null;
            ApplyDropdownTextPresentation(commonScriptDropdown);
            _commonScriptDropdownTextRefreshCoroutine = null;
        }

        private void CancelPendingCommonScriptDropdownTextRefresh()
        {
            if (_commonScriptDropdownTextRefreshCoroutine == null)
                return;

            StopCoroutine(_commonScriptDropdownTextRefreshCoroutine);
            _commonScriptDropdownTextRefreshCoroutine = null;
        }

        private static void ApplyDropdownTextPresentation(DropDownListSetting dropdown)
        {
            if (dropdown?.dropdown == null)
                return;

            foreach (TextMeshProUGUI text in dropdown.dropdown.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null)
                    continue;

                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private string GetDefaultOrFullScriptNameHoverHint(string canonicalName, string defaultHintKey, params string[] fallbackValues)
        {
            if (!CameraSongScriptConfig.Instance.ShowHoverHints)
                return string.Empty;

            if (string.IsNullOrEmpty(canonicalName))
                return HoverHintLocalization.Get(defaultHintKey);

            if (fallbackValues != null)
            {
                for (int i = 0; i < fallbackValues.Length; i++)
                {
                    if (string.Equals(canonicalName, fallbackValues[i], StringComparison.Ordinal))
                        return HoverHintLocalization.Get(defaultHintKey);
                }
            }

            return SongScriptDisplayLabelFormatter.NeedsShortening(canonicalName)
                ? canonicalName
                : HoverHintLocalization.Get(defaultHintKey);
        }

        private string GetSelectedScriptFileNameForHoverHint()
        {
            if (_scriptDetector == null || !_scriptDetector.HasSongScript)
                return UiLocalization.OptionNone;

            var specificSettings = SongSettingsManager.GetCurrentSettings();
            if (specificSettings != null &&
                !string.IsNullOrEmpty(specificSettings.SelectedScriptFileName) &&
                _scriptDetector.AvailableScriptFiles.Contains(specificSettings.SelectedScriptFileName))
            {
                return specificSettings.SelectedScriptFileName;
            }

            return _scriptDetector.SelectedScriptDisplayName;
        }

        private string GetSelectedCommonScriptNameForHoverHint()
        {
            string selected = CameraSongScriptConfig.Instance.SelectedCommonScript;
            if (!string.IsNullOrEmpty(selected) && CommonScriptCache.IsReady)
            {
                var names = CommonScriptCache.GetDisplayNames();
                if (names.Contains(selected))
                    return selected;
            }

            return CommonScriptCache.IsReady
                ? UiLocalization.OptionRandom
                : UiLocalization.OptionNone;
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
            NotifyPropertyChanged(nameof(HintDownloadMissingBeatmaps));
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
                        EnsureScriptFileDropdownTextPresentation();
                    }
                    if (cameraHeightOffsetSlider != null)
                    {
                        cameraHeightOffsetSlider.ReceiveValue();
                    }

                    RefreshSelectedScriptUi(
                        refreshScriptOptions: true,
                        refreshOffsetInteractable: true);
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

        [UIValue("section-beatmap-script-management")]
        public string SectionBeatmapScriptManagement => UiLocalization.Get("section-beatmap-script-management");

        [UIValue("toggle-show-status-panel")]
        public string ToggleShowStatusPanel => UiLocalization.Get("toggle-show-status-panel");

        [UIValue("label-panel-position")]
        public string LabelPanelPosition => UiLocalization.Get("label-panel-position");

        [UIValue("label-panel-adjust-position")]
        public string LabelPanelAdjustPosition => UiLocalization.Get("label-panel-adjust-position");

        [UIValue("label-panel-adjust-rotation")]
        public string LabelPanelAdjustRotation => UiLocalization.Get("label-panel-adjust-rotation");

        [UIValue("button-status-panel-transform-reset")]
        public string ButtonStatusPanelTransformReset => UiLocalization.Get("button-status-panel-transform-reset");

        [UIValue("status-panel-transform-summary")]
        public string StatusPanelTransformSummary
        {
            get
            {
                if (!TryGetSelectedStatusPanelTransform(out _, out Vector3 position, out Vector3 rotation))
                    return "Pos: -, -, - | Rot: -, -, -";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Pos: {0:0.00}, {1:0.00}, {2:0.00} | Rot: {3:0}, {4:0}, {5:0}",
                    position.x,
                    position.y,
                    position.z,
                    rotation.x,
                    rotation.y,
                    rotation.z);
            }
        }

        [UIValue("button-rerun-songscript-caches")]
        public string ButtonRerunSongScriptCaches => UiLocalization.Get("button-rerun-songscript-caches");

        [UIValue("button-download-missing-beatmaps")]
        public string ButtonDownloadMissingBeatmaps => UiLocalization.Get("button-download-missing-beatmaps");

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
        public HMUI.ScrollView settingsContainerScrollView;

        [UIObject("settings-container")]
        public GameObject settingsContainerObject;

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

        private sealed class ScriptFileDropdownOption
        {
            public ScriptFileDropdownOption(string canonicalName)
            {
                CanonicalName = canonicalName ?? string.Empty;
                DisplayLabel = SongScriptDisplayLabelFormatter.Format(CanonicalName);
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

                if (obj is ScriptFileDropdownOption option)
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

        [UIValue("script-file-options")]
        public List<object> ScriptFileOptions
        {
            get
            {
                var list = new List<object>();
                foreach (var fileName in _scriptDetector.AvailableScriptFiles)
                {
                    list.Add(CreateScriptFileDropdownOption(fileName));
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
                    return GetSelectedScriptFileValue(_scriptDetector.SelectedScriptDisplayName);
                }
                return UiLocalization.GetOptionDisplay(UiLocalization.OptionNone, UiLocalization.OptionNone);
            }
            set
            {
                string fileName = GetCanonicalScriptFileName(value);
                if (!string.IsNullOrEmpty(fileName) && fileName != UiLocalization.OptionNone)
                {
                    _scriptDetector.UpdateSelectedScript(fileName);
                    SongSettingsManager.UpdateCurrentScriptFileName(fileName);

                    RefreshSelectedScriptUi(
                        refreshScriptOptions: false,
                        refreshOffsetInteractable: false);
                }
            }
        }

        private static ScriptFileDropdownOption CreateScriptFileDropdownOption(string fileName)
        {
            return new ScriptFileDropdownOption(fileName);
        }

        private object GetSelectedScriptFileValue(string fileName)
        {
            if (scriptFileDropdown?.values != null)
            {
                foreach (object value in scriptFileDropdown.values)
                {
                    if (string.Equals(GetCanonicalScriptFileName(value), fileName, StringComparison.Ordinal))
                        return value;
                }
            }

            return CreateScriptFileDropdownOption(fileName);
        }

        private static string GetCanonicalScriptFileName(object value)
        {
            if (value is ScriptFileDropdownOption option)
                return option.CanonicalName;

            return UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNone);
        }

        private void RefreshLayout()
        {
            if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            {
                _needsRefresh = true;
                return;
            }

            CancelPendingLayoutRefresh();
            _refreshLayoutCoroutine = StartCoroutine(RefreshLayoutCoroutine());
        }

        private System.Collections.IEnumerator RefreshLayoutCoroutine()
        {
            // BSMLの要素が有効/無効になった直後だと、Unityが推奨サイズ(preferredHeight)を
            // 正しく計算できていない場合があるため、複数フレーム待機する
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;

            try
            {
                RectTransform contentContainer = settingsContainerObject?.transform as RectTransform;
                if (contentContainer != null)
                {
                    Canvas.ForceUpdateCanvases();
                    RebuildLayoutHierarchy(contentContainer);
                    Canvas.ForceUpdateCanvases();

                    var scrollView = settingsContainerScrollView ?? contentContainer.GetComponentInParent<HMUI.ScrollView>();
                    if (scrollView != null)
                    {
                        float contentHeight = LayoutUtility.GetPreferredHeight(contentContainer);
                        contentHeight = Mathf.Max(contentHeight, contentContainer.rect.height);

                        if (contentContainer.parent is RectTransform parentTransform)
                        {
                            float parentPreferredHeight = LayoutUtility.GetPreferredHeight(parentTransform);
                            contentHeight = Mathf.Max(contentHeight, parentPreferredHeight, parentTransform.rect.height);
                        }

                        scrollView.SetContentSize(contentHeight);
                        scrollView.RefreshButtons();
                    }
                }
            }
            finally
            {
                _refreshLayoutCoroutine = null;
            }
        }

        private void CancelPendingLayoutRefresh()
        {
            if (_refreshLayoutCoroutine == null)
            {
                return;
            }

            StopCoroutine(_refreshLayoutCoroutine);
            _refreshLayoutCoroutine = null;
        }

        private static void RebuildLayoutHierarchy(RectTransform rectTransform)
        {
            var rebuildTargets = new List<RectTransform>();
            for (RectTransform current = rectTransform; current != null; current = current.parent as RectTransform)
            {
                rebuildTargets.Add(current);
            }

            for (int i = rebuildTargets.Count - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rebuildTargets[i]);
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

        private string AppendStatusWarnings(string statusText)
        {
            statusText = AppendWarningLine(statusText, GetCamera2UnsupportedWarningText());
            statusText = AppendWarningLine(statusText, Plugin.GetUnsupportedAdapterVersionWarningText());
            return statusText;
        }

        private string GetCamera2UnsupportedWarningText()
        {
            if (!CameraModDetector.IsCamera2 || !_scriptDetector.HasCurrentUnsupportedFeatures)
                return string.Empty;

            return UiLocalization.Format(
                "warning-camera2-unsupported",
                _scriptDetector.CurrentUnsupportedFeatureSummary);
        }

        private static string AppendWarningLine(string statusText, string warningText)
        {
            if (string.IsNullOrEmpty(warningText))
                return statusText;

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
                    commonName = SongScriptDisplayLabelFormatter.Format(commonName);
                    if (count > 0)
                        return AppendStatusWarnings(UiLocalization.Format("song-status-common-with-count", commonName, count));
                    else
                        return AppendStatusWarnings(UiLocalization.Format("song-status-common", commonName));
                }

                if (count > 0)
                {
                    string selected = _scriptDetector.HasSongScript
                        ? SongScriptDisplayLabelFormatter.Format(_scriptDetector.SelectedScriptDisplayName)
                        : "?";
                    return AppendStatusWarnings(UiLocalization.Format("song-status-found", count, selected));
                }
                else
                {
                    return AppendStatusWarnings(UiLocalization.Get("song-status-none"));
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
                NotifyPropertyChanged(nameof(StatusPanelTransformSummary));
            }
        }

        [UIAction("status-panel-position-x-increase")]
        private void StatusPanelPositionXIncrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(StatusPanelPositionStep, 0f, 0f));
        }

        [UIAction("status-panel-position-x-decrease")]
        private void StatusPanelPositionXDecrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(-StatusPanelPositionStep, 0f, 0f));
        }

        [UIAction("status-panel-position-y-increase")]
        private void StatusPanelPositionYIncrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(0f, StatusPanelPositionStep, 0f));
        }

        [UIAction("status-panel-position-y-decrease")]
        private void StatusPanelPositionYDecrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(0f, -StatusPanelPositionStep, 0f));
        }

        [UIAction("status-panel-position-z-increase")]
        private void StatusPanelPositionZIncrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(0f, 0f, StatusPanelPositionStep));
        }

        [UIAction("status-panel-position-z-decrease")]
        private void StatusPanelPositionZDecrease()
        {
            AdjustSelectedStatusPanelPosition(new Vector3(0f, 0f, -StatusPanelPositionStep));
        }

        [UIAction("status-panel-rotation-x-increase")]
        private void StatusPanelRotationXIncrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(StatusPanelRotationStep, 0f, 0f));
        }

        [UIAction("status-panel-rotation-x-decrease")]
        private void StatusPanelRotationXDecrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(-StatusPanelRotationStep, 0f, 0f));
        }

        [UIAction("status-panel-rotation-y-increase")]
        private void StatusPanelRotationYIncrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(0f, StatusPanelRotationStep, 0f));
        }

        [UIAction("status-panel-rotation-y-decrease")]
        private void StatusPanelRotationYDecrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(0f, -StatusPanelRotationStep, 0f));
        }

        [UIAction("status-panel-rotation-z-increase")]
        private void StatusPanelRotationZIncrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(0f, 0f, StatusPanelRotationStep));
        }

        [UIAction("status-panel-rotation-z-decrease")]
        private void StatusPanelRotationZDecrease()
        {
            AdjustSelectedStatusPanelRotation(new Vector3(0f, 0f, -StatusPanelRotationStep));
        }

        [UIAction("reset-status-panel-transform")]
        private void ResetStatusPanelTransform()
        {
            if (!TryGetSelectedStatusPanelTransform(
                out int index,
                out Vector3 currentPosition,
                out Vector3 currentRotation))
            {
                return;
            }

            var defaults = new CameraSongScriptConfig();
            Vector3 defaultPosition = StatusPanelPresetCatalog.GetPosition(defaults, index);
            Vector3 defaultRotation = StatusPanelPresetCatalog.GetRotation(defaults, index);

            if (AreApproximatelyEqual(currentPosition, defaultPosition) &&
                AreApproximatelyEqual(currentRotation, defaultRotation))
            {
                return;
            }

            SaveAndApplySelectedStatusPanelTransform(index, defaultPosition, defaultRotation);
        }

        private void AdjustSelectedStatusPanelPosition(Vector3 delta)
        {
            if (!TryGetSelectedStatusPanelTransform(
                out int index,
                out Vector3 currentPosition,
                out Vector3 currentRotation))
            {
                return;
            }

            Vector3 updatedPosition = new Vector3(
                RoundToStep(currentPosition.x + delta.x, StatusPanelPositionStep),
                RoundToStep(currentPosition.y + delta.y, StatusPanelPositionStep),
                RoundToStep(currentPosition.z + delta.z, StatusPanelPositionStep));

            if (AreApproximatelyEqual(currentPosition, updatedPosition))
                return;

            SaveAndApplySelectedStatusPanelTransform(index, updatedPosition, currentRotation);
        }

        private void AdjustSelectedStatusPanelRotation(Vector3 delta)
        {
            if (!TryGetSelectedStatusPanelTransform(
                out int index,
                out Vector3 currentPosition,
                out Vector3 currentRotation))
            {
                return;
            }

            Vector3 updatedRotation = new Vector3(
                RoundToDecimals(currentRotation.x + delta.x, 0),
                RoundToDecimals(currentRotation.y + delta.y, 0),
                RoundToDecimals(currentRotation.z + delta.z, 0));

            if (AreApproximatelyEqual(currentRotation, updatedRotation))
                return;

            SaveAndApplySelectedStatusPanelTransform(index, currentPosition, updatedRotation);
        }

        private bool TryGetSelectedStatusPanelTransform(
            out int index,
            out Vector3 position,
            out Vector3 rotation)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
            {
                index = 0;
                position = Vector3.zero;
                rotation = Vector3.zero;
                return false;
            }

            index = StatusPanelPresetCatalog.ClampIndex(config.StatusPanelPosition);
            if (_statusView != null && _statusView.TryGetTransform(out position, out rotation))
                return true;

            position = StatusPanelPresetCatalog.GetPosition(config, index);
            rotation = StatusPanelPresetCatalog.GetRotation(config, index);
            return true;
        }

        private void SaveAndApplySelectedStatusPanelTransform(int index, Vector3 position, Vector3 rotation)
        {
            var config = CameraSongScriptConfig.Instance;
            if (config == null)
                return;

            StatusPanelPresetCatalog.SetPosition(config, index, position);
            StatusPanelPresetCatalog.SetRotation(config, index, rotation);

            if (_statusView != null)
            {
                _statusView.SetTransform(position, rotation);
                NotifyPropertyChanged(nameof(StatusPanelTransformSummary));
                return;
            }

            _statusView?.SetPosition(index);
            NotifyPropertyChanged(nameof(StatusPanelTransformSummary));
        }

        private static bool AreApproximatelyEqual(Vector3 left, Vector3 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                Mathf.Approximately(left.y, right.y) &&
                Mathf.Approximately(left.z, right.z);
        }

        private static float RoundToStep(float value, float step)
        {
            if (step <= 0f)
                return value;

            float snapped = (float)Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
            return RoundToDecimals(snapped, 2);
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

        [UIValue("missing-beatmap-download-status")]
        public string MissingBeatmapDownloadStatus
        {
            get
            {
                if (_missingBeatmapDownloadService == null)
                {
                    return UiLocalization.Get("missing-beatmaps-status-loading-songs");
                }

                if (_missingBeatmapDownloadService.IsDownloading)
                {
                    return UiLocalization.Format(
                        "missing-beatmaps-status-downloading",
                        _missingBeatmapDownloadService.DownloadProgressCurrent,
                        _missingBeatmapDownloadService.DownloadProgressTotal);
                }

                if (!_missingBeatmapDownloadService.IsSongScriptFolderReady ||
                    _missingBeatmapDownloadService.IsSongScriptFolderScanning)
                {
                    return UiLocalization.Get("missing-beatmaps-status-scanning");
                }

                if (!_missingBeatmapDownloadService.IsSongDetailsReady)
                {
                    return UiLocalization.Get("missing-beatmaps-status-waiting-songdetails");
                }

                if (!_missingBeatmapDownloadService.AreSongsLoaded ||
                    _missingBeatmapDownloadService.AreSongsLoading)
                {
                    return UiLocalization.Get("missing-beatmaps-status-loading-songs");
                }

                if (_missingBeatmapDownloadService.MissingMapIdCount == 0)
                {
                    return UiLocalization.Get("missing-beatmaps-status-none");
                }

                if (_missingBeatmapDownloadService.DownloadableMapIdCount == 0)
                {
                    return UiLocalization.Format(
                        "missing-beatmaps-status-no-downloadable",
                        _missingBeatmapDownloadService.MissingMapIdCount,
                        _missingBeatmapDownloadService.UnavailableOnBeatSaverCount,
                        _missingBeatmapDownloadService.AlreadyLoadedLatestHashCount);
                }

                return UiLocalization.Format(
                    "missing-beatmaps-status-ready",
                    _missingBeatmapDownloadService.MissingMapIdCount,
                    _missingBeatmapDownloadService.DownloadableMapIdCount,
                    _missingBeatmapDownloadService.UnavailableOnBeatSaverCount,
                    _missingBeatmapDownloadService.AlreadyLoadedLatestHashCount);
            }
        }

        [UIValue("is-missing-beatmap-download-available")]
        public bool IsMissingBeatmapDownloadAvailable =>
            _missingBeatmapDownloadService != null &&
            _missingBeatmapDownloadService.IsDownloadAvailable;

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

        [UIAction("download-missing-beatmaps")]
        private void DownloadMissingBeatmaps()
        {
            if (!IsMissingBeatmapDownloadAvailable || _missingBeatmapDownloadService == null)
            {
                return;
            }

            _ = DownloadMissingBeatmapsAsync();
        }

        private async Task DownloadMissingBeatmapsAsync()
        {
            try
            {
                await _missingBeatmapDownloadService.DownloadMissingBeatmapsAsync().ConfigureAwait(false);
                _scriptDetector.ReevaluateCurrentLevel();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SettingsView: Failed to download missing beatmaps: {ex.Message}");
            }
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
        public string HintScriptFile => GetDefaultOrFullScriptNameHoverHint(
            GetSelectedScriptFileNameForHoverHint(),
            "hint-script-file",
            UiLocalization.OptionNone);

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
        public string HintCommonScriptFile => GetDefaultOrFullScriptNameHoverHint(
            GetSelectedCommonScriptNameForHoverHint(),
            "hint-common-script-file",
            UiLocalization.OptionRandom,
            UiLocalization.OptionNone);

        [UIValue("hint-common-target-camera")]
        public string HintCommonTargetCamera => HoverHintLocalization.Get("hint-common-target-camera");

        [UIValue("hint-common-custom-scene")]
        public string HintCommonCustomScene => HoverHintLocalization.Get("hint-common-custom-scene");

        [UIValue("hint-common-profile")]
        public string HintCommonProfile => HoverHintLocalization.Get("hint-common-profile");

        [UIValue("hint-rerun-songscript-caches")]
        public string HintRerunSongScriptCaches => HoverHintLocalization.Get("hint-rerun-songscript-caches");

        [UIValue("hint-download-missing-beatmaps")]
        public string HintDownloadMissingBeatmaps => HoverHintLocalization.Get("hint-download-missing-beatmaps");

        #endregion
    }
}


