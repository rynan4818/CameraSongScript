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
        internal SongScriptFilter()
        {
        }

        private static SongScriptBeatmapIndexService IndexService => SongScriptBeatmapIndexService.Current;

        public string name => "SongScript";

        public bool visible { get; private set; }

        public bool isReady => IndexService != null && IndexService.CanFilter;

        public void ContextSwitch(LevelCategory levelCategory, BeatmapLevelPack playlist)
        {
            visible = levelCategory == LevelCategory.CustomSongs || levelCategory == LevelCategory.None;
        }

        public async Task Prepare(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                SongScriptBeatmapIndexService indexService = IndexService;
                if (indexService != null && indexService.CanFilter)
                {
                    return;
                }

                await Task.Delay(100, cancelToken).ConfigureAwait(false);
            }
        }

        public bool GetValueFor(BeatmapLevel level)
        {
            SongScriptBeatmapIndexService indexService = IndexService;
            return indexService != null && indexService.HasAnySongScript(level);
        }
    }
}
