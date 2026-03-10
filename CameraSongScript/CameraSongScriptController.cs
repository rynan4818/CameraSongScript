using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using Zenject;

namespace CameraSongScript
{
    /// <summary>
    /// SongScript制御のメインコントローラー（Camera2モード専用）
    /// PlayerInstallerでバインドされ、GameCoreシーンでのみ動作する
    /// ICameraToken経由でカメラを操作する
    /// </summary>
    public class CameraSongScriptController : IInitializable, IDisposable, ITickable
    {
#pragma warning disable 0649
        [Inject] private AudioTimeSyncController _audioTimeSyncController;
        [Inject(Optional = true)] private PauseController _pauseController;
#pragma warning restore 0649

        private bool _dataLoaded = false;
        private CameraSongScriptData _data;
        private readonly Dictionary<string, ICameraToken> _tokens = new Dictionary<string, ICameraToken>();

        // 現在セクションのLerp補間パラメータ
        private LerpState _lerp;
        private float _movePerc;
        private int _eventID;

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

        // 初回Tickで初期化するためのフラグ
        private bool _isFirstTick = false;

        // Camera2のゲームシーン読み込み完了待ちフラグ
        private bool _pendingCustomSceneSwitch = false;
        private int _customSceneSwitchDelay = 0;

        // 汎用スクリプト使用時フラグ
        private bool _isUsingCommonScript = false;

        public void Initialize()
        {
            bool useCommon = CameraSongScriptDetector.IsUsingCommonScript;
            bool hasNormalScript = CameraSongScriptDetector.HasSongScript;

            // ForceCommonの場合はEnabled無視
            if (!useCommon && !CameraSongScriptConfig.Instance.Enabled)
                return;

            if (!useCommon && !hasNormalScript)
                return;

            if (CameraModDetector.IsCamera2 && !Plugin.IsCamHelperReady)
            {
                Plugin.Log.Error("SongScript: Camera2 adapter is not initialized.");
                return;
            }

            if (CameraModDetector.IsCameraPlus && !Plugin.IsCamPlusHelperReady)
            {
                Plugin.Log.Error("SongScript: CameraPlus adapter is not initialized.");
                return;
            }

            // ポーズ対応
            if (_pauseController != null)
            {
                _pauseController.didResumeEvent += Resume;
                _pauseController.didPauseEvent += Pause;
            }

            // スクリプトパスの決定
            string scriptPath;
            _isUsingCommonScript = useCommon;

            if (useCommon)
            {
                // Camera2: ランダムの場合は毎回プレイ開始時に再選択する
                if (CameraSongScriptConfig.Instance.SelectedCommonScript == "(Random)")
                {
                    CameraSongScriptDetector.ResolveAndSetCommonScriptPath();
                }
                scriptPath = CameraSongScriptDetector.ResolvedCommonScriptPath;

                if (string.IsNullOrEmpty(scriptPath))
                {
                    Plugin.Log.Warn("SongScript: Common script path could not be resolved.");
                    return;
                }

                // 汎用スクリプト確定後にハッシュ対応表からオフセットを復元する
                // ランダム時はここで初めてスクリプトが確定するため、このタイミングで復元が必要
                if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
                {
                    int savedOffset = ScriptOffsetManager.GetOffsetForScript(scriptPath);
                    CameraSongScriptConfig.Instance.CameraHeightOffsetCm = savedOffset;
                }
            }
            else
            {
                scriptPath = CameraSongScriptDetector.SelectedScriptPath;
            }

            // SongScriptをロード
            LoadSongScript(scriptPath);

            if (_dataLoaded && _data.ActiveInPauseMenu && _pauseController != null)
            {
                // ActiveInPauseMenu=trueはポーズ中もスクリプトを動作させるためイベント解除
                _pauseController.didResumeEvent -= Resume;
                _pauseController.didPauseEvent -= Pause;
            }
            // ActiveInPauseMenu=falseの場合はポーズ対応のイベントを維持

            // Camera2のゲームシーン読み込み完了後にカスタムシーンを適用する
            string customScene;
            if (useCommon && !string.IsNullOrEmpty(CameraSongScriptConfig.Instance.CommonScriptCustomScene))
            {
                customScene = CameraSongScriptConfig.Instance.CommonScriptCustomScene;
            }
            else
            {
                customScene = CameraSongScriptConfig.Instance.CustomSceneToSwitch;
            }
            if (CameraModDetector.IsCamera2 && !string.IsNullOrEmpty(customScene) && customScene != "(Default)")
            {
                _pendingCustomSceneSwitch = true;
                _customSceneSwitchDelay = 2; // 2フレーム待機（Camera2のコルーチン完了後）
            }
        }

