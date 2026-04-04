using CameraSongScript.Detectors;
using CameraSongScript.Interfaces;
using CameraSongScript.Services;
using CameraSongScript.UI;
using Zenject;

namespace CameraSongScript.Installers
{
    public class CameraSongScriptMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptPreviewController>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptSettingsView>().FromNewComponentAsViewController().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptStatusView>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<LevelSelectionDetector>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<CameraSongScriptModSettingView>().AsSingle().NonLazy();

            IHttpSiraStatusHelper httpSiraStatusHelper = this.Container.TryResolve<IHttpSiraStatusHelper>();
            if (httpSiraStatusHelper != null && httpSiraStatusHelper.IsInitialized)
            {
                this.Container.BindInterfacesAndSelfTo<CameraSongScriptMenuHttpSiraStatusSender>().AsSingle().NonLazy();
            }
#if DEBUG
            this.Container.BindInterfacesAndSelfTo<BSMLtestFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
        }
    }
}
