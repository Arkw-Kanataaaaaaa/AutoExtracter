namespace AutoExtracter;

/// <summary>
/// GIFのフレームを通知領域用アイコンとして保持する
/// </summary>
internal sealed class GifTrayAnimation : IDisposable
{
    private readonly List<Icon> frames = new();
    private readonly List<int> delays = new();
    private int index;
    private long nextFrame;

    /// <summary>
    /// GIFを読み込み、フレームと表示時間を作成する
    /// </summary>
    /// <param name="path">GIFファイルのパス</param>
    /// <param name="defaultResource">パスが空の場合に使用する同梱GIFのリソース名</param>
    public GifTrayAnimation(string path, string? defaultResource = null)
    {
        try
        {
            using var image = new GifImage(path, defaultResource);
            for (int index = 0; index < image.Delays.Length; index++)
            {
                using var bitmap = image.RenderFrame(index, 32, square: true);
                frames.Add(TrayIconRenderer.FromBitmap(bitmap));
                delays.Add(image.Delays[index]);
            }

            nextFrame = Environment.TickCount64 + delays[0];
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// 現在表示するアイコン
    /// </summary>
    public Icon Current => frames[index];

    /// <summary>
    /// Windows通知で使用する先頭フレーム
    /// </summary>
    public Icon FirstFrame => frames[0];

    /// <summary>
    /// 状態が切り替わった際に先頭フレームから再生する
    /// </summary>
    public void Reset()
    {
        index = 0;
        nextFrame = Environment.TickCount64 + delays[0];
    }

    /// <summary>
    /// 表示時間を過ぎたフレームを進める
    /// </summary>
    /// <returns>表示フレームが変わった場合はtrue</returns>
    public bool Advance()
    {
        if (frames.Count <= 1 || Environment.TickCount64 < nextFrame)
        {
            return false;
        }

        index = (index + 1) % frames.Count;
        nextFrame = Environment.TickCount64 + delays[index];
        return true;
    }

    /// <summary>
    /// 作成したアイコンを解放する
    /// </summary>
    public void Dispose()
    {
        foreach (Icon frame in frames)
        {
            frame.Dispose();
        }

        frames.Clear();
    }
}
