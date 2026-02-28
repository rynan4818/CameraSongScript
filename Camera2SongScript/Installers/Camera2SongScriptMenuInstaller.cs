using Camera2SongScript.UI;
using Zenject;

namespace Camera2SongScript.Installers
{
    public class Camera2SongScriptMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SongScriptSettingsView>().AsSingle().NonLazy();
        }
    }
}