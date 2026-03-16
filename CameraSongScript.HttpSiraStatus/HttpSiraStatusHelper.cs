using System;
using System.Linq;
using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using HttpSiraStatus.Enums;
using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using UnityEngine;
using Zenject;

namespace CameraSongScript.HttpSiraStatus
{
    /// <summary>
    /// HttpSiraStatusへCameraSongScriptの状態を送信するアダプタ
    /// </summary>
    public class HttpSiraStatusHelper : IHttpSiraStatusHelper
    {
        private IStatusManager _statusManager;

        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            IsInitialized = true;
            return true;
        }

        public void SendPlayContext(CameraSongScriptPlayContext playContext)
        {
            if (!IsInitialized || playContext == null)
            {
                return;
            }

            var statusManager = ResolveStatusManager();
            if (statusManager == null)
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

            statusManager.OtherJSON["CameraSongScript"] = rootObject;
            statusManager.EmitStatusUpdate(ChangedProperty.Other, BeatSaberEvent.Other);
        }

        private IStatusManager ResolveStatusManager()
        {
            if (_statusManager != null)
            {
                return _statusManager;
            }

            try
            {
                var gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();
                _statusManager = TryResolveStatusManager(gameScenesManager?.currentScenesContainer);
                if (_statusManager != null)
                {
                    return _statusManager;
                }

                foreach (var sceneContext in Resources.FindObjectsOfTypeAll<SceneContext>())
                {
                    _statusManager = TryResolveStatusManager(sceneContext.Container);
                    if (_statusManager != null)
                    {
                        return _statusManager;
                    }
                }

                if (ProjectContext.HasInstance)
                {
                    _statusManager = TryResolveStatusManager(ProjectContext.Instance.Container);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CameraSongScript.HttpSiraStatus] Failed to resolve IStatusManager: {ex}");
            }

            return _statusManager;
        }

        private static IStatusManager TryResolveStatusManager(DiContainer container)
        {
            return container?.TryResolve<IStatusManager>();
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
