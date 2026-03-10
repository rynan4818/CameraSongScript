using System;
using System.Collections.Generic;
using System.IO;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Zenject;

namespace CameraSongScript.UI
{
    /// <summary>
    /// メニューシーン用のSongScriptプレビュー。
    /// 可視ミニチュアは1/10スケールで表示し、RenderTextureは実ワールドを描画する。
    /// </summary>
    internal class CameraSongScriptPreviewController : IInitializable, IDisposable, ITickable
    {
        private const float DefaultFov = 90f;
        private const float MiniatureScale = 0.1f;
        private const float ScreenScale = 0.0025f;
        private const float EndPoseEpsilon = 0.0001f;
        private const float StageHalfWidth = 1.5f;
        private const float StageFrontZ = -1.0f;
        private const float StageBackZ = 1.0f;
        private const float StageDepthDown = 10.0f;
        private const int PreviewDisplayLayer = 5;
        private const int MiniatureLayer = 5;
        private const int PreviewTextureWidth = 512;
        private const int PreviewTextureHeight = 288;

        private static readonly Vector3 VisiblePreviewPosition = new Vector3(0f, 1f, 0.5f);
        private static readonly Vector3 AvatarHeadTarget = new Vector3(0f, 1.52f, 0f);
        private static readonly Vector2 PreviewPanelSize = new Vector2(360f, 220f);
        private static readonly Color StageFrameColor = new Color(0.55f, 0.42f, 0.14f, 0.45f);
        private static readonly Color PathColor = new Color(0.06f, 0.28f, 0.38f, 0.35f);
        private static readonly Color AvatarColor = new Color(0.86f, 0.86f, 0.9f, 1f);
        private static readonly Color CameraMarkerColor = new Color(1f, 0.12f, 0.12f, 1f);

        private static Material _lineMaterial;
        private static Material _solidMaterialTemplate;

        private readonly List<TimelineSegment> _segments = new List<TimelineSegment>();

        private GameObject _visibleRoot;
        private Transform _miniatureRoot;
        private Transform _screenRoot;
        private Camera _previewCamera;
        private RenderTexture _previewTexture;
        private RawImage _previewDisplayImage;
        private Transform _miniCameraMarker;
        private float _currentTime;
        private float _duration;
        private int _speedMultiplier = 1;
        private bool _isPlaying;
        private string _loadedScriptPath = string.Empty;
        private string _loadedScriptDisplayName = string.Empty;

        public bool IsVisible => _visibleRoot != null;
        public bool IsPlaying => _isPlaying;
        public float CurrentTime => _currentTime;
        public float Duration => _duration;
        public int SpeedMultiplier => _speedMultiplier;
        public string LoadedScriptDisplayName => _loadedScriptDisplayName;
        public event Action StateChanged;

        public bool CanPreviewSelection
        {
            get
            {
                if (CameraSongScriptDetector.IsUsingCommonScript)
                {
                    if (!string.IsNullOrEmpty(CameraSongScriptDetector.ResolvedCommonScriptPath))
                        return File.Exists(CameraSongScriptDetector.ResolvedCommonScriptPath);

                    if (CameraSongScriptConfig.Instance.SelectedCommonScript == "(Random)")
                        return CommonScriptCache.Scripts.Count > 0;

                    string path = CommonScriptCache.GetPathByDisplayName(CameraSongScriptConfig.Instance.SelectedCommonScript);
                    return !string.IsNullOrEmpty(path) && File.Exists(path);
                }

                return CameraSongScriptDetector.HasSongScript && File.Exists(CameraSongScriptDetector.SelectedScriptPath);
            }
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
            Clear();
        }

        public void Tick()
        {
            if (!IsVisible)
                return;

            if (_isPlaying && _duration > 0f && _segments.Count > 0)
            {
                _currentTime += Time.unscaledDeltaTime * _speedMultiplier;
                if (_currentTime >= _duration)
                    _currentTime = Mathf.Repeat(_currentTime, _duration);

                ApplyCurrentPose(true);
                NotifyStateChanged();
            }

            UpdateScreenOrientation();
        }

        public void ShowAndStart()
        {
            StartAtSpeedInternal(1);
        }

        public void StartAtSpeed(int speedMultiplier)
        {
            StartAtSpeedInternal(speedMultiplier);
        }

        public void Stop()
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;
            NotifyStateChanged();
        }

