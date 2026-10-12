using System.Text.Json;

namespace AutoExtracter;

/// <summary>
/// フォルダの解決、ジョブ情報の読み込み、完了データの公開処理を提供する
/// </summary>
internal static class FileOperations
{
    /// <summary>
    /// 設定ファイルの場所を基準に、フォルダの絶対パスを解決する
    /// </summary>
    /// <param name="path">設定されたフォルダのパス</param>
    /// <param name="configDirectory">設定ファイルのあるフォルダ</param>
    /// <returns>末尾の区切り文字を除いたフォルダの絶対パス</returns>
    public static string ResolveFolder(string path, string configDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(Messages.Get(MessageId.E101));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path, configDirectory));
    }

    /// <summary>
    /// パスワード未検出のジョブを優先し、各グループ内は受け入れ順に取得する
    /// </summary>
    /// <param name="folder">一時フォルダ</param>
    /// <returns>処理順の受け入れ済みジョブ</returns>
    public static IEnumerable<string> EnumerateReadyJobs(string folder)
    {
        return Directory.EnumerateDirectories(folder).Where(job =>
        {
            return File.Exists(Path.Combine(job, "ready"));
        }).OrderBy(job =>
        {
            return File.Exists(Path.Combine(job, "password-required"));
        }).ThenBy(job =>
        {
            return Path.GetFileName(job);
        }, StringComparer.Ordinal);
    }

    /// <summary>
    /// ジョブ情報から元の圧縮ファイル名を取得する
    /// </summary>
    /// <param name="job">ジョブ情報を保存したフォルダ</param>
    /// <returns>元の圧縮ファイル名</returns>
    public static string ReadJobName(string job)
    {
        string? name = JsonSerializer.Deserialize<string>(File.ReadAllText(Path.Combine(job, "name.json")));
        if (string.IsNullOrWhiteSpace(name) || !string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal) || name.Contains(':'))
        {
            throw new InvalidDataException(Messages.Get(MessageId.E114));
        }

        return name;
    }

    /// <summary>
    /// 記録された退避先に圧縮ファイルが保存されているか確認する
    /// </summary>
    /// <param name="job">確認するジョブフォルダ</param>
    /// <returns>退避先の記録と圧縮ファイルが存在する場合はtrue</returns>
    public static bool IsRetiredJob(string job)
    {
        string marker = Path.Combine(job, "retired");
        return File.Exists(marker) && File.Exists(File.ReadAllText(marker));
    }

    /// <summary>
    /// 解凍データを完了先へ転送し、転送完了後に正式なフォルダ名で公開する
    /// </summary>
    /// <param name="source">転送元のフォルダ</param>
    /// <param name="destination">公開する完了フォルダ</param>
    /// <param name="token">転送を中断するためのキャンセルトークン</param>
    public static void PublishDirectory(string source, string destination, CancellationToken token)
    {
        // 別ドライブでも最後の名前変更が同一ボリューム内で行われるよう、完了先で準備する
        string staging = Path.Combine(Path.GetDirectoryName(destination)!, ".pending-" + Path.GetFileName(destination));
        if (Directory.Exists(staging))
        {
            Directory.Delete(staging, true);
        }

        Directory.CreateDirectory(staging);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(staging, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            File.Copy(file, Path.Combine(staging, Path.GetRelativePath(source, file)));
        }

        Directory.Move(staging, destination);
    }
}
