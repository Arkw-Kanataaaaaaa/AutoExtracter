using System.Globalization;
using System.Text;

namespace AutoExtracter;

/// <summary>
/// 識別コード付きのログをコンソールと日別ファイルへ出力する
/// </summary>
internal sealed class FileLogger
{
    private readonly object logGate = new();
    private string? logFolder;

    /// <summary>
    /// ログファイルの保存先を設定する
    /// </summary>
    /// <param name="folder">ログファイルの保存先フォルダ</param>
    public void SetFolder(string folder)
    {
        lock (logGate)
        {
            logFolder = folder;
        }
    }

    /// <summary>
    /// 種別コードと番号に対応するメッセージをログへ出力する
    /// </summary>
    /// <param name="id">I・W・Eの種別コードと3桁の番号</param>
    /// <param name="exception">ログに付記する例外例外がない場合はnull</param>
    /// <param name="arguments">メッセージに埋め込む値</param>
    public void Write(MessageId id, Exception? exception = null, params object[] arguments)
    {
        lock (logGate)
        {
            string line = FormatLine(id, Messages.Get(id, arguments));
            if (exception != null)
            {
                // ライブラリ由来の例外情報も診断用に保持する
                line += Environment.NewLine + exception;
            }

            Console.WriteLine(line);
            try
            {
                if (!string.IsNullOrEmpty(logFolder))
                {
                    File.AppendAllText(GetTodayPath(logFolder), line + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                // ログ書き込みの失敗を再びファイルへ書くと再帰するため、標準エラーだけへ出力する
                Console.Error.WriteLine(FormatLine(MessageId.E006, Messages.Get(MessageId.E006, ex.Message)));
            }
        }
    }

    /// <summary>
    /// 本日のログファイルのパスを取得する
    /// </summary>
    /// <param name="folder">ログフォルダ</param>
    /// <returns>本日分のログファイルのパス</returns>
    public static string GetTodayPath(string folder)
    {
        return Path.Combine(folder, "AutoExtracter-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
    }

    /// <summary>
    /// 日時、ログ種別、識別コードを含む一行を作成する
    /// </summary>
    /// <param name="id">メッセージの識別コード</param>
    /// <param name="message">出力するメッセージ</param>
    /// <returns>ログの一行</returns>
    private static string FormatLine(MessageId id, string message)
    {
        string code = id.ToString();
        string level;
        switch (code[0])
        {
            case 'I':
                level = "INFO";
                break;
            case 'W':
                level = "WARN";
                break;
            default:
                level = "ERROR";
                break;
        }

        return $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] [{code}] {message}";
    }
}
