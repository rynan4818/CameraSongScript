using System;
using System.Collections.Generic;
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
            if (beatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                CameraSongScriptDetector.ProcessLevel(customLevel);
            }
        }

        private void OnContentChanged(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
        {
            if (controller?.selectedDifficultyBeatmap?.level is CustomPreviewBeatmapLevel customLevel)
            {
                CameraSongScriptDetector.ProcessLevel(customLevel);
            }
        }
    }
}
