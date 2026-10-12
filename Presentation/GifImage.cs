using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AutoExtracter;

/// <summary>
/// GIFの読み込み、検証、フレーム描画を共有する
/// </summary>
internal sealed class GifImage : IDisposable
{
    private readonly MemoryStream stream;
    private readonly Image image;
    private Rectangle? contentBounds;

    /// <summary>
    /// 各フレームの表示時間、単位はミリ秒
    /// </summary>
    internal int[] Delays
    {
        get;
    }

    /// <summary>
    /// 指定ファイルまたは同梱GIFを検証して読み込む
    /// </summary>
    /// <param name="path">GIFファイルのパス</param>
    /// <param name="resource">パスが空の場合に使用する同梱リソース</param>
    internal GifImage(string path, string? resource = null)
    {
        byte[] bytes;
        if (string.IsNullOrWhiteSpace(path) && resource != null)
        {
            using var embedded = typeof(GifImage).Assembly.GetManifestResourceStream(
                "AutoExtracter.Resources.Images." + resource)!;
            using var buffer = new MemoryStream();
            embedded.CopyTo(buffer);
            bytes = buffer.ToArray();
        }
        else
        {
            if (new FileInfo(path).Length > 10 * 1024 * 1024)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E116));
            }

            bytes = File.ReadAllBytes(path);
        }

        stream = new MemoryStream(bytes);
        try
        {
            image = Image.FromStream(stream);
            if (bytes.Length > 10 * 1024 * 1024 || image.RawFormat.Guid != ImageFormat.Gif.Guid ||
                image.Width > 2048 || image.Height > 2048)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E116));
            }

            int count = image.GetFrameCount(FrameDimension.Time);
            if (count > 256)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E116));
            }

            byte[]? timing = image.PropertyIdList.Contains(0x5100)
                ? image.GetPropertyItem(0x5100)?.Value : null;
            Delays = new int[count];
            for (int index = 0; index < count; index++)
            {
                int delay = timing != null && timing.Length >= (index + 1) * 4
                    ? BitConverter.ToInt32(timing, index * 4) : 10;
                Delays[index] = (int)Math.Clamp((long)delay * 10, 50, 60000);
            }
        }
        catch
        {
            image?.Dispose();
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 共通の透明余白を除いて指定フレームを描画する
    /// </summary>
    /// <param name="index">フレーム位置</param>
    /// <param name="maximumSize">表示範囲の最大辺長</param>
    /// <param name="square">正方形の中央へ配置するか</param>
    /// <returns>呼び出し元で解放するフレーム画像</returns>
    internal Bitmap RenderFrame(int index, int maximumSize, bool square = false)
    {
        Rectangle content = contentBounds ??= GifContentBounds.Find(image, Delays.Length);
        image.SelectActiveFrame(FrameDimension.Time, index);
        int longest = Math.Max(content.Width, content.Height);
        int size = square ? maximumSize : Math.Min(longest, maximumSize);
        int width = Math.Max(1, content.Width * size / longest);
        int height = Math.Max(1, content.Height * size / longest);
        var bitmap = new Bitmap(square ? size : width, square ? size : height);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(image, new Rectangle((bitmap.Width - width) / 2,
                (bitmap.Height - height) / 2, width, height), content, GraphicsUnit.Pixel);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 元画像と読み込みストリームを解放する
    /// </summary>
    public void Dispose()
    {
        image.Dispose();
        stream.Dispose();
    }
}
