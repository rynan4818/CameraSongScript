using CameraSongScript.UI;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptSettingsView>().AsSingle().NonLazy();
        }
    }
}
