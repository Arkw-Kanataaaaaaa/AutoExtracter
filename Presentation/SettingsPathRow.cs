namespace AutoExtracter;

/// <summary>
/// フォルダ入力と参照操作をまとめる
/// </summary>
internal sealed class SettingsPathRow : UserControl
{
    private readonly TextBox input = new();
    private readonly SettingsInputFrame frame;
    private readonly bool requireExistingFolder;

    /// <summary>
    /// パスの表示と操作を初期化する
    /// </summary>
    /// <param name="title">項目名</param>
    /// <param name="value">現在のパス</param>
    /// <param name="requireExistingFolder">存在するフォルダだけを許可するか</param>
    /// <param name="showTitle">見出しを表示するか</param>
    public SettingsPathRow(MessageId title, string value, bool requireExistingFolder = false, bool showTitle = true)
    {
        this.requireExistingFolder = requireExistingFolder;
        Size = new Size(596, showTitle ? 76 : 46);
        int inputTop = showTitle ? 30 : 0;
        var label = new Label();
        label.Text = Messages.Get(title);
        label.SetBounds(0, 0, 596, 26);
        label.Visible = showTitle;
        Controls.Add(label);
        input.AccessibleName = Messages.Get(title);
        input.Text = value;
        frame = new SettingsInputFrame(input);
        frame.SetBounds(0, inputTop, 496, 42);
        Controls.Add(frame);
        input.TextChanged += (_, _) =>
        {
            ValidateFolder();
        };

        ValidateFolder();
        EnabledChanged += (_, _) =>
        {
            ValidateFolder();
        };

        var button = new SettingsButton();
        button.Text = Messages.Get(MessageId.I336);
        button.SetBounds(512, inputTop, 84, 42);
        Controls.Add(button);
        button.Click += (_, _) =>
        {
            using var picker = new FolderBrowserDialog();
            picker.Description = Messages.Get(title);
            picker.SelectedPath = Value;
            if (picker.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Value = picker.SelectedPath;
            }
        };
    }

    /// <summary>
    /// 入力されたパス
    /// </summary>
    public string Value
    {
        get => input.Text;
        set => input.Text = value;
    }

    /// <summary>
    /// 入力したフォルダの存在を確認して下線を更新する
    /// </summary>
    /// <returns>入力が有効の場合はtrue</returns>
    public bool ValidateFolder()
    {
        bool valid = !Enabled || !requireExistingFolder || Directory.Exists(Value);
        frame.IsInvalid = !valid;
        return valid;
    }

    /// <summary>
    /// パス入力欄へフォーカスを移す
    /// </summary>
    public void FocusInput()
    {
        input.Focus();
    }
}
