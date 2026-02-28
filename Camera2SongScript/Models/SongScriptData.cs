using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace Camera2SongScript.Models
{
    /// <summary>
    /// CameraPlus形式のMovementScript JSONをパースしたデータクラス
    /// CameraPlusのCameraMovement.CameraData / Movementsに相当
    /// </summary>
    public class SongScriptMovement
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
        // CameraEffect、WindowControlは読み込むが適用しない
        public bool HasCameraEffect = false;
        public bool HasWindowControl = false;
    }

    public class SongScriptData
    {
        public bool ActiveInPauseMenu = true;
        public bool TurnToHeadUseCameraSetting = false;
        public List<SongScriptMovement> Movements = new List<SongScriptMovement>();

        // 非対応機能の検出結果
        public bool ContainsCameraEffect = false;
        public bool ContainsWindowControl = false;

        public bool LoadFromJson(string jsonString)
        {
            Movements.Clear();
            ContainsCameraEffect = false;
            ContainsWindowControl = false;

            MovementScriptJson movementScriptJson = null;
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string sepCheck = (sep == "." ? "," : ".");

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
                    SongScriptMovement newMovement = new SongScriptMovement();

                    // StartPos / StartFOV
                    AxizWithFoVElements startPos = jsonmovement.startPos;
                    AxisElements startRot = new AxisElements();
                    AxisElements startHeadOffset = new AxisElements();
                    if (jsonmovement.startRot != null) startRot = jsonmovement.startRot;
                    if (jsonmovement.startHeadOffset != null) startHeadOffset = jsonmovement.startHeadOffset;

                    if (startPos?.x != null)
                        newMovement.StartPos = ParseVector3(startPos.x, startPos.y, startPos.z, sep, sepCheck);
                    if (startRot.x != null)
                        newMovement.StartRot = ParseVector3(startRot.x, startRot.y, startRot.z, sep, sepCheck);
                    else
                        newMovement.StartRot = Vector3.zero;

                    if (startHeadOffset.x != null)
                        newMovement.StartHeadOffset = ParseVector3(startHeadOffset.x, startHeadOffset.y, startHeadOffset.z, sep, sepCheck);
                    else
                        newMovement.StartHeadOffset = Vector3.zero;

                    if (startPos?.FOV != null)
                        newMovement.StartFOV = ParseFloat(startPos.FOV, sep, sepCheck);
                    else
                        newMovement.StartFOV = 0;

                    // EndPos / EndFOV
                    AxizWithFoVElements endPos = jsonmovement.endPos;
                    AxisElements endRot = new AxisElements();
                    AxisElements endHeadOffset = new AxisElements();
                    if (jsonmovement.endRot != null) endRot = jsonmovement.endRot;
                    if (jsonmovement.endHeadOffset != null) endHeadOffset = jsonmovement.endHeadOffset;

                    if (endPos?.x != null)
                        newMovement.EndPos = ParseVector3(endPos.x, endPos.y, endPos.z, sep, sepCheck);
                    if (endRot.x != null)
                        newMovement.EndRot = ParseVector3(endRot.x, endRot.y, endRot.z, sep, sepCheck);
                    else
                        newMovement.EndRot = Vector3.zero;

                    if (endHeadOffset.x != null)
                        newMovement.EndHeadOffset = ParseVector3(endHeadOffset.x, endHeadOffset.y, endHeadOffset.z, sep, sepCheck);
                    else
                        newMovement.EndHeadOffset = Vector3.zero;

                    if (endPos?.FOV != null)
                        newMovement.EndFOV = ParseFloat(endPos.FOV, sep, sepCheck);
                    else
                        newMovement.EndFOV = 0;

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
                        newMovement.Duration = Mathf.Clamp(ParseFloat(jsonmovement.Duration, sep, sepCheck), 0.01f, float.MaxValue);
                    if (jsonmovement.Delay != null)
                        newMovement.Delay = ParseFloat(jsonmovement.Delay, sep, sepCheck);

                    // EaseTransition
                    if (jsonmovement.EaseTransition != null)
                        newMovement.EaseTransition = Convert.ToBoolean(jsonmovement.EaseTransition);

                    // CameraEffect（読み込まないが存在チェック）
                    if (jsonmovement.cameraEffect != null)
                    {
                        newMovement.HasCameraEffect = true;
                        ContainsCameraEffect = true;
                    }

                    // WindowControl（読み込まないが存在チェック）
                    if (jsonmovement.windowControl != null)
                    {
                        newMovement.HasWindowControl = true;
                        ContainsWindowControl = true;
                    }

                    Movements.Add(newMovement);
                }
                return true;
            }
            return false;
        }

        private static float ParseFloat(string value, string sep, string sepCheck)
        {
            return float.Parse(value.Contains(sepCheck) ? value.Replace(sepCheck, sep) : value);
        }

        private static Vector3 ParseVector3(string x, string y, string z, string sep, string sepCheck)
        {
            return new Vector3(ParseFloat(x, sep, sepCheck), ParseFloat(y, sep, sepCheck), ParseFloat(z, sep, sepCheck));
        }
    }
}
