using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// チェックボックスの選択状態を丸い記号で描画する
/// </summary>
internal sealed class RoundCheckBox : Control
{
    private bool isChecked;

    /// <summary>
    /// 他の選択肢とは独立した選択状態
    /// </summary>
    internal bool Checked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 独自描画とちらつき防止を有効にする
    /// </summary>
    internal RoundCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.CheckButton;
        Cursor = Cursors.Hand;
    }

    /// <summary>
    /// クリックで選択状態を切り替える
    /// </summary>
    /// <param name="e">クリック情報</param>
    protected override void OnClick(EventArgs e)
    {
        Focus();
        Checked = !Checked;
        base.OnClick(e);
    }

    /// <summary>
    /// Spaceキーで選択状態を切り替える
    /// </summary>
    /// <param name="e">キー入力情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <summary>
    /// 前回の文字や選択記号を背景色で消去する
    /// </summary>
    /// <param name="e">背景の描画情報</param>
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    /// <summary>
    /// 文言の変更を画面へ反映する
    /// </summary>
    /// <param name="e">変更情報</param>
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    /// <summary>
    /// フォーカスの取得を描画へ反映する
    /// </summary>
    /// <param name="e">フォーカス情報</param>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <summary>
    /// フォーカス枠を消去する
    /// </summary>
    /// <param name="e">フォーカス情報</param>
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    /// <summary>
    /// 丸い枠と選択状態、文言を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        var colors = BackColor.GetBrightness() < 0.5f ? ThemePalette.Dark : ThemePalette.Light;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int top = (Height - 14) / 2;
        using var outline = new Pen(Checked ? colors.Accent : colors.Muted, 1.5f);
        e.Graphics.DrawEllipse(outline, 2, top, 14, 14);
        if (Checked)
        {
            using var dot = new SolidBrush(colors.Accent);
            e.Graphics.FillEllipse(dot, 5, top + 3, 8, 8);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(20, 0, Width - 20, Height),
            ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(1, 2, Width - 3, Height - 4), ForeColor, BackColor);
        }
    }
}
