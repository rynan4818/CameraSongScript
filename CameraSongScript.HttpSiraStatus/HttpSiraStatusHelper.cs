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

        public void SendPlayContext(CameraSongScriptPlayContext playContext)
        {
            if (!IsInitialized || playContext == null)
            {
                return;
            }

            var rootObject = new JSONObject();
            rootObject["status"] = playContext.StatusKey;

            var metadataObject = new JSONObject();
            if (playContext.HasScript)
            {
                var metadata = playContext.Metadata;
                AddString(metadataObject, "cameraScriptAuthorName", metadata?.cameraScriptAuthorName);
                AddString(metadataObject, "songName", metadata?.songName);
                AddString(metadataObject, "songSubName", metadata?.songSubName);
                AddString(metadataObject, "songAuthorName", metadata?.songAuthorName);
                AddString(metadataObject, "levelAuthorName", metadata?.levelAuthorName);
                AddString(metadataObject, "mapId", metadata?.mapId);
                AddString(metadataObject, "hash", metadata?.hash);
                AddNumber(metadataObject, "bpm", metadata?.bpm);
                AddNumber(metadataObject, "duration", metadata?.duration);
                AddNumber(metadataObject, "avatarHeight", metadata?.avatarHeight);
                AddString(metadataObject, "description", metadata?.description);
                metadataObject["cameraHeightOffsetCm"] = playContext.CameraHeightOffsetCm;
                AddString(metadataObject, "scriptFileName", playContext.ScriptFileName);
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