        public void Clear()
        {
            DestroyPreviewObjects();
            ResetLoadedState();
            NotifyStateChanged();
        }

        public void Seek(float time, bool ensureVisible)
        {
            if (!IsVisible)
            {
                if (!ensureVisible)
                    return;

                if (!ReloadPreviewFromCurrentSelection(false, false, 1, false))
                    return;
            }

            _isPlaying = false;
            _currentTime = ClampPreviewTime(time);
            ApplyCurrentPose(false);
            NotifyStateChanged();
        }

        public void HandleSelectionChanged()
        {
            if (!IsVisible)
            {
                ResetLoadedState();
                NotifyStateChanged();
                return;
            }

            ReloadPreviewFromCurrentSelection(false, false, 1, false);
        }

        public void HandleVisualChange()
        {
            if (!IsVisible)
                return;

            ReloadPreviewFromCurrentSelection(true, _isPlaying, _speedMultiplier, true);
        }

        private void StartAtSpeedInternal(int speedMultiplier)
        {
            if (!IsVisible)
            {
                if (!ReloadPreviewFromCurrentSelection(false, false, speedMultiplier, false))
                    return;
            }

            _speedMultiplier = Mathf.Max(1, speedMultiplier);
            _isPlaying = _segments.Count > 0;
            ApplyCurrentPose(true);
            NotifyStateChanged();
        }

        private bool ReloadPreviewFromCurrentSelection(bool keepTime, bool keepPlaying, int preferredSpeed, bool preserveRandomSelection)
        {
            float previousTime = _currentTime;
            int previousSpeed = _speedMultiplier;
            bool previousPlaying = _isPlaying;

            string path;
            string displayName;
            if (!TryResolveCurrentScript(preserveRandomSelection, out path, out displayName))
            {
                Clear();
                return false;
            }

            CameraSongScriptData data;
            if (!TryLoadPreviewData(path, out data))
            {
                Clear();
                return false;
            }

            DestroyPreviewObjects();

            _loadedScriptPath = path;
            _loadedScriptDisplayName = displayName;

            BuildTimeline(data);
            CreatePreviewObjects();

            _currentTime = keepTime ? ClampPreviewTime(previousTime) : 0f;
            _speedMultiplier = keepPlaying ? previousSpeed : Mathf.Max(1, preferredSpeed);
            _isPlaying = keepPlaying ? previousPlaying : false;

            ApplyCurrentPose(false);
            NotifyStateChanged();
            return true;
        }

        private bool TryResolveCurrentScript(bool preserveRandomSelection, out string path, out string displayName)
        {
            path = string.Empty;
            displayName = string.Empty;

            if (CameraSongScriptDetector.IsUsingCommonScript)
            {
                if (CameraSongScriptConfig.Instance.SelectedCommonScript == "(Random)")
                {
                    if (preserveRandomSelection && !string.IsNullOrEmpty(_loadedScriptPath) && File.Exists(_loadedScriptPath))
                    {
                        path = _loadedScriptPath;
                        displayName = _loadedScriptDisplayName;
                    }
                    else
                    {
                        CommonScriptEntry entry = CommonScriptCache.GetRandom();
                        if (entry != null)
                        {
                            path = entry.FilePath;
                            displayName = entry.DisplayName;
                        }
                    }
                }
                else
                {
                    path = CommonScriptCache.GetPathByDisplayName(CameraSongScriptConfig.Instance.SelectedCommonScript);
                    displayName = CameraSongScriptConfig.Instance.SelectedCommonScript ?? string.Empty;
                }

                if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(CameraSongScriptDetector.ResolvedCommonScriptPath))
                {
                    path = CameraSongScriptDetector.ResolvedCommonScriptPath;
                    displayName = string.IsNullOrEmpty(CameraSongScriptDetector.ResolvedCommonScriptDisplayName)
                        ? displayName
                        : CameraSongScriptDetector.ResolvedCommonScriptDisplayName;
                }
            }
            else if (CameraSongScriptDetector.HasSongScript)
            {
                path = CameraSongScriptDetector.SelectedScriptPath;
                displayName = CameraSongScriptDetector.SelectedScriptDisplayName;
            }

