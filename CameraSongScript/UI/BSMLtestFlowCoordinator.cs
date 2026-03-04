using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;

namespace CameraSongScript.UI
{
    public class BSMLtestFlowCoordinator : FlowCoordinator, IInitializable
    {
        private CameraSongScriptSettingsView _cameraSongScriptSettingsView;
        private MenuButton _menuButton;

        [Inject]
        public void Constractor(CameraSongScriptSettingsView cameraSongScriptSettingsView)
        {
            this._cameraSongScriptSettingsView = cameraSongScriptSettingsView;
        }

        public void Initialize()
        {
            this._menuButton = new MenuButton("CameraSongScriptDebug", "", this.ShowMainFlowCoodniator);
            MenuButtons.instance?.RegisterButton(this._menuButton);
        }

        public void OnDestroy()
        {
            MenuButtons.instance?.UnregisterButton(this._menuButton);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                this.SetTitle("CameraSongScriptDebug");
                this.showBackButton = true;
                // topScreenViewControllerを使用するとタイトルとバックボタンが消える（たぶん自前で実装が必要）
                this.ProvideInitialViewControllers(this._cameraSongScriptSettingsView);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
            base.BackButtonWasPressed(topViewController);
        }

        private void ShowMainFlowCoodniator()
        {
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(this);
        }
    }
}
