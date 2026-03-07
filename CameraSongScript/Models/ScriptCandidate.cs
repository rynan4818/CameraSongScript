namespace CameraSongScript.Models
{
    internal enum ScriptSource
    {
        ChartFolder,
        SongScriptFolder
    }

    /// <summary>
    /// スクリプト候補の統一表現。
    /// 譜面フォルダ内スクリプトとSongScriptフォルダ内スクリプトの両方を扱う。
    /// </summary>
    internal class ScriptCandidate
    {
        /// <summary>UIドロップダウンに表示する名前</summary>
        public string DisplayName { get; set; }

        /// <summary>ディスク上のフルパス（.jsonまたは.zipファイル）</summary>
        public string FilePath { get; set; }

        /// <summary>zipエントリの場合のエントリ名（非zip時はnull）</summary>
        public string ZipEntryName { get; set; }

        /// <summary>スクリプトの出自</summary>
        public ScriptSource Source { get; set; }

        /// <summary>キャッシュ済みメタデータ</summary>
        public MetadataElements Metadata { get; set; }

        /// <summary>zipエントリかどうか</summary>
        public bool IsZipEntry => !string.IsNullOrEmpty(ZipEntryName);
    }
}
