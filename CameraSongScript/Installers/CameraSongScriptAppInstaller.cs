using CameraSongScript.Detectors;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.Bind<CameraSongScriptDetector>().AsSingle().NonLazy();
        }
    }
}
