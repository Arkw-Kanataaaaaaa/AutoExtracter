using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// 同梱フォントをアプリ内で読み込み、システムへのインストールなしで使用する
/// </summary>
internal sealed class PixelFont : IDisposable
{
    private readonly PrivateFontCollection collection = new();
    private IntPtr memory;
    private IntPtr registration;

    /// <summary>
    /// 日本語ドットフォントをGDIとGDI+へ登録する
    /// </summary>
    public PixelFont()
    {
        try
        {
            using var stream = typeof(PixelFont).Assembly.GetManifestResourceStream(
                "AutoExtracter.Resources.Fonts.DotGothic16-Regular.ttf")!;
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            byte[] bytes = buffer.ToArray();
            memory = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, memory, bytes.Length);
            collection.AddMemoryFont(memory, bytes.Length);
            uint count = 0;
            registration = AddFontMemResourceEx(memory, (uint)bytes.Length, IntPtr.Zero, ref count);
            Font = new Font(collection.Families[0], 12, FontStyle.Regular, GraphicsUnit.Point);
            ButtonFont = new Font(collection.Families[0], 9, FontStyle.Regular, GraphicsUnit.Point);
            TooltipFont = new Font(collection.Families[0], 10, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// パネルに使用するフォント
    /// </summary>
    public Font Font
    {
        get;
        private set;
    } = null!;

    /// <summary>
    /// 操作ボタンに使用する小さいフォント
    /// </summary>
    public Font ButtonFont
    {
        get;
        private set;
    } = null!;

    /// <summary>
    /// ツールチップに使用するフォント
    /// </summary>
    public Font TooltipFont
    {
        get;
        private set;
    } = null!;

    /// <summary>
    /// フォントの登録とメモリを解放する
    /// </summary>
    public void Dispose()
    {
        Font?.Dispose();
        ButtonFont?.Dispose();
        TooltipFont?.Dispose();
        collection.Dispose();
        if (registration != IntPtr.Zero)
        {
            RemoveFontMemResourceEx(registration);
            registration = IntPtr.Zero;
        }

        if (memory != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(memory);
            memory = IntPtr.Zero;
        }
    }

    /// <summary>
    /// メモリ上のフォントをプロセス内に登録する
    /// </summary>
    /// <param name="data">フォントデータ</param>
    /// <param name="size">データの長さ</param>
    /// <param name="reserved">予約領域</param>
    /// <param name="count">登録したフォント数</param>
    /// <returns>登録を解除するためのハンドル</returns>
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr data, uint size, IntPtr reserved, ref uint count);

    /// <summary>
    /// プロセス内のフォント登録を解除する
    /// </summary>
    /// <param name="handle">登録ハンドル</param>
    /// <returns>解除に成功した場合はtrue</returns>
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveFontMemResourceEx(IntPtr handle);
}
