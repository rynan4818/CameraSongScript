using System;
using BetterSongList;
using CameraSongScript.Interfaces;

namespace CameraSongScript.BetterSongList
{
    public class BetterSongListHelper : IBetterSongListHelper
    {
        private static readonly SongScriptFilter FilterInstance = new SongScriptFilter();
        private static bool _registered;

        public bool IsInitialized { get; private set; }

        public bool Initialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (_registered)
            {
                IsInitialized = true;
                return true;
            }

            try
            {
                var registered = FilterMethods.Register(FilterInstance);
                if (!registered)
                {
                    return false;
                }

                _registered = true;
                IsInitialized = true;
                return true;
            }
            catch (ArgumentException)
            {
                // BetterSongList already has this filter registered.
                _registered = true;
                IsInitialized = true;
                return true;
            }
            catch (Exception)
            {
                _registered = false;
                IsInitialized = false;
                return false;
            }
        }
    }
}
