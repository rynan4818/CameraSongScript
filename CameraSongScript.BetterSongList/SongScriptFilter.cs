using System.Threading;
using System.Threading.Tasks;
using BetterSongList.FilterModels;
using BetterSongList.Interfaces;
using CameraSongScript.Services;
using static SelectLevelCategoryViewController;

namespace CameraSongScript.BetterSongList
{
    internal sealed class SongScriptFilter : IFilter, ITransformerPlugin
    {
        private readonly SongScriptBeatmapIndexService _indexService;

        internal SongScriptFilter(SongScriptBeatmapIndexService indexService)
        {
            _indexService = indexService;
        }

        public string name => "SongScript";

        public bool visible { get; private set; }

        public bool isReady => _indexService != null && _indexService.CanFilter;

        public void ContextSwitch(LevelCategory levelCategory, BeatmapLevelPack playlist)
        {
            visible = levelCategory == LevelCategory.CustomSongs || levelCategory == LevelCategory.None;
        }

        public async Task Prepare(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                if (_indexService != null && _indexService.CanFilter)
                {
                    return;
                }

                await Task.Delay(100, cancelToken).ConfigureAwait(false);
            }
        }

        public bool GetValueFor(BeatmapLevel level)
        {
            return _indexService != null && _indexService.HasAnySongScript(level);
        }
    }
}
