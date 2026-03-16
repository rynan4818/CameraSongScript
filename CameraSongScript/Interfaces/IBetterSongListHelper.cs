namespace CameraSongScript.Interfaces
{
    /// <summary>
    /// BetterSongList との連携を抽象化するインターフェース
    /// CameraSongScript.BetterSongList プロジェクトで実装される
    /// </summary>
    public interface IBetterSongListHelper
    {
        bool Initialize();
        bool IsInitialized { get; }
    }
}
