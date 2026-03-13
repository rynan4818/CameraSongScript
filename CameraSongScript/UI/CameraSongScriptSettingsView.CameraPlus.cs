using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
using UnityEngine;

namespace CameraSongScript.UI
{
    public partial class CameraSongScriptSettingsView
    {
        #region CameraPlus専用設定

        [UIValue("show-cameraplus-settings")]
        public bool ShowCameraPlusSettings => CameraModDetector.IsCameraPlus;

        [UIValue("profile-options")]
        public List<object> ProfileOptions
        {
            get
            {
                var list = new List<string> { UiLocalization.OptionNoChange, UiLocalization.OptionDelete };
                if (Plugin.IsCamPlusHelperReady)
                {
                    foreach (var profile in Plugin.CamPlusHelper.GetProfileList())
                    {
                        if (!string.IsNullOrEmpty(profile))
                            list.Add(profile);
                    }
                }
                return UiLocalization.LocalizeOptions(list, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
            }
        }

        [UIValue("song-script-profile")]
        public object SongScriptProfile
        {
            get
            {
                if (!Plugin.IsCamPlusHelperReady) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                string profile = CameraSongScriptDetector.ResolvedProfileName;
                if (string.IsNullOrEmpty(profile)) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                return UiLocalization.GetOptionDisplay(profile, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
            }
            set
            {
                if (!Plugin.IsCamPlusHelperReady) return;
                string profile = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
                if (string.IsNullOrEmpty(profile))
                    profile = UiLocalization.OptionNoChange;

                // グローバル設定として保存
                CameraSongScriptConfig.Instance.SongScriptProfile = profile;

                // 解決済みプロファイル名を直接更新
                CameraSongScriptDetector.SetResolvedProfileName(profile);

                // 即座に全体のパス・プロファイル状態を同期
                CameraSongScriptDetector.SyncCameraPlusPath();
            }
        }


        #endregion
    }
}
