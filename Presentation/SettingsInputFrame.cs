namespace AutoExtracter;

/// <summary>
/// 入力欄に余白とフォーカス時の下線を付ける
/// </summary>
internal sealed class SettingsInputFrame : Panel
{
    private bool invalid;

    /// <summary>
    /// 入力エラーの場合は赤い下線を表示する
    /// </summary>
    internal bool IsInvalid
    {
        get => invalid;
        set
        {
            if (invalid != value)
            {
                invalid = value;
                Invalidate();
                Update();
            }
        }
    }
    /// <summary>
    /// 入力欄の下線に使用する配色
    /// </summary>
    internal ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// 標準の立体枠を外し入力欄を内側へ配置する
    /// </summary>
    /// <param name="input">配置する入力欄</param>
    public SettingsInputFrame(Control input)
    {
        DoubleBuffered = true;
        if (input is TextBox text)
        {
            text.BorderStyle = BorderStyle.None;
        }

        Controls.Add(input);
        input.GotFocus += (_, _) =>
        {
            Invalidate();
        };

        input.LostFocus += (_, _) =>
        {
            Invalidate();
        };

        Resize += (_, _) =>
        {
            input.SetBounds(10, 8, Math.Max(1, Width - 20), Math.Max(1, Height - 16));
        };
    }

    /// <summary>
    /// 下線で入力欄とフォーカスを表す
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Color lineColor = IsInvalid ? Color.FromArgb(220, 60, 60) : ContainsFocus ? Palette.Accent : Palette.Track;
        using var pen = new Pen(lineColor, IsInvalid || ContainsFocus ? 2 : 1);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
    }
}
