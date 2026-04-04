using System;
using CameraSongScript.Detectors;
using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using Zenject;

namespace CameraSongScript.Services
{
    /// <summary>
    /// メニューシーン中の CameraSongScript 状態を HttpSiraStatus へ送信する
    /// </summary>
    public sealed class CameraSongScriptMenuHttpSiraStatusSender : IInitializable, IDisposable
    {
        private readonly CameraSongScriptDetector _scriptDetector;
        private readonly IHttpSiraStatusHelper _httpSiraStatusHelper;

        internal CameraSongScriptMenuHttpSiraStatusSender(
            CameraSongScriptDetector scriptDetector,
            [InjectOptional] IHttpSiraStatusHelper httpSiraStatusHelper)
        {
            _scriptDetector = scriptDetector;
            _httpSiraStatusHelper = httpSiraStatusHelper;
        }

        public void Initialize()
        {
            if (_httpSiraStatusHelper == null || !_httpSiraStatusHelper.IsInitialized)
            {
                return;
            }

            _scriptDetector.StatusSnapshotChanged += OnStatusSnapshotChanged;
            SendSnapshot(GetInitialUpdateReason());
        }

        public void Dispose()
        {
            if (_scriptDetector != null)
            {
                _scriptDetector.StatusSnapshotChanged -= OnStatusSnapshotChanged;
            }
        }

        private void OnStatusSnapshotChanged(string updateReason)
        {
            SendSnapshot(updateReason);
        }

        private void SendSnapshot(string updateReason)
        {
            try
            {
                _httpSiraStatusHelper.SendStatusSnapshot(_scriptDetector.CreateMenuStatusSnapshot(updateReason));
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScript: Failed to send menu HttpSiraStatus snapshot: {ex.Message}");
            }
        }

        private string GetInitialUpdateReason()
        {
            return string.IsNullOrEmpty(_scriptDetector.CurrentLevelPath)
                ? CameraSongScriptStatusSnapshot.UpdateReasonSelectionCleared
                : CameraSongScriptStatusSnapshot.UpdateReasonSelectionChanged;
        }
    }
}
