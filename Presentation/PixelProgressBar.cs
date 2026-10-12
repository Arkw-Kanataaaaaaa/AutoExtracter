using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// ブロック単位で進捗を描画するドット絵風ゲージ
/// </summary>
internal sealed class PixelProgressBar : Control
{
    private int value;
    private ProgressBarStyle style;
    private int step;
    private readonly System.Windows.Forms.Timer timer = new();
    /// <summary>
    /// 進捗表示の配色
    /// </summary>
    public ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// 不定進捗用の描画タイマーを初期化する
    /// </summary>
    public PixelProgressBar()
    {
        DoubleBuffered = true;
        timer.Interval = 140;
        VisibleChanged += (_, _) =>
        {
            timer.Enabled = Visible && style == ProgressBarStyle.Marquee;
        };

        timer.Tick += (_, _) =>
        {
            step = (step + 1) % 20;
            Invalidate();
        };
    }

    /// <summary>
    /// 表示する進捗率
    /// </summary>
    public int Value
    {
        get => value;
        set
        {
            this.value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    /// <summary>
    /// 確定進捗と不定進捗の表示方式
    /// </summary>
    public ProgressBarStyle Style
    {
        get => style;
        set
        {
            style = value;
            timer.Enabled = Visible && value == ProgressBarStyle.Marquee;
            Invalidate();
        }
    }

    /// <summary>
    /// 四角いブロックで進捗を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using var track = new SolidBrush(Palette.Track);
        using var fill = new SolidBrush(Palette.Accent);
        int filled = (int)Math.Ceiling(value / 5.0);
        for (int i = 0; i < 20; i++)
        {
            bool active = style == ProgressBarStyle.Marquee
                ? (i - step + 20) % 20 < 4 : i < filled;
            int left = i * Width / 20;
            int right = (i + 1) * Width / 20;
            e.Graphics.FillRectangle(active ? fill : track, left, 0, Math.Max(1, right - left - 2), Height);
        }
    }

    /// <summary>
    /// 描画タイマーを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
        }

        base.Dispose(disposing);
    }
}

