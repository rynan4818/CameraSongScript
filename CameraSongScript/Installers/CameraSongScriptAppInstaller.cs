using CameraSongScript.Detectors;
using CameraSongScript.Services;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.Bind<CameraSongScriptDetector>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<SongScriptBeatmapIndexService>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<SongScriptMissingBeatmapDownloadService>().AsSingle().NonLazy();
        }
    }
}
