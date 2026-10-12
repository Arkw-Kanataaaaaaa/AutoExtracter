using System.Diagnostics;

namespace AutoExtracter;

/// <summary>
/// フォルダをエクスプローラーで開く共通処理
/// </summary>
internal static class FolderLauncher
{
    /// <summary>
    /// 本日のログを開き、未作成の場合は案内を表示する
    /// </summary>
    /// <param name="folder">ログフォルダ</param>
    /// <param name="owner">メッセージの親画面</param>
    public static void OpenTodayLog(string folder, IWin32Window owner)
    {
        string path = FileLogger.GetTodayPath(folder);
        if (File.Exists(path))
        {
            Open(path, owner);
        }
        else
        {
            ThemedMessageDialog.ShowNotice(owner, Messages.Get(MessageId.I342), Messages.Get(MessageId.I310));
        }
    }
    /// <summary>
    /// 指定フォルダを開き、失敗した場合はメッセージを表示する
    /// </summary>
    /// <param name="path">フォルダのパス</param>
    /// <param name="owner">メッセージの親画面</param>
    public static void Open(string path, IWin32Window owner)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ThemedMessageDialog.ShowNotice(owner, Messages.Get(MessageId.E115, path, ex.Message), Messages.Get(MessageId.I310));
        }
    }
}
