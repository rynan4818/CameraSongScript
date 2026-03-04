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

        public IReadOnlyList<string> GetProfileList()
        {
            return CameraUtilities.ProfileList().ToList();
        }

        public string GetSongSpecificScriptProfile()
        {
            return PluginConfig.Instance?.SongSpecificScriptProfile ?? string.Empty;
        }

        public void SetSongSpecificScriptProfile(string profileName)
        {
            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.SongSpecificScriptProfile = profileName ?? string.Empty;
                PluginConfig.Instance.Changed();
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

        #region SongSpecificScript

        /// <summary>
        /// 全てのカメラの名前のリストを返す
        /// </summary>
        public IReadOnlyList<string> GetAllCameras()
        {
            var controller = GetController();
            if (controller == null) return new List<string>();

            return controller.Cameras.Keys.ToList();
        }

        /// <summary>
        /// 全カメラの名前とsongSpecificScript設定の状態を表示用文字列で返す。
        /// 例: "cameraplus: ON, second: OFF"
        /// </summary>
        public string GetSongSpecificScriptStatus()
        {
            var controller = GetController();
            if (controller == null) return "CameraPlus not ready";

            var parts = new List<string>();
            foreach (var kvp in controller.Cameras)
            {
                bool enabled = kvp.Value.Config.movementScript.songSpecificScript;
                parts.Add($"{kvp.Key}: {(enabled ? "ON" : "OFF")}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "No cameras";
        }

        /// <summary>
        /// songSpecificScript=trueのカメラ名をカンマ区切りで返す。
        /// </summary>
        public string GetSongSpecificScriptCameras()
        {
            var controller = GetController();
            if (controller == null) return string.Empty;

            var names = new List<string>();
            foreach (var kvp in controller.Cameras)
            {
                if (kvp.Value.Config.movementScript.songSpecificScript)
                    names.Add(kvp.Key);
            }
            return string.Join(", ", names);
        }

        /// <summary>
        /// 指定されたカメラ名（カンマ区切り）のsongSpecificScriptをtrueに、
        /// それ以外のカメラはfalseに設定して保存する。
        /// </summary>
        public void SetSongSpecificScriptCameras(string cameraNames)
        {
            var controller = GetController();
            if (controller == null) return;

            var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(cameraNames))
            {
                foreach (var name in cameraNames.Split(','))
                {
                    var trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        targetNames.Add(trimmed);
                }
            }

            foreach (var kvp in controller.Cameras)
            {
                bool shouldEnable = targetNames.Contains(kvp.Key);
                if (kvp.Value.Config.movementScript.songSpecificScript != shouldEnable)
                {
                    kvp.Value.Config.movementScript.songSpecificScript = shouldEnable;
                    kvp.Value.Config.Save();
                }
            }
        }

        #endregion
    }
}
