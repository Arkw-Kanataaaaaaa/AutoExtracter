namespace AutoExtracter;

/// <summary>
/// 退避フォルダの確認結果を保持する
/// </summary>
/// <param name="ErrorFolder">エラー退避先</param>
/// <param name="ErrorCount">エラーファイル件数</param>
/// <param name="PasswordFolder">パスワード退避先</param>
/// <param name="PasswordCount">パスワード退避ファイル件数</param>
internal sealed record FolderIssueState(string ErrorFolder, long ErrorCount, string PasswordFolder, long PasswordCount)
{
    /// <summary>
    /// 問題ファイルの合計件数
    /// </summary>
    public long TotalCount => ErrorCount + PasswordCount;

    /// <summary>
    /// ファイルがあるフォルダだけを取得する
    /// </summary>
    /// <returns>開く対象のフォルダ</returns>
    public IEnumerable<string> GetFolderTargets()
    {
        if (ErrorCount > 0)
        {
            yield return ErrorFolder;
        }

        if (PasswordCount > 0)
        {
            yield return PasswordFolder;
        }
    }
}
