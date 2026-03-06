using System;
using System.Collections.Generic;
using CameraSongScript.Models;
using HMUI;
using UnityEngine;
using Zenject;

namespace CameraSongScript.Detectors
{
    public class LevelSelectionDetector : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetail;

        public LevelSelectionDetector(StandardLevelDetailViewController standardLevelDetail)
        {
            _standardLevelDetail = standardLevelDetail;
        }

        public void Initialize()
        {
            if (_standardLevelDetail != null)
            {
                _standardLevelDetail.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
                _standardLevelDetail.didChangeContentEvent += OnContentChanged;
                Plugin.Log.Info("CameraSongScriptDetector: Hooked into StandardLevelDetailViewController.");
            }
            
            // データロードの初期化を開始
            _ = SongSettingsManager.InitializeAsync();
        }

        public void Dispose()
        {
            if (_standardLevelDetail != null)
            {
                _standardLevelDetail.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
                _standardLevelDetail.didChangeContentEvent -= OnContentChanged;
            }
        }

        private void OnDifficultyChanged(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
        {
            UpdateCurrentSongSettings(beatmap);
            if (beatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                CameraSongScriptDetector.ProcessLevel(customLevel);
            }
        }

        private void OnContentChanged(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
        {
            UpdateCurrentSongSettings(controller?.selectedDifficultyBeatmap);
            if (controller?.selectedDifficultyBeatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                CameraSongScriptDetector.ProcessLevel(customLevel);
            }
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
