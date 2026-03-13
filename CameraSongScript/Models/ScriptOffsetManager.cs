using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CameraSongScript.Models
{
    /// <summary>
    /// スクリプトファイルのSHA1ハッシュをキーとして、高さのオフセット(cm)を管理するクラス。
    /// 実際のデータの実体と保存は SongSettingsManager に委譲し、ここでは計算と設定のインターフェースを提供する。
    /// オフセットが0の場合は保存対象とせず、0以外の場合のみ保存する。
    /// </summary>
    public static class ScriptOffsetManager
    {
        private static string _lastHashedPath = null;
        private static string _lastHashValue = null;
        private static long _lastHashedLength = -1;
        private static DateTime _lastHashedWriteTimeUtc = DateTime.MinValue;

        /// <summary>
        /// 指定されたファイルのSHA1ハッシュを計算して小文字の16進数文字列で返す
        /// </summary>
        public static string CalculateFileSHA1(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            var fileInfo = new FileInfo(filePath);
            if (_lastHashedPath == filePath &&
                _lastHashValue != null &&
                _lastHashedLength == fileInfo.Length &&
                _lastHashedWriteTimeUtc == fileInfo.LastWriteTimeUtc)
            {
                return _lastHashValue;
            }

            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(stream);
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    _lastHashValue = sb.ToString();
                    _lastHashedPath = filePath;
                    _lastHashedLength = fileInfo.Length;
                    _lastHashedWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                    return _lastHashValue;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to calculate SHA1 for {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// スクリプトファイルのパスからハッシュを計算し、保存されているオフセットを取得する。
        /// 保存されていない場合（またはハッシュ計算失敗時）は0を返す。
        /// </summary>
        public static int GetOffsetForScript(string scriptPath)
        {
            string hash = CalculateFileSHA1(scriptPath);
            if (string.IsNullOrEmpty(hash)) return 0;

            if (SongSettingsManager.ScriptOffsetsDict.TryGetValue(hash, out int offset))
            {
                return offset;
            }
            return 0;
        }

        /// <summary>
        /// スクリプトファイルのパスに対するオフセット値を更新する。
        /// オフセットが0の場合は辞書から削除する（デフォルト状態に戻すため）。
        /// 変更があった場合は非同期でファイルに保存する。
        /// </summary>
        public static void UpdateOffsetForScript(string scriptPath, int offsetCm)
        {
            string hash = CalculateFileSHA1(scriptPath);
            if (string.IsNullOrEmpty(hash)) return;

            bool changed = false;

            if (offsetCm == 0)
            {
                changed = SongSettingsManager.ScriptOffsetsDict.TryRemove(hash, out _);
            }
            else
            {
                SongSettingsManager.ScriptOffsetsDict.AddOrUpdate(hash, offsetCm, (key, oldValue) =>
                {
                    if (oldValue != offsetCm)
                    {
                        changed = true;
                    }
                    return offsetCm;
                });
                
                // AddOrUpdateが新規追加だった場合もchangedをtrueにする必要があるため、
                // 上記ラムダ内だけではカバーできないケースの補完
                if (!changed)
                {
                    changed = true; // 簡略化のため、更新・追加されたとみなして保存走らせる
                }
            }

            if (changed)
            {
                _ = SongSettingsManager.SaveSettingsAsync();
            }
        }
    }
}