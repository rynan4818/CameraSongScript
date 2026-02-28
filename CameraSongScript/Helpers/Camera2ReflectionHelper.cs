using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace CameraSongScript.Helpers
{
    /// <summary>
    /// Camera2 SDKの全呼び出しをリフレクションでラップするクラス
    /// コンパイル時のCamera2.dll依存を排除する
    /// </summary>
    public class Camera2ReflectionHelper
    {
        // Camera2アセンブリ
        private Assembly _camera2Assembly;

        // OverrideToken
        private Type _overrideTokenType;
        private MethodInfo _getTokenForCamera;
        // 毎フレーム呼ばれる setter/getter はコンパイル済みデリゲートで Reflection コストを排除
        private Action<object, Vector3> _setTokenPosition;
        private Action<object, Vector3> _setTokenRotation;
        private Action<object, float> _setTokenFOV;
        private Func<object, float> _getTokenFOV;
        private PropertyInfo _tokenVisibleObjects;
        private MethodInfo _tokenClose;

        // Cameras
        private Type _camerasType;
        private PropertyInfo _camerasActive;
        private PropertyInfo _camerasAvailable;

        // GameObjects (Camera2.Configuration.GameObjects)
        private Type _gameObjectsType;
        private PropertyInfo _goAvatar;
        private PropertyInfo _goNotes;
        private PropertyInfo _goUI;
        private PropertyInfo _goSabers;
        private PropertyInfo _goDebris;
        private PropertyInfo _goWalls;
        private PropertyInfo _goCutParticles;

        // Enum types
        private Type _avatarVisibilityType;
        private Type _noteVisibilityType;
        private Type _wallVisibilityType;

        // Enum cached values
        private object _avatarVisible;
        private object _avatarHidden;
        private object _noteVisible;
        private object _noteHidden;
        private object _wallVisible;
        private object _wallTransparent;
        private object _wallHidden;

        private bool _initialized = false;
        public bool IsInitialized => _initialized;

        public bool Initialize()
        {
            try
            {
                _camera2Assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Camera2");

                if (_camera2Assembly == null)
                {
                    Plugin.Log.Error("Camera2 assembly not found");
                    return false;
                }

                // SDK types
                _overrideTokenType = _camera2Assembly.GetType("Camera2.SDK.OverrideToken");
                _camerasType = _camera2Assembly.GetType("Camera2.SDK.Cameras");
                _gameObjectsType = _camera2Assembly.GetType("Camera2.Configuration.GameObjects");

                // Enum types
                _avatarVisibilityType = _camera2Assembly.GetType("Camera2.Configuration.AvatarVisibility");
                _noteVisibilityType = _camera2Assembly.GetType("Camera2.Configuration.NoteVisibility");
                _wallVisibilityType = _camera2Assembly.GetType("Camera2.Configuration.WallVisiblity"); // typo in Camera2

                if (_overrideTokenType == null || _camerasType == null || _gameObjectsType == null)
                {
                    Plugin.Log.Error("Camera2 SDK types not found");
                    return false;
                }

                // OverrideToken static method
                _getTokenForCamera = _overrideTokenType.GetMethod("GetTokenForCamera",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);

                // OverrideToken instance - position/rotation are FIELDS, not properties
                // コンパイル済みデリゲートで毎フレームの Reflection コスト・ボクシングを排除
                var posField = _overrideTokenType.GetField("position", BindingFlags.Public | BindingFlags.Instance);
                var rotField = _overrideTokenType.GetField("rotation", BindingFlags.Public | BindingFlags.Instance);
                var fovProp  = _overrideTokenType.GetProperty("FOV", BindingFlags.Public | BindingFlags.Instance);
                _setTokenPosition = BuildFieldSetter<Vector3>(posField);
                _setTokenRotation = BuildFieldSetter<Vector3>(rotField);
                _setTokenFOV      = BuildPropertySetter<float>(fovProp);
                _getTokenFOV      = BuildPropertyGetter<float>(fovProp);
                _tokenVisibleObjects = _overrideTokenType.GetProperty("visibleObjects", BindingFlags.Public | BindingFlags.Instance);
                _tokenClose = _overrideTokenType.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance);

                // GameObjects properties
                _goAvatar = _gameObjectsType.GetProperty("Avatar", BindingFlags.Public | BindingFlags.Instance);
                _goNotes = _gameObjectsType.GetProperty("Notes", BindingFlags.Public | BindingFlags.Instance);
                _goUI = _gameObjectsType.GetProperty("UI", BindingFlags.Public | BindingFlags.Instance);
                _goSabers = _gameObjectsType.GetProperty("Sabers", BindingFlags.Public | BindingFlags.Instance);
                _goDebris = _gameObjectsType.GetProperty("Debris", BindingFlags.Public | BindingFlags.Instance);
                _goWalls = _gameObjectsType.GetProperty("Walls", BindingFlags.Public | BindingFlags.Instance);
                _goCutParticles = _gameObjectsType.GetProperty("CutParticles", BindingFlags.Public | BindingFlags.Instance);

                // Cameras static properties
                _camerasActive = _camerasType.GetProperty("active", BindingFlags.Public | BindingFlags.Static);
                _camerasAvailable = _camerasType.GetProperty("available", BindingFlags.Public | BindingFlags.Static);

                // Cache enum values
                // AvatarVisibility: Hidden=0, Visible=1
                // NoteVisibility: Hidden=0, Visible=1
                // WallVisiblity: Visible=0, Transparent=1, Hidden=2
                if (_avatarVisibilityType != null)
                {
                    _avatarVisible = Enum.ToObject(_avatarVisibilityType, 1);
                    _avatarHidden = Enum.ToObject(_avatarVisibilityType, 0);
                }
                if (_noteVisibilityType != null)
                {
                    _noteVisible = Enum.ToObject(_noteVisibilityType, 1);
                    _noteHidden = Enum.ToObject(_noteVisibilityType, 0);
                }
                if (_wallVisibilityType != null)
                {
                    _wallVisible = Enum.ToObject(_wallVisibilityType, 0);
                    _wallTransparent = Enum.ToObject(_wallVisibilityType, 1);
                    _wallHidden = Enum.ToObject(_wallVisibilityType, 2);
                }

                _initialized = true;
                Plugin.Log.Info("Camera2ReflectionHelper initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Camera2ReflectionHelper init failed: {ex.Message}");
                return false;
            }
        }

        #region OverrideToken

        public object GetTokenForCamera(string camName)
        {
            if (!_initialized || _getTokenForCamera == null) return null;
            try
            {
                return _getTokenForCamera.Invoke(null, new object[] { camName });
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"GetTokenForCamera failed: {ex.Message}");
                return null;
            }
        }

        public void SetTokenPosition(object token, Vector3 pos)
        {
            if (!_initialized || token == null || _setTokenPosition == null) return;
            _setTokenPosition(token, pos);
        }

        public void SetTokenRotation(object token, Vector3 rot)
        {
            if (!_initialized || token == null || _setTokenRotation == null) return;
            _setTokenRotation(token, rot);
        }

        public float GetTokenFOV(object token)
        {
            if (!_initialized || token == null || _getTokenFOV == null) return 90f;
            return _getTokenFOV(token);
        }

        public void SetTokenFOV(object token, float fov)
        {
            if (!_initialized || token == null || _setTokenFOV == null) return;
            _setTokenFOV(token, fov);
        }

        public object GetTokenVisibleObjects(object token)
        {
            if (!_initialized || token == null || _tokenVisibleObjects == null) return null;
            return _tokenVisibleObjects.GetValue(token);
        }

        /// <summary>
        /// OverrideTokenをCloseする（IDisposableではなくClose()メソッド）
        /// </summary>
        public void CloseToken(object token)
        {
            if (!_initialized || token == null || _tokenClose == null) return;
            try
            {
                _tokenClose.Invoke(token, null);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CloseToken failed: {ex.Message}");
            }
        }

        // --- コンパイル済みデリゲートビルダー ---

        private static Action<object, T> BuildFieldSetter<T>(FieldInfo field)
        {
            var objParam   = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(T), "value");
            var body = Expression.Assign(
                Expression.Field(Expression.Convert(objParam, field.DeclaringType), field),
                valueParam);
            return Expression.Lambda<Action<object, T>>(body, objParam, valueParam).Compile();
        }

        private static Action<object, T> BuildPropertySetter<T>(PropertyInfo prop)
        {
            var objParam   = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(T), "value");
            var body = Expression.Assign(
                Expression.Property(Expression.Convert(objParam, prop.DeclaringType), prop),
                valueParam);
            return Expression.Lambda<Action<object, T>>(body, objParam, valueParam).Compile();
        }

        private static Func<object, T> BuildPropertyGetter<T>(PropertyInfo prop)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var body     = Expression.Property(Expression.Convert(objParam, prop.DeclaringType), prop);
            return Expression.Lambda<Func<object, T>>(body, objParam).Compile();
        }

        #endregion

        #region GameObjects visibility

        public void SetVisibleAvatar(object go, bool visible)
        {
            if (go == null || _goAvatar == null) return;
            _goAvatar.SetValue(go, visible ? _avatarVisible : _avatarHidden);
        }

        public void SetVisibleNotes(object go, bool visible)
        {
            if (go == null || _goNotes == null) return;
            _goNotes.SetValue(go, visible ? _noteVisible : _noteHidden);
        }

        public void SetVisibleUI(object go, bool visible)
        {
            if (go == null || _goUI == null) return;
            _goUI.SetValue(go, visible);
        }

        public void SetVisibleSabers(object go, bool visible)
        {
            if (go == null || _goSabers == null) return;
            _goSabers.SetValue(go, visible);
        }

        public void SetVisibleDebris(object go, bool visible)
        {
            if (go == null || _goDebris == null) return;
            _goDebris.SetValue(go, visible);
        }

        public void SetVisibleWalls(object go, bool visible)
        {
            if (go == null || _goWalls == null) return;
            _goWalls.SetValue(go, visible ? _wallVisible : _wallHidden);
        }

        public void SetVisibleWallsTransparent(object go)
        {
            if (go == null || _goWalls == null) return;
            _goWalls.SetValue(go, _wallTransparent);
        }

        public void SetVisibleCutParticles(object go, bool visible)
        {
            if (go == null || _goCutParticles == null) return;
            _goCutParticles.SetValue(go, visible);
        }

        /// <summary>
        /// VisibleObjectsをデフォルト状態にリセット
        /// </summary>
        public void ResetVisibleObjects(object go)
        {
            if (go == null) return;
            SetVisibleAvatar(go, true);
            SetVisibleNotes(go, true);
            SetVisibleUI(go, true);
            SetVisibleSabers(go, true);
            SetVisibleDebris(go, true);
            SetVisibleWalls(go, true);
            SetVisibleCutParticles(go, true);
        }

        #endregion

        #region Cameras

        public IEnumerable<string> GetActiveCameras()
        {
            if (!_initialized || _camerasActive == null) return Enumerable.Empty<string>();
            try
            {
                return _camerasActive.GetValue(null) as IEnumerable<string> ?? Enumerable.Empty<string>();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetAvailableCameras()
        {
            if (!_initialized || _camerasAvailable == null) return Enumerable.Empty<string>();
            try
            {
                return _camerasAvailable.GetValue(null) as IEnumerable<string> ?? Enumerable.Empty<string>();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        #endregion
    }
}
