using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// デスクトップとログイン時起動のショートカットを管理する
/// </summary>
internal sealed class ApplicationShortcuts
{
    private readonly string executable;
    private readonly string desktop;
    private readonly string startup;

    /// <summary>
    /// 実行ファイルとショートカットの保存先を保持する
    /// </summary>
    /// <param name="executable">起動する実行ファイル</param>
    /// <param name="desktop">デスクトップの保存先、未指定の場合は現在のユーザーのフォルダ</param>
    /// <param name="startup">スタートアップの保存先、未指定の場合は現在のユーザーのフォルダ</param>
    public ApplicationShortcuts(string executable, string? desktop = null, string? startup = null)
    {
        this.executable = Path.GetFullPath(executable);
        this.desktop = desktop ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        this.startup = startup ?? Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    }

    /// <summary>
    /// 自動起動用のショートカットが存在するか
    /// </summary>
    public bool StartupEnabled => File.Exists(Path.Combine(startup, "AutoExtracter.lnk"));

    /// <summary>
    /// デスクトップにアプリのショートカットを作成する
    /// </summary>
    public void CreateDesktop()
    {
        Create(desktop);
    }

    /// <summary>
    /// 自動起動用ショートカットを作成または削除する
    /// </summary>
    /// <param name="enabled">ログイン時に起動するか</param>
    public void SetStartup(bool enabled)
    {
        if (enabled)
        {
            Create(startup);
        }
        else
        {
            File.Delete(Path.Combine(startup, "AutoExtracter.lnk"));
        }
    }

    /// <summary>
    /// 実行ファイルと作業フォルダを指定したショートカットを保存する
    /// </summary>
    /// <param name="folder">保存先のフォルダ</param>
    private void Create(string folder)
    {
        Directory.CreateDirectory(folder);
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", true)!);
            dynamic automation = shell!;
            shortcut = automation.CreateShortcut(Path.Combine(folder, "AutoExtracter.lnk"));
            dynamic link = shortcut;
            link.TargetPath = executable;
            link.WorkingDirectory = Path.GetDirectoryName(executable)!;
            link.IconLocation = executable + ",0";
            link.Save();
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            if (shell != null && Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
