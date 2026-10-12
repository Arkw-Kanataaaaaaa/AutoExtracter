using System.Text.Json;

namespace AutoExtracter;

/// <summary>
/// 受け入れ済みのジョブを順番に解凍する監視サービス
/// </summary>
internal sealed class WatcherService
{
    private readonly Settings settings;
    private readonly FileLogger logger;
    private readonly ProgressState progress;
    private long sequence = DateTime.UtcNow.Ticks;

    /// <summary>
    /// インスタンスを初期化する
    /// </summary>
    /// <param name="settings">監視と解凍の設定</param>
    /// <param name="logger">ログ出力先</param>
    /// <param name="progress">画面と共有する処理状況</param>
    public WatcherService(Settings settings, FileLogger logger, ProgressState progress)
    {
        this.settings = settings;
        this.logger = logger;
        this.progress = progress;
    }

    /// <summary>
    /// 新しい圧縮ファイルを監視し、順次処理を実行する
    /// </summary>
    /// <returns>監視終了までの非同期処理</returns>
    /// <param name="cancellationToken">外部から終了を要求するトークン</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;
        Task? worker = null;
        try
        {
            logger.Write(MessageId.I001, null, settings.WatchFolder);
            Recover();
            progress.UpdateQueue(settings.TemporaryFolder);
            worker = Task.Run(() =>
            {
                return Work(stop.Token);
            });

            var observations = new Dictionary<string, (long Length, DateTime Write, DateTime Since)>(StringComparer.OrdinalIgnoreCase);
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    Accept(observations);
                    progress.UpdateQueue(settings.TemporaryFolder);
                }
                catch (Exception ex)
                {
                    logger.Write(MessageId.E002, ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(settings.PollSeconds), stop.Token);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
        finally
        {
            stop.Cancel();
            try
            {
                if (worker != null)
                {
                    await worker;
                }
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        logger.Write(MessageId.I002);
    }

    /// <summary>
    /// コピーが完了した圧縮ファイルを一時フォルダへ受け入れる
    /// </summary>
    /// <param name="observations">ファイルのサイズと更新日時の観測結果</param>
    private void Accept(Dictionary<string, (long Length, DateTime Write, DateTime Since)> observations)
    {
        var extensions = new HashSet<string>(settings.ArchiveExtensions, StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(settings.WatchFolder).Where(p =>
            {
                return extensions.Contains(Path.GetExtension(p));
            })
            .Select(p =>
            {
                return new FileInfo(p);
            }).OrderBy(f =>
            {
                return f.CreationTimeUtc;
            }).ThenBy(f =>
            {
                return f.Name;
            }, StringComparer.OrdinalIgnoreCase).ToArray();
        var present = files.Select(f =>
            {
                return f.FullName;
            }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var gone in observations.Keys.Where(k =>
            {
                return !present.Contains(k);
            }).ToArray())
        {
            observations.Remove(gone);
        }

        foreach (var file in files)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (!observations.TryGetValue(file.FullName, out var old) || old.Length != file.Length || old.Write != file.LastWriteTimeUtc)
                {
                    observations[file.FullName] = (file.Length, file.LastWriteTimeUtc, now);
                    continue;
                }

                if ((now - old.Since).TotalSeconds < settings.StableSeconds)
                {
                    continue;
                }
                // コピー元がファイルを閉じていることを排他読み込みで確認する
                try
                {
                    using var input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    continue;
                }

                var id = $"{sequence = Math.Max(sequence + 1, DateTime.UtcNow.Ticks):D19}-{Guid.NewGuid():N}";
                var job = Path.Combine(settings.TemporaryFolder, id);
                Directory.CreateDirectory(job);
                File.WriteAllText(Path.Combine(job, "name.json"), JsonSerializer.Serialize(file.Name));
                try
                {
                    File.Move(file.FullName, Path.Combine(job, file.Name));
                }
                catch
                {
                    Directory.Delete(job, true);
                    throw;
                }

                File.WriteAllText(Path.Combine(job, "ready"), "");
                observations.Remove(file.FullName);
                logger.Write(MessageId.I003, null, file.Name, id);
            }
            catch (Exception ex)
            {
                logger.Write(MessageId.E003, ex, file.Name);
            }
        }
    }

    /// <summary>
    /// 前回の実行で残ったジョブを復旧する
    /// </summary>
    private void Recover()
    {
        foreach (var job in Directory.EnumerateDirectories(settings.TemporaryFolder))
        {
            var metadata = Path.Combine(job, "name.json");
            if (!File.Exists(metadata))
            {
                continue;
            }

            var name = FileOperations.ReadJobName(job);
            if (File.Exists(Path.Combine(job, name)))
            {
                File.WriteAllText(Path.Combine(job, "ready"), "");
            }
            else if (CompletedArchiveStorage.FindPublished(job, settings.CompletedFolder) != null ||
                     FileOperations.IsRetiredJob(job))
            {
                Directory.Delete(job, true);
            }
        }
    }

    /// <summary>
    /// 受け入れ順にジョブを一件ずつ処理する
    /// </summary>
    /// <param name="token">処理を中断するためのキャンセルトークン</param>
    /// <returns>ジョブ処理終了までの非同期処理</returns>
    private async Task Work(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var job = FileOperations.EnumerateReadyJobs(settings.TemporaryFolder).FirstOrDefault();
                if (job != null)
                {
                    Process(job, token);
                    progress.UpdateQueue(settings.TemporaryFolder);
                    continue;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.Write(MessageId.E004, ex);
                progress.SetPhase(MessageId.I304);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.PollSeconds), token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 一件のジョブを解凍し、完了または退避フォルダへ保存する
    /// </summary>
    /// <param name="job">処理対象のジョブフォルダ</param>
    /// <param name="token">処理を中断するためのキャンセルトークン</param>
    private void Process(string job, CancellationToken token)
    {
        var extractor = new ArchiveExtractor(settings, progress);
        var name = FileOperations.ReadJobName(job);
        var archive = Path.Combine(job, name);
        var output = Path.Combine(job, "result");
        var completed = CompletedArchiveStorage.FindPublished(job, settings.CompletedFolder);
        if (!File.Exists(archive) && FileOperations.IsRetiredJob(job))
        {
            Directory.Delete(job, true);
            return;
        }

        if (completed == null)
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            Directory.CreateDirectory(output);
            logger.Write(MessageId.I004, null, name);
            progress.Begin(job, name, new FileInfo(archive).Length);
            progress.UpdateQueue(settings.TemporaryFolder);
            try
            {
                if (!ExtractWithPassword(extractor, archive, output, settings, token))
                {
                    Directory.Delete(output, true);
                    File.Delete(Path.Combine(job, "completed"));
                    progress.Finish(MessageId.I301);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var password = ArchiveExtractor.IsPasswordRequired(ex);
                var destination = password ? settings.PasswordRequiredFolder : settings.ErrorFolder;
                var retired = Path.Combine(destination, Path.GetFileName(job) + "-" + name);
                // 再起動後に退避結果を確認できるよう、移動前に退避先を保存する
                File.WriteAllText(Path.Combine(job, "retired"), retired);
                File.Move(archive, retired);
                logger.Write(password ? MessageId.W001 : MessageId.E005, password ? null : ex, name, retired);
                Directory.Delete(job, true);
                progress.Finish(password ? MessageId.W001 : MessageId.E005);
                return;
            }
            // 優先度変更で同名ジョブが先行しても保存先が衝突しないよう、解凍後に予約する
            completed = CompletedArchiveStorage.GetDestination(job, name, settings.CompletedFolder);
            // 完了先への公開が成功するまで元の圧縮ファイルを一時フォルダで保持する
            progress.SetPhase(MessageId.I303);
            FileOperations.PublishDirectory(output, completed, token);
        }

        CompletedArchiveStorage.FinishOriginal(job, archive, settings, token);
        logger.Write(MessageId.I005, null, name, completed);
        Directory.Delete(job, true);
        progress.Finish(MessageId.I005);
    }

    /// <summary>
    /// 対話式の場合は入力を待ち、同じジョブを先頭から再試行する
    /// </summary>
    /// <param name="extractor">解凍処理</param>
    /// <param name="archive">圧縮ファイル</param>
    /// <param name="output">解凍先</param>
    /// <param name="settings">ジョブ開始時の設定</param>
    /// <param name="token">停止要求</param>
    /// <returns>解凍完了の場合はtrue、パスワード検出で後回しにした場合はfalse</returns>
    private bool ExtractWithPassword(ArchiveExtractor extractor, string archive, string output, Settings settings, CancellationToken token)
    {
        string? password = null;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                extractor.Extract(archive, output, token, password);
                return true;
            }
            catch (Exception ex) when (ArchiveExtractor.IsPasswordRequired(ex) ||
                settings.InteractivePasswords && password != null && ArchiveExtractor.IsPasswordRejected(ex))
            {
                string marker = Path.Combine(Path.GetDirectoryName(archive)!, "password-required");
                if (!File.Exists(marker))
                {
                    File.WriteAllText(marker, string.Empty);
                    logger.Write(MessageId.I006, null, Path.GetFileName(archive));
                    return false;
                }

                if (!settings.InteractivePasswords)
                {
                    throw;
                }

                // パスワードや復号例外の内容はログへ出さない
                logger.Write(password == null ? MessageId.I387 : MessageId.W007, null, Path.GetFileName(archive));
                Directory.Delete(output, true);
                Directory.CreateDirectory(output);
                password = progress.RequestPassword(token, password != null);
                if (password == null)
                {
                    throw new InvalidOperationException(Messages.Get(MessageId.E113));
                }

                progress.Begin(Path.GetDirectoryName(archive)!, Path.GetFileName(archive), new FileInfo(archive).Length);
            }
        }
    }

}

