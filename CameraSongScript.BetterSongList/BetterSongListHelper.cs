using System;
using BetterSongList;
using CameraSongScript.Interfaces;

namespace CameraSongScript.BetterSongList
{
    public class BetterSongListHelper : IBetterSongListHelper
    {
        private static readonly SongScriptFilter FilterInstance = new SongScriptFilter();
        private static readonly SongScriptSorter SorterInstance = new SongScriptSorter();
        private static bool _filterRegistered;
        private static bool _sorterRegistered;

        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (_filterRegistered && _sorterRegistered)
            {
                IsInitialized = true;
                return true;
            }

            try
            {
                if (!EnsureFilterRegistered())
                {
                    Plugin.Log.Warn("BetterSongList filter registration did not complete successfully.");
                    return false;
                }

                if (!EnsureSorterRegistered())
                {
                    Plugin.Log.Warn("BetterSongList sorter registration did not complete successfully.");
                    return false;
                }

                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"BetterSongList helper initialization failed: {ex}");
                IsInitialized = false;
                return false;
            }
        }

        private static bool EnsureFilterRegistered()
        {
            if (_filterRegistered)
            {
                return true;
            }

            try
            {
                _filterRegistered = FilterMethods.Register(FilterInstance);
                if (!_filterRegistered)
                {
                    Plugin.Log.Warn("BetterSongList filter registration was rejected. AllowPluginSortsAndFilters may be disabled.");
                }

                return _filterRegistered;
            }
            catch (ArgumentException ex)
            {
                Plugin.Log.Warn($"BetterSongList filter registration failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"BetterSongList filter registration failed unexpectedly: {ex}");
                return false;
            }
        }

        private static bool EnsureSorterRegistered()
        {
            if (_sorterRegistered)
            {
                return true;
            }

            try
            {
                _sorterRegistered = SortMethods.RegisterCustomSorter(SorterInstance);
                if (!_sorterRegistered)
                {
                    Plugin.Log.Warn("BetterSongList sorter registration was rejected. AllowPluginSortsAndFilters may be disabled.");
                }

                return _sorterRegistered;
            }
            catch (ArgumentException ex)
            {
                Plugin.Log.Warn($"BetterSongList sorter registration failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"BetterSongList sorter registration failed unexpectedly: {ex}");
                return false;
            }
        }
    }
}
