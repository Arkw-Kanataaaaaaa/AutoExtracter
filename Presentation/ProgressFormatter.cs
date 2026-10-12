namespace AutoExtracter;

/// <summary>
/// 処理状況を通知領域と詳細画面向けに整形する
/// </summary>
internal static class ProgressFormatter
{
    /// <summary>
    /// バイト数を読みやすい容量へ変換する
    /// </summary>
    /// <param name="bytes">表示するバイト数</param>
    /// <returns>単位付きの容量</returns>
    public static string Size(long bytes)
    {
        string[] units =
        {
            "B", "KiB", "MiB", "GiB", "TiB"
        };

        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    /// <summary>
    /// 通知領域の文字数制限内で概要を作成する
    /// </summary>
    /// <param name="state">表示する処理状況</param>
    /// <returns>63文字以下の通知領域ツールチップ</returns>
    public static string Tooltip(ProgressSnapshot state)
    {
        if (state.Phase == MessageId.I301)
        {
            return Messages.Get(MessageId.I301);
        }

        bool extracting = state.Phase == MessageId.I302 || state.Phase == MessageId.I303;
        MessageId phase = extracting ? MessageId.I302 : state.Phase;
        string text = extracting || state.Phase == MessageId.I386
            ? Messages.Get(MessageId.I313, Messages.Get(phase), state.ArchiveName)
            : Messages.Get(phase);
        return text.Length > 63 ? text[..62] + "…" : text;
    }
}
