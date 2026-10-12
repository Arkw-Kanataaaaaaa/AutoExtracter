namespace AutoExtracter;

/// <summary>
/// 拡張子を丸い選択表示で複数選択する
/// </summary>
internal sealed class ArchiveExtensionSelector : UserControl
{
    private readonly RoundCheckBox[] options;
    private static readonly string[] Supported = new[]
    {
        ".zip", ".7z", ".rar"
    };

    /// <summary>
    /// 個別に切り替えられる拡張子の選択肢を配置する
    /// </summary>
    internal ArchiveExtensionSelector()
    {
        options = new RoundCheckBox[Supported.Length];
        for (int index = 0; index < options.Length; index++)
        {
            var option = new RoundCheckBox();
            option.Text = Supported[index];
            option.SetBounds(index * 80, 0, 80, 40);
            options[index] = option;
            Controls.Add(option);
        }
    }

    /// <summary>
    /// 入力領域に合わせて三つの選択肢を等間隔に配置する
    /// </summary>
    /// <param name="e">サイズ変更情報</param>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (options == null)
        {
            return;
        }

        int width = Width / options.Length;
        for (int index = 0; index < options.Length; index++)
        {
            options[index].SetBounds(index * width, 0, width, Height);
        }
    }

    /// <summary>
    /// 選択した拡張子の組合せ
    /// </summary>
    internal string[] Value
    {
        get
        {
            return options.Where(option =>
            {
                return option.Checked;
            }).Select(option =>
            {
                return option.Text;
            }).ToArray();
        }

        set
        {
            foreach (RoundCheckBox option in options)
            {
                option.Checked = value.Contains(option.Text);
            }
        }
    }

}
