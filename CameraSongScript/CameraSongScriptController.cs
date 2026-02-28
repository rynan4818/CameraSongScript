using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CameraSongScript.Models;
using CameraSongScript.Configuration;
using CameraSongScript.HarmonyPatches;
using Zenject;

namespace CameraSongScript
{
    /// <summary>
    /// SongScript制御のメインコントローラー（Camera2モード専用）
    /// PlayerInstallerでバインドされ、GameCoreシーンでのみ動作する
    /// Camera2 OverrideToken SDKをリフレクション経由で呼び出す
    /// </summary>
    public class CameraSongScriptController : IInitializable, IDisposable, ITickable
    {
        [Inject] private readonly AudioTimeSyncController _audioTimeSyncController = null;
        [Inject(Optional = true)] private readonly PauseController _pauseController = null;

        private bool _dataLoaded = false;
        private CameraSongScriptData _data;
        private readonly Dictionary<string, object> _tokens = new Dictionary<string, object>();

        // Lerp用変数
        private Vector3 _startPos = Vector3.zero;
        private Vector3 _endPos = Vector3.zero;
        private Vector3 _startRot = Vector3.zero;
        private Vector3 _endRot = Vector3.zero;
        private Vector3 _startHeadOffset = Vector3.zero;
        private Vector3 _endHeadOffset = Vector3.zero;
        private float _startFOV = 0;
        private float _endFOV = 0;
        private bool _easeTransition = true;
        private float _movePerc;
        private int _eventID;
        private bool _turnToHead = false;
        private bool _turnToHeadHorizontal = false;

        // AudioSync用
        private float _movementStartTime, _movementEndTime, _movementNextStartTime;

        // リアルタイム計測用（DateTime.Now より低コスト）
        private float _movementStartRealtime, _movementEndRealtime, _movementDelayEndRealtime;

        // ポーズ対応
        private bool _paused = false;

        // 前セクションのVisibleObject有無のトラッキング
        private bool _prevSectionHadVisibleObject = false;

        private const float DefaultFOV = 90f;

        // カメラのデフォルトFOV
        private float _defaultFOV = DefaultFOV;

        public void Initialize()
        {
            if (!CameraModDetector.IsCamera2)
                return;

            if (!CameraSongScriptConfig.Instance.Enabled)
                return;

            if (!CameraSongScriptDetector.HasSongScript)
                return;

            if (Plugin.Cam2Helper == null || !Plugin.Cam2Helper.IsInitialized)
            {
                Plugin.Log.Error("SongScript: Camera2ReflectionHelper is not initialized.");
                return;
            }

            // ポーズ対応
            if (_pauseController != null)
            {
                _pauseController.didResumeEvent += Resume;
                _pauseController.didPauseEvent += Pause;
            }

            // SongScriptをロード
            LoadSongScript(CameraSongScriptDetector.SongScriptPath);

            if (_dataLoaded && _data.ActiveInPauseMenu && _pauseController != null)
            {
                // ActiveInPauseMenu=trueはポーズ中もスクリプトを動作させるためイベント解除
                _pauseController.didResumeEvent -= Resume;
                _pauseController.didPauseEvent -= Pause;
            }
            // ActiveInPauseMenu=falseの場合はポーズ対応のイベントを維持（行77-78で登録済み）
        }

        public void Dispose()
        {
            if (_pauseController != null)
            {
                _pauseController.didResumeEvent -= Resume;
                _pauseController.didPauseEvent -= Pause;
            }
            StopScript();
        }

