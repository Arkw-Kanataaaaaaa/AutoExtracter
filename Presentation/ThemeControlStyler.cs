namespace AutoExtracter;

/// <summary>
/// パネルと設定ダイアログのコントロールへ共通のテーマを反映する
/// </summary>
internal static class ThemeControlStyler
{
    /// <summary>
    /// 入力欄とボタンを含めて配色を再帰的に反映する
    /// </summary>
    /// <param name="control">対象コントロール</param>
    /// <param name="palette">テーマの配色</param>
    /// <param name="backgroundSelector">役割ごとの背景色、未指定の場合は入力欄を強調する</param>
    /// <param name="inheritedBackground">親の背景色</param>
    public static void Apply(Control control, ThemePalette palette,
        Func<Control, Color, Color>? backgroundSelector = null, Color? inheritedBackground = null)
    {
        Color background = inheritedBackground ?? palette.Background;
        if (control is TextBox or ListBox or Button or SettingsButton or SettingsInputFrame)
        {
            background = palette.WorkSurface;
        }

        if (backgroundSelector != null)
        {
            background = backgroundSelector(control, background);
        }

        control.BackColor = background;
        control.ForeColor = palette.Text;
        if (control is ThemeColorPicker themePicker)
        {
            themePicker.Palette = palette;
            themePicker.Invalidate();
        }

        if (control is SettingsButton settingsButton)
        {
            settingsButton.Palette = palette;
            settingsButton.Invalidate();
        }

        if (control is SettingsInputFrame inputFrame)
        {
            inputFrame.Palette = palette;
            inputFrame.Invalidate();
        }

        if (control is Button button)
        {
            button.FlatAppearance.BorderColor = palette.Border;
            button.FlatAppearance.MouseOverBackColor = palette.Surface;
            button.FlatAppearance.MouseDownBackColor = palette.Background;
        }

        foreach (Control child in control.Controls)
        {
            Apply(child, palette, backgroundSelector, background);
        }
    }
}
