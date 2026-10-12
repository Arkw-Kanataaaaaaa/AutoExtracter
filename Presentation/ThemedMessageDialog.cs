namespace AutoExtracter;

/// <summary>
/// 再起動の承認と次回の確認省略を保持する
/// </summary>
/// <param name="Accepted">再起動を承認したか</param>
/// <param name="SkipNextTime">次回の確認を省略するか</param>
internal readonly record struct RestartConfirmationResult(bool Accepted, bool SkipNextTime);

/// <summary>
/// 設定画面と共通の配色とフォントでメッセージを表示する
/// </summary>
internal sealed class ThemedMessageDialog : Form
{
    private readonly PixelFont pixelFont = new();
    private readonly RoundCheckBox skip = new();

    /// <summary>
    /// メッセージ本文と必要な操作を配置する
    /// </summary>
    /// <param name="text">本文</param>
    /// <param name="title">タイトル</param>
    /// <param name="palette">適用するテーマ</param>
    /// <param name="confirmation">再起動確認の操作を表示するか</param>
    internal ThemedMessageDialog(string text, string title, ThemePalette palette, bool confirmation = false)
    {
        Text = title;
        Font = pixelFont.Font;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        int contentWidth = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
        if (confirmation)
        {
            int checkboxWidth = TextRenderer.MeasureText(Messages.Get(MessageId.I403), Font).Width + 28;
            contentWidth = Math.Max(contentWidth, checkboxWidth);
        }

        contentWidth = Math.Clamp(contentWidth, 240, 640);
        int textHeight = TextRenderer.MeasureText(text, Font, new Size(contentWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 6;
        int bodyHeight = Math.Clamp(textHeight, 44, 320);
        int scrollWidth = textHeight > bodyHeight ? SystemInformation.VerticalScrollBarWidth : 0;
        ClientSize = new Size(contentWidth + scrollWidth + 40, bodyHeight + (confirmation ? 148 : 112));
        var heading = new Label();
        heading.Text = title;
        heading.SetBounds(20, 14, ClientSize.Width - 90, 30);
        heading.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowDragHelper.Start(this);
            }
        };

        Controls.Add(heading);
        var body = new Panel();
        body.AutoScroll = true;
        body.SetBounds(20, 54, contentWidth + scrollWidth, bodyHeight);
        var message = new Label();
        message.Text = text;
        message.Name = "MessageText";
        message.SetBounds(0, 0, contentWidth, Math.Max(bodyHeight, textHeight));
        body.Controls.Add(message);
        Controls.Add(body);
        skip.Name = "SkipRestartConfirmationInput";
        skip.Text = Messages.Get(MessageId.I403);
        skip.SetBounds(20, 58 + bodyHeight, contentWidth, 32);
        skip.Visible = confirmation;
        Controls.Add(skip);
        var ok = AddButton(MessageId.I400, "MessageOkButton", ClientSize.Width - (confirmation ? 250 : 130), ClientSize.Height - 52, 110, 36);
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
        };

        AcceptButton = ok;
        var cancel = AddButton(MessageId.I401, "MessageNoButton", ClientSize.Width - 130, ClientSize.Height - 52, 110, 36);
        cancel.Visible = confirmation;
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.No;
        };

        var close = AddButton(MessageId.I352, "CloseMessageButton", ClientSize.Width - 52, 5, 42, 38);
        close.BorderSize = 0;
        close.Click += (_, _) =>
        {
            DialogResult = confirmation ? DialogResult.No : DialogResult.OK;
        };

        CancelButton = confirmation ? cancel : close;
        ThemeControlStyler.Apply(this, palette);
        close.BackColor = palette.Background;
        RoundedFrame.ApplyRegion(this);
    }

    /// <summary>
    /// 再起動確認を表示し、承認した場合だけ省略設定を返す
    /// </summary>
    /// <param name="owner">親ウィンドウ</param>
    /// <param name="palette">適用するテーマ</param>
    /// <returns>承認結果と次回の確認省略</returns>
    public static RestartConfirmationResult ConfirmRestart(IWin32Window owner, ThemePalette palette)
    {
        using var dialog = new ThemedMessageDialog(Messages.Get(MessageId.I402), Messages.Get(MessageId.I334), palette, true);
        bool accepted = dialog.ShowDialog(owner) == DialogResult.OK;
        return new RestartConfirmationResult(accepted, accepted && dialog.skip.Checked);
    }

    /// <summary>
    /// 親画面に合わせたテーマで案内やエラーを表示する
    /// </summary>
    /// <param name="owner">親ウィンドウ、未指定の場合は画面中央に表示する</param>
    /// <param name="text">本文</param>
    /// <param name="title">タイトル</param>
    /// <param name="palette">適用するテーマ、未指定の場合は親画面またはシステムから取得する</param>
    public static void ShowNotice(IWin32Window? owner, string text, string title, ThemePalette? palette = null)
    {
        palette ??= owner is Control control
            ? control.BackColor.GetBrightness() < 0.5f ? ThemePalette.Dark : ThemePalette.Light
            : ThemePalette.Resolve("System");
        using var dialog = new ThemedMessageDialog(text, title, palette);
        if (owner == null)
        {
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.ShowDialog();
        }
        else
        {
            dialog.ShowDialog(owner);
        }
    }

    /// <summary>
    /// 共通のボタンを追加する
    /// </summary>
    /// <param name="id">表示文言</param>
    /// <param name="name">操作の識別名</param>
    /// <param name="x">左位置</param>
    /// <param name="y">上位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>追加したボタン</returns>
    private SettingsButton AddButton(MessageId id, string name, int x, int y, int width, int height)
    {
        var button = new SettingsButton();
        button.Text = Messages.Get(id);
        button.Name = name;
        button.SetBounds(x, y, width, height);
        Controls.Add(button);
        return button;
    }

    /// <summary>
    /// サイズや表示倍率に合わせて角丸形状を更新する
    /// </summary>
    /// <param name="e">変更情報</param>
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RoundedFrame.ApplyRegion(this);
    }

    /// <summary>
    /// 使用したフォントを解放する
    /// </summary>
    /// <param name="disposing">管理リソースを解放するか</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            pixelFont.Dispose();
        }
    }
}
