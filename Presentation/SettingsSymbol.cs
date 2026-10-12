using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// パネルと設定ウィンドウで同じ歯車記号を使用する
/// </summary>
internal static class SettingsSymbol
{
    /// <summary>
    /// 指定した中心位置へ歯車を描画する
    /// </summary>
    /// <param name="graphics">描画先</param>
    /// <param name="center">中心位置</param>
    /// <param name="color">記号の色</param>
    internal static void Draw(Graphics graphics, PointF center, Color color)
    {
        graphics.SmoothingMode = SmoothingMode.None;
        using var pen = new Pen(color, 2);
        graphics.DrawEllipse(pen, center.X - 8, center.Y - 8, 16, 16);
        graphics.DrawRectangle(pen, center.X - 2, center.Y - 2, 4, 4);
        for (int index = 0; index < 8; index++)
        {
            double angle = index * Math.PI / 4;
            graphics.DrawLine(pen, center.X + (float)Math.Cos(angle) * 8,
                center.Y + (float)Math.Sin(angle) * 8,
                center.X + (float)Math.Cos(angle) * 12,
                center.Y + (float)Math.Sin(angle) * 12);
        }
    }

    /// <summary>
    /// タスクバーとウィンドウ用の歯車アイコンを作成する
    /// </summary>
    /// <param name="color">記号の色</param>
    /// <returns>呼び出し元で解放するアイコン</returns>
    internal static Icon CreateIcon(Color color)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        Draw(graphics, new PointF(16, 16), color);
        return TrayIconRenderer.FromBitmap(bitmap);
    }
}
