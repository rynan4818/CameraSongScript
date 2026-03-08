using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using System;
using Zenject;

namespace CameraSongScript
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
        public void Dispose()
        {
            if (!CameraSongScriptDetector.IsUsingCommonScript)
                return;

            // プレイ終了時にランダム汎用スクリプトを再抽選し、次回プレイに備える
            if (CameraSongScriptConfig.Instance.SelectedCommonScript == "(Random)")
            {
                CameraSongScriptDetector.ResolveAndSetCommonScriptPath();
                CameraSongScriptDetector.SyncCameraPlusPath();
                Plugin.Log.Info($"CameraPlusPlayStartResolver: Re-randomized common script for next play: {CameraSongScriptDetector.ResolvedCommonScriptDisplayName}");
            }
        }
    }
}
