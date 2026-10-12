using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// GIFをドットの輪郭を保って描画する
/// </summary>
internal sealed class AnimatedGifView : Control
{
    private readonly List<Bitmap> frames = new();
    private readonly int[] delays;
    private readonly System.Windows.Forms.Timer timer = new();
    private int frame;
    private long nextFrame;
    private bool released;
    private bool animationEnabled = true;

    /// <summary>
    /// GIFのフレーム更新を有効にするか
    /// </summary>
    internal bool AnimationEnabled
    {
        get => animationEnabled;
        set
        {
            if (released || animationEnabled == value)
            {
                return;
            }

            animationEnabled = value;
            frame = 0;
            nextFrame = Environment.TickCount64 + delays[0];
            timer.Enabled = value && Visible && delays.Length > 1;
            Invalidate();
        }
    }

    /// <summary>
    /// 指定GIFまたは同梱GIFを読み込む
    /// </summary>
    /// <param name="path">待機用GIFのパス</param>
    /// <param name="logger">読み込み失敗のログ出力先</param>
    /// <param name="defaultResource">同梱GIFのリソース名</param>
    /// <param name="warning">読み込み失敗のログ識別子</param>
    public AnimatedGifView(string path, FileLogger? logger, string defaultResource = "Idle.gif", MessageId warning = MessageId.W003)
    {
        DoubleBuffered = true;
        GifImage image;
        try
        {
            image = new GifImage(path, defaultResource);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(path))
        {
            logger?.Write(warning, ex, path);
            image = new GifImage(string.Empty, defaultResource);
        }

        using (image)
        {
            delays = image.Delays;
            try
            {
                for (int index = 0; index < delays.Length; index++)
                {
                    frames.Add(image.RenderFrame(index, 256));
                }
            }
            catch
            {
                foreach (Bitmap previous in frames)
                {
                    previous.Dispose();
                }

                timer.Dispose();
                throw;
            }
        }

        nextFrame = Environment.TickCount64 + delays[0];
        timer.Interval = 50;
        timer.Tick += (_, _) =>
        {
            if (!released && animationEnabled && Visible && delays.Length > 1 && Environment.TickCount64 >= nextFrame)
            {
                frame = (frame + 1) % delays.Length;
                nextFrame = Environment.TickCount64 + delays[frame];
                Invalidate();
            }
        };

        VisibleChanged += (_, _) =>
        {
            if (released)
            {
                return;
            }

            timer.Enabled = Visible && animationEnabled && delays.Length > 1;
            nextFrame = Environment.TickCount64 + delays[frame];
        };
    }

    /// <summary>
    /// 現在のGIFフレームを描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (released)
        {
            return;
        }

        Bitmap image = frames[frame];
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        float scale = Math.Min(Width / (float)image.Width, Height / (float)image.Height);
        int width = Math.Max(1, (int)(image.Width * scale));
        int height = Math.Max(1, (int)(image.Height * scale));
        e.Graphics.DrawImage(image, new Rectangle((Width - width) / 2, (Height - height) / 2, width, height));
    }

    /// <summary>
    /// 画像とタイマーを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !released)
        {
            released = true;
            timer.Stop();
            timer.Dispose();
            foreach (Bitmap bitmap in frames)
            {
                bitmap.Dispose();
            }

            frames.Clear();
        }

        base.Dispose(disposing);
    }
}

