namespace AutoExtracter;

/// <summary>
/// 処理設定、外観、その他を切り替える独立した設定ウィンドウ
/// </summary>
internal sealed class SettingsEditorForm : Form
{
    private readonly PixelFont pixelFont = new();
    private readonly SettingsStore store;
    private readonly FileLogger? logger;
    private readonly Action? restart;
    private readonly Func<IWin32Window, RestartConfirmationResult> confirmRestart;
    private readonly Panel[] pages = new Panel[3];
    private readonly SettingsButton[] navigation = new SettingsButton[3];
    private readonly MessageId[] sectionNames = new[]
    {
        MessageId.I353, MessageId.I378, MessageId.I355
    };

    private readonly SettingsButton close;
    private readonly ArchiveExtensionSelector extensions = new();
    private readonly SettingsNumberInput capacity = new();
    private readonly ThemeColorPicker appearance = new();
    private readonly SettingsToggle animationSwitch = new();
    private readonly Font sectionFont;
    private readonly SettingsToggle passwordMode = new();
    private readonly SettingsToggle startupSwitch = new();
    private readonly SettingsToggle keepArchives = new();
    private readonly SettingsPathRow archiveBackup;
    private readonly ApplicationShortcuts shortcuts;
    private readonly Label otherResult = new();
    private readonly SettingsPathRow watch;
    private readonly SettingsPathRow destination;
    private readonly SettingsGifCard idle;
    private readonly SettingsGifCard extracting;
    private readonly SettingsGifCard dialogue;
    private readonly System.Windows.Forms.Timer themeTimer = new();
    private ThemePalette? palette;
    private Icon? settingsIcon;
    private int section = -1;

