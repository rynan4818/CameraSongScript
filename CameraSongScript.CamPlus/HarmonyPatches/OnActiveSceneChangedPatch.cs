using CameraPlus;
using CameraPlus.HarmonyPatches;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace CameraSongScript.CamPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(CameraPlusController), nameof(CameraPlusController.OnActiveSceneChanged))]
    internal static class OnActiveSceneChangedPatch
    {
        private static void Prefix(Scene from, Scene to)
        {
            CustomPreviewBeatmapLevelPatch.customLevelPath = CameraPlusHelper.PendingScriptPath;

            if (to.name == "GameCore" && string.IsNullOrEmpty(CameraPlusHelper.PendingScriptPath))
            {
                CameraPlusHelper.RefreshActiveSongSpecificCameras();
            }
        }
    }
}
