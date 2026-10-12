using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// 解凍待ちファイルを処理順で表示する一覧
/// </summary>
internal sealed class ArchiveQueueList : ListBox
{
    private bool hidingScrollBars;
    /// <summary>
    /// 容量と区切り線の配色
    /// </summary>
    public ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// スクロールバーのないウィンドウを作成する
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.Style &= ~0x00300000;
            return parameters;
        }
    }

    /// <summary>
    /// 項目更新やサイズ変更後もスクロールバーを隠す
    /// </summary>
    /// <param name="message">ウィンドウメッセージ</param>
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (!hidingScrollBars && IsHandleCreated &&
            message.Msg is 0x0005 or 0x000F or 0x0018 or 0x0114 or 0x0115 or 0x020A or
                0x0180 or 0x0181 or 0x0182 or 0x0184)
        {
            hidingScrollBars = true;
            try
            {
                ShowScrollBar(Handle, 3, false);
            }
            finally
            {
                hidingScrollBars = false;
            }
        }
    }

    /// <summary>
    /// ホイールで一覧の表示位置を変更する
    /// </summary>
    /// <param name="e">ホイール操作</param>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Items.Count > 0)
        {
            int rows = Math.Max(1, ClientSize.Height / ItemHeight);
            int direction = Math.Sign(e.Delta);
            TopIndex = Math.Clamp(TopIndex - direction, 0, Math.Max(0, Items.Count - rows));
        }

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
    }

    /// <summary>
    /// ネイティブのスクロールバー表示を切り替える
    /// </summary>
    /// <param name="window">対象ウィンドウ</param>
    /// <param name="bar">対象のスクロールバー</param>
    /// <param name="show">表示するか</param>
    /// <returns>処理に成功した場合はtrue</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowScrollBar(IntPtr window, int bar, [MarshalAs(UnmanagedType.Bool)] bool show);

    /// <summary>
    /// ドットフォントでファイル名と容量を描画する一覧を初期化する
    /// </summary>
    public ArchiveQueueList()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 62;
        IntegralHeight = false;
        BorderStyle = BorderStyle.None;
        BackColor = Color.FromArgb(28, 40, 56);
        ForeColor = Color.FromArgb(216, 236, 220);
    }

    /// <summary>
    /// 一覧を更新し、同じ内容の場合はスクロール位置を保持する
    /// </summary>
    /// <param name="archives">解凍予定の圧縮ファイル</param>
    public void UpdateArchives(IReadOnlyList<QueuedArchive> archives)
    {
        if (Items.Cast<QueuedArchive>().SequenceEqual(archives))
        {
            return;
        }

        int top = TopIndex;
        BeginUpdate();
        try
        {
            Items.Clear();
            foreach (var archive in archives)
            {
                Items.Add(archive);
            }

            if (Items.Count > 0)
            {
                TopIndex = Math.Min(top, Items.Count - 1);
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    /// <summary>
    /// 一行分のファイル名と容量を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        var archive = (QueuedArchive)Items[e.Index];
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(e.Graphics, archive.Name, Font,
            new Rectangle(e.Bounds.X, e.Bounds.Y + 2, e.Bounds.Width - 6, 28), ForeColor, flags);
        TextRenderer.DrawText(e.Graphics, ProgressFormatter.Size(archive.Bytes), Font,
            new Rectangle(e.Bounds.X, e.Bounds.Y + 31, e.Bounds.Width - 6, 28), Palette.Muted, flags);
        using var line = new Pen(Palette.Track);
        e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }
}

