using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace AutoExtracter;

/// <summary>
/// 圧縮ファイルを安全に解凍する
/// </summary>
internal sealed class ArchiveExtractor
{
    private readonly Settings settings;
    private readonly ProgressState progress;

    /// <summary>
    /// インスタンスを初期化する
    /// </summary>
    /// <param name="settings">監視と解凍の設定</param>
    /// <param name="progress">画面と共有する処理状況</param>
    public ArchiveExtractor(Settings settings, ProgressState progress)
    {
        this.settings = settings;
        this.progress = progress;
    }

    /// <summary>
    /// アーカイブ内のファイルを指定フォルダへ解凍する
    /// </summary>
    /// <param name="archive">解凍対象の圧縮ファイル</param>
    /// <param name="output">解凍データの保存先</param>
    /// <param name="token">処理を中断するためのキャンセルトークン</param>
    /// <param name="password">復号用パスワード、未指定の場合は入力要求を検出する</param>
    public void Extract(string archive, string output, CancellationToken token, string? password = null)
    {
        if (new FileInfo(archive).Length > settings.MaxArchiveBytes)
        {
            throw new InvalidDataException(Messages.Get(MessageId.E120));
        }

        // ヘッダー内の容量から解凍予定サイズを取得し、実際の書き込み量と比較する
        var options = new ReaderOptions
        {
            Password = password
        };

        using var metadata = ArchiveFactory.OpenArchive(archive, options);
        {
            long totalBytes = 0;
            int totalEntries = 0;
            int metadataEntries = 0;
            foreach (var entry in metadata.Entries)
            {
                token.ThrowIfCancellationRequested();
                if (++metadataEntries > settings.MaxEntries)
                {
                    throw new InvalidDataException(Messages.Get(MessageId.E107));
                }

                if (password == null && RequiresPassword(entry, metadata.Type))
                {
                    throw new PasswordNeededException();
                }

                if (!entry.IsDirectory)
                {
                    totalEntries++;
                    totalBytes = checked(totalBytes + entry.Size);
                }

                if (totalBytes > settings.MaxExtractedBytes)
                {
                    throw new InvalidDataException(Messages.Get(MessageId.E112));
                }
            }

            progress.SetTotals(totalEntries, totalBytes);
        }

        // 7zを含む形式固有のReaderを使用し、ソリッド圧縮も順次読み込む
        using var source = metadata.Type == ArchiveType.SevenZip ? null : File.OpenRead(archive);
        using var reader = metadata.Type == ArchiveType.SevenZip
            ? metadata.ExtractAllEntries() : ReaderFactory.OpenReader(source!, options);
        long total = 0;
        int entries = 0;
        int extractedEntries = 0;
        var root = Path.GetFullPath(output) + Path.DirectorySeparatorChar;
        var buffer = new byte[81920];
        while (reader.MoveToNextEntry())
        {
            token.ThrowIfCancellationRequested();
            var entry = reader.Entry;
            if (password == null && RequiresPassword(entry, metadata.Type))
            {
                throw new PasswordNeededException();
            }

            if (entry.IsSplitAfter || entry.VolumeIndexLast != entry.VolumeIndexFirst)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E106));
            }

            if (++entries > settings.MaxEntries)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E107));
            }

            if (entry.LinkTarget != null)
            {
                throw new InvalidDataException(Messages.Get(MessageId.E108));
            }

            var key = entry.Key ?? throw new InvalidDataException(Messages.Get(MessageId.E109));
            if (key.Contains(':') || Path.IsPathRooted(key))
            {
                throw new InvalidDataException(Messages.Get(MessageId.E110));
            }

            var path = Path.GetFullPath(Path.Combine(output, key.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(Messages.Get(MessageId.E111));
            }

            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var input = reader.OpenEntryStream();
            using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            progress.Report(key, extractedEntries, total);
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                total = checked(total + read);
                if (total > settings.MaxExtractedBytes)
                {
                    throw new InvalidDataException(Messages.Get(MessageId.E112));
                }

                target.Write(buffer, 0, read);
                progress.Report(key, extractedEntries, total);
            }

            extractedEntries++;
            progress.Report(key, extractedEntries, total);
        }
    }

    /// <summary>
    /// エントリの暗号化フラグからパスワード要求を判定する
    /// </summary>
    /// <param name="entry">判定するエントリ</param>
    /// <param name="type">アーカイブ形式</param>
    /// <returns>暗号化されたデータを含む場合はtrue</returns>
    private static bool RequiresPassword(IEntry entry, ArchiveType type)
    {
        // SharpCompress 0.50.5はデータを持たない7z項目も暗号化扱いにするため除外する
        // 暗号化ヘッダーやデータ読み込み時のパスワード例外は呼び出し元へ伝える
        return entry.IsEncrypted && !(type == ArchiveType.SevenZip && entry.Size == 0);
    }

    /// <summary>
    /// 例外がパスワード要求を示すか判定する
    /// </summary>
    /// <param name="ex">判定対象の例外</param>
    /// <returns>パスワードが必要な場合はtrue</returns>
    public static bool IsPasswordRequired(Exception ex)
    {
        return ex is PasswordNeededException ||
            ex.Message == Messages.Get(MessageId.E113) ||
            ex.GetType().Name.Contains("Cryptographic", StringComparison.OrdinalIgnoreCase) ||
            ex.GetType().Name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            (ex.InnerException != null && IsPasswordRequired(ex.InnerException));
    }

    /// <summary>
    /// 入力済みパスワードによる復号の失敗を判定する
    /// </summary>
    /// <param name="ex">解凍時の例外</param>
    /// <returns>復号または整合性確認に失敗した場合はtrue</returns>
    internal static bool IsPasswordRejected(Exception ex)
    {
        return IsPasswordRequired(ex) || ex is InvalidFormatException ||
            ex.GetType().Name.Contains("DataError", StringComparison.OrdinalIgnoreCase) ||
            ex.GetType().Name.Contains("Crc", StringComparison.OrdinalIgnoreCase) ||
            ex.GetType().Name.Contains("Zlib", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Data Error", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("CRC", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("decrypt", StringComparison.OrdinalIgnoreCase) ||
            (ex.InnerException != null && IsPasswordRejected(ex.InnerException));
    }

    /// <summary>
    /// パスワードが必要な圧縮ファイルを通知する
    /// </summary>
    private sealed class PasswordNeededException : Exception
    {
        /// <summary>
        /// インスタンスを初期化する
        /// </summary>
        public PasswordNeededException() : base(Messages.Get(MessageId.E113))
        {
        }
    }
}

