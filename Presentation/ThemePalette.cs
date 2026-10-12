using Microsoft.Win32;

namespace AutoExtracter;

/// <summary>
/// 通知パネルと補助表示のテーマ配色を共有する
/// </summary>
internal sealed class ThemePalette
{
    /// <summary>
    /// 現在のダークテーマ
    /// </summary>
    public static ThemePalette Dark
    {
        get;
    } = new(false);

    /// <summary>
    /// 白を基調としたライトテーマ
    /// </summary>
    public static ThemePalette Light
    {
        get;
    } = new(true);

    /// <summary>
    /// 背景色
    /// </summary>
    public Color Background
    {
        get;
    }

    /// <summary>
    /// 一覧と待機領域の背景色
    /// </summary>
    public Color Surface
    {
        get;
    }

    /// <summary>
    /// 作業領域と選択項目の背景色
    /// </summary>
    public Color WorkSurface
    {
        get;
    }

    /// <summary>
    /// 通常の文字色
    /// </summary>
    public Color Text
    {
        get;
    }

    /// <summary>
    /// 容量と無効項目の文字色
    /// </summary>
    public Color Muted
    {
        get;
    }

    /// <summary>
    /// 枠線の色
    /// </summary>
    public Color Border
    {
        get;
    }

    /// <summary>
    /// 進捗と選択表示の色
    /// </summary>
    public Color Accent
    {
        get;
    }

    /// <summary>
    /// 進捗の未完了部分と区切り線の色
    /// </summary>
    public Color Track
    {
        get;
    }

    /// <summary>
    /// 状態表示の文字色
    /// </summary>
    public Color Status
    {
        get;
    }

    /// <summary>
    /// ボタンへポインターを重ねたときの背景色
    /// </summary>
    public Color Hover
    {
        get;
    }

    /// <summary>
    /// 明暗に応じた配色を生成する
    /// </summary>
    /// <param name="light">ライトテーマか</param>
    private ThemePalette(bool light)
    {
        // Windows設定画面と同じく全体の背景と内容領域を明暗で区別する
        Background = light ? Color.White : Color.FromArgb(32, 32, 32);
        Surface = light ? Color.FromArgb(230, 230, 230) : Color.FromArgb(43, 43, 43);
        WorkSurface = light ? Color.FromArgb(245, 245, 245) : Color.FromArgb(58, 58, 58);
        Hover = light ? Color.FromArgb(220, 220, 220) : Color.FromArgb(78, 78, 78);
        Text = light ? Color.FromArgb(24, 45, 58) : Color.FromArgb(216, 236, 220);
        Muted = light ? Color.FromArgb(82, 102, 116) : Color.Silver;
        Border = light ? Color.Black : Color.White;
        Accent = light ? Color.FromArgb(16, 145, 103) : Color.FromArgb(116, 232, 180);
        Track = light ? Color.FromArgb(170, 170, 170) : Color.FromArgb(92, 92, 92);
        Status = light ? Color.FromArgb(20, 115, 65) : Color.FromArgb(120, 210, 140);
    }

    /// <summary>
    /// 設定とWindowsのアプリテーマから配色を選択する
    /// </summary>
    /// <param name="appearance">Light、Dark、Systemのいずれか</param>
    /// <param name="systemIsLight">システムテーマ判定、未指定の場合はWindows設定を参照する</param>
    /// <returns>適用する配色</returns>
    public static ThemePalette Resolve(string appearance, Func<bool>? systemIsLight = null)
    {
        bool light = appearance == "Light" ||
            appearance == "System" && (systemIsLight ?? ReadSystemIsLight)();
        return light ? Light : Dark;
    }

    /// <summary>
    /// Windowsのアプリ用テーマを参照する
    /// </summary>
    /// <returns>ライトテーマの場合はtrue、読み取り不可の場合はfalse</returns>
    private static bool ReadSystemIsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value != 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}


