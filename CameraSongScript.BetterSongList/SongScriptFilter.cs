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
        public string name => "SongScript";

        public bool visible { get; private set; }

        public bool isReady =>
            SongScriptBeatmapIndexService.Instance != null &&
            SongScriptBeatmapIndexService.Instance.CanFilter;

        public void ContextSwitch(LevelCategory levelCategory, BeatmapLevelPack playlist)
        {
            visible = levelCategory == LevelCategory.CustomSongs || levelCategory == LevelCategory.None;
        }

        public async Task Prepare(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                var indexService = SongScriptBeatmapIndexService.Instance;
                if (indexService != null && indexService.CanFilter)
                {
                    return;
                }

                await Task.Delay(100, cancelToken).ConfigureAwait(false);
            }
        }

        public bool GetValueFor(BeatmapLevel level)
        {
            var indexService = SongScriptBeatmapIndexService.Instance;
            return indexService != null && indexService.HasAnySongScript(level);
        }
    }
}
