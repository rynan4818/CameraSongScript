using System;
using System.Collections.Generic;
using System.Linq;
using CameraPlus;
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
