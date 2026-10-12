using System.Diagnostics;

namespace AutoExtracter;

/// <summary>
/// 同じ設定ファイルでアプリを起動し直す
/// </summary>
internal static class ApplicationRestarter
{
    /// <summary>
    /// 実行形式に応じた再起動コマンドを作成する
    /// </summary>
    /// <param name="config">設定ファイルのパス</param>
    /// <returns>起動情報</returns>
    public static ProcessStartInfo CreateStartInfo(string config)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };

        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, typeof(Program).Assembly.GetName().Name + ".dll"));
        }

        start.ArgumentList.Add(config);
        return start;
    }

    /// <summary>
    /// 排他ロックの解放後に新しいプロセスを起動する
    /// </summary>
    /// <param name="config">設定ファイルのパス</param>
    public static void Start(string config)
    {
        using var process = Process.Start(CreateStartInfo(config));
    }
}
