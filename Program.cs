using System.Text;
using System.Text.Json;

namespace AutoExtracter;

/// <summary>
/// アプリケーションの起動と終了を管理する
/// </summary>
internal static class Program
{
    /// <summary>
    /// 設定を読み込み、通知領域または画面なしで監視サービスを起動する
    /// </summary>
    /// <param name="args">設定ファイルと任意の--headless引数</param>
    /// <returns>正常終了時は0、異常終了時は1</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var logger = new FileLogger();
        string appearance = "System";
        bool headless = args.Contains("--headless", StringComparer.OrdinalIgnoreCase);
        try
        {
            using var startup = new SingleInstanceGuard();
            if (!startup.IsFirstInstance)
            {
                logger.Write(MessageId.W009);
                if (!headless)
                {
                    startup.RequestActivation();
                }

                return 0;
            }

            var paths = new List<string>();
            foreach (string argument in args)
            {
                if (!argument.Equals("--headless", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(argument);
                }
            }

            if (paths.Count > 1)
            {
                throw new ArgumentException(Messages.Get(MessageId.E104));
            }

            string config = Path.GetFullPath(paths.Count == 1 ? paths[0] : ConfigurationBootstrapper.GetDefaultPath(AppContext.BaseDirectory));
            if (paths.Count == 0)
            {
                ConfigurationBootstrapper.Ensure(config, Path.Combine(AppContext.BaseDirectory, "appsettings.default.json"));
            }

            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(config))
                ?? throw new Exception(Messages.Get(MessageId.E105));
            appearance = settings.Appearance;
            settings.Validate(Path.GetDirectoryName(config)!);
            logger.SetFolder(settings.LogFolder);
            // 排他ファイルを保持し、同じジョブを処理する二重起動を防止する
            using var instance = new FileStream(Path.Combine(settings.TemporaryFolder, ".watcher.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var progress = new ProgressState();
            if (headless && settings.InteractivePasswords)
            {
                logger.Write(MessageId.W006);
                settings.InteractivePasswords = false;
            }

            var settingsStore = new SettingsStore(settings, config);
            var service = new WatcherService(settings, logger, progress);
            if (headless)
            {
                service.RunAsync().GetAwaiter().GetResult();
                return 0;
            }

            ApplicationConfiguration.Initialize();
            int exitCode;
            bool restartRequested;
            using (var context = new TrayApplicationContext(service, progress, logger, settings, settingsStore, startup))
            {
                Application.Run(context);
                exitCode = context.ExitCode;
                restartRequested = context.RestartRequested;
            }

            if (restartRequested && exitCode == 0)
            {
                instance.Dispose();
                logger.Write(MessageId.I372);
                startup.Dispose();
                ApplicationRestarter.Start(config);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            logger.Write(MessageId.E001, ex);
            if (!headless)
            {
                ThemedMessageDialog.ShowNotice(null, Messages.Get(MessageId.E001) + Environment.NewLine + ex.Message,
                    Messages.Get(MessageId.I310), ThemePalette.Resolve(appearance));
            }

            return 1;
        }
    }
}
