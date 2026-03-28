using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CameraPlus;
using CameraPlus.Behaviours;
using CameraPlus.Configuration;
using CameraPlus.Utilities;
using CameraSongScript.Interfaces;

namespace CameraSongScript.CamPlus
{
    /// <summary>
    /// CameraPlusのcustomLevelPathを制御するICameraPlusHelper実装
    /// Publicize済みのCameraPlus.dllを参照してinternalクラスにアクセスする
    /// </summary>
    public class CameraPlusHelper : ICameraPlusHelper
    {
        internal static string PendingScriptPath { get; private set; } = string.Empty;

        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            IsInitialized = true;
            return true;
        }

        /// <summary>
        /// CameraPlusに注入するスクリプトパスを設定する（Pending方式）
        /// </summary>
        public void SetScriptPath(string fullPath)
        {
            PendingScriptPath = fullPath ?? string.Empty;
        }

        /// <summary>
        /// SongScript が見つからないケースで、すでに MovementScript 実行中の
        /// songSpecificScript カメラだけを CameraPlus 本来の分岐へ戻す。
        /// </summary>
        internal static void RefreshActiveSongSpecificCameras()
        {
            if (!string.IsNullOrEmpty(PendingScriptPath))
                return;

            var controller = GetController();
            if (controller == null)
                return;

            CameraPlusBehaviour[] cameras = controller.Cameras.Values
                .Where(camera =>
                    camera != null &&
                    camera.isActiveAndEnabled &&
                    camera.Config?.movementScript != null &&
                    camera.Config.movementScript.songSpecificScript &&
                    camera.GetComponentInChildren<CameraMovement>(true) != null)
                .ToArray();

            foreach (var camera in cameras)
            {
                try
                {
                    camera.ClearMovementScript();
                    string result = camera.AddMovementScript();
                    Plugin.Log.Debug(
                        $"CameraPlusHelper: Refreshed songSpecificScript camera '{Path.GetFileName(camera.Config.FilePath)}' with pending path '{PendingScriptPath}'. Result: {result}");
                }
                catch (Exception ex)
                {
                    string cameraName = camera?.Config?.FilePath != null
                        ? Path.GetFileName(camera.Config.FilePath)
                        : "(unknown)";
                    Plugin.Log.Warn($"CameraPlusHelper: Failed to refresh songSpecificScript camera '{cameraName}': {ex.Message}");
                }
            }
        }

        public string GetCurrentPath()
        {
            return PendingScriptPath;
        }

        /// <summary>
        /// CameraPlus の CameraController を取得する。未初期化の場合は null。
        /// </summary>
        private static CameraPlusController GetController()
        {
            return CameraPlus.Plugin.cameraController;
        }

        #region プロファイル

        private string _backedUpProfile = null;
        private bool _hasBackup = false;

        public IReadOnlyList<string> GetProfileList()
        {
            return CameraUtilities.ProfileList().ToList();
        }

        public void PreGameSceneCurrentSetup(string profileName)
        {
            _backedUpProfile = PluginConfig.Instance?.SongSpecificScriptProfile;
            _hasBackup = true;

            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.SongSpecificScriptProfile = profileName ?? string.Empty;
                PluginConfig.Instance.Changed();
            }
        }

        public void RestoreGameSceneSetup()
        {
            if (_hasBackup && PluginConfig.Instance != null)
            {
                PluginConfig.Instance.SongSpecificScriptProfile = _backedUpProfile ?? string.Empty;
                PluginConfig.Instance.Changed();
                _hasBackup = false;
            }
        }

        #endregion


        #region Profile Switch (Native CameraPlus Feature)

        /// <summary>
        /// 曲固有スクリプト検出時に切り替えるプロファイル名を取得する
        /// </summary>
        public string GetSongSpecificScriptProfile()
        {
            return CameraPlus.Configuration.PluginConfig.Instance.SongSpecificScriptProfile;
        }

        /// <summary>
        /// 曲固有スクリプト検出時に切り替えるプロファイル名を設定する
        /// </summary>
        public void SetSongSpecificScriptProfile(string profileName)
        {
            CameraPlus.Configuration.PluginConfig.Instance.SongSpecificScriptProfile = profileName ?? string.Empty;
        }

        #endregion

        public UnityEngine.Material GetPreviewMaterial()
        {
            var controller = GetController();
            if (controller != null && controller.Shaders.TryGetValue("BeatSaber/BlitCopyWithDepth", out var shader))
            {
                var material = new UnityEngine.Material(shader);
                material.SetFloat("_IsVRCameraOnly", 0f);
                material.renderQueue = 3000;
                return material;
            }
            return null;
        }
    }
}
