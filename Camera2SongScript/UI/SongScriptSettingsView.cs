using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using Camera2.SDK;
using Camera2SongScript.Configuration;
using Camera2SongScript.HarmonyPatches;
using Zenject;

namespace Camera2SongScript.UI
{
    /// <summary>
    /// BSML設定UI
    /// MenuInstallerでバインドされ、メニューシーンで表示
    /// </summary>
    [HotReload]
    internal class SongScriptSettingsView : BSMLAutomaticViewController, IInitializable, System.IDisposable
    {
        private bool _disposedValue;
        public const string TabName = "Camera2 SongScript";
        public string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(TabName, this.ResourceName, this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    GameplaySetup.instance?.RemoveTab(TabName);
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        #region SongScript有効/無効

        [UIValue("enabled")]
        public bool Enabled
        {
            get => SongScriptConfig.Instance.Enabled;
            set => SongScriptConfig.Instance.Enabled = value;
        }

        #endregion

        #region AudioSync

        [UIValue("use-audio-sync")]
        public bool UseAudioSync
        {
            get => SongScriptConfig.Instance.UseAudioSync;
            set => SongScriptConfig.Instance.UseAudioSync = value;
        }

        #endregion

        #region TargetCameras

        [UIValue("target-cameras")]
        public string TargetCameras
        {
            get => SongScriptConfig.Instance.TargetCameras;
            set => SongScriptConfig.Instance.TargetCameras = value;
        }

        #endregion

        #region SongScript検出状態

        [UIValue("song-script-status")]
        public string SongScriptStatus
        {
            get
            {
                if (SongScriptDetector.HasSongScript)
                    return "<color=#00FF00>SongScript.json detected</color>";
                else
                    return "<color=#888888>No SongScript.json</color>";
            }
        }

        #endregion

        #region 非対応機能表示

        [UIValue("unsupported-features")]
        public string UnsupportedFeatures
        {
            get
            {
                // Playerシーンでのみ有効なのでここでは静的情報のみ
                return "";
            }
        }

        #endregion

        #region OverrideToken状態表示

        [UIValue("override-token-status")]
        public string OverrideTokenStatus
        {
            get
            {
                // Playerシーンでのみ有効なのでここでは静的情報のみ
                return "";
            }
        }

        #endregion

        #region 利用可能カメラ一覧

        [UIValue("available-cameras")]
        public string AvailableCameras
        {
            get
            {
                try
                {
                    var cams = Cameras.available?.ToList() ?? new List<string>();
                    if (cams.Count == 0)
                        return "No cameras available";
                    return string.Join(", ", cams);
                }
                catch
                {
                    return "Camera2 not ready";
                }
            }
        }

        #endregion
    }
}
