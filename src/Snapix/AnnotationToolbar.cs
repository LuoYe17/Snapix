using System;
using System.Drawing;
using System.Windows.Forms;

namespace Snapix
{
    /// <summary>
    /// 截图完成后显示的浮动工具栏。
    /// </summary>
    internal sealed class AnnotationToolbar : UserControl
    {
        public event Action<ToolType> ToolSelected;
        public event Action<Color> ColorSelected;
        #pragma warning disable CS0067
        public event Action<int> ThicknessSelected;
        #pragma warning restore CS0067
        public event Action ConfirmClicked;
        public event Action CancelClicked;
        public event Action SaveClicked;

        private ToolType _activeTool = ToolType.None;

        public AnnotationToolbar()
        {
            this.Size = new Size(380, 36);
            this.BackColor = Color.FromArgb(240, 30, 30, 30);
            this.Padding = new Padding(4);
            BuildButtons();
        }

        private void BuildButtons()
        {
            int x = 4;

            // 工具按钮
            x = AddToolButton(x, "▭", ToolType.Rectangle, "矩形");
            x = AddToolButton(x, "→", ToolType.Arrow, "箭头");
            x = AddToolButton(x, "✎", ToolType.Pen, "画笔");
            x = AddToolButton(x, "T", ToolType.Text, "文字");
            x = AddToolButton(x, "▦", ToolType.Mosaic, "马赛克");

            x += 8; // 分隔

            // 颜色选择
            x = AddColorButton(x, Color.Red);
            x = AddColorButton(x, Color.Yellow);
            x = AddColorButton(x, Color.Lime);
            x = AddColorButton(x, Color.DeepSkyBlue);
            x = AddColorButton(x, Color.White);

            x += 8;

            // 操作按钮
            x = AddActionButton(x, "✓", () => ConfirmClicked?.Invoke(), Color.FromArgb(0, 180, 0), "确认 (Enter)");
            x = AddActionButton(x, "✕", () => CancelClicked?.Invoke(), Color.FromArgb(200, 50, 50), "取消 (Esc)");
            x = AddActionButton(x, "💾", () => SaveClicked?.Invoke(), Color.FromArgb(80, 80, 80), "保存 (Ctrl+S)");
        }

        private int AddToolButton(int x, string text, ToolType tool, string tooltip)
        {
            var btn = CreateButton(text, x, tooltip);
            btn.Click += (s, e) =>
            {
                _activeTool = tool;
                ToolSelected?.Invoke(tool);
                HighlightActive(btn);
            };
            this.Controls.Add(btn);
            return x + btn.Width + 2;
        }

        private int AddColorButton(int x, Color color)
        {
            var btn = new Panel
            {
                Location = new Point(x, 8),
                Size = new Size(20, 20),
                BackColor = color,
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => ColorSelected?.Invoke(color);
            this.Controls.Add(btn);
            return x + 22;
        }

        private int AddActionButton(int x, string text, Action action, Color bgColor, string tooltip)
        {
            var btn = CreateButton(text, x, tooltip);
            btn.BackColor = bgColor;
            btn.Click += (s, e) => action?.Invoke();
            this.Controls.Add(btn);
            return x + btn.Width + 2;
        }

        private Button CreateButton(string text, int x, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 4),
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltip);

            return btn;
        }

        private void HighlightActive(Button activeBtn)
        {
            foreach (Control c in this.Controls)
            {
                if (c is Button b && b != activeBtn)
                    b.BackColor = Color.FromArgb(60, 60, 60);
            }
            activeBtn.BackColor = Color.FromArgb(0, 120, 215);
        }
    }
}
