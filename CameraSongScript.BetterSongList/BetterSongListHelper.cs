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
                    return false;
                }

                if (!EnsureSorterRegistered())
                {
                    return false;
                }

                IsInitialized = true;
                return true;
            }
            catch (Exception)
            {
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
                return _filterRegistered;
            }
            catch (ArgumentException)
            {
                _filterRegistered = true;
                return true;
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
                return _sorterRegistered;
            }
            catch (ArgumentException)
            {
                _sorterRegistered = true;
                return true;
            }
        }
    }
}
