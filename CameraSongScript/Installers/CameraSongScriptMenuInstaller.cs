using CameraSongScript.UI;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            //this.Container.BindInterfacesAndSelfTo<BSMLtestFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptSettingsView>().FromNewComponentAsViewController().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptStatusView>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        }
    }
}
