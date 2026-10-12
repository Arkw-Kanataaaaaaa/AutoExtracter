using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// 通知領域から開く解凍状況パネル
/// </summary>
internal sealed class ProgressForm : Form
{
    private readonly PixelFont pixelFont = new();
    private readonly Label phase = new();
    private readonly Label archive = new();
    private readonly Label totals = new();
    private readonly Label capacity = new();
    private readonly Panel card = new();
    private readonly Panel workBar = new();

    private ThemePalette? appliedPalette;
    private readonly ArchiveQueueList queueList = new();
    private readonly Panel idlePanel = new();
    private readonly Label idleMessage = new();
    private readonly System.Windows.Forms.Timer loadingTimer = new();
    private int loadingDots;
    private readonly PixelProgressBar progressBar = new();

    private readonly Label watchLink = new();
    private readonly PixelToolTip toolTip = new();
    private readonly ProblemIconButton issueButton = new();
    private readonly FolderIssueMonitor issueMonitor;
    private readonly Settings initialSettings;
    private readonly SettingsStore? settingsStore;
    private readonly FileLogger? logger;
    private Settings CurrentSettings => settingsStore?.Current ?? initialSettings;
    private SettingsEditorForm? settingsWindow;
    private readonly Action? restart;
    private AnimatedGifView? idleGif;
    private AnimatedGifView? extractingGif;
    private string loadedIdlePath = "";
    private string loadedExtractingPath = "";
    private bool showingDialogue;
    private readonly ProgressState? passwordProgress;
    private readonly Panel passwordPanel = new();
    private readonly TextBox passwordInput = new();
    private long passwordRequestId;
    private bool stopping;

    /// <summary>
    /// パスワード入力待ちの場合に入力欄へフォーカスする
    /// </summary>
    internal void FocusPasswordInput()
    {
        if (passwordPanel.Visible && passwordPanel.Enabled)
        {
            passwordInput.Focus();
        }
    }

