using System.Globalization;
using System.Resources;

namespace AutoExtracter;

/// <summary>
/// リソースファイルに定義したメッセージを取得する
/// </summary>
internal static class Messages
{
    private static readonly ResourceManager ResourceManager = new(
        "AutoExtracter.Resources.Messages", typeof(Messages).Assembly);

    /// <summary>
    /// 識別コードに対応するメッセージを取得し、引数を埋め込む
    /// </summary>
    /// <param name="id">メッセージの種別コードと番号</param>
    /// <param name="arguments">メッセージに埋め込む値</param>
    /// <returns>引数を埋め込んだメッセージ</returns>
    public static string Get(MessageId id, params object[] arguments)
    {
        string template = ResourceManager.GetString(id.ToString(), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(id.ToString());
        return string.Format(CultureInfo.CurrentCulture, template, arguments);
    }
}
