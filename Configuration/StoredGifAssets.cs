using System.Security.Cryptography;

namespace AutoExtracter;

/// <summary>
/// 選択したGIFを設定と同じ場所で管理する
/// </summary>
internal static class StoredGifAssets
{
    /// <summary>
    /// 元ファイルに依存しない検証済みのコピーを保存する
    /// </summary>
    /// <param name="source">選択したGIF、空欄は同梱画像</param>
    /// <param name="directory">設定ファイルのフォルダ</param>
    /// <returns>保存したGIFのパス</returns>
    internal static string Import(string source, string directory)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        if (new FileInfo(source).Length > 10 * 1024 * 1024)
        {
            throw new InvalidDataException(Messages.Get(MessageId.E116));
        }

        byte[] bytes = File.ReadAllBytes(source);
        if (bytes.Length > 10 * 1024 * 1024)
        {
            throw new InvalidDataException(Messages.Get(MessageId.E116));
        }

        string folder = Path.Combine(directory, "Icons");
        Directory.CreateDirectory(folder);
        string target = Path.Combine(folder, Convert.ToHexString(SHA256.HashData(bytes)) + ".gif");
        string temporary = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllBytes(temporary, bytes);
            using var validation = new GifImage(temporary);
            if (!File.Exists(target))
            {
                File.Move(temporary, target);
            }

            return target;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
