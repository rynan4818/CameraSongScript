using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace CameraSongScript.Models
{
    /// <summary>
    /// CameraPlus形式のMovementScript JSONをパースしたデータクラス
    /// CameraPlusのCameraMovement.CameraData / Movementsに相当
    /// </summary>
    public class CameraSongScriptMovement
    {
        public Vector3 StartPos;
        public Vector3 StartRot;
        public Vector3 StartHeadOffset;
        public float StartFOV;
        public Vector3 EndPos;
        public Vector3 EndRot;
        public Vector3 EndHeadOffset;
        public float EndFOV;
        public float Duration;
        public float Delay;
        public VisibleObject SectionVisibleObject;
        public bool TurnToHead = false;
        public bool TurnToHeadHorizontal = false;
        public bool EaseTransition = true;
    }

    public class CameraSongScriptData
    {
        private const float MinDuration = 0.01f;

        public bool ActiveInPauseMenu = true;
        public bool TurnToHeadUseCameraSetting = false;
        public List<CameraSongScriptMovement> Movements = new List<CameraSongScriptMovement>();

        // 非対応機能の検出結果
        public bool ContainsCameraEffect = false;
        public bool ContainsWindowControl = false;

        public bool LoadFromJson(string jsonString)
        {
            Movements.Clear();
            ContainsCameraEffect = false;
            ContainsWindowControl = false;

            MovementScriptJson movementScriptJson = null;

            try
            {
                movementScriptJson = JsonConvert.DeserializeObject<MovementScriptJson>(jsonString);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"SongScript JSON syntax error. {ex.Message}");
            }

            if (movementScriptJson != null && movementScriptJson.Jsonmovement != null)
            {
                if (movementScriptJson.ActiveInPauseMenu != null)
                    ActiveInPauseMenu = Convert.ToBoolean(movementScriptJson.ActiveInPauseMenu);
                if (movementScriptJson.TurnToHeadUseCameraSetting != null)
                    TurnToHeadUseCameraSetting = Convert.ToBoolean(movementScriptJson.TurnToHeadUseCameraSetting);

                foreach (JSONMovement jsonmovement in movementScriptJson.Jsonmovement)
                {
                    CameraSongScriptMovement newMovement = new CameraSongScriptMovement();

                    // StartPos / StartFOV
                    ParsePosRotOffset(
                        jsonmovement.startPos, jsonmovement.startRot, jsonmovement.startHeadOffset,
                        out newMovement.StartPos, out newMovement.StartRot, out newMovement.StartHeadOffset, out newMovement.StartFOV);

                    // EndPos / EndFOV
                    ParsePosRotOffset(
                        jsonmovement.endPos, jsonmovement.endRot, jsonmovement.endHeadOffset,
                        out newMovement.EndPos, out newMovement.EndRot, out newMovement.EndHeadOffset, out newMovement.EndFOV);

                    // VisibleObject
                    if (jsonmovement.visibleObject != null)
                        newMovement.SectionVisibleObject = jsonmovement.visibleObject;

                    // TurnToHead
                    if (jsonmovement.TurnToHead != null)
                        newMovement.TurnToHead = Convert.ToBoolean(jsonmovement.TurnToHead);
                    if (jsonmovement.TurnToHeadHorizontal != null)
                        newMovement.TurnToHeadHorizontal = Convert.ToBoolean(jsonmovement.TurnToHeadHorizontal);

                    // Duration / Delay
                    if (jsonmovement.Duration != null)
                        newMovement.Duration = Mathf.Clamp(ParseFloat(jsonmovement.Duration), MinDuration, float.MaxValue);
                    if (jsonmovement.Delay != null)
                        newMovement.Delay = ParseFloat(jsonmovement.Delay);

                    // EaseTransition
                    if (jsonmovement.EaseTransition != null)
                        newMovement.EaseTransition = Convert.ToBoolean(jsonmovement.EaseTransition);

                    // CameraEffect（存在チェックのみ）
                    if (jsonmovement.cameraEffect != null)
                        ContainsCameraEffect = true;

                    // WindowControl（存在チェックのみ）
                    if (jsonmovement.windowControl != null)
                        ContainsWindowControl = true;

                    Movements.Add(newMovement);
                }
                return true;
            }
            return false;
        }

        private static float ParseFloat(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static Vector3 ParseVector3(string x, string y, string z)
        {
            return new Vector3(ParseFloat(x), ParseFloat(y), ParseFloat(z));
        }

        private static void ParsePosRotOffset(
            AxizWithFoVElements pos, AxisElements rot, AxisElements headOffset,
            out Vector3 outPos, out Vector3 outRot, out Vector3 outHeadOffset, out float outFOV)
        {
            outPos = (pos?.x != null) ? ParseVector3(pos.x, pos.y, pos.z) : Vector3.zero;
            outRot = (rot?.x != null) ? ParseVector3(rot.x, rot.y, rot.z) : Vector3.zero;
            outHeadOffset = (headOffset?.x != null) ? ParseVector3(headOffset.x, headOffset.y, headOffset.z) : Vector3.zero;
            outFOV = (pos?.FOV != null) ? ParseFloat(pos.FOV) : 0f;
        }
    }
}
