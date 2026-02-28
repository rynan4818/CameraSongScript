using System;
using System.Linq;
using System.Reflection;

namespace CameraSongScript.Helpers
{
    /// <summary>
    /// CameraPlusのCustomPreviewBeatmapLevelPatch.customLevelPath静的フィールドを
    /// リフレクションで操作し、ユーザー選択のスクリプトファイルパスに差し替える
    /// </summary>
    public static class CameraPlusHarmonyHelper
    {
        private static FieldInfo _customLevelPathField;
        private static bool _initialized = false;

        public static bool IsInitialized => _initialized;

        /// <summary>
        /// CameraPlusアセンブリからcustomLevelPathフィールドを取得して初期化する
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                var cpAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "CameraPlus");

                if (cpAssembly == null)
                {
                    Plugin.Log.Error("CameraPlus assembly not found");
                    return false;
                }

                var patchType = cpAssembly.GetType("CameraPlus.HarmonyPatches.CustomPreviewBeatmapLevelPatch");
                if (patchType == null)
                {
                    Plugin.Log.Error("CameraPlus CustomPreviewBeatmapLevelPatch type not found");
                    return false;
                }

                _customLevelPathField = patchType.GetField("customLevelPath",
                    BindingFlags.Public | BindingFlags.Static);

                if (_customLevelPathField == null)
                {
                    Plugin.Log.Error("CameraPlus customLevelPath field not found");
                    return false;
                }

                _initialized = true;
                Plugin.Log.Info("CameraPlusHarmonyHelper initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraPlusHarmonyHelper init failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// CameraPlusのcustomLevelPathを指定のスクリプトファイルパスに設定する
        /// CameraPlusのAddMovementScript()がこのパスを使用してスクリプトを読み込む
        /// </summary>
        public static void SetScriptPath(string fullPath)
        {
            if (!_initialized) return;
            _customLevelPathField.SetValue(null, fullPath);
            Plugin.Log.Info($"CameraPlus script path set to: {fullPath}");
        }

        /// <summary>
        /// CameraPlusの現在のcustomLevelPathを取得する
        /// </summary>
        public static string GetCurrentPath()
        {
            if (!_initialized) return string.Empty;
            return _customLevelPathField.GetValue(null) as string ?? string.Empty;
        }
    }
}
