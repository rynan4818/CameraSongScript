using Camera2.Configuration;
using Camera2.SDK;
using CameraSongScript.Interfaces;
using CameraSongScript.Models;
using UnityEngine;

namespace CameraSongScript.Cam2
{
    /// <summary>
    /// Camera2 OverrideTokenをラップするICameraToken実装
    /// </summary>
    public class Camera2Token : ICameraToken
    {
        private readonly OverrideToken _token;

        public Camera2Token(OverrideToken token)
        {
            _token = token;
        }

        public void SetPosition(Vector3 pos)
        {
            _token.position = pos;
        }

        public void SetRotation(Vector3 rot)
        {
            _token.rotation = rot;
        }

        public float GetFOV()
        {
            return _token.FOV;
        }

        public void SetFOV(float fov)
        {
            _token.FOV = fov;
        }

        public void ApplyVisibleObject(VisibleObject scriptVisible)
        {
            var go = _token.visibleObjects;
            if (go == null) return;

            if (scriptVisible.avatar.HasValue)
                go.Avatar = scriptVisible.avatar.Value ? AvatarVisibility.Visible : AvatarVisibility.Hidden;
            if (scriptVisible.notes.HasValue)
                go.Notes = scriptVisible.notes.Value ? NoteVisibility.Visible : NoteVisibility.Hidden;
            if (scriptVisible.ui.HasValue)
                go.UI = scriptVisible.ui.Value;
            if (scriptVisible.saber.HasValue)
                go.Sabers = scriptVisible.saber.Value;
            if (scriptVisible.debris.HasValue)
                go.Debris = scriptVisible.debris.Value;
            if (scriptVisible.wall.HasValue)
                go.Walls = scriptVisible.wall.Value ? WallVisiblity.Visible : WallVisiblity.Hidden;
            if (scriptVisible.wallFrame.HasValue && scriptVisible.wallFrame.Value)
                go.Walls = WallVisiblity.Transparent;
            if (scriptVisible.cutParticles.HasValue)
                go.CutParticles = scriptVisible.cutParticles.Value;

            _token.UpdateVisibleObjects();
        }

        public void ResetVisibleObjects()
        {
            var go = _token.visibleObjects;
            if (go == null) return;

            go.Avatar = AvatarVisibility.Visible;
            go.Notes = NoteVisibility.Visible;
            go.UI = true;
            go.Sabers = true;
            go.Debris = true;
            go.Walls = WallVisiblity.Visible;
            go.CutParticles = true;

            _token.UpdateVisibleObjects();
        }

        public void Dispose()
        {
            _token?.Close();
        }
    }
}
