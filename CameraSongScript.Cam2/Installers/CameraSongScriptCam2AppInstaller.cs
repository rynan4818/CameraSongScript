using Zenject;

namespace CameraSongScript.Cam2.Installers
{
    internal sealed class CameraSongScriptCam2AppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<Camera2Helper>().AsSingle();
            Container.BindInterfacesAndSelfTo<CameraSongScriptCam2Bootstrap>().AsSingle().NonLazy();
        }
    }
}
