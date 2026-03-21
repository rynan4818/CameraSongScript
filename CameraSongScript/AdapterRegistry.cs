using System;
using CameraSongScript.Detectors;
using CameraSongScript.Interfaces;

namespace CameraSongScript
{
    public static class AdapterRegistry
    {
        private static readonly object SyncRoot = new object();

        private static ICameraHelper _cameraHelper;
        private static ICameraPlusHelper _cameraPlusHelper;
        private static IHttpSiraStatusHelper _httpSiraStatusHelper;
        private static IBetterSongListHelper _betterSongListHelper;

        public static ICameraHelper CameraHelper
        {
            get
            {
                lock (SyncRoot)
                {
                    return _cameraHelper;
                }
            }
        }

        public static ICameraPlusHelper CameraPlusHelper
        {
            get
            {
                lock (SyncRoot)
                {
                    return _cameraPlusHelper;
                }
            }
        }

        public static IHttpSiraStatusHelper HttpSiraStatusHelper
        {
            get
            {
                lock (SyncRoot)
                {
                    return _httpSiraStatusHelper;
                }
            }
        }

        public static IBetterSongListHelper BetterSongListHelper
        {
            get
            {
                lock (SyncRoot)
                {
                    return _betterSongListHelper;
                }
            }
        }

        public static void RegisterCameraHelper(ICameraHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            bool changed;
            lock (SyncRoot)
            {
                changed = !ReferenceEquals(_cameraHelper, helper);
                _cameraHelper = helper;
            }

            CameraModDetector.RegisterDetectedMod(CameraModType.Camera2);
            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void UnregisterCameraHelper(ICameraHelper helper = null)
        {
            bool changed = false;
            lock (SyncRoot)
            {
                if (_cameraHelper != null && (helper == null || ReferenceEquals(_cameraHelper, helper)))
                {
                    _cameraHelper = null;
                    changed = true;
                }
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void RegisterCameraPlusHelper(ICameraPlusHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            bool changed;
            lock (SyncRoot)
            {
                changed = !ReferenceEquals(_cameraPlusHelper, helper);
                _cameraPlusHelper = helper;
            }

            CameraModDetector.RegisterDetectedMod(CameraModType.CameraPlus);
            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void UnregisterCameraPlusHelper(ICameraPlusHelper helper = null)
        {
            bool changed = false;
            lock (SyncRoot)
            {
                if (_cameraPlusHelper != null && (helper == null || ReferenceEquals(_cameraPlusHelper, helper)))
                {
                    _cameraPlusHelper = null;
                    changed = true;
                }
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void RegisterHttpSiraStatusHelper(IHttpSiraStatusHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            bool changed;
            lock (SyncRoot)
            {
                changed = !ReferenceEquals(_httpSiraStatusHelper, helper);
                _httpSiraStatusHelper = helper;
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void UnregisterHttpSiraStatusHelper(IHttpSiraStatusHelper helper = null)
        {
            bool changed = false;
            lock (SyncRoot)
            {
                if (_httpSiraStatusHelper != null && (helper == null || ReferenceEquals(_httpSiraStatusHelper, helper)))
                {
                    _httpSiraStatusHelper = null;
                    changed = true;
                }
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void RegisterBetterSongListHelper(IBetterSongListHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            bool changed;
            lock (SyncRoot)
            {
                changed = !ReferenceEquals(_betterSongListHelper, helper);
                _betterSongListHelper = helper;
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }

        public static void UnregisterBetterSongListHelper(IBetterSongListHelper helper = null)
        {
            bool changed = false;
            lock (SyncRoot)
            {
                if (_betterSongListHelper != null && (helper == null || ReferenceEquals(_betterSongListHelper, helper)))
                {
                    _betterSongListHelper = null;
                    changed = true;
                }
            }

            if (changed)
            {
                PluginAdapterManager.NotifyAdapterStateChanged();
            }
        }
    }
}
