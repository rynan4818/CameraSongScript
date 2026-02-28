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
        }
    }
}
