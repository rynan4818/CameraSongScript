using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CameraPlus;
using CameraPlus.Configuration;
using CameraPlus.HarmonyPatches;
using CameraPlus.Utilities;
using CameraSongScript.Interfaces;
using HarmonyLib;

namespace CameraSongScript.CamPlus
{
    /// <summary>
    /// CameraPlusのcustomLevelPathを制御するICameraPlusHelper実装
    /// Publicize済みのCameraPlus.dllを参照してinternalクラスにアクセスする
    ///
    /// タイミング保証:
    /// SetScriptPath()で受け取ったパスは _pendingScriptPath に保持され、
    /// CameraPlusController.OnActiveSceneChanged の Harmony Prefix で
    /// customLevelPath に書き込まれる。これにより、CameraPlusがシーン遷移時に
    /// customLevelPath を参照する前に、必ずCameraSongScript側の値が反映される。
    /// </summary>
    public class CameraPlusHelper : ICameraPlusHelper
    {
        private static string _pendingScriptPath = string.Empty;
        private static Harmony _harmony;

        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            try
            {
                _harmony = new Harmony("com.github.rynan4818.CameraSongScript.CamPlus");

                var original = typeof(CameraPlusController).GetMethod(
                    nameof(CameraPlusController.OnActiveSceneChanged),
                    BindingFlags.Public | BindingFlags.Instance);

                if (original == null)
                    return false;

                var prefix = typeof(CameraPlusHelper).GetMethod(
                    nameof(OnActiveSceneChangedPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
                IsInitialized = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// CameraPlusに注入するスクリプトパスを設定する（Pending方式）
        /// 実際の customLevelPath への書き込みは OnActiveSceneChanged の Prefix で行われる
        /// </summary>
        public void SetScriptPath(string fullPath)
        {
            _pendingScriptPath = fullPath ?? string.Empty;
        }

        public string GetCurrentPath()
        {
            return _pendingScriptPath;
        }

        /// <summary>
        /// CameraPlusController.OnActiveSceneChanged の直前に実行される Harmony Prefix
        /// CameraPlusがcustomLevelPathを参照する前に、CameraSongScript側の値を確実に反映する
        /// </summary>
        private static void OnActiveSceneChangedPrefix()
        {
            CustomPreviewBeatmapLevelPatch.customLevelPath = _pendingScriptPath;
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
