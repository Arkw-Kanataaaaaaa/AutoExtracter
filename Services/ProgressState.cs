namespace AutoExtracter;

/// <summary>
/// 監視スレッドと画面の間で処理状況を安全に共有する状態
/// </summary>
internal sealed class ProgressState
{
    private readonly object gate = new();
    private ProgressSnapshot value = new("", "", MessageId.I301, 0, 0, 0, 0, 0, null, 0, null, 0);
    private string? activeJob;
    private TaskCompletionSource<string?>? passwordInput;
    private long passwordSequence;

    /// <summary>
    /// パスワードの入力または退避指示まで処理を待機する
    /// </summary>
    /// <param name="token">停止要求</param>
    /// <param name="retry">パスワードの再入力を求めるか</param>
    /// <returns>入力パスワード、退避指示の場合はnull</returns>
    internal string? RequestPassword(CancellationToken token, bool retry = false)
    {
        var request = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            passwordInput = request;
            value = value with
            {
                Phase = MessageId.I386,
                PasswordRequestId = ++passwordSequence,
                PasswordRetry = retry
            };
        }

        try
        {
            return request.Task.WaitAsync(token).GetAwaiter().GetResult();
        }
        finally
        {
            lock (gate)
            {
                passwordInput = null;
            }
        }
    }

    /// <summary>
    /// 現在の入力要求にだけパスワードまたは退避指示を渡す
    /// </summary>
    /// <param name="requestId">画面が表示している要求の識別子</param>
    /// <param name="password">入力パスワード、nullは退避指示</param>
    /// <returns>要求を受け付けた場合はtrue</returns>
    internal bool SubmitPassword(long requestId, string? password)
    {
        lock (gate)
        {
            return value.PasswordRequestId == requestId && passwordInput != null &&
                passwordInput.TrySetResult(password);
        }
    }

    /// <summary>
    /// 現在の処理状況を取得する
    /// </summary>
    /// <returns>変更されない処理状況のスナップショット</returns>
    public ProgressSnapshot Read()
    {
        lock (gate)
        {
            return value;
        }
    }

    /// <summary>
    /// 永続キューから待機件数を更新する
    /// </summary>
    /// <param name="temporaryFolder">ジョブを保存した一時フォルダ</param>
    public void UpdateQueue(string temporaryFolder)
    {
        string? processingJob;
        lock (gate)
        {
            processingJob = activeJob;
        }

        int waiting = 0;
        var archives = new List<QueuedArchive>();
        foreach (string job in FileOperations.EnumerateReadyJobs(temporaryFolder))
        {
            if (File.Exists(Path.Combine(job, "ready")) && job != processingJob)
            {
                waiting++;
                try
                {
                    if (File.Exists(Path.Combine(job, "name.json")))
                    {
                        string name = FileOperations.ReadJobName(job);
                        var file = new FileInfo(Path.Combine(job, name));
                        if (file.Exists)
                        {
                            archives.Add(new QueuedArchive(job, name, file.Length));
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                    System.Text.Json.JsonException or ArgumentException)
                {
                    // 変化したジョブの表示を次回更新へ回し、処理側でエラーを記録する
                }
            }
        }

        lock (gate)
        {
            if (activeJob != processingJob)
            {
                return;
            }

            value = value with
            {
                WaitingCount = waiting,
                WaitingArchives = archives.AsReadOnly()
            };
        }
    }

    /// <summary>
    /// 解凍対象と容量を設定する
    /// </summary>
    /// <param name="job">処理中のジョブフォルダ</param>
    /// <param name="name">圧縮ファイル名</param>
    /// <param name="archiveBytes">圧縮ファイルの容量</param>
    public void Begin(string job, string name, long archiveBytes)
    {
        lock (gate)
        {
            activeJob = job;
            value = value with
            {
                ArchiveName = name,
                EntryName = "",
                Phase = MessageId.I302,
                ExtractedEntries = 0,
                TotalEntries = null,
                ExtractedBytes = 0,
                TotalBytes = null,
                ArchiveBytes = archiveBytes
            };
        }
    }

    /// <summary>
    /// 解凍予定の合計件数と容量を設定する
    /// </summary>
    /// <param name="entries">解凍対象ファイルの合計件数</param>
    /// <param name="bytes">解凍対象ファイルの合計容量</param>
    public void SetTotals(int entries, long bytes)
    {
        lock (gate)
        {
            value = value with
            {
                TotalEntries = entries,
                TotalBytes = bytes
            };
        }
    }

    /// <summary>
    /// 解凍中のファイルと実際の処理済み容量を更新する
    /// </summary>
    /// <param name="entry">解凍中のファイル名</param>
    /// <param name="entries">解凍を完了したファイル数</param>
    /// <param name="bytes">解凍先へ書き込んだバイト数</param>
    public void Report(string entry, int entries, long bytes)
    {
        lock (gate)
        {
            value = value with
            {
                EntryName = entry,
                ExtractedEntries = entries,
                ExtractedBytes = bytes
            };
        }
    }

    /// <summary>
    /// 処理段階を更新する
    /// </summary>
    /// <param name="phase">画面に表示する処理段階のリソース識別子</param>
    public void SetPhase(MessageId phase)
    {
        lock (gate)
        {
            value = value with
            {
                Phase = phase
            };
        }
    }

    /// <summary>
    /// ジョブの結果を記録して待機状態へ戻す
    /// </summary>
    /// <param name="result">成功、エラー、パスワード退避のログ識別子</param>
    public void Finish(MessageId result)
    {
        lock (gate)
        {
            activeJob = null;
            value = value with
            {
                Phase = MessageId.I301,
                EntryName = "",
                CompletedCount = value.CompletedCount + (result == MessageId.I005 ? 1 : 0),
                FailedCount = value.FailedCount + (result == MessageId.E005 ? 1 : 0),
                PasswordCount = value.PasswordCount + (result == MessageId.W001 ? 1 : 0)
            };
        }
    }
}
