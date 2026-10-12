namespace AutoExtracter;

/// <summary>
/// 監視フォルダと解凍処理の設定を保持する
/// </summary>
internal sealed class Settings
{
    /// <summary>
    /// 処理設定の適用時に再起動確認を省略するか
    /// </summary>
    public bool SkipRestartConfirmation
    {
        get;
        set;
    }

    /// <summary>
    /// GIFと待機表示のアニメーションを有効にするか
    /// </summary>
    public bool AnimationsEnabled
    {
        get;
        set;
    } = true;

    /// <summary>
    /// パスワードが必要な場合に画面から入力を求めるか
    /// </summary>
    public bool InteractivePasswords
    {
        get;
        set;
    }

    /// <summary>
    /// パスワード入力待ちのGIF、空欄は同梱画像
    /// </summary>
    public string DialogueGifPath
    {
        get;
        set;
    } = "";
    /// <summary>
    /// 通知パネルの外観、Light、Dark、Systemのいずれか
    /// </summary>
    public string Appearance
    {
        get;
        set;
    } = "Dark";

    /// <summary>
    /// 監視対象の圧縮ファイル拡張子
    /// </summary>
    public string[] ArchiveExtensions
    {
        get;
        set;
    } = new[]
    {
        ".zip", ".7z", ".rar"
    };

    /// <summary>
    /// 解凍中のGIFのパス、空の場合は同梱画像を使用する
    /// </summary>
    public string ExtractingGifPath
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 待機パネルのGIFのパス、空の場合は同梱画像を使用する
    /// </summary>
    public string IdleGifPath
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 圧縮ファイルを監視するフォルダを取得または設定する
    /// </summary>
    public string WatchFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 解凍作業用の一時フォルダを取得または設定する
    /// </summary>
    public string TemporaryFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 解凍完了データの保存先を取得または設定する
    /// </summary>
    public string CompletedFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 解凍成功後に元の圧縮ファイルを保持するか
    /// </summary>
    public bool KeepOriginalArchives
    {
        get;
        set;
    }

    /// <summary>
    /// 保持する元の圧縮ファイルの退避先
    /// </summary>
    public string ArchiveBackupFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// パスワードが必要な圧縮ファイルの退避先を取得または設定する
    /// </summary>
    public string PasswordRequiredFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 処理に失敗した圧縮ファイルの退避先を取得または設定する
    /// </summary>
    public string ErrorFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// ログファイルの保存先を取得または設定する
    /// </summary>
    public string LogFolder
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 監視間隔の秒数を取得または設定する
    /// </summary>
    public int PollSeconds
    {
        get;
        set;
    } = 2;

    /// <summary>
    /// ファイルが変化しないことを確認する秒数を取得または設定する
    /// </summary>
    public int StableSeconds
    {
        get;
        set;
    } = 10;

    /// <summary>
    /// 一件の圧縮ファイルのバイト数の上限を取得または設定する
    /// </summary>
    public long MaxArchiveBytes
    {
        get;
        set;
    } = 100_000_000_000;

    /// <summary>
    /// 一件の解凍バイト数の安全上限を取得または設定する
    /// </summary>
    public long MaxExtractedBytes
    {
        get;
        set;
    } = 100L * 1024 * 1024 * 1024;

    /// <summary>
    /// 一件のエントリ数の上限を取得または設定する
    /// </summary>
    public int MaxEntries
    {
        get;
        set;
    } = 100000;

    /// <summary>
    /// 設定値を検証し、使用するフォルダを作成する
    /// </summary>
    /// <param name="configDirectory">設定ファイルのあるフォルダ</param>
    /// <param name="createDirectories">検証後に使用フォルダを作成するか</param>
    public void Validate(string configDirectory, bool createDirectories = true)
    {
        if (Appearance is not ("Light" or "Dark" or "System"))
        {
            throw new ArgumentException(Messages.Get(MessageId.E119));
        }

        if (ArchiveExtensions == null || ArchiveExtensions.Length == 0 ||
            ArchiveExtensions.Any(extension =>
            {
                return extension == null || !new[]
                {
                    ".zip", ".7z", ".rar"
                }.Contains(extension.Trim(), StringComparer.OrdinalIgnoreCase);
            }))
        {
            throw new ArgumentException(Messages.Get(MessageId.E117));
        }

        ArchiveExtensions = ArchiveExtensions.Select(extension =>
        {
            return extension.Trim().ToLowerInvariant();
        }).Distinct().ToArray();
        if (!string.IsNullOrWhiteSpace(ExtractingGifPath))
        {
            ExtractingGifPath = Path.GetFullPath(ExtractingGifPath, configDirectory);
        }

        if (!string.IsNullOrWhiteSpace(IdleGifPath))
        {
            IdleGifPath = Path.GetFullPath(IdleGifPath, configDirectory);
        }

        if (!string.IsNullOrWhiteSpace(DialogueGifPath))
        {
            DialogueGifPath = Path.GetFullPath(DialogueGifPath, configDirectory);
        }

        WatchFolder = FileOperations.ResolveFolder(WatchFolder, configDirectory);
        TemporaryFolder = FileOperations.ResolveFolder(TemporaryFolder, configDirectory);
        CompletedFolder = FileOperations.ResolveFolder(string.IsNullOrWhiteSpace(CompletedFolder) ? "Completed" : CompletedFolder, configDirectory);
        ArchiveBackupFolder = FileOperations.ResolveFolder(string.IsNullOrWhiteSpace(ArchiveBackupFolder)
            ? Path.Combine(Path.GetDirectoryName(TemporaryFolder)!, "Archives") : ArchiveBackupFolder, configDirectory);
        PasswordRequiredFolder = FileOperations.ResolveFolder(PasswordRequiredFolder, configDirectory);
        ErrorFolder = FileOperations.ResolveFolder(ErrorFolder, configDirectory);
        LogFolder = FileOperations.ResolveFolder(LogFolder, configDirectory);
        var paths = new[]
        {
            WatchFolder,
            TemporaryFolder,
            CompletedFolder,
            PasswordRequiredFolder,
            ErrorFolder,
            LogFolder
        }.Concat(KeepOriginalArchives ? new[]
        {
            ArchiveBackupFolder
        } : Array.Empty<string>()).ToArray();
        for (int i = 0; i < paths.Length; i++)
        {
            for (int j = i + 1; j < paths.Length; j++)
            {
                if (paths[i].Equals(paths[j], StringComparison.OrdinalIgnoreCase) ||
                    paths[i].StartsWith(paths[j] + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    paths[j].StartsWith(paths[i] + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(Messages.Get(MessageId.E102));
                }
            }
        }

        if (PollSeconds < 1 || StableSeconds < 1 || MaxArchiveBytes < 1 || MaxExtractedBytes < 1 || MaxEntries < 1)
        {
            throw new ArgumentException(Messages.Get(MessageId.E103));
        }

        if (createDirectories)
        {
            foreach (var path in paths)
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}

