namespace AutoExtracter;

/// <summary>
/// 解凍結果の保存名と元の圧縮ファイルの後処理を管理する
/// </summary>
internal static class CompletedArchiveStorage
{
    /// <summary>
    /// 公開済みの保存先を確認し、従来のジョブ名による保存にも対応する
    /// </summary>
    /// <param name="job">一時ジョブ</param>
    /// <param name="folder">完了フォルダ</param>
    /// <returns>公開済みのパス、未公開の場合はnull</returns>
    public static string? FindPublished(string job, string folder)
    {
        string? recorded = ReadDestination(job, "completed", folder);
        if (recorded != null && Directory.Exists(recorded))
        {
            return recorded;
        }

        string legacy = Path.Combine(folder, Path.GetFileName(job));
        return Directory.Exists(legacy) ? legacy : null;
    }

    /// <summary>
    /// 拡張子を除いた名前で完了先を予約し、再試行でも同じ保存先を使用する
    /// </summary>
    /// <param name="job">一時ジョブ</param>
    /// <param name="name">圧縮ファイル名</param>
    /// <param name="folder">完了フォルダ</param>
    /// <returns>公開先のパス</returns>
    public static string GetDestination(string job, string name, string folder)
    {
        string? destination = ReadDestination(job, "completed", folder) ?? FindPublished(job, folder);
        if (destination == null)
        {
            string stem = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(stem))
            {
                throw new InvalidDataException(Messages.Get(MessageId.E114));
            }

            destination = UniquePath(folder, stem, "");
            File.WriteAllText(Path.Combine(job, "completed"), destination);
        }

        return destination;
    }

    /// <summary>
    /// 公開成功後に元ファイルを退避または削除する
    /// </summary>
    /// <param name="job">一時ジョブ</param>
    /// <param name="archive">元の圧縮ファイル</param>
    /// <param name="settings">ジョブの設定</param>
    /// <param name="token">停止要求</param>
    public static void FinishOriginal(string job, string archive, Settings settings, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (settings.KeepOriginalArchives)
        {
            string? backup = ReadDestination(job, "backup", settings.ArchiveBackupFolder);
            if (backup == null)
            {
                backup = UniquePath(settings.ArchiveBackupFolder, Path.GetFileNameWithoutExtension(archive), Path.GetExtension(archive));
                File.WriteAllText(Path.Combine(job, "backup"), backup);
            }

            if (!File.Exists(backup))
            {
                Directory.CreateDirectory(settings.ArchiveBackupFolder);
                string pending = backup + ".pending-" + Path.GetFileName(job);
                File.Copy(archive, pending, true);
                token.ThrowIfCancellationRequested();
                File.Move(pending, backup);
            }
        }

        File.Delete(archive);
    }

    /// <summary>
    /// 記録された保存先が設定フォルダの直下にあることを確認する
    /// </summary>
    /// <param name="job">一時ジョブ</param>
    /// <param name="marker">記録ファイル名</param>
    /// <param name="folder">許可された保存フォルダ</param>
    /// <returns>記録された保存先、未記録の場合はnull</returns>
    private static string? ReadDestination(string job, string marker, string folder)
    {
        string path = Path.Combine(job, marker);
        if (!File.Exists(path))
        {
            return null;
        }

        string destination = Path.GetFullPath(File.ReadAllText(path));
        if (!string.Equals(Path.GetDirectoryName(destination), Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Messages.Get(MessageId.E114));
        }

        return destination;
    }

    /// <summary>
    /// 同名データがある場合は連番を付けて上書きを避ける
    /// </summary>
    /// <param name="folder">保存先フォルダ</param>
    /// <param name="stem">拡張子を除いた名前</param>
    /// <param name="extension">拡張子</param>
    /// <returns>未使用のパス</returns>
    private static string UniquePath(string folder, string stem, string extension)
    {
        string path = Path.Combine(folder, stem + extension);
        for (int index = 2; File.Exists(path) || Directory.Exists(path); index++)
        {
            path = Path.Combine(folder, stem + " (" + index + ")" + extension);
        }

        return path;
    }
}
