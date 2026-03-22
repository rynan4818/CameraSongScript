using Zenject;

namespace CameraSongScript.CamPlus.Installers
{
    internal sealed class CameraSongScriptCamPlusAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<CameraPlusHelper>().AsSingle();
            Container.BindInterfacesAndSelfTo<CameraSongScriptCamPlusBootstrap>().AsSingle().NonLazy();
        }
    }
}
