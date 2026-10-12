namespace AutoExtracter;

/// <summary>
/// 通知領域のアイコンと監視サービスのライフサイクルを管理する
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly TrayNotifyIcon icon;
    private readonly ProgressForm form;
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly System.Windows.Forms.Timer animationTimer = new();
    private StateTrayAnimations animations;
    private readonly Settings initialSettings;
    private readonly SettingsStore? settingsStore;
    private string idleGifPath;
    private string extractingGifPath;
    private string dialogueGifPath;
    private long notifiedPasswordRequest;
    private bool animationsEnabled;
    private GifTrayAnimation? animation;
    private readonly CancellationTokenSource stop = new();
    private readonly ProgressState progress;
    private readonly FileLogger logger;
    private readonly Task worker;
    private bool stopping;
    private Icon? renderedIcon;
    private (MessageId Phase, int? Percent)? iconState;

    /// <summary>
    /// 監視サービスの終了コード
    /// </summary>
    public int ExitCode
    {
        get;
        private set;
    }

    /// <summary>
    /// 処理設定を適用する再起動が要求されたか
    /// </summary>
    public bool RestartRequested
    {
        get;
        private set;
    }

    /// <summary>
    /// 通知領域を初期化し、バックグラウンド監視を開始する
    /// </summary>
    /// <param name="service">監視サービス</param>
    /// <param name="progress">画面と共有する処理状況</param>
    /// <param name="logger">エラーログの出力先</param>
    /// <param name="settings">フォルダの設定</param>
    /// <param name="settingsStore">保存先と実行中の設定</param>
    /// <param name="instanceGuard">重複起動からのパネル表示要求を受け取る起動管理</param>
    public TrayApplicationContext(WatcherService service, ProgressState progress, FileLogger logger, Settings settings, SettingsStore? settingsStore = null, SingleInstanceGuard? instanceGuard = null)
    {
        initialSettings = settings;
        animationsEnabled = settings.AnimationsEnabled;
        this.settingsStore = settingsStore;
        idleGifPath = settings.IdleGifPath;
        extractingGifPath = settings.ExtractingGifPath;
        dialogueGifPath = settings.DialogueGifPath;
        form = new ProgressForm(settings, RequestStop, logger, settingsStore, RequestRestart, progress);
        this.progress = progress;
        this.logger = logger;
        icon = new TrayNotifyIcon();
        icon.Icon = SystemIcons.Application;
        icon.Text = Messages.Get(MessageId.I310);
        icon.BalloonTipClicked += (_, _) =>
        {
            ShowDetails();
        };

        animations = new StateTrayAnimations(settings, logger);
        animation = animations.Select(progress.Read().Phase);
        if (animation != null)
        {
            icon.Icon = animation.Current;
        }

        animationTimer.Interval = 50;
        animationTimer.Tick += (_, _) =>
        {
            if (animationsEnabled && animation != null && animation.Advance())
            {
                icon.Icon = animation.Current;
            }
        };

        animationTimer.Enabled = animationsEnabled && animation != null;
        icon.Visible = true;
        icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowDetails();
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (form.Visible)
                {
                    form.Hide();
                }
                else
                {
                    ShowDetails();
                }
            }
        };

        worker = Task.Run(() =>
        {
            return service.RunAsync(stop.Token);
        });

        timer.Interval = 500;
        timer.Tick += (_, _) =>
        {
            if (!stopping && instanceGuard?.TakeActivationRequest() == true)
            {
                ShowDetails();
            }

            RefreshStatus();
        };

        timer.Start();
    }

    /// <summary>
    /// 詳細画面を表示する
    /// </summary>
    private void ShowDetails()
    {
        form.UpdateStatus(progress.Read());
        form.PositionNearNotificationArea();
        form.Show();
        form.WindowState = FormWindowState.Normal;
        form.Activate();
        form.FocusPasswordInput();
    }

    /// <summary>
    /// 画面を更新し、監視サービスの終了を確認する
    /// </summary>
    private void RefreshStatus()
    {
        var state = progress.Read();
        Settings settings = settingsStore?.Current ?? initialSettings;
        StateTrayAnimations? previousAnimations = null;
        if (idleGifPath != settings.IdleGifPath || extractingGifPath != settings.ExtractingGifPath || dialogueGifPath != settings.DialogueGifPath)
        {
            var next = new StateTrayAnimations(settings, logger);
            previousAnimations = animations;
            animations = next;
            animation = null;
            iconState = null;
            idleGifPath = settings.IdleGifPath;
            extractingGifPath = settings.ExtractingGifPath;
            dialogueGifPath = settings.DialogueGifPath;
        }

        var selected = animations.Select(state.Phase);
        if (animationsEnabled != settings.AnimationsEnabled)
        {
            animationsEnabled = settings.AnimationsEnabled;
            if (selected != null)
            {
                selected.Reset();
                icon.Icon = selected.Current;
            }
        }

        animationTimer.Enabled = animationsEnabled && selected != null;
        if (!ReferenceEquals(animation, selected))
        {
            animation = selected;
            iconState = null;
            if (animation != null)
            {
                animation.Reset();
                icon.Icon = animation.Current;
            }
        }

        var nextIconState = (state.Phase, state.Percent);
        if (animation == null && iconState != nextIconState)
        {
            Icon nextIcon = TrayIconRenderer.Create(state);
            icon.Icon = nextIcon;
            renderedIcon?.Dispose();
            renderedIcon = nextIcon;
            iconState = nextIconState;
        }

        previousAnimations?.Dispose();
        icon.Text = ProgressFormatter.Tooltip(state);
        form.UpdateStatus(state);
        if (!stopping && state.Phase == MessageId.I386 && state.PasswordRequestId != notifiedPasswordRequest)
        {
            notifiedPasswordRequest = state.PasswordRequestId;
            icon.ShowBalloonTip(10000, Messages.Get(MessageId.I310),
                Messages.Get(state.PasswordRetry ? MessageId.W007 : MessageId.I387, state.ArchiveName), ToolTipIcon.Info, animations.Select(MessageId.I386)?.FirstFrame);
            ShowDetails();
        }

        if (!worker.IsCompleted)
        {
            return;
        }

        if (worker.IsFaulted)
        {
            ExitCode = 1;
            logger.Write(MessageId.E001, worker.Exception);
            ThemedMessageDialog.ShowNotice(form, Messages.Get(MessageId.E001), Messages.Get(MessageId.I310));
        }

        timer.Stop();
        icon.Visible = false;
        ExitThread();
    }

    /// <summary>
    /// 処理中のジョブを保持し、監視サービスへ終了を要求する
    /// </summary>
    internal void RequestStop()
    {
        if (stopping)
        {
            return;
        }

        stopping = true;
        progress.SetPhase(MessageId.I305);
        stop.Cancel();
    }

    /// <summary>
    /// 処理中のジョブを保持して再起動を要求する
    /// </summary>
    internal void RequestRestart()
    {
        if (stopping)
        {
            return;
        }

        RestartRequested = true;
        RequestStop();
    }

    /// <summary>
    /// 通知領域と画面のリソースを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            animationTimer.Dispose();
            icon.Visible = false;
            icon.Dispose();
            animations.Dispose();
            renderedIcon?.Dispose();
            form.Dispose();
            stop.Dispose();
        }

        base.Dispose(disposing);
    }
}



