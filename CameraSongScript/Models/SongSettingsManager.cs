using System;
using System.Collections.Concurrent;
using System.IO;
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
        private static readonly string SettingsFilePath = Path.Combine(UnityGame.UserDataPath, "CameraSongScript_SongSettings.json");
        private static SettingsData _settingsData = new SettingsData();
        private static bool _isInitialized = false;
        private static string _currentSongKey = null;

        /// <summary>
        /// データの初期化（ロード）を非同期で行う
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_isInitialized) return;

            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(SettingsFilePath))
                    {
                        string json = await reader.ReadToEndAsync();
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

        /// <summary>
        /// データをファイルに非同期で保存する
        /// </summary>
        public static async Task SaveSettingsAsync()
        {
            if (!_isInitialized) return;

            try
            {
                string json = JsonConvert.SerializeObject(_settingsData, Formatting.Indented);
                using (StreamWriter writer = new StreamWriter(SettingsFilePath, false))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to save song settings: {ex.Message}");
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
            if (string.IsNullOrEmpty(_currentSongKey)) return;

            var settings = _settingsData.SongSettings.GetOrAdd(_currentSongKey, new SongSpecificSettings());
            settings.SelectedScriptFileName = fileName;
            
            // 変更後は非同期で保存
            _ = SaveSettingsAsync();
        }

        /// <summary>
        /// 現在の譜面のプロファイル名設定を更新する
        /// </summary>
        public static void UpdateCurrentProfileName(string profileName)
        {
            if (string.IsNullOrEmpty(_currentSongKey)) return;

            var settings = _settingsData.SongSettings.GetOrAdd(_currentSongKey, new SongSpecificSettings());
            settings.SelectedProfileName = profileName;

            // 変更後は非同期で保存
            _ = SaveSettingsAsync();
        }

        /// <summary>
        /// スクリプトオフセット用の辞書を提供する
        /// </summary>
        public static ConcurrentDictionary<string, int> ScriptOffsetsDict => _settingsData.ScriptOffsets;

    }
}