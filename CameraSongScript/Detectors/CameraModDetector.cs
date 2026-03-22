using System;
using System.Linq;

namespace CameraSongScript.Detectors
{
    public enum CameraModType
    {
        None,
        Camera2,
        CameraPlus
    }

    /// <summary>
    /// ランタイムでCamera2またはCameraPlusのどちらがロードされているかを検出する
    /// </summary>
    public static class CameraModDetector
    {
        public static CameraModType DetectedMod { get; private set; } = CameraModType.None;

        public static bool IsCamera2 => DetectedMod == CameraModType.Camera2;
        public static bool IsCameraPlus => DetectedMod == CameraModType.CameraPlus;

        /// <summary>
        /// ロード済みアセンブリからカメラModを検出する
        /// Plugin.OnApplicationStart()のHarmonyパッチ適用前に呼び出すこと
        /// </summary>
        public static void Detect()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                string name = asm.GetName().Name;
                if (name == "Camera2")
                {
                    DetectedMod = CameraModType.Camera2;
                    Plugin.Log.Info("Detected camera mod: Camera2");
                    return;
                }
                if (name == "CameraPlus")
                {
                    DetectedMod = CameraModType.CameraPlus;
                    Plugin.Log.Info("Detected camera mod: CameraPlus");
                    return;
                }
            }
            Plugin.Log.Warn("No supported camera mod detected (Camera2 or CameraPlus).");
        }
    }
}
