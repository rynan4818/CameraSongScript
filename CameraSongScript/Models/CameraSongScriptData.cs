using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CameraSongScript.Models
{
    /// <summary>
    /// CameraPlus形式のMovementScript JSONをパースしたデータクラス
    /// パフォーマンス上の理由からプロパティではなくpublicフィールドを使用
    /// （ParsePosRotOffsetでout引数として直接書き込むため）
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

            if (movementScriptJson != null && movementScriptJson.JsonMovements != null)
            {
                try
                {
                    if (movementScriptJson.ActiveInPauseMenu != null)
                        ActiveInPauseMenu = Convert.ToBoolean(movementScriptJson.ActiveInPauseMenu);
                    if (movementScriptJson.TurnToHeadUseCameraSetting != null)
                        TurnToHeadUseCameraSetting = Convert.ToBoolean(movementScriptJson.TurnToHeadUseCameraSetting);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"SongScript: Failed to parse global settings: {ex.Message}");
                }

                for (int movementIndex = 0; movementIndex < movementScriptJson.JsonMovements.Length; movementIndex++)
                {
                    JsonMovement jsonmovement = movementScriptJson.JsonMovements[movementIndex];

                    try
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
                        {
                            float parsedDelay = ParseFloat(jsonmovement.Delay);
                            if (parsedDelay < 0f)
                            {
                                Plugin.Log.Warn($"SongScript: Movement #{movementIndex + 1} has a negative Delay ({parsedDelay}). Clamping to 0.");
                                parsedDelay = 0f;
                            }

                            newMovement.Delay = parsedDelay;
                        }

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
                    catch (Exception ex)
                    {
                        Plugin.Log.Warn($"SongScript: Skipping malformed movement #{movementIndex + 1}: {ex.Message}");
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// ロケール安全な浮動小数点パース（'.' と ',' の両方のデシマルセパレータに対応）
        /// </summary>
        private static float ParseFloat(string value)
        {
            return NumericStringParser.Parse(value);
        }

        private static Vector3 ParseVector3(string x, string y, string z)
        {
            return new Vector3(ParseFloat(x), ParseFloat(y), ParseFloat(z));
        }

        private static void ParsePosRotOffset(
            AxisWithFoVElements pos, AxisElements rot, AxisElements headOffset,
            out Vector3 outPos, out Vector3 outRot, out Vector3 outHeadOffset, out float outFOV)
        {
            outPos = (pos?.x != null) ? ParseVector3(pos.x, pos.y, pos.z) : Vector3.zero;
            outRot = (rot?.x != null) ? ParseVector3(rot.x, rot.y, rot.z) : Vector3.zero;
            outHeadOffset = (headOffset?.x != null) ? ParseVector3(headOffset.x, headOffset.y, headOffset.z) : Vector3.zero;
            outFOV = (pos?.FOV != null) ? ParseFloat(pos.FOV) : 0f;
        }
    }
}