        public void Tick()
        {
            if (!_dataLoaded || _paused) return;
            if (!CameraSongScriptConfig.Instance.Enabled) return;

            bool useAudioSync = CameraSongScriptConfig.Instance.UseAudioSync;

            if (useAudioSync)
            {
                if (_audioTimeSyncController == null)
                    return;

                while (_movementNextStartTime <= _audioTimeSyncController.songTime)
                    UpdatePosAndRot();

                float difference = _movementEndTime - _movementStartTime;
                float current = _audioTimeSyncController.songTime - _movementStartTime;
                if (difference != 0)
                    _movePerc = Mathf.Clamp(current / difference, 0, 1);
            }
            else
            {
                float now = Time.realtimeSinceStartup;
                if (_movePerc == 1 && _movementDelayEndRealtime <= now)
                    UpdatePosAndRot();

                float difference = _movementEndRealtime - _movementStartRealtime;
                float current = now - _movementStartRealtime;
                if (difference != 0)
                    _movePerc = Mathf.Clamp(current / difference, 0, 1);
            }

            // カメラの位置・回転・FOVを更新
            ApplyToAllTokens();
        }

        /// <summary>
        /// SongScriptファイルを読み込む
        /// </summary>
        private bool LoadSongScript(string path)
        {
            if (!File.Exists(path))
                return false;

            string jsonText = File.ReadAllText(path);
            _data = new CameraSongScriptData();

            if (_data.LoadFromJson(jsonText))
            {
                if (_data.Movements.Count == 0)
                {
                    Plugin.Log.Warn("SongScript: No movement data!");
                    return false;
                }

                // OverrideTokenを取得
                AcquireTokens();

                if (_tokens.Count == 0)
                {
                    Plugin.Log.Warn("SongScript: No cameras available for override.");
                    return false;
                }

                // AudioSync初期化
                if (CameraSongScriptConfig.Instance.UseAudioSync)
                {
                    _movementNextStartTime = 0;
                }

                _eventID = 0;
                UpdatePosAndRot();
                _dataLoaded = true;

                Plugin.Log.Info($"SongScript: Loaded {_data.Movements.Count} movements from: {path}");

                if (_data.ContainsCameraEffect)
                    Plugin.Log.Warn("SongScript: This script contains CameraEffect which is not supported by Camera2.");
                if (_data.ContainsWindowControl)
                    Plugin.Log.Warn("SongScript: This script contains WindowControl which is not supported by Camera2.");

                return true;
            }
            return false;
        }

        /// <summary>
        /// Configで指定されたターゲットカメラ名、未指定ならアクティブな全カメラ名を返す
        /// </summary>
        private IEnumerable<string> GetTargetOrActiveCameras()
        {
            string[] targetNames = CameraSongScriptConfig.Instance.GetTargetCameraNames();
            return targetNames.Length > 0
                ? (IEnumerable<string>)targetNames
                : Plugin.Cam2Helper.GetActiveCameras();
        }

        /// <summary>
        /// OverrideTokenを対象カメラから取得（リフレクション経由）
        /// </summary>
        private void AcquireTokens()
        {
            ReleaseAllTokens();

            foreach (var camName in GetTargetOrActiveCameras())
            {
                var token = Plugin.Cam2Helper.GetTokenForCamera(camName);
                if (token != null)
                {
                    _tokens[camName] = token;
                    // デフォルトFOVを記録（最初のカメラのものを使用）
                    if (_tokens.Count == 1)
                        _defaultFOV = Plugin.Cam2Helper.GetTokenFOV(token);
                    Plugin.Log.Info($"SongScript: Acquired OverrideToken for camera '{camName}'.");
                }
                else
                {
                    Plugin.Log.Warn($"SongScript: Could not acquire OverrideToken for camera '{camName}'. Another mod may be using it.");
                }
            }
        }

        /// <summary>
        /// 全てのOverrideTokenを解放（Close()メソッド経由）
        /// </summary>
        private void ReleaseAllTokens()
        {
            foreach (var token in _tokens.Values)
            {
                Plugin.Cam2Helper.CloseToken(token);
            }
            _tokens.Clear();
        }

        /// <summary>
        /// スクリプトを停止しリセット
        /// </summary>
        public void StopScript()
        {
            _dataLoaded = false;
            _paused = false;
            ReleaseAllTokens();
        }

        private void Pause()
        {
            if (_paused) return;
            _paused = true;
        }

