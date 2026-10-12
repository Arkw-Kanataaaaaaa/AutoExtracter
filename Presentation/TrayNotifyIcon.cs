using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// 状態別のトレイアイコンと独自画像を使うWindows通知を管理する
/// </summary>
internal sealed class TrayNotifyIcon : NativeWindow, IDisposable
{
    private const int CallbackMessage = 0x8001;
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private readonly Func<uint, TrayNotificationData, bool>? sender;
    private Icon? currentIcon;
    private Icon? notificationIcon;
    private string text = "";
    private bool visible;
    private bool installed;
    private bool disposed;

    /// <summary>
    /// 通知のクリックを通知する
    /// </summary>
    internal event EventHandler? BalloonTipClicked;

    /// <summary>
    /// トレイアイコンのクリックを通知する
    /// </summary>
    internal event MouseEventHandler? MouseClick;

    /// <summary>
    /// 表示画像を更新する
    /// </summary>
    internal Icon? Icon
    {
        get => currentIcon;
        set
        {
            currentIcon = value;
            UpdateIcon();
        }
    }

    /// <summary>
    /// Windows標準ツールチップの文言を更新する
    /// </summary>
    internal string Text
    {
        get => text;
        set
        {
            if (text != value)
            {
                text = value;
                UpdateIcon();
            }
        }
    }

    /// <summary>
    /// トレイアイコンの表示を切り替える
    /// </summary>
    internal bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (value)
            {
                UpdateIcon();
            }
            else if (installed)
            {
                Send(2, CreateData());
                installed = false;
            }
        }
    }

    /// <summary>
    /// 通知の操作を受け取る非表示ウィンドウを作成する
    /// </summary>
    /// <param name="sender">テスト用の通知送信先、未指定の場合はWindowsへ送信する</param>
    internal TrayNotifyIcon(Func<uint, TrayNotificationData, bool>? sender = null)
    {
        this.sender = sender;
        CreateHandle(new CreateParams
        {
            Caption = "AutoExtracter notification"
        });
    }

    /// <summary>
    /// 対話GIFから作成した画像をWindows通知のアイコンとして表示する
    /// </summary>
    /// <param name="timeout">通知時間</param>
    /// <param name="title">通知タイトル</param>
    /// <param name="message">通知本文</param>
    /// <param name="type">標準通知アイコンの種別</param>
    /// <param name="customIcon">独自の通知アイコン、未指定の場合は標準アイコン</param>
    internal void ShowBalloonTip(int timeout, string title, string message, ToolTipIcon type, Icon? customIcon = null)
    {
        if (disposed || !installed)
        {
            return;
        }

        Icon? previous = notificationIcon;
        notificationIcon = customIcon == null ? null : (Icon)customIcon.Clone();
        var data = CreateData();
        data.Flags = 0x10;
        data.InfoTitle = Clip(title, 63);
        data.Info = Clip(message, 255);
        data.Timeout = (uint)Math.Max(0, timeout);
        // 独自画像と大きいアイコンを指定し、Windowsの通知抑制設定を尊重する
        data.InfoFlags = notificationIcon == null ? (uint)type | 0x80 : 0x04 | 0x20 | 0x80;
        data.BalloonIcon = notificationIcon?.Handle ?? IntPtr.Zero;
        Send(1, data);
        previous?.Dispose();
    }

    /// <summary>
    /// 表示画像とツールチップをWindowsへ反映する
    /// </summary>
    private void UpdateIcon()
    {
        if (disposed || !visible || currentIcon == null)
        {
            return;
        }

        var data = CreateData();
        data.Flags = 0x01 | 0x02 | 0x04;
        data.Callback = CallbackMessage;
        data.Icon = currentIcon.Handle;
        data.Tip = Clip(text, 127);
        installed = Send(installed ? 1u : 0u, data);
    }

    /// <summary>
    /// 自分のウィンドウと識別番号で通知データを初期化する
    /// </summary>
    /// <returns>初期化した通知データ</returns>
    private TrayNotificationData CreateData()
    {
        return new TrayNotificationData
        {
            Size = (uint)Marshal.SizeOf<TrayNotificationData>(),
            Window = Handle,
            Id = 1,
            Tip = "",
            Info = "",
            InfoTitle = ""
        };
    }

    /// <summary>
    /// 通知の追加、変更、削除を実行する
    /// </summary>
    /// <param name="command">操作番号</param>
    /// <param name="data">通知データ</param>
    /// <returns>Windowsが操作を受け付けた場合はtrue</returns>
    private bool Send(uint command, TrayNotificationData data)
    {
        return sender != null ? sender(command, data) : ShellNotifyIcon(command, ref data);
    }

    /// <summary>
    /// UTF-16の文字を途中で切らずに通知欄へ収める
    /// </summary>
    /// <param name="value">元の文言</param>
    /// <param name="length">最大長</param>
    /// <returns>通知欄に収まる文言</returns>
    private static string Clip(string value, int length)
    {
        if (value.Length <= length)
        {
            return value;
        }

        if (char.IsHighSurrogate(value[length - 1]))
        {
            length--;
        }

        return value[..length];
    }

    /// <summary>
    /// トレイや通知のクリックとExplorerの再起動を処理する
    /// </summary>
    /// <param name="message">Windowsからのメッセージ</param>
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == taskbarCreated)
        {
            installed = false;
            UpdateIcon();
        }
        else if (message.Msg == CallbackMessage)
        {
            int notification = message.LParam.ToInt32();
            if (notification == 0x0202 || notification == 0x0205 || notification == 0x007B)
            {
                OnMouseClick(new MouseEventArgs(notification == 0x0202 ? MouseButtons.Left : MouseButtons.Right, 1, 0, 0, 0));
            }
            else if (notification == 0x0405)
            {
                BalloonTipClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        base.WndProc(ref message);
    }

    /// <summary>
    /// マウス操作を既存のパネル表示処理へ渡す
    /// </summary>
    /// <param name="e">マウス操作</param>
    private void OnMouseClick(MouseEventArgs e)
    {
        MouseClick?.Invoke(this, e);
    }

    /// <summary>
    /// アイコンを取り除き、通知画像とウィンドウを解放する
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Visible = false;
        disposed = true;
        notificationIcon?.Dispose();
        notificationIcon = null;
        DestroyHandle();
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint command, ref TrayNotificationData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);
}

/// <summary>
/// Windows通知領域へ渡すUnicode形式のデータ
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct TrayNotificationData
{
    internal uint Size;
    internal IntPtr Window;
    internal uint Id;
    internal uint Flags;
    internal uint Callback;
    internal IntPtr Icon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    internal string Tip;
    internal uint State;
    internal uint StateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    internal string Info;
    internal uint Timeout;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    internal string InfoTitle;
    internal uint InfoFlags;
    internal Guid Guid;
    internal IntPtr BalloonIcon;
}
