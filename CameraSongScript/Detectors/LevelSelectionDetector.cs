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

        private void OnDifficultyChanged(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
        {
            UpdateCurrentSongSettings(beatmap);
            _scriptDetector.ResolveProfileName();
            if (beatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                _scriptDetector.ProcessLevel(customLevel);
            }
            // ProcessLevelが同一曲ガードでスキップされた場合でも、
            // プロファイル名のキャッシュを更新して再同期する
            _scriptDetector.SyncCameraPlusPath();
        }

        private void OnContentChanged(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
        {
            UpdateCurrentSongSettings(controller?.selectedDifficultyBeatmap);
            _scriptDetector.ResolveProfileName();
            if (controller?.selectedDifficultyBeatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                _scriptDetector.ProcessLevel(customLevel);
            }
            // ProcessLevelが同一曲ガードでスキップされた場合でも、
            // プロファイル名のキャッシュを更新して再同期する
            _scriptDetector.SyncCameraPlusPath();
        }

        private void OnLevelDetailDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _statusView?.SetLevelDetailVisible(false);
            _previewController?.Clear();
        }

        private void UpdateCurrentSongSettings(IDifficultyBeatmap beatmap)
        {
            if (beatmap != null)
            {
                string levelId = beatmap.level.levelID;
                int difficulty = (int)beatmap.difficulty;
                string characteristic = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
                SongSettingsManager.SetCurrentSong(levelId, difficulty, characteristic);
            }
            else
            {
                SongSettingsManager.ClearCurrentSong();
            }
        }
    }
}
