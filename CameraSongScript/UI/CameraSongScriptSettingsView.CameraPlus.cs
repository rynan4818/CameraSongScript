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
                if (_cameraPlusHelper != null && _cameraPlusHelper.IsInitialized)
                {
                    foreach (var profile in _cameraPlusHelper.GetProfileList())
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
                if (_cameraPlusHelper == null || !_cameraPlusHelper.IsInitialized) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                string profile = _scriptDetector.ResolvedProfileName;
                if (string.IsNullOrEmpty(profile)) return UiLocalization.GetOptionDisplay(UiLocalization.OptionNoChange, UiLocalization.OptionNoChange);
                return UiLocalization.GetOptionDisplay(profile, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
            }
            set
            {
                if (_cameraPlusHelper == null || !_cameraPlusHelper.IsInitialized) return;
                string profile = UiLocalization.ToCanonicalOption(value as string, UiLocalization.OptionNoChange, UiLocalization.OptionDelete);
                if (string.IsNullOrEmpty(profile))
                    profile = UiLocalization.OptionNoChange;

                // グローバル設定として保存
                CameraSongScriptConfig.Instance.SongScriptProfile = profile;

                // 解決済みプロファイル名を直接更新
                _scriptDetector.SetResolvedProfileName(profile);

                // 即座に全体のパス・プロファイル状態を同期
                _scriptDetector.SyncCameraPlusPath();
            }
        }


        #endregion
    }
}