        private void Resume()
        {
            if (!_paused) return;
            _paused = false;
        }

        /// <summary>
        /// セクション切替時に位置・回転やFOVのパラメータを更新
        /// CameraPlusのUpdatePosAndRotに相当
        /// </summary>
        private void UpdatePosAndRot()
        {
            if (_eventID >= _data.Movements.Count)
                _eventID = 0;

            var movement = _data.Movements[_eventID];

            // TurnToHead
            _turnToHead = _data.TurnToHeadUseCameraSetting ? false : movement.TurnToHead;
            _turnToHeadHorizontal = movement.TurnToHeadHorizontal;

            _easeTransition = movement.EaseTransition;

            _startPos = movement.StartPos;
            _endPos = movement.EndPos;
            _startRot = movement.StartRot;
            _endRot = movement.EndRot;

            // VisibleObject
            if (movement.SectionVisibleObject != null)
            {
                ApplyVisibleObject(movement.SectionVisibleObject);
                _prevSectionHadVisibleObject = true;
            }
            else
            {
                if (_prevSectionHadVisibleObject)
                {
                    ResetVisibleObjects();
                }
                _prevSectionHadVisibleObject = false;
            }

            // HeadOffset
            _startHeadOffset = movement.StartHeadOffset;
            _endHeadOffset = movement.EndHeadOffset;

            // FOV
            _startFOV = movement.StartFOV != 0 ? movement.StartFOV : _defaultFOV;
            _endFOV = movement.EndFOV != 0 ? movement.EndFOV : _defaultFOV;

            FindShortestDelta(ref _startRot, ref _endRot);

            bool useAudioSync = CameraSongScriptConfig.Instance.UseAudioSync;

            if (useAudioSync)
            {
                _movementStartTime = _movementNextStartTime;
                _movementEndTime = _movementNextStartTime + movement.Duration;
                _movementNextStartTime = _movementEndTime + movement.Delay;
            }
            else
            {
                _movementStartRealtime = Time.realtimeSinceStartup;
                _movementEndRealtime = _movementStartRealtime + movement.Duration;
                _movementDelayEndRealtime = _movementStartRealtime + movement.Duration + movement.Delay;
            }

            _eventID++;
        }

        /// <summary>
        /// 全てのOverrideTokenに対してLerp結果を適用（リフレクション経由）
        /// </summary>
        private void ApplyToAllTokens()
        {
            float easedPerc = Ease(_movePerc);

            Vector3 pos = LerpVector3(_startPos, _endPos, easedPerc);
            Vector3 rot = LerpVector3Angle(_startRot, _endRot, easedPerc);
            float fov = Mathf.Lerp(_startFOV, _endFOV, easedPerc);

            // TurnToHead処理
            if (_turnToHead)
            {
                Vector3 headOffset = LerpVector3(_startHeadOffset, _endHeadOffset, easedPerc);
                Vector3 headPos = GetHMDPosition() + headOffset;
                Vector3 lookDirection = headPos - pos;

                if (lookDirection != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                    Vector3 lookEuler = lookRotation.eulerAngles;

                    if (_turnToHeadHorizontal)
                    {
                        rot = new Vector3(rot.x, lookEuler.y, rot.z);
                    }
                    else
                    {
                        rot = lookEuler;
                    }
                }
            }

            foreach (var kvp in _tokens)
            {
                var token = kvp.Value;
                Plugin.Cam2Helper.SetTokenPosition(token, pos);
                Plugin.Cam2Helper.SetTokenRotation(token, rot);
                Plugin.Cam2Helper.SetTokenFOV(token, fov);
            }
        }

