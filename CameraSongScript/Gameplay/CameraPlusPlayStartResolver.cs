using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
using System;
using Zenject;

namespace CameraSongScript.Gameplay
{
    /// <summary>
    /// CameraPlusモード用のプレイ終了時ランダム再抽選処理
    /// PlayerInstallerでバインドされ、GameCoreシーン終了時に次回プレイ用のランダム汎用スクリプトを再選択する
    ///
    /// CameraPlusはシーン遷移時（OnActiveSceneChanged）にスクリプトパスを読み込むため、
    /// プレイ開始時ではなく、プレイ終了時（Dispose）に次回用のパスを事前に確定させる
    /// </summary>
    public class CameraPlusPlayStartResolver : IDisposable
    {
        private readonly CameraSongScriptDetector _scriptDetector;

        internal CameraPlusPlayStartResolver(CameraSongScriptDetector scriptDetector)
        {
            _scriptDetector = scriptDetector;
        }

        public void Dispose()
        {
            if (!_scriptDetector.IsUsingCommonScript)
                return;

            // プレイ終了時にランダム汎用スクリプトを再抽選し、次回プレイに備える
            if (CameraSongScriptConfig.Instance.SelectedCommonScript == UiLocalization.OptionRandom)
            {
                _scriptDetector.ResolveAndSetCommonScriptPath();
                _scriptDetector.SyncCameraPlusPath();
                Plugin.Log.Info($"CameraPlusPlayStartResolver: Re-randomized common script for next play: {_scriptDetector.ResolvedCommonScriptDisplayName}");
            }
        }
    }
}
