namespace AutoExtracter;

/// <summary>
/// 待機、解凍、対話のGIFアイコンを保持し、処理状態に応じて選択する
/// </summary>
internal sealed class StateTrayAnimations : IDisposable
{
    private readonly GifTrayAnimation idle;
    private readonly GifTrayAnimation extracting;
    private readonly GifTrayAnimation dialogue;

    /// <summary>
    /// パネルと同じ設定から状態別GIFを読み込む
    /// </summary>
    /// <param name="settings">GIFの設定</param>
    /// <param name="logger">読み込み失敗のログ出力先</param>
    public StateTrayAnimations(Settings settings, FileLogger logger)
    {
        idle = Load(settings.IdleGifPath, "Idle.gif", MessageId.W003, logger);
        try
        {
            extracting = Load(settings.ExtractingGifPath, "Extracting.gif", MessageId.W004, logger);
            try
            {
                dialogue = Load(settings.DialogueGifPath, "Dialogue.gif", MessageId.W008, logger);
            }
            catch
            {
                extracting.Dispose();
                throw;
            }
        }
        catch
        {
            idle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 状態に対応するGIFを選択する
    /// </summary>
    /// <param name="phase">処理状態</param>
    /// <returns>待機用、解凍用、対話用GIF、それ以外の状態はnull</returns>
    public GifTrayAnimation? Select(MessageId phase)
    {
        if (phase == MessageId.I386)
        {
            return dialogue;
        }

        if (phase == MessageId.I301)
        {
            return idle;
        }

        if (phase == MessageId.I302 || phase == MessageId.I303)
        {
            return extracting;
        }

        return null;
    }

    /// <summary>
    /// 指定GIFを読み込み、失敗時は同梱GIFを使用する
    /// </summary>
    /// <param name="path">GIFのパス</param>
    /// <param name="resource">同梱GIFのリソース名</param>
    /// <param name="warning">警告の識別子</param>
    /// <param name="logger">警告の出力先</param>
    /// <returns>読み込んだGIFアイコン</returns>
    private static GifTrayAnimation Load(string path, string resource, MessageId warning, FileLogger logger)
    {
        try
        {
            return new GifTrayAnimation(path, resource);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(path))
        {
            logger.Write(warning, ex, path);
            return new GifTrayAnimation("", resource);
        }
    }

    /// <summary>
    /// 状態別のGIFアイコンを解放する
    /// </summary>
    public void Dispose()
    {
        idle.Dispose();
        extracting.Dispose();
        dialogue.Dispose();
    }
}
