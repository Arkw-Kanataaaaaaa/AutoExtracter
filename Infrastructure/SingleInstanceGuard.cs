using System.Security.Principal;

namespace AutoExtracter;

/// <summary>
/// 同じユーザーによるアプリの重複起動を防ぐ
/// </summary>
internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle activation;
    private bool ownsMutex;
    private bool disposed;

    /// <summary>
    /// ユーザー固有の名前で起動権を取得する
    /// </summary>
    /// <param name="applicationName">プロセス間で共有するアプリ名</param>
    internal SingleInstanceGuard(string applicationName = "AutoExtracter")
    {
        using var identity = WindowsIdentity.GetCurrent();
        string user = identity.User!.Value;
        activation = new EventWaitHandle(false, EventResetMode.AutoReset, @"Global\" + applicationName + "-Activate-" + user);
        mutex = new Mutex(false, @"Global\" + applicationName + "-" + user);
        try
        {
            ownsMutex = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            ownsMutex = true;
        }
    }

    /// <summary>
    /// このプロセスで監視を開始できるか
    /// </summary>
    internal bool IsFirstInstance => ownsMutex;

    /// <summary>
    /// 起動済みのアプリへ通知パネルの表示を要求する
    /// </summary>
    internal void RequestActivation()
    {
        if (!disposed)
        {
            activation.Set();
        }
    }

    /// <summary>
    /// 表示要求を一度だけ受け取る
    /// </summary>
    /// <returns>起動済みのアプリに表示要求がある場合はtrue</returns>
    internal bool TakeActivationRequest()
    {
        return !disposed && ownsMutex && activation.WaitOne(0);
    }

    /// <summary>
    /// 終了または再起動前に起動権を解放する
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (ownsMutex)
        {
            mutex.ReleaseMutex();
            ownsMutex = false;
        }

        mutex.Dispose();
        activation.Dispose();
    }
}
