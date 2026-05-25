using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Snapix.UI;

namespace Snapix
{
    /// <summary>
    /// 截图完成后显示的浮动工具栏。
    /// 圆角磨砂深色卡片 + 矢量图标。
    /// </summary>
    internal sealed class AnnotationToolbar : Control
    {
        public event Action<ToolType> ToolSelected;
        public event Action<Color> ColorSelected;
        public event Action ConfirmClicked;
        public event Action CancelClicked;
        public event Action SaveClicked;
        public event Action UndoClicked;
        public event Action RedoClicked;

        private const int Pad = 8;
        private const int ButtonSize = 30;
        private const int SwatchSize = 22;
        private const int Gap = 2;
        private const int GroupGap = 10;
        private const int CornerRadius = 12;
        private const int Height_ = 46;

        private readonly List<IconButton> _toolButtons = new List<IconButton>();
        private readonly List<ColorSwatch> _swatches = new List<ColorSwatch>();

        public AnnotationToolbar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.Transparent;
            this.Height = Height_;
            BuildButtons();
        }

        private void BuildButtons()
        {
            int x = Pad;
            int y = (Height_ - ButtonSize) / 2;
            int swatchY = (Height_ - SwatchSize) / 2;

            // ========== 工具组 ==========
            x = AddTool(x, y, Icons.IconKind.Rectangle, ToolType.Rectangle, "矩形");
            x = AddTool(x, y, Icons.IconKind.Arrow, ToolType.Arrow, "箭头");
            x = AddTool(x, y, Icons.IconKind.Pen, ToolType.Pen, "画笔");
            x = AddTool(x, y, Icons.IconKind.Text, ToolType.Text, "文字");
            x = AddTool(x, y, Icons.IconKind.Mosaic, ToolType.Mosaic, "马赛克");

            x += GroupGap;

            // ========== 颜色组 ==========
            foreach (var c in Theme.PaletteColors)
            {
                var sw = new ColorSwatch(c) { Location = new Point(x, swatchY) };
                sw.Click += (s, e) =>
                {
                    foreach (var other in _swatches) other.SetActive(false);
                    sw.SetActive(true);
                    ColorSelected?.Invoke(c);
                };
                _swatches.Add(sw);
                this.Controls.Add(sw);
                x += SwatchSize + Gap;
            }
            // 默认第一个色板为选中
            if (_swatches.Count > 0) _swatches[0].SetActive(true);

            x += GroupGap;

            // ========== 撤销/重做 ==========
            x = AddAction(x, y, Icons.IconKind.Undo, () => UndoClicked?.Invoke(), "撤销 (Ctrl+Z)", null);
            x = AddAction(x, y, Icons.IconKind.Redo, () => RedoClicked?.Invoke(), "重做 (Ctrl+Y)", null);

            x += GroupGap;

            // ========== 操作组 ==========
            x = AddAction(x, y, Icons.IconKind.Save, () => SaveClicked?.Invoke(), "保存 (Ctrl+S)", null);
            x = AddAction(x, y, Icons.IconKind.Close, () => CancelClicked?.Invoke(), "取消 (Esc)", Theme.Danger);
            x = AddAction(x, y, Icons.IconKind.Check, () => ConfirmClicked?.Invoke(), "确认 (Enter)", Theme.Success);

            this.Width = x + Pad;
        }

        private int AddTool(int x, int y, Icons.IconKind icon, ToolType type, string tooltip)
        {
            var btn = new IconButton
            {
                Icon = icon,
                Tooltip = tooltip,
                Location = new Point(x, y),
                Size = new Size(ButtonSize, ButtonSize),
            };
            btn.Click += (s, e) =>
            {
                bool wasActive = btn.IsActive;
                foreach (var other in _toolButtons) other.SetActive(false);

                if (!wasActive)
                {
                    btn.SetActive(true);
                    ToolSelected?.Invoke(type);
                }
                else
                {
                    // 再次点击取消选中
                    ToolSelected?.Invoke(ToolType.None);
                }
            };
            _toolButtons.Add(btn);
            this.Controls.Add(btn);
            return x + ButtonSize + Gap;
        }

        private int AddAction(int x, int y, Icons.IconKind icon, Action action, string tooltip, Color? accentBg)
        {
            var btn = new IconButton
            {
                Icon = icon,
                Tooltip = tooltip,
                Location = new Point(x, y),
                Size = new Size(ButtonSize, ButtonSize),
                AccentBackground = accentBg,
            };
            btn.Click += (s, e) => action?.Invoke();
            this.Controls.Add(btn);
            return x + ButtonSize + Gap;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = new RectangleF(0, 0, Width, Height);

            // 背景圆角卡片
            using (var path = GraphicsHelpers.RoundRect(rect, CornerRadius))
            using (var brush = new SolidBrush(Theme.ToolbarBackground))
            {
                g.FillPath(brush, path);

                // 高光描边
                using (var pen = new Pen(Theme.ToolbarBorder, 1f))
                    g.DrawPath(pen, path);
            }

            // 分组分隔竖线（在工具组、颜色组、撤销组之间）
            DrawDividers(g);
        }

        private void DrawDividers(Graphics g)
        {
            // 找出每组最后一个控件的右边界，向右画一条竖线
            int[] dividerXs = ComputeDividerPositions();
            using (var pen = new Pen(Theme.Divider, 1f))
            {
                int yTop = 10;
                int yBottom = Height - 10;
                foreach (var x in dividerXs)
                {
                    g.DrawLine(pen, x, yTop, x, yBottom);
                }
            }
        }

        private int[] ComputeDividerPositions()
        {
            // 工具组结束 / 颜色组结束 / 撤销组结束
            // 工具按钮 5 个 + 间隙
            int x1 = Pad + (ButtonSize + Gap) * 5 - Gap + GroupGap / 2;
            int x2 = x1 + GroupGap / 2 + (SwatchSize + Gap) * Theme.PaletteColors.Length - Gap + GroupGap / 2;
            int x3 = x2 + GroupGap / 2 + (ButtonSize + Gap) * 2 - Gap + GroupGap / 2;
            return new[] { x1, x2, x3 };
        }
    }
}
