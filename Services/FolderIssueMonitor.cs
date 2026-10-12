namespace AutoExtracter;

/// <summary>
/// 画面を止めずに退避フォルダ内のファイル件数を確認する
/// </summary>
internal sealed class FolderIssueMonitor
{
    private readonly FileLogger? logger;
    private readonly HashSet<string> failedFolders = new(StringComparer.OrdinalIgnoreCase);
    private Task<FolderIssueState>? pending;
    private DateTime nextRead;
    private FolderIssueState current = new("", 0, "", 0);

    /// <summary>
    /// 確認失敗のログ出力先を保持する
    /// </summary>
    /// <param name="logger">ログ出力先</param>
    public FolderIssueMonitor(FileLogger? logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 二秒ごとに確認を開始し、完了済みの結果を返す
    /// </summary>
    /// <param name="settings">退避フォルダの設定</param>
    /// <returns>最新の確認済み件数</returns>
    public FolderIssueState Refresh(Settings settings)
    {
        if (pending?.IsCompleted == true)
        {
            current = pending.GetAwaiter().GetResult();
            pending = null;
        }

        if (pending == null && DateTime.UtcNow >= nextRead)
        {
            StartRead(settings);
        }

        return current;
    }

    /// <summary>
    /// ボタン押下時の最新の確認結果を取得する
    /// </summary>
    /// <param name="settings">退避フォルダの設定</param>
    /// <returns>確認結果</returns>
    public async Task<FolderIssueState> ReadNowAsync(Settings settings)
    {
        if (pending?.IsCompleted == true)
        {
            current = await pending;
            pending = null;
        }

        if (pending == null)
        {
            StartRead(settings);
        }

        Task<FolderIssueState> operation = pending!;
        FolderIssueState result = await operation;
        current = result;
        if (ReferenceEquals(pending, operation))
        {
            pending = null;
        }

        nextRead = DateTime.UtcNow.AddSeconds(2);
        return result;
    }

    /// <summary>
    /// 現在の設定と前回結果を固定して非同期の確認を開始する
    /// </summary>
    /// <param name="settings">退避フォルダの設定</param>
    private void StartRead(Settings settings)
    {
        FolderIssueState previous = current;
        string errors = settings.ErrorFolder;
        string passwords = settings.PasswordRequiredFolder;
        pending = Task.Run(() =>
        {
            return new FolderIssueState(errors,
                CountFiles(errors, previous.ErrorFolder == errors ? previous.ErrorCount : 0),
                passwords, CountFiles(passwords, previous.PasswordFolder == passwords ? previous.PasswordCount : 0));
        });

        nextRead = DateTime.UtcNow.AddSeconds(2);
    }

    /// <summary>
    /// 子フォルダを含む通常ファイルを数え、読み取り失敗時は前回値を保持する
    /// </summary>
    /// <param name="folder">対象フォルダ</param>
    /// <param name="previous">前回の件数</param>
    /// <returns>ファイル件数</returns>
    private long CountFiles(string folder, long previous)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return 0;
        }

        try
        {
            long count = Directory.EnumerateFiles(folder, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint
            }).LongCount();
            failedFolders.Remove(folder);
            return count;
        }
        catch (DirectoryNotFoundException)
        {
            failedFolders.Remove(folder);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            if (failedFolders.Add(folder))
            {
                logger?.Write(MessageId.W005, ex, folder);
            }

            return previous;
        }
    }
}
