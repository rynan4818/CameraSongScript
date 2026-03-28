using System;
using CameraSongScript.Models;
using CameraSongScript.UI;
using HMUI;
using Zenject;

namespace CameraSongScript.Detectors
{
    public class LevelSelectionDetector : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetail;
        private readonly CameraSongScriptDetector _scriptDetector;
        private readonly CameraSongScriptPreviewController _previewController;
        private readonly CameraSongScriptStatusView _statusView;

        internal LevelSelectionDetector(
            StandardLevelDetailViewController standardLevelDetail,
            CameraSongScriptDetector scriptDetector,
            CameraSongScriptPreviewController previewController,
            CameraSongScriptStatusView statusView)
        {
            _standardLevelDetail = standardLevelDetail;
            _scriptDetector = scriptDetector;
            _previewController = previewController;
            _statusView = statusView;
        }

        public void Initialize()
        {
            if (_standardLevelDetail != null)
            {
                _standardLevelDetail.didActivateEvent += OnLevelDetailActivated;
                _standardLevelDetail.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
                _standardLevelDetail.didChangeContentEvent += OnContentChanged;
                _standardLevelDetail.didDeactivateEvent += OnLevelDetailDeactivated;
                Plugin.Log.Info("CameraSongScriptDetector: Hooked into StandardLevelDetailViewController.");
            }

            _statusView?.SetLevelDetailVisible(_standardLevelDetail != null && _standardLevelDetail.isActiveAndEnabled);
            
            // データロードの初期化を開始
            _ = SongSettingsManager.InitializeAsync();
        }

        public void Dispose()
        {
            if (_standardLevelDetail != null)
            {
                _standardLevelDetail.didActivateEvent -= OnLevelDetailActivated;
                _standardLevelDetail.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
                _standardLevelDetail.didChangeContentEvent -= OnContentChanged;
                _standardLevelDetail.didDeactivateEvent -= OnLevelDetailDeactivated;
            }
        }

        private void OnLevelDetailActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _statusView?.SetLevelDetailVisible(true);
        }

        private void OnDifficultyChanged(StandardLevelDetailViewController controller)
        {
            HandleLevelSelectionChanged(controller);
        }

        private void OnContentChanged(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
        {
            UpdateCurrentSongSettings(controller?.beatmapKey);
            _scriptDetector.ResolveProfileName();

            if (controller == null || contentType == StandardLevelDetailViewController.ContentType.Inactive)
            {
                _scriptDetector.SyncCameraPlusPath();
                return;
            }

            HandleLevelSelectionChanged(controller, updateSongSettings: false);
        }

        private void HandleLevelSelectionChanged(StandardLevelDetailViewController controller, bool updateSongSettings = true)
        {
            if (updateSongSettings)
            {
                UpdateCurrentSongSettings(controller?.beatmapKey);
            }

            _scriptDetector.ResolveProfileName();
            if (IsCustomLevel(controller?.beatmapLevel))
            {
                _scriptDetector.ProcessLevel(controller.beatmapLevel);
            }
            else
            {
                _scriptDetector.ClearSelectedLevel(allowCommonScript: true);
            }

            // ProcessLevelが同一曲ガードでスキップされた場合でも、
            // プロファイル名のキャッシュを更新して再同期する
            _scriptDetector.SyncCameraPlusPath();
        }

        private static bool IsCustomLevel(BeatmapLevel beatmapLevel)
        {
            return beatmapLevel != null &&
                !string.IsNullOrEmpty(beatmapLevel.levelID) &&
                beatmapLevel.levelID.StartsWith("custom_level_", StringComparison.OrdinalIgnoreCase);
        }

        private void OnLevelDetailDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _statusView?.SetLevelDetailVisible(false);
            _previewController?.Clear();
        }

        private void UpdateCurrentSongSettings(BeatmapKey? beatmapKey)
        {
            if (beatmapKey.HasValue && beatmapKey.Value.IsValid())
            {
                BeatmapKey key = beatmapKey.Value;
                string levelId = key.levelId;
                int difficulty = (int)key.difficulty;
                string characteristic = key.beatmapCharacteristic?.serializedName;

                if (!string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(characteristic))
                {
                    SongSettingsManager.SetCurrentSong(levelId, difficulty, characteristic);
                    return;
                }
            }

            SongSettingsManager.ClearCurrentSong();
        }
    }
}
