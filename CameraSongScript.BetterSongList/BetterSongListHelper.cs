using System;
using BetterSongList;
using CameraSongScript.Interfaces;

namespace CameraSongScript.BetterSongList
{
    public class BetterSongListHelper : IBetterSongListHelper
    {
        private readonly SongScriptFilter _filter;
        private readonly SongScriptSorter _sorter;
        private bool _filterRegistered;
        private bool _sorterRegistered;

        public bool IsInitialized { get; private set; }

        internal BetterSongListHelper(SongScriptFilter filter, SongScriptSorter sorter)
        {
            _filter = filter;
            _sorter = sorter;
        }

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

        private bool EnsureFilterRegistered()
        {
            if (_filterRegistered)
            {
                return true;
            }

            try
            {
                _filterRegistered = FilterMethods.Register(_filter);
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

        private bool EnsureSorterRegistered()
        {
            if (_sorterRegistered)
            {
                return true;
            }

            try
            {
                _sorterRegistered = SortMethods.RegisterCustomSorter(_sorter);
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