        public void Dispose()
        {
            // カスタムシーンの復元
            if (CameraModDetector.IsCamera2)
            {
                Plugin.CamHelper.RestoreGameSceneSetup();
            }

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
            if (!_isUsingCommonScript && !CameraSongScriptConfig.Instance.Enabled) return;

            if (_isFirstTick)
            {
                _isFirstTick = false;
                UpdatePosAndRot();
            }

            // Camera2のゲームシーン読み込み完了を確認してからカスタムシーンに切り替える
            if (CameraModDetector.IsCamera2 && _pendingCustomSceneSwitch)
            {
                _customSceneSwitchDelay--;
                if (_customSceneSwitchDelay <= 0)
                {
                    _pendingCustomSceneSwitch = false;
                    Plugin.CamHelper.PreGameSceneCurrentSetup(CameraSongScriptConfig.Instance.CustomSceneToSwitch);
                }
            }

            bool useAudioSync = CameraSongScriptConfig.Instance.UseAudioSync;
            float startTime, endTime, currentTime;

            if (useAudioSync)
            {
                if (_audioTimeSyncController == null)
                    return;

                while (_movementNextStartTime <= _audioTimeSyncController.songTime)
                    UpdatePosAndRot();

                startTime = _movementStartTime;
                endTime = _movementEndTime;
                currentTime = _audioTimeSyncController.songTime;
            }
            else
            {
                float now = Time.realtimeSinceStartup;
                if (_movePerc == 1 && _movementDelayEndRealtime <= now)
                    UpdatePosAndRot();

                startTime = _movementStartRealtime;
                endTime = _movementEndRealtime;
                currentTime = now;
            }

            // 補間割合を計算（共通処理）
            float difference = endTime - startTime;
            if (difference != 0)
                _movePerc = Mathf.Clamp((currentTime - startTime) / difference, 0f, 1f);

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
                _isFirstTick = true;
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
        /// 汎用スクリプト使用時はCommonScriptTargetCameraを優先
        /// </summary>
        private IEnumerable<string> GetTargetOrActiveCameras()
        {
            // 汎用スクリプト使用時は専用設定を優先
            if (_isUsingCommonScript)
            {
                string commonTarget = CameraSongScriptConfig.Instance.CommonScriptTargetCamera;
                if (!string.IsNullOrEmpty(commonTarget))
                {
                    return new List<string> { commonTarget };
                }
            }

            string[] targetNames = CameraSongScriptConfig.Instance.GetTargetCameraNames();
            if (targetNames.Length > 0)
                return targetNames;
            return Plugin.CamHelper.GetActiveCameras();
        }

        /// <summary>
        /// OverrideTokenを対象カメラから取得
        /// </summary>
        private void AcquireTokens()
        {
            ReleaseAllTokens();

            foreach (var camName in GetTargetOrActiveCameras())
            {
                var token = Plugin.CamHelper.GetTokenForCamera(camName);
                if (token != null)
                {
                    _tokens[camName] = token;
                    // デフォルトFOVを記録（最初のカメラのものを使用）
                    if (_tokens.Count == 1)
                        _defaultFOV = token.GetFOV();
                    Plugin.Log.Info($"SongScript: Acquired OverrideToken for camera '{camName}'.");
                }
                else
                {
                    Plugin.Log.Warn($"SongScript: Could not acquire OverrideToken for camera '{camName}'. Another mod may be using it.");
                }
            }
        }

        /// <summary>
        /// 全てのOverrideTokenを解放
        /// </summary>
        private void ReleaseAllTokens()
        {
            foreach (var token in _tokens.Values)
            {
                token.Dispose();
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
            _lerp.TurnToHead = _data.TurnToHeadUseCameraSetting ? false : movement.TurnToHead;
            _lerp.TurnToHeadHorizontal = movement.TurnToHeadHorizontal;

            _lerp.EaseTransition = movement.EaseTransition;

            _lerp.StartPos = movement.StartPos;
            _lerp.EndPos = movement.EndPos;
            _lerp.StartRot = movement.StartRot;
            _lerp.EndRot = movement.EndRot;

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
            _lerp.StartHeadOffset = movement.StartHeadOffset;
            _lerp.EndHeadOffset = movement.EndHeadOffset;

            // FOV
            _lerp.StartFOV = movement.StartFOV != 0 ? movement.StartFOV : _defaultFOV;
            _lerp.EndFOV = movement.EndFOV != 0 ? movement.EndFOV : _defaultFOV;

            CameraSongScriptMath.FindShortestDelta(ref _lerp.StartRot, ref _lerp.EndRot);

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
        /// 全てのOverrideTokenに対してLerp結果を適用
        /// </summary>
        private void ApplyToAllTokens()
        {
            float easedPerc = CameraSongScriptMath.Ease(_movePerc, _lerp.EaseTransition);

            Vector3 pos = CameraSongScriptMath.LerpVector3(_lerp.StartPos, _lerp.EndPos, easedPerc);

            // Y座標へのオフセット適用（Camera2モードではここで動的に適用する）
            int offsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            pos = CameraSongScriptMath.ApplyHeightOffset(pos, offsetCm);

            Vector3 rot = CameraSongScriptMath.LerpVector3Angle(_lerp.StartRot, _lerp.EndRot, easedPerc);
            float fov = Mathf.Lerp(_lerp.StartFOV, _lerp.EndFOV, easedPerc);

            // TurnToHead処理
            if (_lerp.TurnToHead)
            {
                Vector3 headOffset = CameraSongScriptMath.LerpVector3(_lerp.StartHeadOffset, _lerp.EndHeadOffset, easedPerc);
                Vector3 headPos = GetHMDPosition() + headOffset;
                Vector3 lookDirection = headPos - pos;

                if (lookDirection != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                    Vector3 lookEuler = lookRotation.eulerAngles;

                    if (_lerp.TurnToHeadHorizontal)
                    {
                        rot = new Vector3(rot.x, lookEuler.y, rot.z);
                    }
                    else
                    {
                        rot = lookEuler;
                    }
                }
            }

            foreach (var token in _tokens.Values)
            {
                token.SetPositionAndRotation(pos, rot);
                token.SetFOV(fov);
            }
        }

        /// <summary>
        /// VisibleObjectをOverrideTokenに適用
        /// </summary>
        private void ApplyVisibleObject(VisibleObject scriptVisible)
        {
            foreach (var token in _tokens.Values)
            {
                token.ApplyVisibleObject(scriptVisible);
            }
        }

        /// <summary>
        /// VisibleObjectをデフォルト状態に戻す
        /// </summary>
        private void ResetVisibleObjects()
        {
            foreach (var token in _tokens.Values)
            {
                token.ResetVisibleObjects();
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
        public IReadOnlyList<string> UnavailableCameras
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

        #region 内部型

        /// <summary>
        /// 現在セクションのLerp補間パラメータ（struct: GC圧回避のため値型）
        /// </summary>
        private struct LerpState
        {
            public Vector3 StartPos;
            public Vector3 EndPos;
            public Vector3 StartRot;
            public Vector3 EndRot;
            public Vector3 StartHeadOffset;
            public Vector3 EndHeadOffset;
            public float StartFOV;
            public float EndFOV;
            public bool EaseTransition;
            public bool TurnToHead;
            public bool TurnToHeadHorizontal;
        }

        #endregion
    }
}
