using System;
using CameraSongScript.Interfaces;
using Zenject;

namespace CameraSongScript.Gameplay
{
    /// <summary>
    /// プレイシーン開始時にHttpSiraStatusへCameraSongScript情報を送信する
    /// </summary>
    public class CameraSongScriptHttpSiraStatusSender : IInitializable
    {
        private readonly CameraSongScriptPlayContextResolver _playContextResolver;
        private readonly IHttpSiraStatusHelper _httpSiraStatusHelper;

        internal CameraSongScriptHttpSiraStatusSender(
            CameraSongScriptPlayContextResolver playContextResolver,
            [InjectOptional] IHttpSiraStatusHelper httpSiraStatusHelper)
        {
            _playContextResolver = playContextResolver;
            _httpSiraStatusHelper = httpSiraStatusHelper;
        }

        public void Initialize()
        {
            try
            {
                if (_httpSiraStatusHelper == null || !_httpSiraStatusHelper.IsInitialized)
                {
                    return;
                }

                _httpSiraStatusHelper.SendPlayContext(_playContextResolver.Resolve());
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"SongScript: Failed to send HttpSiraStatus metadata: {ex.Message}");
            }
        }
    }
}
