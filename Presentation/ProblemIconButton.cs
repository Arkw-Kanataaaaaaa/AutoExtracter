using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// 問題マークと赤い件数バッジを表示する
/// </summary>
internal sealed class ProblemIconButton : Button
{
    private long count;

    /// <summary>
    /// 枠のない操作ボタンを初期化する
    /// </summary>
    public ProblemIconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    /// <summary>
    /// バッジに表示する件数
    /// </summary>
    public long Count
    {
        get => count;
        set
        {
            count = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 三角の問題マークと数字入りの丸いバッジを描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using var outline = new Pen(ForeColor, 2);
        e.Graphics.DrawPolygon(outline, new[]
        {
            new Point(3, 36), new Point(23, 36), new Point(13, 16)
        });

        e.Graphics.DrawLine(outline, 13, 23, 13, 28);
        using var dot = new SolidBrush(ForeColor);
        e.Graphics.FillRectangle(dot, 12, 31, 2, 2);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var badge = count > 99 ? new Rectangle(16, 0, 28, 28) : new Rectangle(18, 0, 24, 24);
        using var red = new SolidBrush(Color.FromArgb(206, 42, 57));
        e.Graphics.FillEllipse(red, badge);
        string number = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Size measured = TextRenderer.MeasureText(number, Font, Size.Empty, TextFormatFlags.NoPadding);
        float scale = Math.Min(1f, (badge.Width - 4) / (float)Math.Max(1, measured.Width));
        using var numberFont = new Font(Font.FontFamily, Font.Size * scale, Font.Style, Font.Unit);
        TextRenderer.DrawText(e.Graphics, number, numberFont, badge, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
