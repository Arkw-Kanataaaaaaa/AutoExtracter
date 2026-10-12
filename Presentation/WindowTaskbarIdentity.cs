using System.Runtime.InteropServices;

namespace AutoExtracter;

/// <summary>
/// ウィンドウ単位の識別子でタスクバーのアイコンをアプリ本体から分ける
/// </summary>
internal static class WindowTaskbarIdentity
{
    /// <summary>
    /// ウィンドウへタスクバーの識別子を設定する
    /// </summary>
    /// <param name="window">対象ウィンドウ</param>
    /// <param name="identity">タスクバー用の識別子、nullの場合は解除する</param>
    internal static void Apply(IntPtr window, string? identity)
    {
        Guid interfaceId = typeof(IPropertyStore).GUID;
        SHGetPropertyStoreForWindow(window, ref interfaceId, out IPropertyStore store);
        var key = new PropertyKey
        {
            FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
            PropertyId = 5
        };

        var value = new PropertyValue
        {
            Type = identity == null ? (ushort)0 : (ushort)31,
            Pointer = identity == null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(identity)
        };

        try
        {
            store.SetValue(ref key, ref value);
        }
        finally
        {
            Marshal.FreeCoTaskMem(value.Pointer);
            Marshal.ReleaseComObject(store);
        }
    }

    /// <summary>
    /// Windowsのプロパティを識別するキー
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        internal Guid FormatId;
        internal uint PropertyId;
    }

    /// <summary>
    /// 文字列形式のPROPVARIANT
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropertyValue
    {
        [FieldOffset(0)]
        internal ushort Type;
        [FieldOffset(8)]
        internal IntPtr Pointer;
    }

    /// <summary>
    /// Windowsが管理するウィンドウのプロパティ
    /// </summary>
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropertyValue value);
        void SetValue(ref PropertyKey key, ref PropertyValue value);
        void Commit();
    }

    /// <summary>
    /// 指定ウィンドウのプロパティを取得する
    /// </summary>
    /// <param name="window">対象ウィンドウ</param>
    /// <param name="interfaceId">取得するインターフェース</param>
    /// <param name="store">取得結果</param>
    [DllImport("shell32.dll", PreserveSig = false)]
    private static extern void SHGetPropertyStoreForWindow(IntPtr window, ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);
}
