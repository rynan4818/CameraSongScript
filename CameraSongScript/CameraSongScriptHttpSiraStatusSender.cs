using System;
using Zenject;

namespace CameraSongScript
{
    /// <summary>
    /// プレイシーン開始時にHttpSiraStatusへCameraSongScript情報を送信する
    /// </summary>
    public class CameraSongScriptHttpSiraStatusSender : IInitializable
    {
        private readonly CameraSongScriptPlayContextResolver _playContextResolver;

        internal CameraSongScriptHttpSiraStatusSender(CameraSongScriptPlayContextResolver playContextResolver)
        {
            _playContextResolver = playContextResolver;
        }

        public void Initialize()
        {
            try
            {
                var helper = Plugin.HttpSiraStatusHelper;
                if (helper == null || !helper.IsInitialized)
                {
                    return;
                }

                helper.SendPlayContext(_playContextResolver.Resolve());
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScript: Failed to send HttpSiraStatus metadata: {ex.Message}");
            }
        }
    }
}
