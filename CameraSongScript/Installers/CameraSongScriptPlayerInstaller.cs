using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptPlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            // CameraSongScriptControllerはCamera2モードのみバインド
            // CameraPlusモードではCameraPlus自身のCameraMovement.csがスクリプトを実行する
            if (CameraModDetector.IsCamera2)
            {
                this.Container.BindInterfacesAndSelfTo<CameraSongScriptController>().AsSingle().NonLazy();
            }
            else if (CameraModDetector.IsCameraPlus)
            {
                // CameraPlusモード: プレイ開始時にランダム汎用スクリプトを解決する
                this.Container.BindInterfacesAndSelfTo<CameraPlusPlayStartResolver>().AsSingle().NonLazy();
            }
        }
    }
}
