namespace AutoExtracter;

/// <summary>
/// 通知パネルと共通の配色でツールチップを描画する
/// </summary>
internal static class PixelTooltipStyle
{
    /// <summary>
    /// 折り返しと余白を含めた表示サイズを求める
    /// </summary>
    /// <param name="text">表示文言</param>
    /// <param name="font">表示フォント</param>
    /// <returns>表示サイズ</returns>
    public static Size Measure(string text, Font font)
    {
        Size size = TextRenderer.MeasureText(text, font, new Size(396, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        return new Size(size.Width + 16, size.Height + 12);
    }

    /// <summary>
    /// 四角い背景と文字を描画する
    /// </summary>
    /// <param name="graphics">描画先</param>
    /// <param name="bounds">描画範囲</param>
    /// <param name="text">表示文言</param>
    /// <param name="font">表示フォント</param>
    /// <param name="palette">適用する配色、未指定の場合はダーク</param>
    public static void Draw(Graphics graphics, Rectangle bounds, string text, Font font, ThemePalette? palette = null)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        palette ??= ThemePalette.Dark;
        using var background = new SolidBrush(palette.Background);
        graphics.FillRectangle(background, bounds);
        var content = new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, bounds.Height - 12);
        if (content.Width <= 0 || content.Height <= 0)
        {
            return;
        }

        TextRenderer.DrawText(graphics, text, font, content, palette.Text,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
    }
}