    /// <summary>
    /// 歯車のクリックで全設定とフォルダ操作のダイアログを開く
    /// </summary>
    private void OpenSettings()
    {
        if (settingsStore == null)
        {
            return;
        }

        Hide();
        if (settingsWindow != null && !settingsWindow.IsDisposed)
        {
            settingsWindow.WindowState = FormWindowState.Normal;
            settingsWindow.Show();
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsEditorForm(settingsStore, nameof(Settings.WatchFolder), logger, restart);
        settingsWindow.FormClosed += (_, _) =>
        {
            settingsWindow = null;
        };

        settingsWindow.Show();
        settingsWindow.Activate();
    }

    /// <summary>
    /// 退避ファイル件数と問題マークの表示を更新する
    /// </summary>
    /// <param name="issues">退避フォルダの確認結果</param>
    private void UpdateIssueIndicator(FolderIssueState issues)
    {
        issueButton.Count = issues.TotalCount;
        issueButton.Visible = issues.TotalCount > 0;
        issueButton.AccessibleName = Messages.Get(MessageId.I348, issues.TotalCount);
        watchLink.Width = issues.TotalCount > 0 ? 198 : 246;
    }

    /// <summary>
    /// 外観の選択状態とWindowsのテーマ変更を表示へ反映する
    /// </summary>
    private void ApplyTheme()
    {
        ThemePalette palette = ThemePalette.Resolve(CurrentSettings.Appearance);
        if (ReferenceEquals(appliedPalette, palette))
        {
            return;
        }

        appliedPalette = palette;
        ThemeControlStyler.Apply(this, palette, (control, background) =>
        {
            if (control == card || control == idlePanel || control == queueList || control == workBar)
            {
                return palette.Surface;
            }

            if (control is PixelIconButton or ProblemIconButton)
            {
                return palette.Background;
            }

            return background;
        });

        phase.ForeColor = palette.Status;
        queueList.Palette = palette;
        progressBar.Palette = palette;
        toolTip.Palette = palette;

        Invalidate(true);

    }

    /// <summary>
    /// サイズ変更後の角丸形状を更新する
    /// </summary>
    /// <param name="e">変更情報</param>
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RoundedFrame.ApplyRegion(this);
    }
    /// <summary>
    /// 状況とフォルダ操作を表示するパネルを初期化する
    /// </summary>
    /// <param name="settings">フォルダの設定</param>
    /// <param name="stop">終了処理</param>
    /// <param name="logger">GIF読み込み失敗のログ出力先</param>
    /// <param name="settingsStore">設定の保存先</param>
    /// <param name="restart">処理設定保存後の再起動要求</param>
    /// <param name="passwordProgress">パスワード入力を待つ処理状態</param>
    public ProgressForm(Settings settings, Action stop, FileLogger? logger = null, SettingsStore? settingsStore = null, Action? restart = null, ProgressState? passwordProgress = null)
    {
        initialSettings = settings;
        this.settingsStore = settingsStore;
        this.logger = logger;
        this.restart = restart;
        this.passwordProgress = passwordProgress;
        loadedIdlePath = settings.IdleGifPath;
        loadedExtractingPath = settings.ExtractingGifPath;
        issueMonitor = new FolderIssueMonitor(logger);
        Text = Messages.Get(MessageId.I310);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(360, 408);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = ThemePalette.Dark.Background;
        ForeColor = Color.FromArgb(216, 236, 220);
        Font = pixelFont.Font;
        var options = new PixelIconButton(false);
        options.Name = "SettingsButton";
        options.AccessibleName = Messages.Get(MessageId.I334);
        options.Enabled = settingsStore != null;
        options.SetBounds(266, 14, 34, 38);
        Controls.Add(options);
        toolTip.SetToolTip(options, options.AccessibleName);
        options.Click += (_, _) =>
        {
            OpenSettings();
        };

        issueButton.Name = "ProblemButton";
        issueButton.Font = pixelFont.ButtonFont;
        issueButton.SetBounds(220, 8, 44, 44);
        issueButton.Visible = false;
        Controls.Add(issueButton);
        issueButton.Click += async (_, _) =>
        {
            issueButton.Enabled = false;
            try
            {
                FolderIssueState issues = await issueMonitor.ReadNowAsync(CurrentSettings);
                if (IsDisposed)
                {
                    return;
                }

                UpdateIssueIndicator(issues);
                foreach (string folder in issues.GetFolderTargets())
                {
                    FolderLauncher.Open(folder, this);
                }

                if (issues.TotalCount > 0)
                {
                    FolderLauncher.OpenTodayLog(CurrentSettings.LogFolder, this);
                }
            }
            finally
            {
                if (!IsDisposed && !stopping)
                {
                    issueButton.Enabled = true;
                }
            }
        };

        var power = new PixelIconButton(true);
        power.Name = "PowerButton";
        power.AccessibleName = Messages.Get(MessageId.I307);
        power.SetBounds(304, 14, 34, 38);
        Controls.Add(power);
        toolTip.SetToolTip(power, power.AccessibleName);
        power.Click += (_, _) =>
        {
            stopping = true;
            power.Enabled = false;
            options.Enabled = false;
            issueButton.Enabled = false;

            stop();
        };

        watchLink.Name = "WatchFolderLink";
        watchLink.Font = pixelFont.Font;
        watchLink.Cursor = Cursors.Hand;
        SetLabel(watchLink, 16, 18, 246, 30);
        watchLink.TextAlign = ContentAlignment.MiddleLeft;
        watchLink.Click += (_, _) =>
        {
            FolderLauncher.Open(CurrentSettings.WatchFolder, this);
        };

        phase.ForeColor = Color.FromArgb(120, 210, 140);
        card.BackColor = Color.FromArgb(28, 40, 56);
        card.SetBounds(16, 68, 328, 324);
        Controls.Add(card);
        workBar.BackColor = Color.FromArgb(36, 52, 68);
        workBar.SetBounds(0, 0, 328, 180);
        workBar.Name = "WorkBar";
        workBar.Paint += (_, e) =>
        {
            using var separator = new Pen((appliedPalette ?? ThemePalette.Dark).Track);
            e.Graphics.DrawLine(separator, 12, workBar.Height - 1, workBar.Width - 12, workBar.Height - 1);
        };

        card.Controls.Add(workBar);
        SetLabel(archive, 12, 10, 212, 52, workBar);
        archive.Font = Font;
        // 二行の容量表示が状態表示の上に重ならない高さにする
        SetLabel(capacity, 12, 66, 212, 48, workBar);
        SetLabel(phase, 12, 118, 250, 28, workBar);
        extractingGif = new AnimatedGifView(settings.ExtractingGifPath, logger, "Extracting.gif", MessageId.W004);
        extractingGif.SetBounds(240, 16, 76, 92);
        workBar.Controls.Add(extractingGif);
        SetLabel(totals, 270, 148, 52, 28, workBar);
        totals.Font = pixelFont.ButtonFont;
        progressBar.SetBounds(12, 156, 250, 12);
        workBar.Controls.Add(progressBar);
        passwordPanel.Name = "PasswordPanel";
        passwordPanel.SetBounds(12, 148, 304, 76);
        passwordPanel.Visible = false;
        workBar.Controls.Add(passwordPanel);
        var passwordLabel = new Label();
        passwordLabel.Text = Messages.Get(MessageId.I391);
        passwordLabel.SetBounds(0, 0, 304, 26);
        passwordPanel.Controls.Add(passwordLabel);
        passwordInput.Name = "PasswordInput";
        passwordInput.UseSystemPasswordChar = true;
        var passwordFrame = new SettingsInputFrame(passwordInput);
        passwordFrame.SetBounds(0, 28, 144, 40);
        passwordPanel.Controls.Add(passwordFrame);
        var submitPassword = new SettingsButton();
        submitPassword.Name = "SubmitPasswordButton";
        submitPassword.Text = Messages.Get(MessageId.I392);
        submitPassword.Font = pixelFont.ButtonFont;
        submitPassword.SetBounds(152, 28, 72, 40);
        submitPassword.Click += (_, _) =>
        {
            if (passwordInput.Text.Length > 0 && passwordProgress?.SubmitPassword(passwordRequestId, passwordInput.Text) == true)
            {
                passwordInput.Clear();
                passwordPanel.Enabled = false;
            }
        };

        passwordPanel.Controls.Add(submitPassword);
        passwordInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                submitPassword.PerformClick();
            }
        };

        var retirePassword = new SettingsButton();
        retirePassword.Name = "RetirePasswordButton";
        retirePassword.Text = Messages.Get(MessageId.I393);
        retirePassword.Font = pixelFont.ButtonFont;
        retirePassword.SetBounds(232, 28, 72, 40);
        retirePassword.Click += (_, _) =>
        {
            if (passwordProgress?.SubmitPassword(passwordRequestId, null) == true)
            {
                passwordInput.Clear();
                passwordPanel.Enabled = false;
            }
        };

        passwordPanel.Controls.Add(retirePassword);
        queueList.Name = "QueueList";
        queueList.SetBounds(12, 188, 304, 126);
        card.Controls.Add(queueList);
        idlePanel.Name = "IdlePanel";
        idlePanel.BackColor = card.BackColor;
        idlePanel.SetBounds(16, 68, 328, 324);
        Controls.Add(idlePanel);
        idleGif = new AnimatedGifView(settings.IdleGifPath, logger);
        idleGif.SetBounds(74, 36, 180, 180);
        idlePanel.Controls.Add(idleGif);
        // 最大の点数を基準に表示領域を中央へ置き、文字の開始位置を固定する
        int idleMessageWidth = TextRenderer.MeasureText(Messages.Get(MessageId.I326, "..."), Font).Width;
        SetLabel(idleMessage, (idlePanel.Width - idleMessageWidth) / 2, 244, idleMessageWidth, 48, idlePanel);
        idleMessage.Name = "IdleMessage";
        idleMessage.TextAlign = ContentAlignment.MiddleLeft;
        idleMessage.Text = Messages.Get(MessageId.I326, "");
        loadingTimer.Interval = 400;
        loadingTimer.Tick += (_, _) =>
        {
            if (!CurrentSettings.AnimationsEnabled)
            {
                return;
            }

            loadingDots = (loadingDots + 1) % 4;
            idleMessage.Text = Messages.Get(MessageId.I326, new string('.', loadingDots));
        };

        VisibleChanged += (_, _) =>
        {
            loadingTimer.Enabled = Visible && idlePanel.Visible && CurrentSettings.AnimationsEnabled;
        };

        UpdateStatus(new ProgressState().Read());
        Deactivate += (_, _) =>
        {
            Hide();
        };

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    /// <summary>
    /// 表示ラベルの位置と省略表示を設定する
    /// </summary>
    /// <param name="label">対象ラベル</param>
    /// <param name="x">左位置</param>
    /// <param name="y">上位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="parent">配置先</param>
    private void SetLabel(Label label, int x, int y, int width, int height, Control? parent = null)
    {
        label.AutoEllipsis = true;
        label.SetBounds(x, y, width, height);
        (parent ?? this).Controls.Add(label);
    }

    /// <summary>
    /// カーソルのある画面の通知領域付近へパネルを配置する
    /// </summary>
    public void PositionNearNotificationArea()
    {
        Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Max(work.Left, work.Right - Width - 12),
            Math.Max(work.Top, work.Bottom - Height - 12));
    }

    /// <summary>
    /// 最新の処理状況を画面へ反映する
    /// </summary>
    /// <param name="state">表示する処理状況</param>
    public void UpdateStatus(ProgressSnapshot state)
    {
        bool dialogue = state.Phase == MessageId.I386;
        RefreshGifViews(dialogue);
        idleGif!.AnimationEnabled = CurrentSettings.AnimationsEnabled;
        extractingGif!.AnimationEnabled = CurrentSettings.AnimationsEnabled;
        ApplyTheme();
        UpdateIssueIndicator(issueMonitor.Refresh(CurrentSettings));
        string folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(CurrentSettings.WatchFolder));
        watchLink.Text = Messages.Get(MessageId.I330, string.IsNullOrEmpty(folderName) ? CurrentSettings.WatchFolder : folderName);
        toolTip.SetToolTip(watchLink, CurrentSettings.WatchFolder);
        bool idle = state.Phase == MessageId.I301;
        card.Visible = !idle;
        idlePanel.Visible = idle;
        phase.Visible = !idle;
        loadingTimer.Enabled = Visible && idle && CurrentSettings.AnimationsEnabled;
        if (!CurrentSettings.AnimationsEnabled)
        {
            loadingDots = 0;
            idleMessage.Text = Messages.Get(MessageId.I326, "");
        }

        phase.Text = Messages.Get(state.Phase);
        passwordPanel.Visible = dialogue;
        progressBar.Visible = !dialogue;
        totals.Visible = !dialogue;
        workBar.Height = dialogue ? 236 : 180;
        queueList.SetBounds(12, dialogue ? 244 : 188, 304, dialogue ? 70 : 126);
        if (dialogue && passwordRequestId != state.PasswordRequestId)
        {
            passwordRequestId = state.PasswordRequestId;
            passwordInput.Clear();
            passwordPanel.Enabled = true;
        }

        if (!dialogue)
        {
            passwordInput.Clear();
        }

        archive.Text = string.IsNullOrEmpty(state.ArchiveName)
            ? Messages.Get(MessageId.I322) : state.ArchiveName;
        string unknown = Messages.Get(MessageId.I311);
        totals.Text = state.Percent.HasValue ? state.Percent + "%" : unknown;
        capacity.Text = Messages.Get(MessageId.I328, ProgressFormatter.Size(state.ArchiveBytes),
            ProgressFormatter.Size(state.ExtractedBytes),
            state.TotalBytes.HasValue ? ProgressFormatter.Size(state.TotalBytes.Value) : unknown);
        queueList.UpdateArchives(state.WaitingArchives);
        queueList.Visible = state.WaitingArchives.Count != 0;
        bool active = state.Phase == MessageId.I302 || state.Phase == MessageId.I303;
        progressBar.Style = CurrentSettings.AnimationsEnabled && active && (!state.Percent.HasValue || state.Phase == MessageId.I303)
            ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        progressBar.Value = state.Percent ?? 0;
    }

    /// <summary>
    /// パネルのメニューを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            loadingTimer.Dispose();
            toolTip.Dispose();
            settingsWindow?.Dispose();

        }

        base.Dispose(disposing);
        if (disposing)
        {
            pixelFont.Dispose();
        }
    }

    /// <summary>
    /// 外観設定の変更後に状態別GIFを読み込み直す
    /// </summary>
    private void RefreshGifViews(bool dialogue)
    {
        if (idleGif != null && loadedIdlePath != CurrentSettings.IdleGifPath)
        {
            var next = new AnimatedGifView(CurrentSettings.IdleGifPath, logger);
            next.Bounds = idleGif.Bounds;
            idlePanel.Controls.Remove(idleGif);
            idleGif.Dispose();
            idleGif = next;
            idlePanel.Controls.Add(next);
            loadedIdlePath = CurrentSettings.IdleGifPath;
        }

        string activePath = dialogue ? CurrentSettings.DialogueGifPath : CurrentSettings.ExtractingGifPath;
        if (extractingGif != null && (loadedExtractingPath != activePath || showingDialogue != dialogue))
        {
            var next = new AnimatedGifView(activePath, logger, dialogue ? "Dialogue.gif" : "Extracting.gif",
                dialogue ? MessageId.W008 : MessageId.W004);
            next.Bounds = extractingGif.Bounds;
            workBar.Controls.Remove(extractingGif);
            extractingGif.Dispose();
            extractingGif = next;
            workBar.Controls.Add(next);
            loadedExtractingPath = activePath;
            showingDialogue = dialogue;
        }
    }
}


