using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoExtracter;

/// <summary>
/// 設定の検証と保存を行い、更新済み設定を監視処理と共有する
/// </summary>
internal sealed class SettingsStore
{
    private readonly string path;
    private Settings current;

    /// <summary>
    /// 設定と保存先を保持する
    /// </summary>
    /// <param name="settings">検証済みの設定</param>
    /// <param name="path">設定ファイルのパス</param>
    public SettingsStore(Settings settings, string path)
    {
        current = settings;
        this.path = Path.GetFullPath(path);
    }

    /// <summary>
    /// 次の監視スキャンとジョブに適用する設定
    /// </summary>
    public Settings Current => Volatile.Read(ref current);

    /// <summary>
    /// 再起動で適用する処理設定を保存し、現在のジョブの設定は保持する
    /// </summary>
    /// <param name="watchFolder">監視対象フォルダ</param>
    /// <param name="completedFolder">解凍後の移動先</param>
    /// <param name="extensions">対象拡張子</param>
    /// <param name="maxBytes">圧縮ファイル容量の上限</param>
    /// <param name="interactivePasswords">パスワードを対話式で入力するか</param>
    /// <param name="skipRestartConfirmation">次回の確認を省略するか、未指定の場合は現在の設定を保持する</param>
    /// <param name="keepOriginalArchives">元の圧縮ファイルを保持するか</param>
    /// <param name="archiveBackupFolder">元の圧縮ファイルの退避先</param>
    public void SaveProcessingForRestart(string watchFolder, string completedFolder, string[] extensions, long maxBytes, bool interactivePasswords = false, bool? skipRestartConfirmation = null, bool? keepOriginalArchives = null, string? archiveBackupFolder = null)
    {
        var next = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(Current))!;
        next.WatchFolder = watchFolder;
        next.CompletedFolder = completedFolder;
        next.InteractivePasswords = interactivePasswords;
        next.SkipRestartConfirmation = skipRestartConfirmation ?? next.SkipRestartConfirmation;
        next.KeepOriginalArchives = keepOriginalArchives ?? next.KeepOriginalArchives;
        next.ArchiveBackupFolder = archiveBackupFolder ?? next.ArchiveBackupFolder;
        next.ArchiveExtensions = extensions;
        next.MaxArchiveBytes = maxBytes;
        Save(next, false, nameof(Settings.WatchFolder), nameof(Settings.CompletedFolder),
            nameof(Settings.ArchiveExtensions), nameof(Settings.MaxArchiveBytes), nameof(Settings.InteractivePasswords), nameof(Settings.SkipRestartConfirmation),
            nameof(Settings.KeepOriginalArchives), nameof(Settings.ArchiveBackupFolder));
    }

    /// <summary>
    /// 外観と状態別GIFを検証して即時適用する
    /// </summary>
    /// <param name="appearance">テーマ名</param>
    /// <param name="idleGif">待機中のGIF</param>
    /// <param name="extractingGif">処理中のGIF</param>
    /// <param name="dialogueGif">対話中のGIF、未指定の場合は現在の設定を保持する</param>
    /// <param name="animationsEnabled">アニメーションの有効設定、未指定の場合は現在の設定を保持する</param>
    public void UpdateVisuals(string appearance, string idleGif, string extractingGif, string? dialogueGif = null, bool? animationsEnabled = null)
    {
        var next = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(Current))!;
        next.Appearance = appearance;
        next.AnimationsEnabled = animationsEnabled ?? next.AnimationsEnabled;
        next.IdleGifPath = idleGif;
        next.ExtractingGifPath = extractingGif;
        next.DialogueGifPath = dialogueGif ?? next.DialogueGifPath;
        next.Validate(Path.GetDirectoryName(path)!);
        using var idle = new GifImage(next.IdleGifPath, "Idle.gif");
        using var extracting = new GifImage(next.ExtractingGifPath, "Extracting.gif");
        using var dialogue = new GifImage(next.DialogueGifPath, "Dialogue.gif");
        next.IdleGifPath = StoredGifAssets.Import(next.IdleGifPath, Path.GetDirectoryName(path)!);
        next.ExtractingGifPath = StoredGifAssets.Import(next.ExtractingGifPath, Path.GetDirectoryName(path)!);
        next.DialogueGifPath = StoredGifAssets.Import(next.DialogueGifPath, Path.GetDirectoryName(path)!);
        Save(next, true, nameof(Settings.Appearance), nameof(Settings.IdleGifPath), nameof(Settings.ExtractingGifPath), nameof(Settings.DialogueGifPath), nameof(Settings.AnimationsEnabled));
    }

    /// <summary>
    /// 変更項目だけを保存し、成功後に設定を公開する
    /// </summary>
    /// <param name="next">更新済みの設定</param>
    /// <param name="publish">実行中の設定へ反映するか</param>
    /// <param name="properties">保存する項目名</param>
    private void Save(Settings next, bool publish, params string[] properties)
    {
        next.Validate(Path.GetDirectoryName(path)!);
        var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var values = JsonSerializer.SerializeToNode(next)!.AsObject();
        foreach (string property in properties)
        {
            document[property] = values[property]!.DeepClone();
        }

        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, document.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        if (publish)
        {
            Volatile.Write(ref current, next);
        }
    }
}