    /// <summary>
    /// 現在の設定を三つのセクションへ読み込む
    /// </summary>
    /// <param name="store">設定の保存先</param>
    /// <param name="focus">初期セクションの選択に使う項目名</param>
    /// <param name="logger">ログ出力先</param>
    /// <param name="restart">処理設定保存後の再起動要求</param>
    /// <param name="confirmRestart">再起動確認の操作、未指定の場合はテーマ付きメッセージを表示する</param>
    public SettingsEditorForm(SettingsStore store, string focus, FileLogger? logger, Action? restart = null, Func<IWin32Window, RestartConfirmationResult>? confirmRestart = null)
    {
        this.store = store;
        this.logger = logger;
        this.restart = restart;
        this.confirmRestart = confirmRestart ?? (owner =>
        {
            return ThemedMessageDialog.ConfirmRestart(owner, ThemePalette.Resolve(SelectedAppearance));
        });

        shortcuts = new ApplicationShortcuts(Application.ExecutablePath);
        Text = Messages.Get(MessageId.I334);
        ClientSize = new Size(840, 710);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        ShowInTaskbar = true;
        TopMost = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = pixelFont.Font;
        sectionFont = new Font(Font.FontFamily, 18, FontStyle.Regular, GraphicsUnit.Point);
        var title = AddLabel(this, MessageId.I334, 16, 14, 740, 28);
        title.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowDragHelper.Start(this);
            }
        };
        // 外枠の描画範囲にボタンの背景が重ならない位置に配置する
        close = AddButton(this, MessageId.I352, ClientSize.Width - 59, 5, 46, 40);
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.BorderSize = 0;
        close.Name = "CancelSettingsButton";
        close.AccessibleName = Messages.Get(MessageId.I371);
        close.Click += (_, _) =>
        {
            Close();
        };

        CancelButton = close;
        for (int index = 0; index < pages.Length; index++)
        {
            int selected = index;
            var page = new Panel();
            page.Name = "SettingsPage" + index;
            page.SetBounds(194, 56, 630, 632);
            Controls.Add(page);
            pages[index] = page;
            var heading = AddLabel(page, sectionNames[index], 16, 10, 596, 38);
            heading.Font = sectionFont;
            var button = AddButton(this, sectionNames[index], 16, 62 + index * 54, 162, 46);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.BorderSize = 0;
            button.AccessibleName = Messages.Get(sectionNames[index]);
            button.Name = "SectionButton" + index;
            button.Click += (_, _) =>
            {
                SelectSection(selected);
            };

            navigation[index] = button;
        }

        watch = AddPath(pages[0], MessageId.I356, store.Current.WatchFolder, 58, "WatchFolderInput", requireExistingFolder: true);
        destination = AddPath(pages[0], MessageId.I357, store.Current.CompletedFolder, 148, "DestinationFolderInput", requireExistingFolder: true);
        var keepLabel = new SettingsRowLabel();
        keepLabel.Text = Messages.Get(MessageId.I404);
        keepLabel.SetBounds(16, 238, 500, 40);
        pages[0].Controls.Add(keepLabel);
        keepArchives.Name = "KeepOriginalArchivesInput";
        keepArchives.AccessibleName = Messages.Get(MessageId.I404);
        keepArchives.Checked = store.Current.KeepOriginalArchives;
        keepArchives.SetBounds(562, 238, 50, 40);
        pages[0].Controls.Add(keepArchives);
        archiveBackup = AddPath(pages[0], MessageId.I405, store.Current.ArchiveBackupFolder, 292,
            "ArchiveBackupFolderInput", requireExistingFolder: true, showTitle: false);
        archiveBackup.Enabled = keepArchives.Checked;
        keepArchives.Click += (_, _) =>
        {
            UpdateProcessingLayout();
            archiveBackup.ValidateFolder();
        };

        AddLabel(pages[0], MessageId.I358, 16, 358, 250, 26);
        AddLabel(pages[0], MessageId.I359, 284, 358, 328, 26);
        extensions.Name = "ArchiveExtensionsInput";
        extensions.SetBounds(16, 388, 250, 42);
        extensions.Value = store.Current.ArchiveExtensions;
        var extensionFrame = new SettingsInputFrame(extensions);
        extensionFrame.SetBounds(16, 388, 250, 48);
        pages[0].Controls.Add(extensionFrame);
        capacity.Name = "CapacityInput";
        capacity.SetBounds(284, 388, 328, 30);
        capacity.Minimum = 0.01m;
        capacity.DecimalPlaces = 2;
        capacity.Maximum = decimal.Floor(long.MaxValue / 1_000_000_000m);
        capacity.ThousandsSeparator = true;
        capacity.Value = store.Current.MaxArchiveBytes / 1_000_000_000m;
        var capacityFrame = new SettingsInputFrame(capacity);
        capacityFrame.SetBounds(284, 388, 328, 48);
        pages[0].Controls.Add(capacityFrame);
        var passwordLabel = new SettingsRowLabel();
        passwordLabel.Text = Messages.Get(MessageId.I385);
        passwordLabel.SetBounds(16, 462, 240, 40);
        pages[0].Controls.Add(passwordLabel);
        passwordMode.Name = "PasswordModeInput";
        passwordMode.AccessibleName = Messages.Get(MessageId.I385);
        passwordMode.Checked = store.Current.InteractivePasswords;
        passwordMode.SetBounds(262, 462, 50, 40);
        pages[0].Controls.Add(passwordMode);
        var startupLabel = new SettingsRowLabel();
        startupLabel.Text = Messages.Get(MessageId.I396);
        startupLabel.SetBounds(334, 462, 210, 40);
        pages[0].Controls.Add(startupLabel);
        startupSwitch.Name = "StartupEnabledInput";
        startupSwitch.AccessibleName = Messages.Get(MessageId.I396);
        startupSwitch.Checked = shortcuts.StartupEnabled;
        startupSwitch.SetBounds(562, 462, 50, 40);
        pages[0].Controls.Add(startupSwitch);
        startupSwitch.Click += (_, _) =>
        {
            try
            {
                shortcuts.SetStartup(startupSwitch.Checked);
                logger?.Write(MessageId.I399);
            }
            catch (Exception ex)
            {
                startupSwitch.Checked = shortcuts.StartupEnabled;
                ShowError(MessageId.E121, ex);
            }
        };

        var applyProcessing = AddButton(pages[0], MessageId.I366, 512, 566, 100, 34);
        applyProcessing.Name = "ApplyProcessingButton";
        applyProcessing.Enabled = restart != null;
        applyProcessing.Click += (_, _) =>
        {
            ApplyProcessing();
        };

        var resetProcessing = AddButton(pages[0], MessageId.I384, 382, 566, 120, 34);
        resetProcessing.Name = "ResetProcessingButton";
        resetProcessing.Click += (_, _) =>
        {
            var defaults = new Settings();
            string root = Path.GetDirectoryName(store.Current.TemporaryFolder)!;
            watch.Value = Path.Combine(root, "Incoming");
            destination.Value = Path.Combine(root, "Completed");
            capacity.Value = defaults.MaxArchiveBytes / 1_000_000_000m;
            passwordMode.Checked = false;
            keepArchives.Checked = false;
            archiveBackup.Value = Path.Combine(root, "Archives");
            UpdateProcessingLayout();
            extensions.Value = defaults.ArchiveExtensions;
        };

        var themeLabel = new SettingsRowLabel();
        themeLabel.Text = Messages.Get(MessageId.I344);
        themeLabel.SetBounds(16, 68, 120, 40);
        pages[1].Controls.Add(themeLabel);
        appearance.Name = "AppearanceInput";
        appearance.SetBounds(140, 68, 170, 40);
        appearance.SelectedIndex = store.Current.Appearance == "Light" ? 0 : store.Current.Appearance == "System" ? 2 : 1;
        appearance.SelectedIndexChanged += (_, _) =>
        {
            ApplyTheme();
        };

        pages[1].Controls.Add(appearance);
        var animationLabel = new SettingsRowLabel();
        animationLabel.Text = Messages.Get(MessageId.I394);
        animationLabel.SetBounds(334, 68, 210, 40);
        pages[1].Controls.Add(animationLabel);
        animationSwitch.Name = "AnimationsEnabledInput";
        animationSwitch.AccessibleName = Messages.Get(MessageId.I394);
        animationSwitch.Checked = store.Current.AnimationsEnabled;
        animationSwitch.SetBounds(562, 68, 50, 40);
        pages[1].Controls.Add(animationSwitch);
        AddLabel(pages[1], MessageId.I381, 16, 148, 596, 26);
        idle = new SettingsGifCard(MessageId.I382, store.Current.IdleGifPath, "Idle.gif", 188);
        idle.Name = "IdleGifInput";
        idle.Location = new Point(16, 178);
        pages[1].Controls.Add(idle);
        extracting = new SettingsGifCard(MessageId.I383, store.Current.ExtractingGifPath, "Extracting.gif", 188);
        extracting.Name = "ExtractingGifInput";
        extracting.Location = new Point(220, 178);
        pages[1].Controls.Add(extracting);
        dialogue = new SettingsGifCard(MessageId.I390, store.Current.DialogueGifPath, "Dialogue.gif", 188);
        dialogue.Name = "DialogueGifInput";
        dialogue.Location = new Point(424, 178);
        pages[1].Controls.Add(dialogue);
        idle.AnimationEnabled = animationSwitch.Checked;
        extracting.AnimationEnabled = animationSwitch.Checked;
        dialogue.AnimationEnabled = animationSwitch.Checked;
        var applyVisuals = AddButton(pages[1], MessageId.I366, 512, 566, 100, 34);
        applyVisuals.Name = "ApplyVisualsButton";
        applyVisuals.Click += (_, _) =>
        {
            ApplyVisuals();
        };

        var resetVisuals = AddButton(pages[1], MessageId.I384, 382, 566, 120, 34);
        resetVisuals.Name = "ResetVisualsButton";
        resetVisuals.Click += (_, _) =>
        {
            appearance.SelectedIndex = 1;
            idle.Value = string.Empty;
            extracting.Value = string.Empty;
            dialogue.Value = string.Empty;
            animationSwitch.Checked = true;
        };

        var desktopShortcut = AddButton(pages[2], MessageId.I395, 16, 68, 596, 40);
        desktopShortcut.Name = "CreateDesktopShortcutButton";
        desktopShortcut.Click += (_, _) =>
        {
            try
            {
                shortcuts.CreateDesktop();
                otherResult.Text = Messages.Get(MessageId.I398);
                logger?.Write(MessageId.I398);
            }
            catch (Exception ex)
            {
                ShowError(MessageId.E121, ex);
            }
        };

        var installation = AddButton(pages[2], MessageId.I397, 16, 134, 596, 40);
        installation.Name = "OpenInstallationFolderButton";
        installation.Click += (_, _) =>
        {
            FolderLauncher.Open(AppContext.BaseDirectory, this);
        };

        var today = AddButton(pages[2], MessageId.I343, 16, 200, 596, 40);
        today.Click += (_, _) =>
        {
            FolderLauncher.OpenTodayLog(store.Current.LogFolder, this);
        };

        otherResult.SetBounds(16, 566, 596, 34);
        pages[2].Controls.Add(otherResult);
        themeTimer.Interval = 500;
        themeTimer.Tick += (_, _) =>
        {
            watch.ValidateFolder();
            if (keepArchives.Checked)
            {
                archiveBackup.ValidateFolder();
            }

            destination.ValidateFolder();
            ApplyTheme();
            idle.AnimationEnabled = animationSwitch.Checked;
            extracting.AnimationEnabled = animationSwitch.Checked;
            dialogue.AnimationEnabled = animationSwitch.Checked;
        };

        UpdateProcessingLayout();
        themeTimer.Start();
        SelectSection(focus == nameof(Settings.Appearance) ? 1 : 0);
        ApplyTheme();
    }

    /// <summary>
    /// 保持設定に合わせて退避先の入力と参照操作を切り替える
    /// </summary>
    private void UpdateProcessingLayout()
    {
        archiveBackup.Enabled = keepArchives.Checked;
    }

    /// <summary>
    /// 設定ウィンドウをアプリ本体とは別のタスクバー表示にする
    /// </summary>
    /// <param name="e">ハンドル生成の通知</param>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTaskbarIdentity.Apply(Handle, "TKer.AutoExtracter.Settings");
    }

    /// <summary>
    /// ウィンドウの破棄前にタスクバー識別子を解放する
    /// </summary>
    /// <param name="message">Windowsの通知</param>
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0002)
        {
            WindowTaskbarIdentity.Apply(message.HWnd, null);
        }

        base.WndProc(ref message);
    }

    /// <summary>
    /// 外観セクションで選択したテーマ
    /// </summary>
    private string SelectedAppearance => appearance.SelectedIndex == 0 ? "Light" : appearance.SelectedIndex == 2 ? "System" : "Dark";

    /// <summary>
    /// 処理設定を保存し、ジョブを保持して再起動を要求する
    /// </summary>
    private void ApplyProcessing()
    {
        bool watchValid = watch.ValidateFolder();
        bool destinationValid = destination.ValidateFolder();
        bool backupValid = !keepArchives.Checked || archiveBackup.ValidateFolder();
        if (!watchValid || !destinationValid || !backupValid)
        {
            (!watchValid ? watch : !destinationValid ? destination : archiveBackup).FocusInput();
            return;
        }

        try
        {
            var confirmation = new RestartConfirmationResult(true, true);
            if (!store.Current.SkipRestartConfirmation)
            {
                confirmation = confirmRestart(this);
                if (!confirmation.Accepted)
                {
                    return;
                }
            }

            store.SaveProcessingForRestart(watch.Value, destination.Value,
                extensions.Value, decimal.ToInt64(capacity.Value * 1_000_000_000m), passwordMode.Checked, confirmation.SkipNextTime, keepArchives.Checked, archiveBackup.Value);
            restart?.Invoke();
            Close();
        }
        catch (Exception ex)
        {
            ShowError(MessageId.E118, ex);
        }
    }

    /// <summary>
    /// 外観だけを保存して即時適用し、ウィンドウを保持する
    /// </summary>
    private void ApplyVisuals()
    {
        try
        {
            store.UpdateVisuals(SelectedAppearance, idle.Value, extracting.Value, dialogue.Value, animationSwitch.Checked);
            idle.Value = store.Current.IdleGifPath;
            extracting.Value = store.Current.ExtractingGifPath;
            dialogue.Value = store.Current.DialogueGifPath;
            ApplyTheme();
        }
        catch (Exception ex)
        {
            ShowError(MessageId.E118, ex);
        }
    }

    /// <summary>
    /// 操作の失敗をログとテーマ付きメッセージへ出力する
    /// </summary>
    /// <param name="id">エラーの識別子</param>
    /// <param name="ex">操作時の例外</param>
    private void ShowError(MessageId id, Exception ex)
    {
        logger?.Write(id, ex, ex.Message);
        ThemedMessageDialog.ShowNotice(this, Messages.Get(id, ex.Message), Text, ThemePalette.Resolve(SelectedAppearance));
    }

    /// <summary>
    /// 左側の選択に応じて右側のセクションを切り替える
    /// </summary>
    /// <param name="selected">表示するセクション</param>
    private void SelectSection(int selected)
    {
        SuspendLayout();
        try
        {
            if (section >= 0 && section != selected)
            {
                RestoreSection(section);
            }

            if (selected == 0)
            {
                startupSwitch.Checked = shortcuts.StartupEnabled;
            }

            section = selected;
            for (int index = 0; index < pages.Length; index++)
            {
                pages[index].Visible = index == section;
            }

            UpdateNavigation();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    /// <summary>
    /// 子コントロールとウィンドウをまとめて描画し、配置変更途中の背景を表示しない
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= 0x02000000;
            return parameters;
        }
    }

    /// <summary>
    /// セクションを離れるときに未適用の入力を現在の設定へ戻す
    /// </summary>
    /// <param name="selected">復元するセクション</param>
    private void RestoreSection(int selected)
    {
        Settings saved = store.Current;
        if (selected == 0)
        {
            watch.Value = saved.WatchFolder;
            destination.Value = saved.CompletedFolder;
            capacity.Value = saved.MaxArchiveBytes / 1_000_000_000m;
            passwordMode.Checked = saved.InteractivePasswords;
            keepArchives.Checked = saved.KeepOriginalArchives;
            archiveBackup.Value = saved.ArchiveBackupFolder;
            UpdateProcessingLayout();
            extensions.Value = saved.ArchiveExtensions;
        }
        else if (selected == 1)
        {
            appearance.SelectedIndex = saved.Appearance == "Light" ? 0 : saved.Appearance == "System" ? 2 : 1;
            idle.Value = saved.IdleGifPath;
            extracting.Value = saved.ExtractingGifPath;
            dialogue.Value = saved.DialogueGifPath;
            animationSwitch.Checked = saved.AnimationsEnabled;
            ApplyTheme();
        }
    }

    /// <summary>
    /// 選択中のセクションを強調する
    /// </summary>
    private void UpdateNavigation()
    {
        ThemePalette colors = palette ?? ThemePalette.Dark;
        for (int index = 0; index < navigation.Length; index++)
        {
            navigation[index].Text = Messages.Get(index == section ? MessageId.I376 : MessageId.I377,
                Messages.Get(sectionNames[index]));
            navigation[index].BackColor = colors.Background;
        }
    }

    /// <summary>
    /// 選択中のテーマとセクションの背景を反映する
    /// </summary>
    private void ApplyTheme()
    {
        ThemePalette next = ThemePalette.Resolve(SelectedAppearance);
        if (ReferenceEquals(palette, next))
        {
            return;
        }

        palette = next;
        Icon? previousIcon = settingsIcon;
        settingsIcon = SettingsSymbol.CreateIcon(next.Text);
        Icon = settingsIcon;
        previousIcon?.Dispose();
        ThemeControlStyler.Apply(this, next, (control, background) =>
        {
            if (pages.Contains(control))
            {
                return next.Surface;
            }

            return background;
        });

        UpdateNavigation();
        close.BackColor = next.Background;
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
    /// 共通のパス行を追加する
    /// </summary>
    /// <param name="parent">配置先</param>
    /// <param name="id">項目名</param>
    /// <param name="value">パス</param>
    /// <param name="y">縦位置</param>
    /// <param name="name">入力行の名前</param>
    /// <param name="requireExistingFolder">存在するフォルダだけを許可するか</param>
    /// <returns>追加した入力行</returns>
    /// <param name="showTitle">見出しを表示するか</param>
    private static SettingsPathRow AddPath(Control parent, MessageId id, string value, int y, string name, bool requireExistingFolder = false, bool showTitle = true)
    {
        var row = new SettingsPathRow(id, value, requireExistingFolder, showTitle);
        row.Name = name;
        row.Location = new Point(16, y);
        parent.Controls.Add(row);
        return row;
    }

    /// <summary>
    /// 角のある操作ボタンを追加する
    /// </summary>
    /// <param name="parent">配置先</param>
    /// <param name="id">文言の識別子</param>
    /// <param name="x">左位置</param>
    /// <param name="y">上位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>追加したボタン</returns>
    private static SettingsButton AddButton(Control parent, MessageId id, int x, int y, int width, int height)
    {
        var button = new SettingsButton();
        button.Text = Messages.Get(id);

        button.SetBounds(x, y, width, height);
        parent.Controls.Add(button);
        return button;
    }

    /// <summary>
    /// 項目名を追加する
    /// </summary>
    /// <param name="parent">配置先</param>
    /// <param name="id">文言の識別子</param>
    /// <param name="x">左位置</param>
    /// <param name="y">上位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>追加したラベル</returns>
    private static Label AddLabel(Control parent, MessageId id, int x, int y, int width, int height)
    {
        var label = new Label();
        label.Text = Messages.Get(id);
        label.SetBounds(x, y, width, height);
        parent.Controls.Add(label);
        return label;
    }

    /// <summary>
    /// テーマ確認とフォントのリソースを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            themeTimer.Dispose();
        }

        base.Dispose(disposing);
        if (disposing)
        {
            sectionFont.Dispose();
            pixelFont.Dispose();
            settingsIcon?.Dispose();
        }
    }
}

