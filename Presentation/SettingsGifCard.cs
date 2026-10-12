namespace AutoExtracter;

/// <summary>
/// プレビューのクリックとキーボード操作でGIFを選択する
/// </summary>
internal sealed class SettingsGifCard : UserControl
{
    private readonly string resource;
    private readonly MessageId title;
    private readonly SettingsButton selector = new();
    private AnimatedGifView? preview;
    private string value = string.Empty;
    private bool animationEnabled = true;

    /// <summary>
    /// プレビューのアニメーションを有効にするか
    /// </summary>
    internal bool AnimationEnabled
    {
        get => animationEnabled;
        set
        {
            animationEnabled = value;
            if (preview != null)
            {
                preview.AnimationEnabled = value;
            }
        }
    }

    /// <summary>
    /// プレビューとその下の状態名を配置する
    /// </summary>
    /// <param name="title">状態名</param>
    /// <param name="value">現在のGIF</param>
    /// <param name="resource">同梱GIFの名前</param>
    /// <param name="width">プレビュー領域の幅</param>
    internal SettingsGifCard(MessageId title, string value, string resource, int width = 286)
    {
        this.title = title;
        this.resource = resource;
        Size = new Size(width, 208);
        selector.SetBounds(0, 0, width, 164);
        selector.BorderSize = 0;
        selector.AccessibleName = Messages.Get(title);
        selector.Cursor = Cursors.Hand;
        selector.Click += (_, _) =>
        {
            SelectFile();
        };

        selector.MouseEnter += (_, _) =>
        {
            selector.BorderSize = 1;
            selector.Invalidate();
        };

        selector.MouseLeave += (_, _) =>
        {
            selector.BorderSize = selector.ContainsFocus ? 1 : 0;
            selector.Invalidate();
        };

        selector.GotFocus += (_, _) =>
        {
            selector.BorderSize = 1;
            selector.Invalidate();
        };

        selector.LostFocus += (_, _) =>
        {
            selector.BorderSize = 0;
            selector.Invalidate();
        };

        Controls.Add(selector);
        var label = new Label();
        label.Text = Messages.Get(title);
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.SetBounds(0, 176, width, 28);
        Controls.Add(label);
        Value = value;
    }

    /// <summary>
    /// 選択したGIFのパス、空欄は同梱画像
    /// </summary>
    public string Value
    {
        get => value;
        set
        {
            var next = new AnimatedGifView(value, null, resource);
            next.AnimationEnabled = animationEnabled;
            next.SetBounds((Width - 128) / 2, 16, 128, 128);
            next.Cursor = Cursors.Hand;
            next.Click += (_, _) =>
            {
                selector.Focus();
                SelectFile();
            };

            next.MouseEnter += (_, _) =>
            {
                selector.BorderSize = 1;
                selector.Invalidate();
            };

            next.MouseLeave += (_, _) =>
            {
                bool inside = selector.ClientRectangle.Contains(selector.PointToClient(Cursor.Position));
                selector.BorderSize = inside || selector.ContainsFocus ? 1 : 0;
                selector.Invalidate();
            };

            if (preview != null)
            {
                selector.Controls.Remove(preview);
                preview.Dispose();
            }

            this.value = value;
            preview = next;
            selector.Controls.Add(next);
        }
    }

    /// <summary>
    /// GIFのファイル選択ダイアログを開く
    /// </summary>
    private void SelectFile()
    {
        using var picker = new OpenFileDialog();
        picker.Filter = Messages.Get(MessageId.I373);
        picker.Title = Messages.Get(title);
        if (picker.ShowDialog(FindForm()) == DialogResult.OK)
        {
            Value = picker.FileName;
        }
    }
}
