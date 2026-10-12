using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// 解凍率を円形ゲージと数値で示す通知領域アイコンを描画する
/// </summary>
internal static class TrayIconRenderer
{
    /// <summary>
    /// 現在の進捗に対応したアイコンを作成する
    /// </summary>
    /// <param name="state">描画する処理状況</param>
    /// <returns>呼び出し元で解放するアイコン</returns>
    public static Icon Create(ProgressSnapshot state)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        bool active = state.Phase == MessageId.I302 || state.Phase == MessageId.I303;
        Color color = active ? Color.DeepSkyBlue : Color.LimeGreen;
        if (state.Phase == MessageId.I304)
        {
            color = Color.OrangeRed;
        }

        using var track = new Pen(Color.DarkSlateGray, 4);
        using var gauge = new Pen(color, 4);
        graphics.DrawEllipse(track, 3, 3, 26, 26);
        float sweep = active && state.Percent.HasValue ? state.Percent.Value * 3.6f : 360;
        graphics.DrawArc(gauge, 3, 3, 26, 26, -90, Math.Max(1, sweep));
        if (active && state.Percent.HasValue)
        {
            using var font = new Font("Segoe UI", 8, FontStyle.Bold);
            using var brush = new SolidBrush(color);
            using var format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            graphics.DrawString(state.Percent.Value.ToString(), font, brush, new RectangleF(0, 0, 32, 32), format);
        }

        return FromBitmap(bitmap);
    }

    /// <summary>
    /// ビットマップを独立したアイコンに変換する
    /// </summary>
    /// <param name="bitmap">変換元の画像</param>
    /// <returns>呼び出し元で解放するアイコン</returns>
    internal static Icon FromBitmap(Bitmap bitmap)
    {
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>
    /// ビットマップから作成したネイティブアイコンを解放する
    /// </summary>
    /// <param name="handle">解放するアイコンのハンドル</param>
    /// <returns>解放に成功した場合はtrue</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
