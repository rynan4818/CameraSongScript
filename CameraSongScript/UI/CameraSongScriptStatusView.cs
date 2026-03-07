using System;
using CameraSongScript.Configuration;
using CameraSongScript.Detectors;
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

        private static readonly string[] PresetNames = new string[]
        {
            "LeftUpperRight",
            "LeftUpperLeft",
            "LeftLowerRight",
            "LeftLowerLeft",
            "CenterUpperRight",
            "CenterUpperLeft",
            "CenterLowerRight",
            "CenterLowerLeft",
            "RightUpperRight",
            "RightUpperLeft",
            "RightLowerRight",
            "RightLowerLeft"
        };

        public static string[] GetPresetNames() => PresetNames;

        /// <summary>
        /// コンフィグからプリセットインデックスに対応する位置を取得する
        /// </summary>
        private static Vector3 GetPresetPositionFromConfig(int index)
        {
            var cfg = CameraSongScriptConfig.Instance;
            switch (index)
            {
                case 0: return new Vector3(cfg.PresetLeftUpperRightPosX, cfg.PresetLeftUpperRightPosY, cfg.PresetLeftUpperRightPosZ);
                case 1: return new Vector3(cfg.PresetLeftUpperLeftPosX, cfg.PresetLeftUpperLeftPosY, cfg.PresetLeftUpperLeftPosZ);
                case 2: return new Vector3(cfg.PresetLeftLowerRightPosX, cfg.PresetLeftLowerRightPosY, cfg.PresetLeftLowerRightPosZ);
                case 3: return new Vector3(cfg.PresetLeftLowerLeftPosX, cfg.PresetLeftLowerLeftPosY, cfg.PresetLeftLowerLeftPosZ);

                case 4: return new Vector3(cfg.PresetCenterUpperRightPosX, cfg.PresetCenterUpperRightPosY, cfg.PresetCenterUpperRightPosZ);
                case 5: return new Vector3(cfg.PresetCenterUpperLeftPosX, cfg.PresetCenterUpperLeftPosY, cfg.PresetCenterUpperLeftPosZ);
                case 6: return new Vector3(cfg.PresetCenterLowerRightPosX, cfg.PresetCenterLowerRightPosY, cfg.PresetCenterLowerRightPosZ);
                case 7: return new Vector3(cfg.PresetCenterLowerLeftPosX, cfg.PresetCenterLowerLeftPosY, cfg.PresetCenterLowerLeftPosZ);

                case 8: return new Vector3(cfg.PresetRightUpperRightPosX, cfg.PresetRightUpperRightPosY, cfg.PresetRightUpperRightPosZ);
                case 9: return new Vector3(cfg.PresetRightUpperLeftPosX, cfg.PresetRightUpperLeftPosY, cfg.PresetRightUpperLeftPosZ);
                case 10: return new Vector3(cfg.PresetRightLowerRightPosX, cfg.PresetRightLowerRightPosY, cfg.PresetRightLowerRightPosZ);
                case 11: return new Vector3(cfg.PresetRightLowerLeftPosX, cfg.PresetRightLowerLeftPosY, cfg.PresetRightLowerLeftPosZ);

                default: return new Vector3(cfg.PresetLeftUpperRightPosX, cfg.PresetLeftUpperRightPosY, cfg.PresetLeftUpperRightPosZ);
            }
        }

        /// <summary>
        /// コンフィグからプリセットインデックスに対応する回転を取得する
        /// </summary>
        private static Vector3 GetPresetRotationFromConfig(int index)
        {
            var cfg = CameraSongScriptConfig.Instance;
            switch (index)
            {
                case 0: return new Vector3(cfg.PresetLeftUpperRightRotX, cfg.PresetLeftUpperRightRotY, cfg.PresetLeftUpperRightRotZ);
                case 1: return new Vector3(cfg.PresetLeftUpperLeftRotX, cfg.PresetLeftUpperLeftRotY, cfg.PresetLeftUpperLeftRotZ);
                case 2: return new Vector3(cfg.PresetLeftLowerRightRotX, cfg.PresetLeftLowerRightRotY, cfg.PresetLeftLowerRightRotZ);
                case 3: return new Vector3(cfg.PresetLeftLowerLeftRotX, cfg.PresetLeftLowerLeftRotY, cfg.PresetLeftLowerLeftRotZ);

                case 4: return new Vector3(cfg.PresetCenterUpperRightRotX, cfg.PresetCenterUpperRightRotY, cfg.PresetCenterUpperRightRotZ);
                case 5: return new Vector3(cfg.PresetCenterUpperLeftRotX, cfg.PresetCenterUpperLeftRotY, cfg.PresetCenterUpperLeftRotZ);
                case 6: return new Vector3(cfg.PresetCenterLowerRightRotX, cfg.PresetCenterLowerRightRotY, cfg.PresetCenterLowerRightRotZ);
                case 7: return new Vector3(cfg.PresetCenterLowerLeftRotX, cfg.PresetCenterLowerLeftRotY, cfg.PresetCenterLowerLeftRotZ);

                case 8: return new Vector3(cfg.PresetRightUpperRightRotX, cfg.PresetRightUpperRightRotY, cfg.PresetRightUpperRightRotZ);
                case 9: return new Vector3(cfg.PresetRightUpperLeftRotX, cfg.PresetRightUpperLeftRotY, cfg.PresetRightUpperLeftRotZ);
                case 10: return new Vector3(cfg.PresetRightLowerRightRotX, cfg.PresetRightLowerRightRotY, cfg.PresetRightLowerRightRotZ);
                case 11: return new Vector3(cfg.PresetRightLowerLeftRotX, cfg.PresetRightLowerLeftRotY, cfg.PresetRightLowerLeftRotZ);

                default: return new Vector3(cfg.PresetLeftUpperRightRotX, cfg.PresetLeftUpperRightRotY, cfg.PresetLeftUpperRightRotZ);
            }
        }

        public void Initialize()
        {
            CreateCanvas();
            CameraSongScriptDetector.ScanCompleted += OnScanCompleted;
            CameraSongScriptConfig.ConfigReloaded += OnConfigReloaded;
            UpdateContent();
        }

        public void Dispose()
        {
            CameraSongScriptDetector.ScanCompleted -= OnScanCompleted;
            CameraSongScriptConfig.ConfigReloaded -= OnConfigReloaded;
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

            int posIndex = Mathf.Clamp(cfg.StatusPanelPosition, 0, PresetNames.Length - 1);
            _rootObject.transform.position = GetPresetPositionFromConfig(posIndex);
            _rootObject.transform.eulerAngles = GetPresetRotationFromConfig(posIndex);
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
            int posIndex = Mathf.Clamp(cfg.StatusPanelPosition, 0, PresetNames.Length - 1);
            _rootObject.transform.position = GetPresetPositionFromConfig(posIndex);
            _rootObject.transform.eulerAngles = GetPresetRotationFromConfig(posIndex);

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
            bool hasScript = CameraSongScriptDetector.HasSongScript;

            // 表示設定OFFの場合は完全に非表示
            if (!show)
            {
                _rootObject.SetActive(false);
                return;
            }

            _rootObject.SetActive(true);

            // スクリプトなしの場合
            if (!hasScript)
            {
                _statusText.text = "<color=#888888>CameraSongScript: NONE</color>";
                return;
            }

            // 機能が無効の場合
            if (!enabled)
            {
                _statusText.text = "<color=#888888>CameraSongScript: OFF</color>";
                return;
            }

            // 以下、有効かつスクリプトありの場合
            string scriptName = CameraSongScriptDetector.SelectedScriptDisplayName;

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
            int idx = Mathf.Clamp(presetIndex, 0, PresetNames.Length - 1);
            _rootObject.transform.position = GetPresetPositionFromConfig(idx);
            _rootObject.transform.eulerAngles = GetPresetRotationFromConfig(idx);
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
