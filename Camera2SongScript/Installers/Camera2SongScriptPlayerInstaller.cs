using Zenject;

namespace Camera2SongScript.Installers
{
    public class Camera2SongScriptPlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SongScriptController>().AsSingle().NonLazy();
        }
    }
}