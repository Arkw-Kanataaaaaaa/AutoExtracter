using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// 余白と角丸、フォーカス表示を揃えた設定用ボタン
/// </summary>
internal sealed class SettingsButton : Control, IButtonControl
{
    private bool hovered;
    private bool pressed;

    /// <summary>
    /// 文言の変更時に古い表示を消して描き直す
    /// </summary>
    /// <param name="e">変更情報</param>
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    /// <summary>
    /// 枠線の表示を制御する
    /// </summary>
    internal int BorderSize
    {
        get;
        set;
    } = 1;

    /// <summary>
    /// ボタン内の文字の配置
    /// </summary>
    internal ContentAlignment TextAlign
    {
        get;
        set;
    } = ContentAlignment.MiddleCenter;

    /// <summary>
    /// ダイアログのボタンとして扱う結果
    /// </summary>
    public DialogResult DialogResult
    {
        get;
        set;
    }

    /// <summary>
    /// 既定ボタンの指定に合わせて再描画する
    /// </summary>
    /// <param name="value">既定ボタンか</param>
    public void NotifyDefault(bool value)
    {
        Invalidate();
    }

    /// <summary>
    /// 操作可能な場合にクリックを実行する
    /// </summary>
    public void PerformClick()
    {
        if (Enabled && Visible)
        {
            OnClick(EventArgs.Empty);
        }
    }

    /// <summary>
    /// フォーカス中のEnterでこのボタンを実行する
    /// </summary>
    /// <param name="keyData">入力キーと修飾キー</param>
    /// <returns>入力を処理した場合はtrue</returns>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter && Focused)
        {
            PerformClick();
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    /// <summary>
    /// EnterとSpaceをボタン操作として受け付ける
    /// </summary>
    /// <param name="e">キー入力情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            PerformClick();
        }
    }
    /// <summary>
    /// 描画するテーマの配色
    /// </summary>
    internal ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// 標準の立体描画を使わず独自描画へ切り替える
    /// </summary>
    public SettingsButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        GotFocus += (_, _) =>
        {
            Invalidate();
        };

        LostFocus += (_, _) =>
        {
            pressed = false;
            Invalidate();
        };

        MouseEnter += (_, _) =>
        {
            hovered = true;
            Invalidate();
        };

        MouseLeave += (_, _) =>
        {
            hovered = false;
            pressed = false;
            Invalidate();
        };

        MouseDown += (_, _) =>
        {
            Focus();
            pressed = true;
            Invalidate();
        };

        MouseUp += (_, _) =>
        {
            pressed = false;
            Invalidate();
        };
    }

    /// <summary>
    /// 標準ボタンの枠を描かず、親と同じ背景で角の外側を消去する
    /// </summary>
    /// <param name="e">背景の描画情報</param>
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new SolidBrush(Parent?.BackColor ?? BackColor);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    /// <summary>
    /// 状態に合わせた背景と文字を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 2 || Height < 2)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // 輪郭を半ピクセル内側へ置き、線の端が描画範囲で欠けるのを防ぐ
        using var path = RoundedFrame.CreatePath(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 8);
        using var fill = new SolidBrush(Enabled && pressed ? Palette.Track : Enabled && hovered ? Palette.Hover : BackColor);
        e.Graphics.FillPath(fill, path);
        if (BorderSize > 0)
        {
            using var border = new Pen(Focused && ShowFocusCues ? Palette.Accent : Palette.Track);
            e.Graphics.DrawPath(border, path);
        }

        var bounds = new Rectangle(12, 0, Math.Max(1, Width - 24), Height);
        TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        flags |= TextAlign == ContentAlignment.MiddleLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, Enabled ? ForeColor : Palette.Muted, flags);
    }
}
