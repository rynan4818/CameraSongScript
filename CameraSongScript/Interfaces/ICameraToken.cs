using System;
using CameraSongScript.Models;
using UnityEngine;

namespace CameraSongScript.Interfaces
{
    /// <summary>
    /// Camera2 OverrideTokenの操作を抽象化するインターフェース
    /// トークンの所有権管理（Close）をIDisposableで表現する
    /// </summary>
    public interface ICameraToken : IDisposable
    {
        void SetPosition(Vector3 pos);
        void SetRotation(Vector3 rot);
        float GetFOV();
        void SetFOV(float fov);
        void ApplyVisibleObject(VisibleObject scriptVisible);
        void ResetVisibleObjects();
    }
}
