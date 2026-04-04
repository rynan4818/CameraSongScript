using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CameraSongScript.Configuration;
using CameraSongScript.Models;
using CameraSongScript.Services;
using HMUI;

namespace CameraSongScript.Detectors
{
    internal partial class CameraSongScriptDetector
    {
        private sealed class DefaultScriptSelection
        {
            public string DisplayName { get; set; } = string.Empty;
            public string ConfigSelection { get; set; } = string.Empty;
            public string ResolvedPath { get; set; } = string.Empty;
        }

        private sealed class ScanResult
        {
            public Dictionary<string, ScriptCandidate> CandidateMap { get; set; } = new Dictionary<string, ScriptCandidate>();
            public List<string> AvailableScriptFiles { get; set; } = new List<string>();
            public string SelectedScriptPath { get; set; } = string.Empty;
            public string SelectedScriptDisplayName { get; set; } = string.Empty;
            public string ResolvedSelectedScriptFile { get; set; } = string.Empty;
            public int ChartCount { get; set; }
            public int SongScriptFolderCount { get; set; }
            public int TotalCount => ChartCount + SongScriptFolderCount;
        }

        private void RequestScan(string levelPath, string levelId)
        {
            DispatchToMainThread(() =>
            {
                CurrentLevelPath = levelPath;
                _currentLevelId = levelId;
                StartScanAsync(levelPath, levelId);
            });
        }

        private void DispatchToMainThread(Action action)
        {
            if (action == null)
                return;

            var dispatcher = _mainThreadDispatcher;
            if (dispatcher != null)
            {
                dispatcher.DispatchOnMainThread(action);
                return;
            }

            action();
        }

