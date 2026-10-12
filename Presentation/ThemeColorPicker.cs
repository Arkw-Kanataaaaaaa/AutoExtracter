using System.Drawing.Drawing2D;

namespace AutoExtracter;

/// <summary>
/// テーマを角丸の選択メニューで指定する
/// </summary>
internal sealed class ThemeColorPicker : Control
{
    private readonly string[] choices = new[]
    {
        Messages.Get(MessageId.I345), Messages.Get(MessageId.I346), Messages.Get(MessageId.I347)
    };

    private int selectedIndex;
    private ToolStripDropDown? popup;

    /// <summary>
    /// テーマの選択変更
    /// </summary>
    internal event EventHandler? SelectedIndexChanged;

    /// <summary>
    /// 描画に使用する配色
    /// </summary>
    internal ThemePalette Palette
    {
        get;
        set;
    } = ThemePalette.Dark;

    /// <summary>
    /// ライト、ダーク、システムの選択位置
    /// </summary>
    internal int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (value < 0 || value >= choices.Length || selectedIndex == value)
            {
                return;
            }

            selectedIndex = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// ちらつき防止とキーボード操作を有効にする
    /// </summary>
    internal ThemeColorPicker()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.ComboBox;
    }

    /// <summary>
    /// クリックで選択メニューを開く
    /// </summary>
    /// <param name="e">クリック情報</param>
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        OpenMenu();
    }

    /// <summary>
    /// Spaceと上下キーでメニューを開く
    /// </summary>
    /// <param name="e">キー入力情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
        {
            OpenMenu();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <summary>
    /// Enterでメニューを開く
    /// </summary>
    /// <param name="keyData">入力キー</param>
    /// <returns>処理済みの場合はtrue</returns>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter)
        {
            OpenMenu();
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    /// <summary>
    /// 角丸のメニューに三つの選択肢を表示する
    /// </summary>
    private void OpenMenu()
    {
        if (popup?.Visible == true)
        {
            return;
        }

        popup?.Dispose();
        var list = new ChoiceList(choices, SelectedIndex, Palette, Font, index =>
        {
            popup?.Close();
            SelectedIndex = index;
            Focus();
        });

        var host = new ToolStripControlHost(list);
        host.AutoSize = false;
        host.Size = list.Size;
        host.Padding = Padding.Empty;
        host.Margin = Padding.Empty;
        popup = new ToolStripDropDown();
        popup.Padding = Padding.Empty;
        popup.BackColor = Palette.WorkSurface;
        popup.Renderer = new BorderlessRenderer();
        popup.Items.Add(host);
        popup.Closed += (_, _) =>
        {
            Invalidate();
        };

        popup.Show(this, new Point(0, Height + 4));
        Invalidate();
        RoundedFrame.ApplyRegion(popup);
        list.Focus();
    }

    /// <summary>
    /// 選択値と下向きの記号を描画する
    /// </summary>
    /// <param name="e">描画情報</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedFrame.CreatePath(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 6);
        using var fill = new SolidBrush(Palette.WorkSurface);
        using var border = new Pen(Palette.Track);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        SettingsRowText.Draw(e.Graphics, choices[SelectedIndex], Font,
            new Rectangle(12, 0, Width - 44, Height), Palette.Text);
        using var arrow = new Pen(Palette.Text, 1.5f);
        float x = Width - 20;
        float y = Height / 2f;
        float direction = popup?.Visible == true ? -1 : 1;
        e.Graphics.DrawLines(arrow, new[]
        {
            new PointF(x - 4, y - 2 * direction), new PointF(x, y + 2 * direction), new PointF(x + 4, y - 2 * direction)
        });
    }

    /// <summary>
    /// メニューを解放する
    /// </summary>
    /// <param name="disposing">管理リソースを解放するか</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            popup?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 標準の四角いメニュー枠を表示しない
    /// </summary>
    private sealed class BorderlessRenderer : ToolStripProfessionalRenderer
    {
        /// <summary>
        /// 枠線の描画を省略する
        /// </summary>
        /// <param name="e">描画情報</param>
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }
    }

    /// <summary>
    /// 選択行を角丸の背景と左の縦線で示す一覧
    /// </summary>
    private sealed class ChoiceList : Control
    {
        private readonly string[] choices;
        private readonly ThemePalette palette;
        private readonly Action<int> choose;
        private readonly int selected;
        private int highlighted;

        /// <summary>
        /// 選択肢と初期位置を指定する
        /// </summary>
        /// <param name="choices">選択肢</param>
        /// <param name="selected">現在の選択位置</param>
        /// <param name="palette">配色</param>
        /// <param name="font">表示フォント</param>
        /// <param name="choose">選択時の処理</param>
        internal ChoiceList(string[] choices, int selected, ThemePalette palette, Font font, Action<int> choose)
        {
            this.choices = choices;
            this.selected = selected;
            highlighted = selected;
            this.palette = palette;
            this.choose = choose;
            Font = font;
            Size = new Size(220, 132);
            TabStop = true;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        }

        /// <summary>
        /// ポインターの行を強調する
        /// </summary>
        /// <param name="e">ポインター位置</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            highlighted = Math.Clamp((e.Y - 6) / 40, 0, choices.Length - 1);
            Invalidate();
        }

        /// <summary>
        /// 押した行を選択する
        /// </summary>
        /// <param name="e">クリック情報</param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                choose(Math.Clamp((e.Y - 6) / 40, 0, choices.Length - 1));
            }
        }

        /// <summary>
        /// メニュー内のキー操作を処理する
        /// </summary>
        /// <param name="keyData">入力キー</param>
        /// <returns>処理済みの場合はtrue</returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down)
            {
                highlighted = Math.Clamp(highlighted + (keyData == Keys.Up ? -1 : 1), 0, choices.Length - 1);
                Invalidate();
                return true;
            }

            if (keyData == Keys.Enter || keyData == Keys.Space)
            {
                choose(highlighted);
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        /// <summary>
        /// 背景、選択中の縦線、各行の文字を描画する
        /// </summary>
        /// <param name="e">描画情報</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(palette.WorkSurface);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int index = 0; index < choices.Length; index++)
            {
                var bounds = new Rectangle(6, 6 + index * 40, Width - 12, 38);
                if (index == selected || index == highlighted)
                {
                    using var path = RoundedFrame.CreatePath(bounds, 5);
                    using var fill = new SolidBrush(palette.Hover);
                    e.Graphics.FillPath(fill, path);
                }

                if (index == selected)
                {
                    using var marker = new Pen(palette.Text, 3);
                    marker.StartCap = LineCap.Round;
                    marker.EndCap = LineCap.Round;
                    e.Graphics.DrawLine(marker, 10, bounds.Top + 11, 10, bounds.Bottom - 11);
                }

                TextRenderer.DrawText(e.Graphics, choices[index], Font,
                    new Rectangle(20, bounds.Top, Width - 30, bounds.Height), palette.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
