using System;
using CameraSongScript.Configuration;
using CameraSongScript.HarmonyPatches;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CameraSongScript.UI
{
    /// <summary>
    /// メニューシーンの3D空間にステータス情報を表示するWorldSpaceパネル。
    /// KosorenToolのKosorenInfoViewと同じパターン（WorldSpace Canvas + CurvedTextMeshPro）を採用。
    /// </summary>
    public class CameraSongScriptStatusView : MonoBehaviour, IInitializable, IDisposable
    {
        private GameObject _rootObject;
        private Canvas _canvas;
        private CurvedTextMeshPro _statusText;

        private static readonly Vector2 CanvasSize = new Vector2(100, 10);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        /// <summary>
        /// プリセット位置の定義（3箇所）
        /// </summary>
        private static readonly Vector3[] PresetPositions = new Vector3[]
        {
            new Vector3(1.0f, 3.0f, 4.5f),   // 0: Left
            new Vector3(2.5f, 3.0f, 4.5f),    // 1: Right
            new Vector3(1.0f, 2.5f, 4.5f),    // 2: Bottom
        };

        private static readonly string[] PresetNames = new string[]
        {
            "Left",
            "Right",
            "Bottom"
        };

        public static string[] GetPresetNames() => PresetNames;

        public void Initialize()
        {
            CreateCanvas();
            CameraSongScriptDetector.ScanCompleted += OnScanCompleted;
            UpdateContent();
        }

        public void Dispose()
        {
            CameraSongScriptDetector.ScanCompleted -= OnScanCompleted;
            if (_rootObject != null)
                Destroy(_rootObject);
        }

        private void CreateCanvas()
        {
            _rootObject = new GameObject("CameraSongScript Status Canvas", typeof(Canvas), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var sizeFitter = _rootObject.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _canvas = _rootObject.GetComponent<Canvas>();
            _canvas.sortingOrder = 3;
            _canvas.renderMode = RenderMode.WorldSpace;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = CanvasSize;

            int posIndex = Mathf.Clamp(CameraSongScriptConfig.Instance.StatusPanelPosition, 0, PresetPositions.Length - 1);
            _rootObject.transform.position = PresetPositions[posIndex];
            _rootObject.transform.eulerAngles = Vector3.zero;
            _rootObject.transform.localScale = Scale;

            _statusText = CreateText(_canvas.transform as RectTransform, string.Empty, new Vector2(10, 31));
            var textRect = _statusText.transform as RectTransform;
            textRect.SetParent(_canvas.transform, false);
            textRect.anchoredPosition = Vector2.zero;
            _statusText.fontSize = 3.5f;
            _statusText.overrideColorTags = false;
            _statusText.richText = true;
            _statusText.color = Color.white;
        }

        private static CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector2 anchoredPosition)
        {
            var gameObj = new GameObject("CSSStatusText");
            gameObj.SetActive(false);
            var textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 3.5f;
            textMesh.overrideColorTags = false;
            textMesh.color = Color.white;
            textMesh.rectTransform.anchorMin = new Vector2(0f, 0f);
            textMesh.rectTransform.anchorMax = new Vector2(0f, 0f);
            textMesh.rectTransform.sizeDelta = Vector2.zero;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;
            gameObj.SetActive(true);
            return textMesh;
        }

        private void OnScanCompleted()
        {
            HMMainThreadDispatcher.instance?.Enqueue(() =>
            {
                try
                {
                    UpdateContent();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"StatusView: Failed to update: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// ステータス表示内容を更新する
        /// </summary>
        public void UpdateContent()
        {
            if (_rootObject == null) return;

            bool show = CameraSongScriptConfig.Instance.ShowStatusPanel;
            bool enabled = CameraSongScriptConfig.Instance.Enabled;
            bool hasScript = CameraSongScriptDetector.HasSongScript;

            // 表示設定OFF、機能無効、スクリプトなしの場合は非表示
            if (!show || !enabled || !hasScript)
            {
                _rootObject.SetActive(false);
                return;
            }

            _rootObject.SetActive(true);

            string scriptName = System.IO.Path.GetFileName(CameraSongScriptDetector.SelectedScriptPath);

            string statusLine = $"<color=#00FF00>CameraSongScript: ON</color>";
            string scriptLine = $"<color=#AAAAAA>Script:</color> {scriptName}";

            var meta = CameraSongScriptDetector.CurrentMetadata;
            string metaLine = string.Empty;
            if (meta != null)
            {
                string author = !string.IsNullOrEmpty(meta.cameraScriptAuthorName)
                    ? meta.cameraScriptAuthorName : "--";
                string song = !string.IsNullOrEmpty(meta.songName)
                    ? meta.songName : "--";
                metaLine = $"<color=#AAAAAA>Author:</color> {author} <color=#AAAAAA>| Song:</color> {song}";
            }

            int offsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            string offsetLine = string.Empty;
            if (offsetCm != 0)
            {
                offsetLine = $"<color=#FFFF00>Y Offset: {offsetCm}cm</color>";
            }

            string fullText = statusLine;
            fullText += "\n" + scriptLine;
            if (!string.IsNullOrEmpty(metaLine))
                fullText += "\n" + metaLine;
            if (!string.IsNullOrEmpty(offsetLine))
                fullText += "\n" + offsetLine;

            _statusText.text = fullText;
        }

        /// <summary>
        /// プリセット位置を変更する
        /// </summary>
        public void SetPosition(int presetIndex)
        {
            if (_rootObject == null) return;
            int idx = Mathf.Clamp(presetIndex, 0, PresetPositions.Length - 1);
            _rootObject.transform.position = PresetPositions[idx];
        }

        /// <summary>
        /// パネルの表示/非表示を切り替える
        /// </summary>
        public void SetVisible(bool visible)
        {
            UpdateContent();
        }
    }
}
