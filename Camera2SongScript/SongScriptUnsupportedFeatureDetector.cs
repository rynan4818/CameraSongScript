using System.Collections.Generic;
using Camera2SongScript.Models;

namespace Camera2SongScript
{
    /// <summary>
    /// SongScript.json内の非対応機能（Camera2にない機能）を検出するクラス
    /// </summary>
    public static class SongScriptUnsupportedFeatureDetector
    {
        /// <summary>
        /// 非対応機能の情報
        /// </summary>
        public class UnsupportedFeatureInfo
        {
            public bool HasCameraEffect { get; set; }
            public bool HasWindowControl { get; set; }

            public bool HasAnyUnsupportedFeature => HasCameraEffect || HasWindowControl;

            public List<string> GetUnsupportedFeatureNames()
            {
                var names = new List<string>();
                if (HasCameraEffect) names.Add("CameraEffect (DoF, Wipe, Outline, Glitch)");
                if (HasWindowControl) names.Add("WindowControl");
                return names;
            }
        }

        /// <summary>
        /// SongScriptDataから非対応機能の情報を取得
        /// </summary>
        public static UnsupportedFeatureInfo Detect(SongScriptData data)
        {
            if (data == null)
                return new UnsupportedFeatureInfo();

            return new UnsupportedFeatureInfo
            {
                HasCameraEffect = data.ContainsCameraEffect,
                HasWindowControl = data.ContainsWindowControl
            };
        }
    }
}
