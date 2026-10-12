using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// ウィンドウと補助表示の角丸形状と枠線を共有する
/// </summary>
internal static class RoundedFrame
{
    /// <summary>
    /// 角丸の輪郭を生成する
    /// </summary>
    /// <param name="bounds">輪郭の範囲</param>
    /// <param name="radius">角の半径</param>
    /// <returns>呼び出し元で解放する輪郭</returns>
    internal static GraphicsPath CreatePath(RectangleF bounds, float radius = 12)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// コントロールの外側の角を切り取る
    /// </summary>
    /// <param name="control">形状を適用するコントロール</param>
    internal static void ApplyRegion(Control control)
    {
        if (control.Width <= 0 || control.Height <= 0)
        {
            return;
        }

        using var path = CreatePath(new RectangleF(0, 0, control.Width, control.Height));
        Region? previous = control.Region;
        control.Region = new Region(path);
        previous?.Dispose();
    }

    /// <summary>
    /// 角丸の枠線を内側へ描画する
    /// </summary>
    /// <param name="graphics">描画先</param>
    /// <param name="bounds">描画範囲</param>
    /// <param name="color">枠線の色</param>
    /// <param name="width">枠線の太さ</param>
    internal static void Draw(Graphics graphics, Rectangle bounds, Color color, float width = 3)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || width <= 0)
        {
            return;
        }

        var state = graphics.Save();
        try
        {
            // 外側の輪郭を切り取り領域と一致させ、背景色の縁を残さない
            graphics.SmoothingMode = SmoothingMode.None;
            using var path = CreatePath(bounds);
            using var inner = CreatePath(new RectangleF(bounds.X + width, bounds.Y + width,
                bounds.Width - width * 2, bounds.Height - width * 2), Math.Max(1, 12 - width));
            path.FillMode = FillMode.Alternate;
            if (inner.PointCount > 0)
            {
                path.AddPath(inner, false);
            }

            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, path);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    /// <summary>
    /// ツールチップの全体範囲を取得し、四角い形状へ戻す
    /// </summary>
    /// <param name="graphics">ツールチップの描画先</param>
    /// <param name="bounds">描画通知の範囲</param>
    /// <returns>部分的な再描画に影響されないウィンドウ全体の範囲</returns>
    internal static Rectangle ApplyTooltipRegion(Graphics graphics, Rectangle bounds)
    {
        IntPtr dc = graphics.GetHdc();
        IntPtr window;
        try
        {
            window = WindowFromDC(dc);
        }
        finally
        {
            graphics.ReleaseHdc(dc);
        }

        if (window == IntPtr.Zero)
        {
            return bounds;
        }
        // 描画通知の範囲ではなく実際のクライアント領域を基準にする
        if (!GetClientRect(window, out NativeRectangle client))
        {
            return bounds;
        }

        bounds = Rectangle.FromLTRB(client.Left, client.Top, client.Right, client.Bottom);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bounds;
        }

        SetWindowRgn(window, IntPtr.Zero, false);
        return bounds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromDC(IntPtr dc);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr window, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

}
