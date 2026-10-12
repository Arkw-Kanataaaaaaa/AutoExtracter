using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// 電源と設定の記号を描画する操作ボタン
/// </summary>
internal sealed class PixelIconButton : Button
{
    private readonly bool power;

    /// <summary>
    /// 記号の種類を指定してボタンを作成する
    /// </summary>
    /// <param name="power">電源記号の場合はtrue、歯車の場合はfalse</param>
    public PixelIconButton(bool power)
    {
        this.power = power;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(20, 28, 44);
        ForeColor = Color.FromArgb(216, 236, 220);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    /// <summary>
    /// 電源または歯車の記号を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        float x = Width / 2f;
        float y = Height / 2f;
        using var pen = new Pen(Enabled ? ForeColor : Color.Gray, 2);
        if (power)
        {
            e.Graphics.DrawArc(pen, x - 9, y - 8, 18, 18, -50, 280);
            e.Graphics.DrawLine(pen, x, y - 12, x, y);
        }
        else
        {
            SettingsSymbol.Draw(e.Graphics, new PointF(x, y), Enabled ? ForeColor : Color.Gray);
        }
    }
}