        /// <summary>
        /// VisibleObjectをOverrideTokenのvisibleObjectsに適用（リフレクション経由）
        /// </summary>
        private void ApplyVisibleObject(VisibleObject scriptVisible)
        {
            foreach (var kvp in _tokens)
            {
                var token = kvp.Value;
                var go = Plugin.Cam2Helper.GetTokenVisibleObjects(token);
                if (go == null) continue;

                if (scriptVisible.avatar.HasValue)
                    Plugin.Cam2Helper.SetVisibleAvatar(go, scriptVisible.avatar.Value);
                if (scriptVisible.notes.HasValue)
                    Plugin.Cam2Helper.SetVisibleNotes(go, scriptVisible.notes.Value);
                if (scriptVisible.ui.HasValue)
                    Plugin.Cam2Helper.SetVisibleUI(go, scriptVisible.ui.Value);
                if (scriptVisible.saber.HasValue)
                    Plugin.Cam2Helper.SetVisibleSabers(go, scriptVisible.saber.Value);
                if (scriptVisible.debris.HasValue)
                    Plugin.Cam2Helper.SetVisibleDebris(go, scriptVisible.debris.Value);
                if (scriptVisible.wall.HasValue)
                    Plugin.Cam2Helper.SetVisibleWalls(go, scriptVisible.wall.Value);
                if (scriptVisible.wallFrame.HasValue && scriptVisible.wallFrame.Value)
                    Plugin.Cam2Helper.SetVisibleWallsTransparent(go);
                if (scriptVisible.cutParticles.HasValue)
                    Plugin.Cam2Helper.SetVisibleCutParticles(go, scriptVisible.cutParticles.Value);
            }
        }

        /// <summary>
        /// VisibleObjectをデフォルト状態に戻す（リフレクション経由）
        /// </summary>
        private void ResetVisibleObjects()
        {
            foreach (var kvp in _tokens)
            {
                var token = kvp.Value;
                var go = Plugin.Cam2Helper.GetTokenVisibleObjects(token);
                if (go == null) continue;
                Plugin.Cam2Helper.ResetVisibleObjects(go);
            }
        }

        /// <summary>
        /// HMD（ヘッドセット）の位置を取得
        /// </summary>
        private Vector3 GetHMDPosition()
        {
            var headTransform = Camera.main?.transform;
            if (headTransform != null)
                return headTransform.position;
            return Vector3.zero;
        }

        #region ユーティリティ

        private static void FindShortestDelta(ref Vector3 from, ref Vector3 to)
        {
            if (Mathf.DeltaAngle(from.x, to.x) < 0)
                from.x += 360.0f;
            if (Mathf.DeltaAngle(from.y, to.y) < 0)
                from.y += 360.0f;
            if (Mathf.DeltaAngle(from.z, to.z) < 0)
                from.z += 360.0f;
        }

        private static Vector3 LerpVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.Lerp(from.x, to.x, percent), Mathf.Lerp(from.y, to.y, percent), Mathf.Lerp(from.z, to.z, percent));
        }

        private static Vector3 LerpVector3Angle(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.LerpAngle(from.x, to.x, percent), Mathf.LerpAngle(from.y, to.y, percent), Mathf.LerpAngle(from.z, to.z, percent));
        }

        private float Ease(float p)
        {
            if (!_easeTransition)
                return p;

            if (p < 0.5f)
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }

        #endregion

        #region 公開プロパティ

        /// <summary>
        /// スクリプトがロード済みかどうか
        /// </summary>
        public bool IsLoaded => _dataLoaded;

        /// <summary>
        /// スクリプトデータ（非対応機能の検出結果含む）
        /// </summary>
        public CameraSongScriptData Data => _data;

        /// <summary>
        /// OverrideTokenを取得できなかったカメラ名リスト
        /// </summary>
        public List<string> UnavailableCameras
        {
            get
            {
                var result = new List<string>();
                foreach (var name in GetTargetOrActiveCameras())
                {
                    if (!_tokens.ContainsKey(name))
                        result.Add(name);
                }
                return result;
            }
        }

        /// <summary>
        /// 現在OverrideTokenを保持しているカメラ名リスト
        /// </summary>
        public IEnumerable<string> ActiveCameras => _tokens.Keys;

        #endregion
    }
}
