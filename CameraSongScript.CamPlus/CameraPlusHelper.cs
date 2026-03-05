using System;
using System.Collections.Generic;
using System.Linq;
using CameraPlus;
using CameraPlus.Configuration;
using CameraPlus.HarmonyPatches;
using CameraPlus.Utilities;
using CameraSongScript.Interfaces;

namespace CameraSongScript.CamPlus
{
    /// <summary>
    /// CameraPlusのcustomLevelPathを直接参照するICameraPlusHelper実装
    /// Publicize済みのCameraPlus.dllを参照してinternalクラスにアクセスする
    /// </summary>
    public class CameraPlusHelper : ICameraPlusHelper
    {
        public bool IsInitialized => true;

        public bool Initialize()
        {
            return true;
        }

        public void SetScriptPath(string fullPath)
        {
            CustomPreviewBeatmapLevelPatch.customLevelPath = fullPath;
        }

        public string GetCurrentPath()
        {
            return CustomPreviewBeatmapLevelPatch.customLevelPath ?? string.Empty;
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
            if (string.IsNullOrEmpty(profileName) || profileName == "(Default)")
                return;

            _backedUpProfile = PluginConfig.Instance?.SongSpecificScriptProfile;
            _hasBackup = true;

            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.SongSpecificScriptProfile = profileName;
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

        #region UseAudioSync

        /// <summary>
        /// 現在のプロファイルでsongSpecificScript=trueのカメラのuseAudioSyncを返す。
        /// 複数カメラがある場合は最初に見つかったものの値を返す。
        /// songSpecificScript=trueのカメラがない場合はtrue（デフォルト）を返す。
        /// </summary>
        public bool GetUseAudioSync()
        {
            var controller = GetController();
            if (controller == null) return true;

            foreach (var cam in controller.Cameras.Values)
            {
                if (cam.Config.movementScript.songSpecificScript)
                    return cam.Config.movementScript.useAudioSync;
            }
            return true;
        }

        /// <summary>
        /// 現在のプロファイルでsongSpecificScript=trueの全カメラのuseAudioSyncを設定し保存する。
        /// </summary>
        public void SetUseAudioSync(bool value)
        {
            var controller = GetController();
            if (controller == null) return;

            foreach (var cam in controller.Cameras.Values)
            {
                if (cam.Config.movementScript.songSpecificScript)
                {
                    cam.Config.movementScript.useAudioSync = value;
                    cam.Config.Save();
                }
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
    }
}
