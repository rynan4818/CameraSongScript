using CameraSongScript.Detectors;
using CameraSongScript.Gameplay;
using CameraSongScript.Services;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptPlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            Plugin.EnsureHttpSiraStatusHelperLoaded();

            this.Container.Bind<CameraSongScriptPlayContextResolver>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<SongScriptGameplayScanPauseController>().AsSingle().NonLazy();

            if (Plugin.IsHttpSiraStatusHelperReady)
            {
                this.Container.BindInterfacesAndSelfTo<CameraSongScriptHttpSiraStatusSender>().AsSingle().NonLazy();
            }

            // CameraSongScriptControllerはCamera2モードのみバインド
            // CameraPlusモードではCameraPlus自身のCameraMovement.csがスクリプトを実行する
            if (CameraModDetector.IsCamera2)
            {
                if (Plugin.IsCamHelperReady)
                {
                    this.Container.BindInterfacesAndSelfTo<CameraSongScriptController>().AsSingle().NonLazy();
                }
                else
                {
                    Plugin.Log?.Warn("CameraSongScriptController binding skipped because the Camera2 helper is not ready.");
                }
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                // CameraPlusモード: プレイ開始時にランダム汎用スクリプトを解決する
                this.Container.BindInterfacesAndSelfTo<CameraPlusPlayStartResolver>().AsSingle().NonLazy();
            }
        }
    }
}
