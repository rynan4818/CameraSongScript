namespace CameraSongScript.Models
{
    /// <summary>
    /// HttpSiraStatus へ送信する現在状態のスナップショット
    /// </summary>
    public sealed class CameraSongScriptStatusSnapshot
    {
        public const string SceneContextMenu = "menu";
        public const string SceneContextPlayer = "player";

        public const string UpdateReasonSelectionChanged = "selectionChanged";
        public const string UpdateReasonSelectionCleared = "selectionCleared";
        public const string UpdateReasonCommonScriptChanged = "commonScriptChanged";
        public const string UpdateReasonPlayStart = "playStart";

        private CameraSongScriptStatusSnapshot(
            string sceneContext,
            string updateReason,
            CameraSongScriptPlaybackStatus status,
            bool isResolved,
            string scriptFileName,
            int cameraHeightOffsetCm,
            MetadataElements metadata)
        {
            SceneContext = sceneContext ?? string.Empty;
            UpdateReason = updateReason ?? string.Empty;
            Status = status;
            IsResolved = isResolved;
            ScriptFileName = scriptFileName ?? string.Empty;
            CameraHeightOffsetCm = cameraHeightOffsetCm;
            Metadata = metadata;
        }

        public string SceneContext { get; }
        public string UpdateReason { get; }
        public CameraSongScriptPlaybackStatus Status { get; }
        public bool IsResolved { get; }
        public string ScriptFileName { get; }
        public int CameraHeightOffsetCm { get; }
        public MetadataElements Metadata { get; }

        public string StatusKey
        {
            get
            {
                switch (Status)
                {
                    case CameraSongScriptPlaybackStatus.SongScript:
                        return "songScript";
                    case CameraSongScriptPlaybackStatus.CommonScript:
                        return "commonScript";
                    default:
                        return "none";
                }
            }
        }

        public static CameraSongScriptStatusSnapshot Create(
            string sceneContext,
            string updateReason,
            CameraSongScriptPlaybackStatus status,
            bool isResolved,
            string scriptFileName,
            int cameraHeightOffsetCm,
            MetadataElements metadata)
        {
            return new CameraSongScriptStatusSnapshot(
                sceneContext,
                updateReason,
                status,
                isResolved,
                scriptFileName,
                cameraHeightOffsetCm,
                metadata);
        }

        public static CameraSongScriptStatusSnapshot CreateNone(string sceneContext, string updateReason)
        {
            return new CameraSongScriptStatusSnapshot(
                sceneContext,
                updateReason,
                CameraSongScriptPlaybackStatus.None,
                true,
                string.Empty,
                0,
                null);
        }

        public static CameraSongScriptStatusSnapshot CreatePlayerSnapshot(string updateReason, CameraSongScriptPlayContext playContext)
        {
            if (playContext == null || !playContext.HasScript)
            {
                return CreateNone(SceneContextPlayer, updateReason);
            }

            return new CameraSongScriptStatusSnapshot(
                SceneContextPlayer,
                updateReason,
                playContext.Status,
                true,
                playContext.ScriptFileName,
                playContext.CameraHeightOffsetCm,
                playContext.Metadata);
        }
    }
}
