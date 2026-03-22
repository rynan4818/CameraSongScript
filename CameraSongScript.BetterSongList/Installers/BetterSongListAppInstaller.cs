using Zenject;

namespace CameraSongScript.BetterSongList.Installers
{
    internal sealed class BetterSongListAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<SongScriptFilter>().AsSingle();
            Container.Bind<SongScriptSorter>().AsSingle();
            Container.BindInterfacesAndSelfTo<BetterSongListHelper>().AsSingle();
            Container.BindInterfacesAndSelfTo<BetterSongListBootstrap>().AsSingle().NonLazy();
        }
    }
}
