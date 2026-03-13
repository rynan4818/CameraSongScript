using System;
using UnityEngine;

namespace CameraSongScript.Configuration
{
    internal sealed class StatusPanelPresetDefinition
    {
        public StatusPanelPresetDefinition(
            string localizationKey,
            string legacyName,
            Func<CameraSongScriptConfig, Vector3> positionAccessor,
            Func<CameraSongScriptConfig, Vector3> rotationAccessor)
        {
            LocalizationKey = localizationKey;
            LegacyName = legacyName;
            PositionAccessor = positionAccessor;
            RotationAccessor = rotationAccessor;
        }

        public string LocalizationKey { get; }
        public string LegacyName { get; }
        public Func<CameraSongScriptConfig, Vector3> PositionAccessor { get; }
        public Func<CameraSongScriptConfig, Vector3> RotationAccessor { get; }
    }

    internal static class StatusPanelPresetCatalog
    {
        private static readonly StatusPanelPresetDefinition[] _definitions = new[]
        {
            new StatusPanelPresetDefinition(
                "status-panel-left-upper-right",
                "LeftUpperRight",
                cfg => new Vector3(cfg.PresetLeftUpperRightPosX, cfg.PresetLeftUpperRightPosY, cfg.PresetLeftUpperRightPosZ),
                cfg => new Vector3(cfg.PresetLeftUpperRightRotX, cfg.PresetLeftUpperRightRotY, cfg.PresetLeftUpperRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-left-upper-left",
                "LeftUpperLeft",
                cfg => new Vector3(cfg.PresetLeftUpperLeftPosX, cfg.PresetLeftUpperLeftPosY, cfg.PresetLeftUpperLeftPosZ),
                cfg => new Vector3(cfg.PresetLeftUpperLeftRotX, cfg.PresetLeftUpperLeftRotY, cfg.PresetLeftUpperLeftRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-left-lower-right",
                "LeftLowerRight",
                cfg => new Vector3(cfg.PresetLeftLowerRightPosX, cfg.PresetLeftLowerRightPosY, cfg.PresetLeftLowerRightPosZ),
                cfg => new Vector3(cfg.PresetLeftLowerRightRotX, cfg.PresetLeftLowerRightRotY, cfg.PresetLeftLowerRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-left-lower-left",
                "LeftLowerLeft",
                cfg => new Vector3(cfg.PresetLeftLowerLeftPosX, cfg.PresetLeftLowerLeftPosY, cfg.PresetLeftLowerLeftPosZ),
                cfg => new Vector3(cfg.PresetLeftLowerLeftRotX, cfg.PresetLeftLowerLeftRotY, cfg.PresetLeftLowerLeftRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-center-upper-right",
                "CenterUpperRight",
                cfg => new Vector3(cfg.PresetCenterUpperRightPosX, cfg.PresetCenterUpperRightPosY, cfg.PresetCenterUpperRightPosZ),
                cfg => new Vector3(cfg.PresetCenterUpperRightRotX, cfg.PresetCenterUpperRightRotY, cfg.PresetCenterUpperRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-center-upper-left",
                "CenterUpperLeft",
                cfg => new Vector3(cfg.PresetCenterUpperLeftPosX, cfg.PresetCenterUpperLeftPosY, cfg.PresetCenterUpperLeftPosZ),
                cfg => new Vector3(cfg.PresetCenterUpperLeftRotX, cfg.PresetCenterUpperLeftRotY, cfg.PresetCenterUpperLeftRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-center-lower-right",
                "CenterLowerRight",
                cfg => new Vector3(cfg.PresetCenterLowerRightPosX, cfg.PresetCenterLowerRightPosY, cfg.PresetCenterLowerRightPosZ),
                cfg => new Vector3(cfg.PresetCenterLowerRightRotX, cfg.PresetCenterLowerRightRotY, cfg.PresetCenterLowerRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-center-lower-left",
                "CenterLowerLeft",
                cfg => new Vector3(cfg.PresetCenterLowerLeftPosX, cfg.PresetCenterLowerLeftPosY, cfg.PresetCenterLowerLeftPosZ),
                cfg => new Vector3(cfg.PresetCenterLowerLeftRotX, cfg.PresetCenterLowerLeftRotY, cfg.PresetCenterLowerLeftRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-right-upper-right",
                "RightUpperRight",
                cfg => new Vector3(cfg.PresetRightUpperRightPosX, cfg.PresetRightUpperRightPosY, cfg.PresetRightUpperRightPosZ),
                cfg => new Vector3(cfg.PresetRightUpperRightRotX, cfg.PresetRightUpperRightRotY, cfg.PresetRightUpperRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-right-upper-left",
                "RightUpperLeft",
                cfg => new Vector3(cfg.PresetRightUpperLeftPosX, cfg.PresetRightUpperLeftPosY, cfg.PresetRightUpperLeftPosZ),
                cfg => new Vector3(cfg.PresetRightUpperLeftRotX, cfg.PresetRightUpperLeftRotY, cfg.PresetRightUpperLeftRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-right-lower-right",
                "RightLowerRight",
                cfg => new Vector3(cfg.PresetRightLowerRightPosX, cfg.PresetRightLowerRightPosY, cfg.PresetRightLowerRightPosZ),
                cfg => new Vector3(cfg.PresetRightLowerRightRotX, cfg.PresetRightLowerRightRotY, cfg.PresetRightLowerRightRotZ)),
            new StatusPanelPresetDefinition(
                "status-panel-right-lower-left",
                "RightLowerLeft",
                cfg => new Vector3(cfg.PresetRightLowerLeftPosX, cfg.PresetRightLowerLeftPosY, cfg.PresetRightLowerLeftPosZ),
                cfg => new Vector3(cfg.PresetRightLowerLeftRotX, cfg.PresetRightLowerLeftRotY, cfg.PresetRightLowerLeftRotZ))
        };

        public static int Count => _definitions.Length;

        public static int ClampIndex(int index)
        {
            return index < 0 || index >= _definitions.Length ? 0 : index;
        }

        public static string[] GetLocalizationKeys()
        {
            var keys = new string[_definitions.Length];
            for (int i = 0; i < _definitions.Length; i++)
            {
                keys[i] = _definitions[i].LocalizationKey;
            }

            return keys;
        }

        public static string[] GetLegacyNames()
        {
            var names = new string[_definitions.Length];
            for (int i = 0; i < _definitions.Length; i++)
            {
                names[i] = _definitions[i].LegacyName;
            }

            return names;
        }

        public static Vector3 GetPosition(CameraSongScriptConfig config, int index)
        {
            return _definitions[ClampIndex(index)].PositionAccessor(config);
        }

        public static Vector3 GetRotation(CameraSongScriptConfig config, int index)
        {
            return _definitions[ClampIndex(index)].RotationAccessor(config);
        }
    }
}
