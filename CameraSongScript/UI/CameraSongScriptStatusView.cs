using System;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
using CameraSongScript.Localization;
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
        private CameraSongScriptDetector _scriptDetector;

        private GameObject _rootObject;
        private Canvas _canvas;
        private CurvedTextMeshPro _statusText;

        public static string[] GetPresetNames() => StatusPanelPresetCatalog.GetLegacyNames();

        [Inject]
        internal void Constractor(CameraSongScriptDetector scriptDetector)
        {
            _scriptDetector = scriptDetector;
        }

        public void Initialize()
        {
            CreateCanvas();
            _scriptDetector.ScanCompleted += OnScanCompleted;
            CameraSongScriptConfig.ConfigReloaded += OnConfigReloaded;
            UiLocalization.LanguageChanged += OnLanguageChanged;
            UpdateContent();
        }

        public void Dispose()
        {
            _scriptDetector.ScanCompleted -= OnScanCompleted;
            CameraSongScriptConfig.ConfigReloaded -= OnConfigReloaded;
            UiLocalization.LanguageChanged -= OnLanguageChanged;
            if (_rootObject != null)
                Destroy(_rootObject);
        }

        private void CreateCanvas()
        {
            var cfg = CameraSongScriptConfig.Instance;

            _rootObject = new GameObject("CameraSongScript Status Canvas", typeof(Canvas), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var sizeFitter = _rootObject.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _canvas = _rootObject.GetComponent<Canvas>();
            _canvas.sortingOrder = 3;
            _canvas.renderMode = RenderMode.WorldSpace;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(cfg.StatusCanvasWidth, cfg.StatusCanvasHeight);

            int posIndex = StatusPanelPresetCatalog.ClampIndex(cfg.StatusPanelPosition);
            _rootObject.transform.position = StatusPanelPresetCatalog.GetPosition(cfg, posIndex);
            _rootObject.transform.eulerAngles = StatusPanelPresetCatalog.GetRotation(cfg, posIndex);
            float s = cfg.StatusScale;
            _rootObject.transform.localScale = new Vector3(s, s, s);

            _statusText = CreateText(_canvas.transform as RectTransform, string.Empty, new Vector2(10, 31), cfg.StatusFontSize);
            var textRect = _statusText.transform as RectTransform;
            textRect.SetParent(_canvas.transform, false);
            textRect.anchoredPosition = Vector2.zero;
            _statusText.fontSize = cfg.StatusFontSize;
            _statusText.overrideColorTags = false;
            _statusText.richText = true;
            _statusText.color = Color.white;
        }

        private static CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector2 anchoredPosition, float fontSize)
        {
            var gameObj = new GameObject("CSSStatusText");
            gameObj.SetActive(false);
            var textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = fontSize;
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

        private void OnLanguageChanged()
        {
            HMMainThreadDispatcher.instance?.Enqueue(() =>
            {
                try
                {
                    UpdateContent();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"StatusView: Failed to apply language change: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// コンフィグがホットリロードされたときのコールバック。
        /// BSIPAのバックグラウンドスレッドから呼ばれるため、メインスレッドへディスパッチする。
        /// </summary>
        private void OnConfigReloaded()
        {
            HMMainThreadDispatcher.instance?.Enqueue(() =>
            {
                try
                {
                    ApplyVisualConfig();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"StatusView: Failed to apply config reload: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// コンフィグの全ビジュアルプロパティを現在のキャンバスに適用する
        /// </summary>
        private void ApplyVisualConfig()
        {
            if (_rootObject == null || _canvas == null || _statusText == null) return;

            var cfg = CameraSongScriptConfig.Instance;

            // 位置・回転
            int posIndex = StatusPanelPresetCatalog.ClampIndex(cfg.StatusPanelPosition);
            _rootObject.transform.position = StatusPanelPresetCatalog.GetPosition(cfg, posIndex);
            _rootObject.transform.eulerAngles = StatusPanelPresetCatalog.GetRotation(cfg, posIndex);

            // スケール
            float s = cfg.StatusScale;
            _rootObject.transform.localScale = new Vector3(s, s, s);

            // キャンバスサイズ
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(cfg.StatusCanvasWidth, cfg.StatusCanvasHeight);

            // フォントサイズ
            _statusText.fontSize = cfg.StatusFontSize;

            // 表示内容
            UpdateContent();
        }

        /// <summary>
        /// ステータス表示内容を更新する
        /// </summary>
        public void UpdateContent()
        {
            if (_rootObject == null) return;

            bool show = CameraSongScriptConfig.Instance.ShowStatusPanel;
            bool enabled = CameraSongScriptConfig.Instance.Enabled;
            bool hasScript = _scriptDetector.HasSongScript;
            bool isCommon = _scriptDetector.IsUsingCommonScript;

            // 表示設定OFFの場合は完全に非表示
            if (!show)
            {
                _rootObject.SetActive(false);
                return;
            }

            _rootObject.SetActive(true);

            // 汎用スクリプト使用中の場合
            if (isCommon)
            {
                string commonName = CameraSongScriptConfig.Instance.SelectedCommonScript == UiLocalization.OptionRandom
                    ? UiLocalization.GetOptionDisplay(UiLocalization.OptionRandom, UiLocalization.OptionRandom)
                    : _scriptDetector.ResolvedCommonScriptDisplayName;
                if (string.IsNullOrEmpty(commonName))
                    commonName = UiLocalization.GetOptionDisplay(
                        CameraSongScriptConfig.Instance.SelectedCommonScript,
                        UiLocalization.OptionRandom);

                string commonText = UiLocalization.Get("panel-common");
                commonText += "\n" + UiLocalization.Format("panel-script-line", commonName);

                // ランダム時はオフセット表示なし（対象スクリプトが不明なため）
                bool isRandom = CameraSongScriptConfig.Instance.SelectedCommonScript == UiLocalization.OptionRandom;
                if (!isRandom)
                {
                    int commonOffsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
                    if (commonOffsetCm != 0)
                    {
                        commonText += "\n" + UiLocalization.Format("panel-y-offset-line", commonOffsetCm);
                    }
                }

                _statusText.text = AppendCamera2UnsupportedWarning(commonText);
                return;
            }

            // スクリプトなしの場合
            if (!hasScript)
            {
                _statusText.text = UiLocalization.Get("panel-none");
                return;
            }

            // 機能が無効の場合
            if (!enabled)
            {
                _statusText.text = AppendCamera2UnsupportedWarning(UiLocalization.Get("panel-off"));
                return;
            }

            // 以下、有効かつスクリプトありの場合
            string scriptName = _scriptDetector.SelectedScriptDisplayName;

            string statusLine = UiLocalization.Get("panel-on");
            string scriptLine = UiLocalization.Format("panel-script-line", scriptName);

            var meta = _scriptDetector.CurrentMetadata;
            string metaLine = string.Empty;
            if (meta != null)
            {
                string author = !string.IsNullOrEmpty(meta.cameraScriptAuthorName)
                    ? meta.cameraScriptAuthorName : "--";
                string song = !string.IsNullOrEmpty(meta.songName)
                    ? meta.songName : "--";
                metaLine = UiLocalization.Format("panel-author-song-line", author, song);
            }

            int offsetCm = CameraSongScriptConfig.Instance.CameraHeightOffsetCm;
            string offsetLine = string.Empty;
            if (offsetCm != 0)
            {
                offsetLine = UiLocalization.Format("panel-y-offset-line", offsetCm);
            }

            string fullText = statusLine;
            fullText += "\n" + scriptLine;
            if (!string.IsNullOrEmpty(metaLine))
                fullText += "\n" + metaLine;
            if (!string.IsNullOrEmpty(offsetLine))
                fullText += "\n" + offsetLine;

            _statusText.text = AppendCamera2UnsupportedWarning(fullText);
        }

        private string AppendCamera2UnsupportedWarning(string statusText)
        {
            if (!CameraModDetector.IsCamera2 || !_scriptDetector.HasCurrentUnsupportedFeatures)
                return statusText;

            string warningText = UiLocalization.Format(
                "warning-camera2-unsupported",
                _scriptDetector.CurrentUnsupportedFeatureSummary);
            return string.IsNullOrEmpty(statusText) ? warningText : $"{statusText}\n{warningText}";
        }

        /// <summary>
        /// プリセット位置を変更する
        /// </summary>
        public void SetPosition(int presetIndex)
        {
            if (_rootObject == null) return;
            int idx = StatusPanelPresetCatalog.ClampIndex(presetIndex);
            var cfg = CameraSongScriptConfig.Instance;
            _rootObject.transform.position = StatusPanelPresetCatalog.GetPosition(cfg, idx);
            _rootObject.transform.eulerAngles = StatusPanelPresetCatalog.GetRotation(cfg, idx);
        }

        /// <summary>
        /// パネルの表示/非表示を切り替える
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_rootObject == null)
                return;

            if (!visible)
            {
                _rootObject.SetActive(false);
                return;
            }

            UpdateContent();
        }
    }
}



