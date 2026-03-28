using System.Reflection;
using CameraPlus;
using CameraPlus.HarmonyPatches;
using HarmonyLib;

namespace CameraSongScript.CamPlus.HarmonyPatches
{
    /// <summary>
    /// CameraPlus の DelayedActiveSceneChanged coroutine 本体でも PendingScriptPath を反映し、
    /// SongScript プロファイル切替判定に CameraSongScript 側の有効/無効状態を反映させる。
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedActiveSceneChangedPatch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo iteratorMethod = AccessTools.Method(typeof(CameraPlusController), nameof(CameraPlusController.DelayedActiveSceneChanged));
            return iteratorMethod == null ? null : AccessTools.EnumeratorMoveNext(iteratorMethod);
        }

        private static void Prefix()
        {
            SongScriptBeatmapPatch.customLevelPath = CameraPlusHelper.PendingScriptPath;
        }
    }
}
