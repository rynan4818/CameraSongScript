namespace CameraSongScript.Models
{
    public enum CameraSongScriptPlaybackStatus
    {
        None,
        SongScript,
        CommonScript
    }

    /// <summary>
    /// プレイ開始時に実際に適用されるスクリプト情報を保持する
    /// </summary>
    public sealed class CameraSongScriptPlayContext
    {
        private CameraSongScriptPlayContext(
            CameraSongScriptPlaybackStatus status,
            string scriptPath,
            string scriptFileName,
            MetadataElements metadata,
            int cameraHeightOffsetCm)
        {
            Status = status;
            ScriptPath = scriptPath ?? string.Empty;
            ScriptFileName = scriptFileName ?? string.Empty;
            Metadata = metadata;
            CameraHeightOffsetCm = cameraHeightOffsetCm;
        }

        public CameraSongScriptPlaybackStatus Status { get; }
        public string ScriptPath { get; }
        public string ScriptFileName { get; }
        public MetadataElements Metadata { get; }
        public int CameraHeightOffsetCm { get; }

        public bool HasScript => Status != CameraSongScriptPlaybackStatus.None && !string.IsNullOrEmpty(ScriptPath);

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

        public static CameraSongScriptPlayContext CreateNone()
        {
            return new CameraSongScriptPlayContext(
                CameraSongScriptPlaybackStatus.None,
                string.Empty,
                string.Empty,
                null,
                0);
        }

        public static CameraSongScriptPlayContext CreateSongScript(
            string scriptPath,
            string scriptFileName,
            MetadataElements metadata,
            int cameraHeightOffsetCm)
        {
            return new CameraSongScriptPlayContext(
                CameraSongScriptPlaybackStatus.SongScript,
                scriptPath,
                scriptFileName,
                metadata,
                cameraHeightOffsetCm);
        }

        public static CameraSongScriptPlayContext CreateCommonScript(
            string scriptPath,
            string scriptFileName,
            MetadataElements metadata,
            int cameraHeightOffsetCm)
        {
            return new CameraSongScriptPlayContext(
                CameraSongScriptPlaybackStatus.CommonScript,
                scriptPath,
                scriptFileName,
                metadata,
                cameraHeightOffsetCm);
        }
    }
}
