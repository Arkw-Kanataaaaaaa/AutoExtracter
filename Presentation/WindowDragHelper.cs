using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// 枠なしの設定ウィンドウを見出しで移動する
/// </summary>
internal static class WindowDragHelper
{
    /// <summary>
    /// ウィンドウの移動を開始する
    /// </summary>
    /// <param name="window">移動するウィンドウ</param>
    public static void Start(Form window)
    {
        ReleaseCapture();
        SendMessage(window.Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
    }

    /// <summary>
    /// マウスキャプチャを解放する
    /// </summary>
    /// <returns>成功した場合はtrue</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    /// <summary>
    /// ウィンドウへ移動操作を送る
    /// </summary>
    /// <param name="window">ウィンドウのハンドル</param>
    /// <param name="message">メッセージ</param>
    /// <param name="wParam">操作種別</param>
    /// <param name="lParam">追加情報</param>
    /// <returns>処理結果</returns>
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
