using System.IO;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Interfaces;
using CameraSongScript.Localization;
using CameraSongScript.Models;
using Zenject;

namespace CameraSongScript.Gameplay
{
    /// <summary>
    /// プレイシーン内で一度だけ実効スクリプト情報を確定させ、同じ結果を共有する
    /// </summary>
    public class CameraSongScriptPlayContextResolver
    {
        private readonly CameraSongScriptDetector _scriptDetector;
        private readonly ICameraHelper _cameraHelper;
        private readonly ICameraPlusHelper _cameraPlusHelper;
        private CameraSongScriptPlayContext _resolvedContext;

        internal CameraSongScriptPlayContextResolver(
            CameraSongScriptDetector scriptDetector,
            [InjectOptional] ICameraHelper cameraHelper,
            [InjectOptional] ICameraPlusHelper cameraPlusHelper)
        {
            _scriptDetector = scriptDetector;
            _cameraHelper = cameraHelper;
            _cameraPlusHelper = cameraPlusHelper;
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

            if (CameraModDetector.IsCamera2 && (_cameraHelper == null || !_cameraHelper.IsInitialized))
            {
                Plugin.Log.Error("SongScript: Camera2 adapter is not initialized.");
                return CameraSongScriptPlayContext.CreateNone();
            }

            if (CameraModDetector.IsCameraPlus && (_cameraPlusHelper == null || !_cameraPlusHelper.IsInitialized))
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
            bool isRandomCommonScript = CameraSongScriptConfig.Instance.SelectedCommonScript == UiLocalization.OptionRandom;

            // Camera2 はプレイ開始時に実際のスクリプトを解決するため、毎回ここで再抽選する。
            // CameraPlus はシーン遷移前に path を読むので、従来どおり前回プレイ終了時に
            // 次回用の抽選結果を仕込んだ値を優先し、未解決時のみここで補完する。
            if ((CameraModDetector.IsCamera2 && isRandomCommonScript) ||
                string.IsNullOrEmpty(_scriptDetector.ResolvedCommonScriptPath))
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
