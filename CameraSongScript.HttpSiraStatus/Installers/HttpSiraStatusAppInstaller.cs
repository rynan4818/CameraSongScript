using Zenject;

namespace CameraSongScript.HttpSiraStatus.Installers
{
    internal sealed class HttpSiraStatusAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<HttpSiraStatusHelper>().AsSingle();
            Container.BindInterfacesAndSelfTo<HttpSiraStatusBootstrap>().AsSingle().NonLazy();
        }
    }
}
