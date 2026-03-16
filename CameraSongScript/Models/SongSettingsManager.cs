using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;

namespace CameraSongScript.Models
{
    public class SettingsData
    {
        public ConcurrentDictionary<string, SongSpecificSettings> SongSettings { get; set; } = new ConcurrentDictionary<string, SongSpecificSettings>();
        public ConcurrentDictionary<string, int> ScriptOffsets { get; set; } = new ConcurrentDictionary<string, int>();
    }

    /// <summary>
    /// 譜面ごとの設定データおよびスクリプトファイルごとのオフセット値を管理・保存・読み込みを行うクラス
    /// </summary>
    public static class SongSettingsManager
    {
        private static readonly string SettingsDirectoryPath = Path.Combine(UnityGame.UserDataPath, "CameraSongScript");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectoryPath, "CameraSongScript_SongSettings.json");
        private static readonly object _initializeLock = new object();
        private static readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);
        private static SettingsData _settingsData = new SettingsData();
        private static Task _initializeTask;
        private static bool _isInitialized = false;
        private static string _currentSongKey = null;

        /// <summary>
        /// データの初期化（ロード）を非同期で行う
        /// </summary>
        public static Task InitializeAsync()
        {
            lock (_initializeLock)
            {
                if (_isInitialized)
                {
                    return Task.CompletedTask;
                }

                if (_initializeTask == null)
                {
                    _initializeTask = Task.Run((Action)LoadSettings);
                }

                return _initializeTask;
            }
        }

        /// <summary>
        /// データをファイルに非同期で保存する
        /// </summary>
        public static async Task SaveSettingsAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _saveLock.WaitAsync().ConfigureAwait(false);

            try
            {
                Directory.CreateDirectory(SettingsDirectoryPath);
                string json = JsonConvert.SerializeObject(_settingsData, Formatting.Indented);
                using (StreamWriter writer = new StreamWriter(SettingsFilePath, false))
                {
                    await writer.WriteAsync(json).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to save song settings: {ex.Message}");
            }
            finally
            {
                _saveLock.Release();
            }
        }

        /// <summary>
        /// 譜面識別用のキーを生成する
        /// KosorenToolに準拠: {LevelID}___{Difficulty}___{Characteristic}
        /// </summary>
        public static string GenerateKey(string levelId, int difficulty, string characteristic)
        {
            if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(characteristic))
                return null;
            return $"{levelId}___{difficulty}___{characteristic}";
        }

        /// <summary>
        /// 現在選択されている譜面のキーを設定する
        /// </summary>
        public static void SetCurrentSong(string levelId, int difficulty, string characteristic)
        {
            _currentSongKey = GenerateKey(levelId, difficulty, characteristic);
        }

        public static void ClearCurrentSong()
        {
            _currentSongKey = null;
        }

        public static bool HasCurrentSong => !string.IsNullOrEmpty(_currentSongKey);
        public static string CurrentSongKey => _currentSongKey;

        /// <summary>
        /// 現在の譜面の設定を取得する
        /// </summary>
        public static SongSpecificSettings GetCurrentSettings()
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(_currentSongKey)) return null;
            
            if (_settingsData.SongSettings.TryGetValue(_currentSongKey, out var settings))
            {
                return settings;
            }
            return null;
        }

        /// <summary>
        /// 現在の譜面のスクリプトファイル名設定を更新する
        /// </summary>
        public static void UpdateCurrentScriptFileName(string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(_currentSongKey)) return;

            var settings = _settingsData.SongSettings.GetOrAdd(_currentSongKey, new SongSpecificSettings());
            settings.SelectedScriptFileName = fileName;
            
            // 変更後は非同期で保存
            _ = SaveSettingsAsync();
        }

        /// <summary>
        /// スクリプトオフセット用の辞書を提供する
        /// </summary>
        public static ConcurrentDictionary<string, int> ScriptOffsetsDict
        {
            get
            {
                EnsureInitialized();
                return _settingsData.ScriptOffsets;
            }
        }

        private static void EnsureInitialized()
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        private static void LoadSettings()
        {
            if (_isInitialized)
                return;

            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(SettingsFilePath))
                    {
                        string json = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<SettingsData>(json);
                        if (data != null)
                        {
                            _settingsData = data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error($"Failed to load song settings: {ex.Message}");
                }
            }

            _isInitialized = true;
        }
    }
}
