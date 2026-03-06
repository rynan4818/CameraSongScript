using CameraSongScript.Detectors;
using CameraSongScript.UI;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
#if DEBUG
            this.Container.BindInterfacesAndSelfTo<BSMLtestFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptSettingsView>().FromNewComponentAsViewController().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptStatusView>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<LevelSelectionDetector>().AsSingle().NonLazy();
        }
    }
}
