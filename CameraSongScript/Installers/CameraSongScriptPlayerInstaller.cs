using CameraSongScript.Detectors;
using CameraSongScript.Gameplay;
using CameraSongScript.Interfaces;
using CameraSongScript.Services;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptPlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.Bind<CameraSongScriptPlayContextResolver>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<SongScriptGameplayScanPauseController>().AsSingle().NonLazy();

            IHttpSiraStatusHelper httpSiraStatusHelper = this.Container.TryResolve<IHttpSiraStatusHelper>();
            if (httpSiraStatusHelper != null && httpSiraStatusHelper.IsInitialized)
            {
                this.Container.BindInterfacesAndSelfTo<CameraSongScriptHttpSiraStatusSender>().AsSingle().NonLazy();
            }

            // CameraSongScriptControllerはCamera2モードのみバインド
            // CameraPlusモードではCameraPlus自身のCameraMovement.csがスクリプトを実行する
            if (CameraModDetector.IsCamera2)
            {
                ICameraHelper cameraHelper = this.Container.TryResolve<ICameraHelper>();
                if (cameraHelper != null && cameraHelper.IsInitialized)
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
                ICameraPlusHelper cameraPlusHelper = this.Container.TryResolve<ICameraPlusHelper>();
                if (cameraPlusHelper != null && cameraPlusHelper.IsInitialized)
                {
                    // CameraPlusモード: プレイ開始時にランダム汎用スクリプトを解決する
                    this.Container.BindInterfacesAndSelfTo<CameraPlusPlayStartResolver>().AsSingle().NonLazy();
                }
                else
                {
                    Plugin.Log?.Warn("CameraPlusPlayStartResolver binding skipped because the CameraPlus helper is not ready.");
                }
            }
        }
    }
}
