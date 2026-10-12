namespace AutoExtracter;

/// <summary>
/// 画面に表示する処理状況のスナップショット
/// </summary>
/// <param name="ArchiveName">圧縮ファイル名</param>
/// <param name="EntryName">解凍中の内部ファイル名</param>
/// <param name="Phase">現在の処理段階</param>
/// <param name="WaitingCount">待機中の圧縮ファイル数</param>
/// <param name="CompletedCount">今回の起動で完了した圧縮ファイル数</param>
/// <param name="FailedCount">今回の起動でエラー退避した圧縮ファイル数</param>
/// <param name="PasswordCount">今回の起動でパスワード退避した圧縮ファイル数</param>
/// <param name="ExtractedEntries">解凍済みの内部ファイル数</param>
/// <param name="TotalEntries">内部ファイルの予定合計件数</param>
/// <param name="ExtractedBytes">解凍済みの容量</param>
/// <param name="TotalBytes">解凍予定の合計容量</param>
/// <param name="ArchiveBytes">圧縮ファイルの容量</param>
internal sealed record ProgressSnapshot(
    string ArchiveName,
    string EntryName,
    MessageId Phase,
    int WaitingCount,
    int CompletedCount,
    int FailedCount,
    int PasswordCount,
    int ExtractedEntries,
    int? TotalEntries,
    long ExtractedBytes,
    long? TotalBytes,
    long ArchiveBytes)
{
    /// <summary>
    /// パスワード入力要求の識別子
    /// </summary>
    public long PasswordRequestId
    {
        get;
        init;
    }
    /// <summary>
    /// 入力済みパスワードの再確認が必要か
    /// </summary>
    public bool PasswordRetry
    {
        get;
        init;
    }
    /// <summary>
    /// 受け入れ順の解凍待ち圧縮ファイル
    /// </summary>
    public IReadOnlyList<QueuedArchive> WaitingArchives
    {
        get;
        init;
    } = Array.Empty<QueuedArchive>();

    /// <summary>
    /// 解凍バイト数から計算した進捗率、合計不明の場合はnull
    /// </summary>
    public int? Percent
    {
        get
        {
            if (!TotalBytes.HasValue || TotalBytes.Value <= 0)
            {
                return null;
            }

            return (int)Math.Clamp(ExtractedBytes * 100.0 / TotalBytes.Value, 0, 100);
        }
    }
}
