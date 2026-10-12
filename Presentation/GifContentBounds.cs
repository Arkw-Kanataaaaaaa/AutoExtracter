using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// GIFの全フレームに共通する透明余白を計算する
/// </summary>
internal static class GifContentBounds
{
    /// <summary>
    /// 不透明な画素を含む全フレームの共通表示範囲を取得する
    /// </summary>
    /// <param name="image">GIF画像</param>
    /// <param name="frameCount">フレーム数</param>
    /// <returns>全透明の場合は画像全体、それ以外は全フレームの画素を含む矩形</returns>
    public static Rectangle Find(Image image, int frameCount)
    {
        Rectangle bounds = Rectangle.Empty;
        for (int frame = 0; frame < frameCount; frame++)
        {
            image.SelectActiveFrame(FrameDimension.Time, frame);
            using var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImageUnscaled(image, 0, 0);
            }

            Rectangle current = FindOpaquePixels(bitmap);
            if (!current.IsEmpty)
            {
                bounds = bounds.IsEmpty ? current : Rectangle.Union(bounds, current);
            }
        }

        return bounds.IsEmpty ? new Rectangle(0, 0, image.Width, image.Height) : bounds;
    }

    /// <summary>
    /// アルファ値がゼロではない画素の範囲を計算する
    /// </summary>
    /// <param name="bitmap">32ビットARGB形式の画像</param>
    /// <returns>画素を含む矩形、全透明の場合は空の矩形</returns>
    internal static Rectangle FindOpaquePixels(Bitmap bitmap)
    {
        int left = bitmap.Width;
        int top = bitmap.Height;
        int right = -1;
        int bottom = -1;
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[bitmap.Width * 4];
            for (int y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (row[x * 4 + 3] == 0)
                    {
                        continue;
                    }

                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
