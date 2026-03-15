using System.IO;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Models;

namespace CameraSongScript
{
    /// <summary>
    /// プレイシーン内で一度だけ実効スクリプト情報を確定させ、同じ結果を共有する
    /// </summary>
    public class CameraSongScriptPlayContextResolver
    {
        private readonly CameraSongScriptDetector _scriptDetector;
        private CameraSongScriptPlayContext _resolvedContext;

        internal CameraSongScriptPlayContextResolver(CameraSongScriptDetector scriptDetector)
        {
            _scriptDetector = scriptDetector;
        }

        public CameraSongScriptPlayContext Resolve()
        {
            if (_resolvedContext != null)
            {
                return _resolvedContext;
            }

            _resolvedContext = ResolveInternal();
            return _resolvedContext;
        }

        private CameraSongScriptPlayContext ResolveInternal()
        {
            if (!CameraModDetector.IsCamera2 && !CameraModDetector.IsCameraPlus)
            {
                return CameraSongScriptPlayContext.CreateNone();
            }

            if (CameraModDetector.IsCamera2 && !Plugin.IsCamHelperReady)
            {
                Plugin.Log.Error("SongScript: Camera2 adapter is not initialized.");
                return CameraSongScriptPlayContext.CreateNone();
            }

            if (CameraModDetector.IsCameraPlus && !Plugin.IsCamPlusHelperReady)
            {
                Plugin.Log.Error("SongScript: CameraPlus adapter is not initialized.");
                return CameraSongScriptPlayContext.CreateNone();
            }

            if (_scriptDetector.IsUsingCommonScript)
            {
                return ResolveCommonScriptContext();
            }

            if (!CameraSongScriptConfig.Instance.Enabled || !_scriptDetector.HasSongScript)
            {
                return CameraSongScriptPlayContext.CreateNone();
            }

            return ResolveSongScriptContext();
        }

        private CameraSongScriptPlayContext ResolveSongScriptContext()
        {
            string scriptPath = _scriptDetector.SelectedScriptPath;
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                return CameraSongScriptPlayContext.CreateNone();
            }

            return CameraSongScriptPlayContext.CreateSongScript(
                scriptPath,
                _scriptDetector.GetSelectedScriptFileName(),
                _scriptDetector.CurrentMetadata,
                CameraSongScriptConfig.Instance.CameraHeightOffsetCm);
        }

        private CameraSongScriptPlayContext ResolveCommonScriptContext()
        {
            if (string.IsNullOrEmpty(_scriptDetector.ResolvedCommonScriptPath))
            {
                _scriptDetector.ResolveAndSetCommonScriptPath();
            }

            string scriptPath = _scriptDetector.ResolvedCommonScriptPath;
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                Plugin.Log.Warn("SongScript: Common script path could not be resolved.");
                return CameraSongScriptPlayContext.CreateNone();
            }

            if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
            {
                int savedOffset = ScriptOffsetManager.GetOffsetForScript(scriptPath);
                CameraSongScriptConfig.Instance.CameraHeightOffsetCm = savedOffset;
            }

            return CameraSongScriptPlayContext.CreateCommonScript(
                scriptPath,
                _scriptDetector.GetResolvedCommonScriptFileName(),
                _scriptDetector.ResolvedCommonMetadata,
                CameraSongScriptConfig.Instance.CameraHeightOffsetCm);
        }
    }
}
