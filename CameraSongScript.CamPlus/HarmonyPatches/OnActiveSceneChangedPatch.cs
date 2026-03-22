using CameraPlus;
using CameraPlus.HarmonyPatches;
using HarmonyLib;

namespace CameraSongScript.CamPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(CameraPlusController), nameof(CameraPlusController.OnActiveSceneChanged))]
    internal static class OnActiveSceneChangedPatch
    {
        private static void Prefix()
        {
            SongScriptBeatmapPatch.customLevelPath = CameraPlusHelper.PendingScriptPath;
        }
    }
}
