namespace AutoExtracter;

/// <summary>
/// パネル内の補足情報をドットフォントで表示する
/// </summary>
internal sealed class PixelToolTip : ToolTip
{
    private readonly PixelFont pixelFont = new();
    /// <summary>
    /// 補足表示の配色
    /// </summary>
    public ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// サイズ計算と描画を独自の外観へ切り替える
    /// </summary>
    public PixelToolTip()
    {
        OwnerDraw = true;
        UseAnimation = false;
        UseFading = false;
        Popup += (_, e) =>
        {
            e.ToolTipSize = PixelTooltipStyle.Measure(GetToolTip(e.AssociatedControl) ?? string.Empty, pixelFont.TooltipFont);
        };

        Draw += (_, e) =>
        {
            Rectangle bounds = RoundedFrame.ApplyTooltipRegion(e.Graphics, e.Bounds);
            PixelTooltipStyle.Draw(e.Graphics, bounds, e.ToolTipText ?? string.Empty, pixelFont.TooltipFont, Palette);
        };
    }

    /// <summary>
    /// フォントとツールチップのリソースを解放する
    /// </summary>
    /// <param name="disposing">管理リソースも解放するか</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            pixelFont.Dispose();
        }
    }
}

