using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using HttpSiraStatus.Enums;
using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;

namespace CameraSongScript.HttpSiraStatus
{
    /// <summary>
    /// HttpSiraStatusへCameraSongScriptの状態を送信するアダプタ
    /// </summary>
    public class HttpSiraStatusHelper : IHttpSiraStatusHelper
    {
        private readonly IStatusManager _statusManager;

        public bool IsInitialized { get; private set; }

        internal HttpSiraStatusHelper(IStatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        public bool Initialize()
        {
            IsInitialized = _statusManager != null;
            return IsInitialized;
        }

        public void SendStatusSnapshot(CameraSongScriptStatusSnapshot snapshot)
        {
            if (!IsInitialized || snapshot == null)
            {
                return;
            }

            var rootObject = new JSONObject();
            rootObject["sceneContext"] = snapshot.SceneContext ?? string.Empty;
            rootObject["updateReason"] = snapshot.UpdateReason ?? string.Empty;
            rootObject["status"] = snapshot.StatusKey;
            rootObject["isResolved"] = snapshot.IsResolved;
            rootObject["scriptFileName"] = snapshot.ScriptFileName ?? string.Empty;
            rootObject["cameraHeightOffsetCm"] = snapshot.CameraHeightOffsetCm;

            var metadataObject = new JSONObject();
            var metadata = snapshot.Metadata;
            if (metadata != null)
            {
                AddString(metadataObject, "cameraScriptAuthorName", metadata.cameraScriptAuthorName);
                AddString(metadataObject, "songName", metadata.songName);
                AddString(metadataObject, "songSubName", metadata.songSubName);
                AddString(metadataObject, "songAuthorName", metadata.songAuthorName);
                AddString(metadataObject, "levelAuthorName", metadata.levelAuthorName);
                AddString(metadataObject, "mapId", metadata.mapId);
                AddString(metadataObject, "hash", metadata.hash);
                AddNumber(metadataObject, "bpm", metadata.bpm);
                AddNumber(metadataObject, "duration", metadata.duration);
                AddNumber(metadataObject, "avatarHeight", metadata.avatarHeight);
                AddString(metadataObject, "description", metadata.description);
            }

            rootObject["metadata"] = metadataObject;

            _statusManager.OtherJSON["CameraSongScript"] = rootObject;
            _statusManager.EmitStatusUpdate(ChangedProperty.Other, BeatSaberEvent.Other);
        }

        private static void AddString(JSONObject parent, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parent[key] = value;
            }
        }

        private static void AddNumber(JSONObject parent, string key, double? value)
        {
            if (value.HasValue)
            {
                parent[key] = value.Value;
            }
        }
    }
}
