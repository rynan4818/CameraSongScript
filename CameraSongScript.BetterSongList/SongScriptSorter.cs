using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BetterSongList.Interfaces;
using BetterSongList.SortModels;
using CameraSongScript.Services;
using static SelectLevelCategoryViewController;

namespace CameraSongScript.BetterSongList
{
    internal sealed class SongScriptSorter : ISorterCustom, ITransformerPlugin
    {
        private readonly SongScriptBeatmapIndexService _indexService;

        internal SongScriptSorter(SongScriptBeatmapIndexService indexService)
        {
            _indexService = indexService;
        }

        private sealed class IndexedLevel
        {
            public IPreviewBeatmapLevel Level { get; set; }
            public int OriginalIndex { get; set; }
            public SongScriptLevelSortInfo SortInfo { get; set; }
        }

        public string name => "SongScript Order";

        public bool visible { get; private set; }

        public bool isReady => _indexService != null && _indexService.CanFilter;

        public void ContextSwitch(LevelCategory levelCategory, IAnnotatedBeatmapLevelCollection playlist)
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

        public void DoSort(ref IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (_indexService == null || levels == null)
            {
                return;
            }

            IndexedLevel[] indexedLevels = levels
                .Select((level, originalIndex) => new IndexedLevel
                {
                    Level = level,
                    OriginalIndex = originalIndex,
                    SortInfo = _indexService.GetLevelSortInfo(level)
                })
                .ToArray();

            IEnumerable<IndexedLevel> songScriptsLevels = indexedLevels
                .Where(indexedLevel => indexedLevel.SortInfo != null && indexedLevel.SortInfo.HasSongScriptsFolderScript);

            songScriptsLevels = ascending
                ? songScriptsLevels
                    .OrderBy(indexedLevel => GetSongScriptsSortKey(indexedLevel.SortInfo), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(indexedLevel => indexedLevel.OriginalIndex)
                : songScriptsLevels
                    .OrderByDescending(indexedLevel => GetSongScriptsSortKey(indexedLevel.SortInfo), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(indexedLevel => indexedLevel.OriginalIndex);

            IEnumerable<IndexedLevel> chartFolderLevels = indexedLevels
                .Where(indexedLevel =>
                    indexedLevel.SortInfo != null &&
                    !indexedLevel.SortInfo.HasSongScriptsFolderScript &&
                    indexedLevel.SortInfo.HasChartFolderSongScript);

            IEnumerable<IndexedLevel> remainingLevels = indexedLevels
                .Where(indexedLevel =>
                    indexedLevel.SortInfo == null ||
                    (!indexedLevel.SortInfo.HasSongScriptsFolderScript &&
                     !indexedLevel.SortInfo.HasChartFolderSongScript));

            levels = songScriptsLevels
                .Concat(chartFolderLevels)
                .Concat(remainingLevels)
                .Select(indexedLevel => indexedLevel.Level);
        }

        private static string GetSongScriptsSortKey(SongScriptLevelSortInfo sortInfo)
        {
            if (sortInfo == null)
            {
                return string.Empty;
            }

            PropertyInfo property =
                sortInfo.GetType().GetProperty("SongScriptsSortKey") ??
                sortInfo.GetType().GetProperty("SongScriptsSortFileName");
            if (property == null || property.PropertyType != typeof(string))
            {
                return string.Empty;
            }

            return (string)property.GetValue(sortInfo, null) ?? string.Empty;
        }
    }
}
