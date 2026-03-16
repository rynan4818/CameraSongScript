using CameraSongScript.Services;
using Zenject;

namespace CameraSongScript.Gameplay
{
    /// <summary>
    /// プレイシーン滞在中は BetterSongList 用の譜面スキャンを停止する。
    /// </summary>
    public class SongScriptGameplayScanPauseController : IInitializable, System.IDisposable
    {
        private readonly SongScriptBeatmapIndexService _indexService;

        internal SongScriptGameplayScanPauseController(SongScriptBeatmapIndexService indexService)
        {
            _indexService = indexService;
        }

        public void Initialize()
        {
            _indexService?.PauseForGameplay();
        }

        public void Dispose()
        {
            _indexService?.ResumeFromGameplay();
        }
    }
}