        private ScanResult CollectScanResult(string levelPath, string levelId, string configuredSelectedScript, CancellationToken ct)
        {
            var chartCandidates = new List<ScriptCandidate>();

            if (Directory.Exists(levelPath))
            {
                string[] jsonFiles;
                try
                {
                    jsonFiles = Directory.GetFiles(levelPath, "*.json");
                    Array.Sort(jsonFiles, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CameraSongScriptDetector: Failed to scan directory: {ex.Message}");
                    jsonFiles = new string[0];
                }

                foreach (var filePath in jsonFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    string fileName = Path.GetFileName(filePath);
                    if (ShouldSkipChartFolderScriptFile(fileName))
                        continue;

                    if (IsValidMovementScript(filePath))
                    {
                        chartCandidates.Add(new ScriptCandidate
                        {
                            DisplayName = fileName,
                            FilePath = filePath,
                            Source = ScriptSource.ChartFolder
                        });
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            var ssCandidates = new List<ScriptCandidate>();
            SongScriptLevelReference levelReference = ResolveSongScriptLevelReference(levelId, levelPath);
            if (levelReference.HasAnyValue && SongScriptFolderCache.IsReady)
            {
                foreach (var entry in SongScriptFolderCache
                    .GetScriptsByLevelReference(levelReference.MapId, levelReference.Hash)
                    .OrderBy(GetSongScriptDisplayPath, StringComparer.OrdinalIgnoreCase))
                {
                    ct.ThrowIfCancellationRequested();
                    string displayName = FormatSongScriptDisplayName(entry);
                    if (chartCandidates.Any(c => c.DisplayName == displayName) || ssCandidates.Any(c => c.DisplayName == displayName))
                        continue;

                    ssCandidates.Add(new ScriptCandidate
                    {
                        DisplayName = displayName,
                        FilePath = entry.FilePath,
                        ZipEntryName = entry.ZipEntryName,
                        Source = ScriptSource.SongScriptFolder,
                        Metadata = entry.Metadata
                    });
                }
            }

            var newCandidateMap = new Dictionary<string, ScriptCandidate>();
            var displayNames = new List<string>();
            foreach (var candidate in chartCandidates.Concat(ssCandidates))
            {
                newCandidateMap[candidate.DisplayName] = candidate;
                displayNames.Add(candidate.DisplayName);
            }

            ct.ThrowIfCancellationRequested();

            DefaultScriptSelection selection = SelectConfiguredDefaultScript(displayNames, newCandidateMap, configuredSelectedScript);
            return new ScanResult
            {
                CandidateMap = newCandidateMap,
                AvailableScriptFiles = displayNames,
                SelectedScriptPath = selection.ResolvedPath,
                SelectedScriptDisplayName = selection.DisplayName,
                ResolvedSelectedScriptFile = selection.ConfigSelection,
                ChartCount = chartCandidates.Count,
                SongScriptFolderCount = ssCandidates.Count
            };
        }

        private DefaultScriptSelection SelectConfiguredDefaultScript(List<string> validFiles, Dictionary<string, ScriptCandidate> candidateMap, string configuredSelectedScript)
        {
            var selection = new DefaultScriptSelection();
            if (validFiles.Count == 0)
                return selection;

            string resolvedConfiguredScript = ResolveAvailableScriptDisplayName(configuredSelectedScript, candidateMap);
            if (!string.IsNullOrEmpty(resolvedConfiguredScript) && validFiles.Contains(resolvedConfiguredScript) && candidateMap.ContainsKey(resolvedConfiguredScript))
            {
                selection.DisplayName = resolvedConfiguredScript;
                selection.ConfigSelection = resolvedConfiguredScript;
                selection.ResolvedPath = ResolveScriptPath(candidateMap[resolvedConfiguredScript]);
                return selection;
            }

            if (validFiles.Contains("SongScript.json") && candidateMap.ContainsKey("SongScript.json"))
            {
                selection.DisplayName = "SongScript.json";
                selection.ConfigSelection = "SongScript.json";
                selection.ResolvedPath = ResolveScriptPath(candidateMap["SongScript.json"]);
                return selection;
            }

            string fallbackName = validFiles[0];
            selection.DisplayName = fallbackName;
            selection.ConfigSelection = fallbackName;
            selection.ResolvedPath = ResolveScriptPath(candidateMap[fallbackName]);
            return selection;
        }
    }
}
namespace CameraSongScript.Detectors
{
    internal partial class CameraSongScriptDetector
    {
        private void ApplyScanResultOnMainThread(string levelPath, string levelId, ScanResult scanResult, CancellationToken ct)
        {
            DispatchToMainThread(() =>
            {
                if (ct.IsCancellationRequested)
                    return;

                if (!string.Equals(CurrentLevelPath, levelPath, StringComparison.Ordinal) ||
                    !string.Equals(_currentLevelId, levelId, StringComparison.Ordinal))
                {
                    return;
                }

#if DEBUG
                if (scanResult.TotalCount > 0)
                {
                    string logName = string.IsNullOrEmpty(scanResult.SelectedScriptDisplayName) ? "?" : scanResult.SelectedScriptDisplayName;
                    if (scanResult.SongScriptFolderCount > 0)
                        Plugin.Log.Info($"CameraSongScriptDetector: Found {scanResult.TotalCount} valid script(s) ({scanResult.ChartCount} chart + {scanResult.SongScriptFolderCount} SS). Selected: {logName}");
                    else
                        Plugin.Log.Info($"CameraSongScriptDetector: Found {scanResult.TotalCount} valid script(s). Selected: {logName}");
                }
#endif

                _candidateMap = scanResult.CandidateMap;
                AvailableScriptFiles = scanResult.AvailableScriptFiles;

                DefaultScriptSelection currentSelection = SelectConfiguredDefaultScript(
                    scanResult.AvailableScriptFiles,
                    scanResult.CandidateMap,
                    GetConfiguredSelectedScriptForCurrentSong());

                if (!string.IsNullOrEmpty(currentSelection.DisplayName))
                {
                    TryApplySelectedScriptInternal(
                        currentSelection.DisplayName,
                        persistConfigSelection: true,
                        syncCameraPlus: false);
                }
                else
                {
                    SelectedScriptPath = string.Empty;
                    SelectedScriptDisplayName = string.Empty;

                    if (CameraSongScriptConfig.Instance.UsePerScriptHeightOffset)
                    {
                        CameraSongScriptConfig.Instance.CameraHeightOffsetCm = 0;
                    }

                    LoadSelectedScriptInfo(SelectedScriptPath);
                    UpdateEffectiveScriptPath();
                }

                DetermineCommonScriptUsage(scanResult.TotalCount);
                SyncCameraPlusPath();
                ScanCompleted?.Invoke();
                NotifyStatusSnapshotChanged(CameraSongScriptStatusSnapshot.UpdateReasonSelectionChanged);
            });
        }
    }
}
