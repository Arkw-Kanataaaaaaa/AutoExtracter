using System.Text;
using System.Text.Json.Nodes;

namespace AutoExtracter;

/// <summary>
/// MSIの初期設定テンプレートからユーザーの設定ファイルを作成する
/// </summary>
internal static class ConfigurationBootstrapper
{
    /// <summary>
    /// MSI版はユーザー領域、それ以外は実行ファイルと同じ場所へ設定を保存する
    /// </summary>
    /// <param name="applicationDirectory">実行ファイルのフォルダ</param>
    /// <param name="localApplicationData">ユーザー領域、未指定の場合はWindowsから取得する</param>
    /// <returns>既定の設定ファイル</returns>
    internal static string GetDefaultPath(string applicationDirectory, string? localApplicationData = null)
    {
        if (File.Exists(Path.Combine(applicationDirectory, "appsettings.default.json")))
        {
            return Path.Combine(localApplicationData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoExtracter", "appsettings.json");
        }

        return Path.Combine(applicationDirectory, "appsettings.json");
    }

    /// <summary>
    /// 既存設定を保持し、未作成の場合だけユーザー用のフォルダを設定する
    /// </summary>
    /// <param name="path">ユーザーの設定ファイル</param>
    /// <param name="template">MSIに同梱する初期設定</param>
    /// <param name="documents">ドキュメントフォルダ、未指定の場合は現在のユーザーのフォルダ</param>
    public static void Ensure(string path, string template, string? documents = null)
    {
        if (File.Exists(path))
        {
            return;
        }

        var settings = JsonNode.Parse(File.ReadAllText(template))!.AsObject();
        string root = Path.Combine(documents ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AutoExtracter");
        var folders = new Dictionary<string, string>
        {
            [nameof(Settings.WatchFolder)] = "Incoming",
            [nameof(Settings.TemporaryFolder)] = "Temporary",
            [nameof(Settings.CompletedFolder)] = "Completed",
            [nameof(Settings.ArchiveBackupFolder)] = "Archives",
            [nameof(Settings.PasswordRequiredFolder)] = "PasswordRequired",
            [nameof(Settings.ErrorFolder)] = "Errors",
            [nameof(Settings.LogFolder)] = "Logs"
        };

        foreach (var folder in folders)
        {
            settings[folder.Key] = Path.Combine(root, folder.Value);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, settings.ToJsonString(), new UTF8Encoding(false));
            try
            {
                File.Move(temporary, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                // 他の処理が作成した設定を上書きしない
            }
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