            if (string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(path))
                displayName = Path.GetFileName(path);

            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        private static bool TryLoadPreviewData(string path, out CameraSongScriptData data)
        {
            data = null;

            try
            {
                string json = File.ReadAllText(path);
                var loadedData = new CameraSongScriptData();
                if (!loadedData.LoadFromJson(json) || loadedData.Movements.Count == 0)
                {
                    Plugin.Log.Warn($"Preview: Failed to parse movements from '{path}'.");
                    return false;
                }

                data = loadedData;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"Preview: Failed to load '{path}': {ex.Message}");
                return false;
            }
        }

        private void BuildTimeline(CameraSongScriptData data)
        {
            _segments.Clear();

            float cursor = 0f;
            for (int i = 0; i < data.Movements.Count; i++)
            {
                CameraSongScriptMovement movement = data.Movements[i];

                Vector3 startRot = movement.StartRot;
                Vector3 endRot = movement.EndRot;
                CameraSongScriptMath.FindShortestDelta(ref startRot, ref endRot);

                _segments.Add(new TimelineSegment
                {
                    StartPos = movement.StartPos,
                    EndPos = movement.EndPos,
                    StartRot = startRot,
                    EndRot = endRot,
                    StartHeadOffset = movement.StartHeadOffset,
                    EndHeadOffset = movement.EndHeadOffset,
                    StartFov = movement.StartFOV != 0f ? movement.StartFOV : DefaultFov,
                    EndFov = movement.EndFOV != 0f ? movement.EndFOV : DefaultFov,
                    StartTime = cursor,
                    EndTime = cursor + movement.Duration,
                    DelayEndTime = cursor + movement.Duration + movement.Delay,
                    EaseTransition = movement.EaseTransition,
                    TurnToHead = !data.TurnToHeadUseCameraSetting && movement.TurnToHead,
                    TurnToHeadHorizontal = movement.TurnToHeadHorizontal
                });

                cursor += movement.Duration + movement.Delay;
            }

            _duration = cursor;
        }

        private void CreatePreviewObjects()
        {
            _visibleRoot = new GameObject("CameraSongScript Preview Root");
            _visibleRoot.transform.position = VisiblePreviewPosition;

            _miniatureRoot = new GameObject("MiniatureRoot").transform;
            _miniatureRoot.SetParent(_visibleRoot.transform, false);
            _miniatureRoot.localScale = Vector3.one * MiniatureScale;
            _miniatureRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);

            _miniCameraMarker = CreatePreviewSceneContents(_miniatureRoot, 0.012f, 0.006f, true);

            _screenRoot = CreatePreviewScreen(_visibleRoot.transform);

            SetLayerRecursively(_visibleRoot, PreviewDisplayLayer);
            SetLayerRecursively(_miniatureRoot.gameObject, MiniatureLayer);
            _previewCamera = CreatePreviewCamera();
            UpdatePreviewRenderTextureAndView();
        }

        private Transform CreatePreviewSceneContents(Transform parent, float stageLineWidth, float pathLineWidth, bool includeCameraMarker)
        {
            CreateStageVolume(parent, stageLineWidth);
            CreateAvatar(parent);
            CreateMovementPath(parent, pathLineWidth);
            return includeCameraMarker ? CreateCameraMarker(parent) : null;
        }

        private void UpdatePreviewRenderTextureAndView()
        {
            bool needsNewTexture = _previewTexture == null
                || _previewTexture.width != PreviewTextureWidth
                || _previewTexture.height != PreviewTextureHeight
                || _previewTexture.antiAliasing != 1;

            if (needsNewTexture)
            {
                if (_previewTexture != null)
                {
                    _previewTexture.Release();
                    UnityEngine.Object.Destroy(_previewTexture);
                }

                _previewTexture = new RenderTexture(PreviewTextureWidth, PreviewTextureHeight, 24)
                {
                    name = "CameraSongScriptPreviewTexture",
                    useMipMap = false,
                    antiAliasing = 1,
                    anisoLevel = 1,
                    useDynamicScale = false,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            if (_previewTexture != null && !_previewTexture.IsCreated())
                _previewTexture.Create();

            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = _previewTexture;
                _previewCamera.aspect = _previewTexture != null && _previewTexture.height > 0
                    ? (float)_previewTexture.width / _previewTexture.height
                    : 16f / 9f;
            }

            if (_previewDisplayImage != null)
                _previewDisplayImage.texture = _previewTexture;
        }

        private Transform CreatePreviewScreen(Transform parent)
        {
            GameObject screenRootObject = new GameObject("PreviewScreen", typeof(Canvas));
            screenRootObject.transform.SetParent(parent, false);

            RectTransform screenTransform = screenRootObject.transform as RectTransform;
            screenTransform.localPosition = new Vector3(0f, 0.78f, 0.05f);
            screenTransform.localScale = Vector3.one * ScreenScale;
            screenTransform.sizeDelta = PreviewPanelSize;

            Canvas screenCanvas = screenRootObject.GetComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.WorldSpace;
            screenCanvas.overrideSorting = true;
            screenCanvas.sortingOrder = 60;

            CreatePreviewScreenElement(
                screenTransform,
                "PreviewBackground",
                PreviewPanelSize,
                Texture2D.whiteTexture,
                new Color(0.02f, 0.02f, 0.03f, 0.96f));

            _previewDisplayImage = CreatePreviewScreenElement(
                screenTransform,
                "PreviewImage",
                new Vector2(PreviewPanelSize.x - 16f, (PreviewPanelSize.x - 16f) * PreviewTextureHeight / (float)PreviewTextureWidth),
                null,
                Color.white);

            UpdatePreviewRenderTextureAndView();

            return screenTransform;
        }

        private static RawImage CreatePreviewScreenElement(RectTransform parent, string name, Vector2 size, Texture texture, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RawImage));
            imageObject.transform.SetParent(parent, false);

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return image;
        }

        private Camera CreatePreviewCamera()
        {
            Camera sourceCamera = GetSourceCamera();
            GameObject cameraObject;
            Camera previewCamera;

            if (sourceCamera != null)
            {
                cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
                cameraObject.name = "CameraSongScript Preview Camera";
                cameraObject.tag = "Untagged";

                while (cameraObject.transform.childCount > 0)
                    UnityEngine.Object.DestroyImmediate(cameraObject.transform.GetChild(0).gameObject);

                previewCamera = cameraObject.GetComponent<Camera>();
                if (previewCamera == null)
                    previewCamera = cameraObject.AddComponent<Camera>();

                RemovePreviewCameraComponents(cameraObject, previewCamera);
            }
            else
            {
                cameraObject = new GameObject("CameraSongScript Preview Camera");
                previewCamera = cameraObject.AddComponent<Camera>();
            }

            cameraObject.transform.SetParent(null, false);
            cameraObject.transform.position = Vector3.zero;
            cameraObject.transform.rotation = Quaternion.identity;
            cameraObject.transform.localScale = Vector3.one;
            cameraObject.SetActive(true);

            previewCamera.tag = "Untagged";
            previewCamera.enabled = false;
            previewCamera.stereoTargetEye = StereoTargetEyeMask.None;
            previewCamera.depth = -1000f;
            UpdatePreviewRenderTextureAndView();
            SyncPreviewCameraSourceSettings(previewCamera);
            return previewCamera;
        }

        private void CreateStageVolume(Transform parent, float lineWidth)
        {
            Vector3 topFrontLeft = new Vector3(-StageHalfWidth, 0f, StageFrontZ);
            Vector3 topFrontRight = new Vector3(StageHalfWidth, 0f, StageFrontZ);
            Vector3 topBackLeft = new Vector3(-StageHalfWidth, 0f, StageBackZ);
            Vector3 topBackRight = new Vector3(StageHalfWidth, 0f, StageBackZ);

            Vector3 bottomFrontLeft = new Vector3(-StageHalfWidth, -StageDepthDown, StageFrontZ);
            Vector3 bottomFrontRight = new Vector3(StageHalfWidth, -StageDepthDown, StageFrontZ);
            Vector3 bottomBackLeft = new Vector3(-StageHalfWidth, -StageDepthDown, StageBackZ);
            Vector3 bottomBackRight = new Vector3(StageHalfWidth, -StageDepthDown, StageBackZ);

            CreateLine(parent, "StageTopFront", topFrontLeft, topFrontRight, lineWidth, StageFrameColor);
            CreateLine(parent, "StageTopBack", topBackLeft, topBackRight, lineWidth, StageFrameColor);
            CreateLine(parent, "StageTopLeft", topFrontLeft, topBackLeft, lineWidth, StageFrameColor);
            CreateLine(parent, "StageTopRight", topFrontRight, topBackRight, lineWidth, StageFrameColor);

            CreateLine(parent, "StageBottomFront", bottomFrontLeft, bottomFrontRight, lineWidth, StageFrameColor);
            CreateLine(parent, "StageBottomBack", bottomBackLeft, bottomBackRight, lineWidth, StageFrameColor);
            CreateLine(parent, "StageBottomLeft", bottomFrontLeft, bottomBackLeft, lineWidth, StageFrameColor);
            CreateLine(parent, "StageBottomRight", bottomFrontRight, bottomBackRight, lineWidth, StageFrameColor);

            CreateLine(parent, "StageVerticalA", topFrontLeft, bottomFrontLeft, lineWidth, StageFrameColor);
            CreateLine(parent, "StageVerticalB", topFrontRight, bottomFrontRight, lineWidth, StageFrameColor);
            CreateLine(parent, "StageVerticalC", topBackLeft, bottomBackLeft, lineWidth, StageFrameColor);
            CreateLine(parent, "StageVerticalD", topBackRight, bottomBackRight, lineWidth, StageFrameColor);
        }

        private void CreateMovementPath(Transform parent, float lineWidth)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                TimelineSegment segment = _segments[i];
                Vector3 start = CameraSongScriptMath.ApplyHeightOffset(segment.StartPos, CameraSongScriptConfig.Instance.CameraHeightOffsetCm);
                Vector3 end = CameraSongScriptMath.ApplyHeightOffset(segment.EndPos, CameraSongScriptConfig.Instance.CameraHeightOffsetCm);
                CreateLine(parent, "PathSegment_" + i, start, end, lineWidth, PathColor);
            }
        }

        private static void CreateLine(Transform parent, string name, Vector3 start, Vector3 end, float width, Color color)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.sharedMaterial = GetLineMaterial();
            lineRenderer.sortingOrder = 50;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private static Material GetLineMaterial()
        {
            if (_lineMaterial != null)
                return _lineMaterial;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Standard");
            _lineMaterial = new Material(shader);
            _lineMaterial.color = Color.white;
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            _lineMaterial.renderQueue = 5000;
            return _lineMaterial;
        }

        private static void CreateAvatar(Transform parent)
        {
            CreatePrimitivePart(parent, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.47f, 0f), new Vector3(0.26f, 0.26f, 0.26f), Vector3.zero, AvatarColor);
            CreatePrimitivePart(parent, PrimitiveType.Cylinder, "Body", new Vector3(0f, 1.02f, 0f), new Vector3(0.12f, 0.36f, 0.12f), Vector3.zero, AvatarColor);
            CreatePrimitivePart(parent, PrimitiveType.Cylinder, "LeftArm", new Vector3(-0.27f, 1.08f, 0f), new Vector3(0.03f, 0.24f, 0.03f), new Vector3(0f, 0f, 55f), AvatarColor);
            CreatePrimitivePart(parent, PrimitiveType.Cylinder, "RightArm", new Vector3(0.27f, 1.08f, 0f), new Vector3(0.03f, 0.24f, 0.03f), new Vector3(0f, 0f, -55f), AvatarColor);
            CreatePrimitivePart(parent, PrimitiveType.Cylinder, "LeftLeg", new Vector3(-0.10f, 0.40f, 0f), new Vector3(0.035f, 0.40f, 0.035f), new Vector3(0f, 0f, 8f), AvatarColor);
            CreatePrimitivePart(parent, PrimitiveType.Cylinder, "RightLeg", new Vector3(0.10f, 0.40f, 0f), new Vector3(0.035f, 0.40f, 0.035f), new Vector3(0f, 0f, -8f), AvatarColor);
        }

        private static void CreatePrimitivePart(Transform parent, PrimitiveType primitiveType, string name, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            primitive.transform.localEulerAngles = localEulerAngles;

            RemoveCollider(primitive);
            ApplyRendererMaterial(primitive, CreateSolidMaterial(color, true));
        }

        private static Transform CreateCameraMarker(Transform parent)
        {
            GameObject markerRoot = new GameObject("MiniCameraMarker");
            markerRoot.transform.SetParent(parent, false);

            CreatePrimitivePart(markerRoot.transform, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.18f, 0.11f, 0.11f), Vector3.zero, CameraMarkerColor);
            CreatePrimitivePart(markerRoot.transform, PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0f, 0.12f), new Vector3(0.03f, 0.06f, 0.03f), new Vector3(90f, 0f, 0f), CameraMarkerColor);

            return markerRoot.transform;
        }

        private void ApplyCurrentPose(bool wrapTime)
        {
            if (_segments.Count == 0)
                return;

            PreviewPose pose = EvaluatePose(_currentTime, wrapTime);

            if (_miniCameraMarker != null)
            {
                _miniCameraMarker.localPosition = pose.Position;
                _miniCameraMarker.localRotation = Quaternion.Euler(pose.Rotation);
            }

            if (_previewCamera != null)
            {
                ApplyPreviewCameraPose(pose);
                RenderPreviewCamera();
            }
        }

        private void ApplyPreviewCameraPose(PreviewPose pose)
        {
            if (_previewCamera == null)
                return;

            Quaternion rotation = Quaternion.Euler(pose.Rotation);
            _previewCamera.transform.position = pose.Position;
            _previewCamera.transform.rotation = rotation;
            _previewCamera.fieldOfView = pose.Fov;
        }

        private PreviewPose EvaluatePose(float time, bool wrapTime)
        {
            float evaluationTime = GetEvaluationTime(time, wrapTime);
            TimelineSegment segment = _segments[_segments.Count - 1];

            for (int i = 0; i < _segments.Count; i++)
            {
                if (evaluationTime <= _segments[i].DelayEndTime || i == _segments.Count - 1)
                {
                    segment = _segments[i];
                    break;
                }
            }

            float duration = segment.EndTime - segment.StartTime;
            float perc = duration > 0f && evaluationTime < segment.EndTime
                ? Mathf.Clamp01((evaluationTime - segment.StartTime) / duration)
                : 1f;

            float easedPerc = CameraSongScriptMath.Ease(perc, segment.EaseTransition);

            Vector3 pos = CameraSongScriptMath.ApplyHeightOffset(
                CameraSongScriptMath.LerpVector3(segment.StartPos, segment.EndPos, easedPerc),
                CameraSongScriptConfig.Instance.CameraHeightOffsetCm);
            Vector3 rot = CameraSongScriptMath.LerpVector3Angle(segment.StartRot, segment.EndRot, easedPerc);
            float fov = Mathf.Lerp(segment.StartFov, segment.EndFov, easedPerc);

            if (segment.TurnToHead)
            {
                Vector3 headOffset = CameraSongScriptMath.LerpVector3(segment.StartHeadOffset, segment.EndHeadOffset, easedPerc);
                // TODO: world-space preview still uses a fixed synthetic head target; revisit against actual avatar/HMD data.
                Vector3 lookDirection = AvatarHeadTarget + headOffset - pos;
                if (lookDirection != Vector3.zero)
                {
                    Vector3 lookEuler = Quaternion.LookRotation(lookDirection).eulerAngles;
                    rot = segment.TurnToHeadHorizontal
                        ? new Vector3(rot.x, lookEuler.y, rot.z)
                        : lookEuler;
                }
            }

            return new PreviewPose
            {
                Position = pos,
                Rotation = rot,
                Fov = fov
            };
        }

        private float GetEvaluationTime(float time, bool wrapTime)
        {
            if (_duration <= 0f)
                return 0f;

            float clamped = Mathf.Clamp(time, 0f, _duration);
            if (wrapTime)
                return Mathf.Repeat(clamped, _duration);

            if (clamped >= _duration)
                return Mathf.Max(0f, _duration - EndPoseEpsilon);

            return clamped;
        }

        private float ClampPreviewTime(float time)
        {
            if (_duration <= 0f)
                return 0f;

            return Mathf.Clamp(time, 0f, _duration);
        }

        private void UpdateScreenOrientation()
        {
            if (_screenRoot == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Vector3 direction = mainCamera.transform.position - _screenRoot.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            _screenRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void DestroyPreviewObjects()
        {
            if (_previewDisplayImage != null)
            {
                _previewDisplayImage.texture = null;
                _previewDisplayImage = null;
            }

            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = null;
                UnityEngine.Object.Destroy(_previewCamera.gameObject);
                _previewCamera = null;
            }

            if (_previewTexture != null)
            {
                _previewTexture.Release();
                UnityEngine.Object.Destroy(_previewTexture);
                _previewTexture = null;
            }

            if (_visibleRoot != null)
            {
                UnityEngine.Object.Destroy(_visibleRoot);
                _visibleRoot = null;
            }

            _miniatureRoot = null;
            _screenRoot = null;
            _miniCameraMarker = null;
        }

        private void ResetLoadedState()
        {
            _segments.Clear();
            _currentTime = 0f;
            _duration = 0f;
            _speedMultiplier = 1;
            _isPlaying = false;
            _loadedScriptPath = string.Empty;
            _loadedScriptDisplayName = string.Empty;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void SyncPreviewCameraSourceSettings(Camera previewCamera)
        {
            if (previewCamera == null)
                return;

            UpdatePreviewRenderTextureAndView();

            RenderTexture targetTexture = _previewTexture;
            Camera sourceCamera = GetSourceCamera();
            int previewCullingMask = GetPreviewCullingMask();

            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.015f, 0.015f, 0.02f, 1f);
            previewCamera.cullingMask = previewCullingMask;
            previewCamera.tag = "Untagged";
            previewCamera.enabled = false;
            previewCamera.stereoTargetEye = StereoTargetEyeMask.None;
            previewCamera.targetTexture = targetTexture;
            previewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            previewCamera.depthTextureMode = DepthTextureMode.None;

            if (sourceCamera != null)
            {
                previewCamera.orthographic = sourceCamera.orthographic;
                previewCamera.nearClipPlane = sourceCamera.nearClipPlane;
                previewCamera.farClipPlane = sourceCamera.farClipPlane;
            }
            else
            {
                previewCamera.orthographic = false;
                previewCamera.nearClipPlane = 0.01f;
                previewCamera.farClipPlane = 100f;
            }

            previewCamera.aspect = targetTexture != null && targetTexture.height > 0
                ? (float)targetTexture.width / targetTexture.height
                : 16f / 9f;
        }

        private void PreparePreviewRender(bool forceRender = false)
        {
            if (_previewCamera == null)
                return;

            UpdatePreviewRenderTextureAndView();
            if (_previewTexture == null)
                return;

            SyncPreviewCameraSourceSettings(_previewCamera);

            GameObject cameraObject = _previewCamera.gameObject;
            if (!cameraObject.activeSelf)
                cameraObject.SetActive(true);

            _previewCamera.enabled = true;
            try
            {
                if (forceRender)
                    _previewCamera.Render();
            }
            finally
            {
                if (_previewCamera != null)
                    _previewCamera.enabled = false;
            }
        }

        private void RenderPreviewCamera()
        {
            PreparePreviewRender(true);
        }

        private static int GetPreviewCullingMask()
        {
            // TODO: Revisit whether helper UI objects such as the preview screen should also be excluded.
            return ~(1 << MiniatureLayer);
        }

        private static Camera GetSourceCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                return mainCamera;

            GameObject[] taggedMainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            for (int i = 0; i < taggedMainCameras.Length; i++)
            {
                if (taggedMainCameras[i] == null)
                    continue;

                Camera camera = taggedMainCameras[i].GetComponent<Camera>();
                if (camera != null)
                    return camera;
            }

            return null;
        }

        private static void RemovePreviewCameraComponents(GameObject cameraObject, Camera previewCamera)
        {
            if (cameraObject == null)
                return;

            Component[] components = cameraObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component is Transform || ReferenceEquals(component, previewCamera))
                    continue;

                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
                return;

            target.layer = layer;
            Transform transform = target.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                    SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            if (target == null)
                return;

            Component collider = target.GetComponent("Collider");
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
        }

        private static void ApplyRendererMaterial(GameObject target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterial = material;
            renderer.sortingOrder = 60;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material CreateSolidMaterial(Color color, bool alwaysOnTop)
        {
            if (_solidMaterialTemplate == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                _solidMaterialTemplate = new Material(shader);
            }

            Material material = new Material(_solidMaterialTemplate);
            if (material.HasProperty("_Color"))
                material.color = color;
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", (int)CullMode.Off);
            if (material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", alwaysOnTop ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);
            material.renderQueue = alwaysOnTop ? 5000 : 3000;
            return material;
        }

        private struct TimelineSegment
        {
            public Vector3 StartPos;
            public Vector3 EndPos;
            public Vector3 StartRot;
            public Vector3 EndRot;
            public Vector3 StartHeadOffset;
            public Vector3 EndHeadOffset;
            public float StartFov;
            public float EndFov;
            public float StartTime;
            public float EndTime;
            public float DelayEndTime;
            public bool EaseTransition;
            public bool TurnToHead;
            public bool TurnToHeadHorizontal;
        }

        private struct PreviewPose
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public float Fov;
        }
    }
}
