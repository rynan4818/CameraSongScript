using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace Camera2SongScript.Configuration
{
    internal class SongScriptConfig
    {
        public static SongScriptConfig Instance { get; set; }

        /// <summary>
        /// SongScript機能の有効/無効
        /// </summary>
        public virtual bool Enabled { get; set; } = true;

        /// <summary>
        /// AudioSync(曲同期)モード
        /// true: 曲のタイムラインに同期
        /// false: DateTime基準で進行
        /// </summary>
        public virtual bool UseAudioSync { get; set; } = true;

        /// <summary>
        /// スクリプトを適用するカメラ名リスト（カンマ区切り）
        /// 空の場合はすべてのアクティブカメラに適用
        /// </summary>
        public virtual string TargetCameras { get; set; } = "";

        /// <summary>
        /// BSIPAが設定ファイルを読み込むたびに呼び出される
        /// </summary>
        public virtual void OnReload()
        {
        }

        /// <summary>
        /// 設定が変更されたときに呼び出される
        /// </summary>
        public virtual void Changed()
        {
        }

        /// <summary>
        /// 値をコピー
        /// </summary>
        public virtual void CopyFrom(SongScriptConfig other)
        {
            Enabled = other.Enabled;
            UseAudioSync = other.UseAudioSync;
            TargetCameras = other.TargetCameras;
        }

        /// <summary>
        /// TargetCamerasをパースして文字列配列で返す
        /// </summary>
        public string[] GetTargetCameraNames()
        {
            if (string.IsNullOrWhiteSpace(TargetCameras))
                return new string[0];
            var names = TargetCameras.Split(',');
            var result = new System.Collections.Generic.List<string>();
            foreach (var name in names)
            {
                var trimmed = name.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result.ToArray();
        }
    }
}