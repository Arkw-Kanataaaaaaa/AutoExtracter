namespace AutoExtracter;

/// <summary>
/// 選択欄と同じ文字描画で行内の項目名を表示する
/// </summary>
internal sealed class SettingsRowLabel : Control
{
    /// <summary>
    /// 独自描画とちらつき防止を有効にする
    /// </summary>
    internal SettingsRowLabel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer, true);
    }

    /// <summary>
    /// 選択欄と共通の方法で文字を縦中央へ描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        SettingsRowText.Draw(e.Graphics, Text, Font, ClientRectangle, ForeColor);
    }
}

/// <summary>
/// 同じ行の項目名と選択値の文字位置を揃える
/// </summary>
internal static class SettingsRowText
{
    /// <summary>
    /// 一行の文字を標準ラベルと同じ余白で縦中央へ描画する
    /// </summary>
    /// <param name="graphics">描画先</param>
    /// <param name="text">表示する文字</param>
    /// <param name="font">表示フォント</param>
    /// <param name="bounds">文字の配置範囲</param>
    /// <param name="color">文字色</param>
    internal static void Draw(Graphics graphics, string text, Font font, Rectangle bounds, Color color)
    {
        TextRenderer.DrawText(graphics, text, font, bounds, color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix);
    }
}
