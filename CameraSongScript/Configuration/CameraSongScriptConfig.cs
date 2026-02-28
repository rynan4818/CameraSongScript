using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace CameraSongScript.Configuration
{
    internal class CameraSongScriptConfig
    {
        public static CameraSongScriptConfig Instance { get; set; }

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
        /// ユーザー選択のスクリプトファイル名（ファイル名のみ、パスではない）
        /// 空文字列 = 自動選択（SongScript.json優先）
        /// </summary>
        public virtual string SelectedScriptFile { get; set; } = "";

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
        public virtual void CopyFrom(CameraSongScriptConfig other)
        {
            Enabled = other.Enabled;
            UseAudioSync = other.UseAudioSync;
            TargetCameras = other.TargetCameras;
            SelectedScriptFile = other.SelectedScriptFile;
        }

        /// <summary>
        /// TargetCamerasをパースして文字列配列で返す
        /// </summary>
        public string[] GetTargetCameraNames()
        {
            if (string.IsNullOrWhiteSpace(TargetCameras))
                return new string[0];
            var names = TargetCameras.Split(',');
            var result = new List<string>();
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