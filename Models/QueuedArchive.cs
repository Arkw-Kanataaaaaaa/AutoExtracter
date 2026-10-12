namespace AutoExtracter;

/// <summary>
/// 解凍待ちの圧縮ファイル
/// </summary>
/// <param name="JobFolder">ジョブフォルダ</param>
/// <param name="Name">圧縮ファイル名</param>
/// <param name="Bytes">圧縮ファイルの容量</param>
internal sealed record QueuedArchive(string JobFolder, string Name, long Bytes);
