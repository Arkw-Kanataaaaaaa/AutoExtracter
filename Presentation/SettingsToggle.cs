using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// 丸いノブでオンとオフを切り替える
/// </summary>
internal sealed class SettingsToggle : Control
{
    private bool isChecked;
    private readonly System.Windows.Forms.Timer slideTimer = new();
    private float position;
    private float startPosition;
    private long startedAt;

    /// <summary>
    /// オンの場合はtrue
    /// </summary>
    internal bool Checked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            slideTimer.Stop();
            position = value ? 1 : 0;
            Invalidate();
        }
    }

    /// <summary>
    /// 独自描画とキー操作を有効にする
    /// </summary>
    internal SettingsToggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        slideTimer.Interval = 16;
        slideTimer.Tick += (_, _) =>
        {
            float elapsed = Math.Clamp((Environment.TickCount64 - startedAt) / 180f, 0, 1);
            float eased = elapsed * elapsed * (3 - 2 * elapsed);
            position = startPosition + ((isChecked ? 1 : 0) - startPosition) * eased;
            if (elapsed >= 1)
            {
                slideTimer.Stop();
            }

            Invalidate();
        };

        VisibleChanged += (_, _) =>
        {
            if (!Visible)
            {
                slideTimer.Stop();
                position = isChecked ? 1 : 0;
            }
        };
    }

    /// <summary>
    /// クリックで状態を切り替える
    /// </summary>
    /// <param name="e">クリック情報</param>
    protected override void OnClick(EventArgs e)
    {
        Focus();
        Slide();
        base.OnClick(e);
    }

    /// <summary>
    /// Spaceで状態を切り替える
    /// </summary>
    /// <param name="e">キー入力情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <summary>
    /// 現在位置から変更後の位置へノブを滑らかに移動する
    /// </summary>
    private void Slide()
    {
        startPosition = position;
        isChecked = !isChecked;
        startedAt = Environment.TickCount64;
        slideTimer.Start();
        Invalidate();
    }

    /// <summary>
    /// 丸い背景と左右に移動するノブを描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new RectangleF(2, (Height - 22) / 2f, 44, 22);
        using var path = RoundedFrame.CreatePath(bounds, 11);
        ThemePalette palette = BackColor.GetBrightness() < 0.5f ? ThemePalette.Dark : ThemePalette.Light;
        using var fill = new SolidBrush(Checked ? palette.Accent : palette.Track);
        e.Graphics.FillPath(fill, path);
        using var knob = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(knob, 5 + position * 24, bounds.Top + 3, 16, 16);
        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle, ForeColor, BackColor);
        }
    }

    /// <summary>
    /// スライド用タイマーを解放する
    /// </summary>
    /// <param name="disposing">管理リソースを解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            slideTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
