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
    /// 独立的无边框 TopMost 小窗口，避免移动时触发父遮罩重绘。
    /// </summary>
    internal sealed class AnnotationToolbar : Form
    {
        public event Action<ToolType> ToolSelected;
        public event Action<Color> ColorSelected;
        public event Action<int> ThicknessSelected;
        public event Action<float> FontSizeSelected;
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
        private readonly List<ThicknessSwatch> _thicknessSwatches = new List<ThicknessSwatch>();
        private readonly List<FontSizeSwatch> _fontSizeSwatches = new List<FontSizeSwatch>();
        private readonly ToolTip _tooltip = new ToolTip
        {
            InitialDelay = 400,
            ReshowDelay = 100,
            AutoPopDelay = 5000,
            ShowAlways = true,
        };

        public AnnotationToolbar()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.AutoScaleMode = AutoScaleMode.None;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(40, 40, 42); // 实色背景，圆角通过 Region 裁剪
            this.Height = Height_;

            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            BuildButtons();
            UpdateRegion();
        }

        protected override bool ShowWithoutActivation => true; // 显示时不抢焦点

        /// <summary>外部切换工具时，让线宽 swatch 同步到该工具的偏好值。</summary>
        public void SetThicknessVisual(int thickness)
        {
            foreach (var sw in _thicknessSwatches)
                sw.SetActive(sw.Thickness == thickness);
        }

        /// <summary>外部同步当前字号档位选中态。</summary>
        public void SetFontSizeVisual(float size)
        {
            foreach (var sw in _fontSizeSwatches)
                sw.SetActive(sw.FontSize == size);
        }

        /// <summary>切换"线宽组 / 字号组"互斥占位显示。文字工具时显示字号组。</summary>
        public void SetFontSizeGroupVisible(bool visible)
        {
            foreach (var sw in _thicknessSwatches) sw.Visible = !visible;
            foreach (var sw in _fontSizeSwatches) sw.Visible = visible;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_NOACTIVATE：永远不获得焦点，避免点工具栏时主遮罩失焦
                cp.ExStyle |= 0x08000000;
                // WS_EX_TOOLWINDOW：不出现在 alt-tab
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        private void UpdateRegion()
        {
            using (var path = GraphicsHelpers.RoundRect(new RectangleF(0, 0, Width, Height), CornerRadius))
                this.Region = new Region(path);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
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

            // ========== 线宽组 ==========
            int[] thicknesses = { 2, 4, 6 };
            string[] thicknessNames = { "细", "中", "粗" };
            int thicknessGroupX = x;
            for (int i = 0; i < thicknesses.Length; i++)
            {
                var t = thicknesses[i];
                var sw = new ThicknessSwatch(t) { Location = new Point(x, swatchY) };
                sw.Click += (s, e) =>
                {
                    foreach (var other in _thicknessSwatches) other.SetActive(false);
                    sw.SetActive(true);
                    ThicknessSelected?.Invoke(t);
                };
                _thicknessSwatches.Add(sw);
                this.Controls.Add(sw);
                _tooltip.SetToolTip(sw, "线宽：" + thicknessNames[i]);
                x += SwatchSize + Gap;
            }
            // 默认中等粗细
            if (_thicknessSwatches.Count >= 2) _thicknessSwatches[1].SetActive(true);

            // ========== 字号组（与线宽组互斥占位，文字工具时显示）==========
            float[] fontSizes = { 12f, 18f, 28f };
            float[] fontEms = { 11f, 15f, 19f }; // 视觉档差，9/13/17 像素 ≈ em 11/15/19
            string[] fontNames = { "小", "中", "大" };
            int fx = thicknessGroupX;
            for (int i = 0; i < fontSizes.Length; i++)
            {
                var size = fontSizes[i];
                var sw = new FontSizeSwatch(size, fontEms[i])
                {
                    Location = new Point(fx, swatchY),
                    Visible = false,
                };
                sw.Click += (s, e) =>
                {
                    foreach (var other in _fontSizeSwatches) other.SetActive(false);
                    sw.SetActive(true);
                    FontSizeSelected?.Invoke(size);
                };
                _fontSizeSwatches.Add(sw);
                this.Controls.Add(sw);
                _tooltip.SetToolTip(sw, "字号：" + fontNames[i]);
                fx += SwatchSize + Gap;
            }
            // 默认中等字号
            if (_fontSizeSwatches.Count >= 2) _fontSizeSwatches[1].SetActive(true);

            x += GroupGap;

            // ========== 颜色组 ==========
            string[] colorNames = { "红色", "黄色", "绿色", "蓝色", "白色" };
            for (int i = 0; i < Theme.PaletteColors.Length; i++)
            {
                var c = Theme.PaletteColors[i];
                var sw = new ColorSwatch(c) { Location = new Point(x, swatchY) };
                sw.Click += (s, e) =>
                {
                    foreach (var other in _swatches) other.SetActive(false);
                    sw.SetActive(true);
                    ColorSelected?.Invoke(c);
                };
                _swatches.Add(sw);
                this.Controls.Add(sw);
                if (i < colorNames.Length) _tooltip.SetToolTip(sw, colorNames[i]);
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
            _tooltip.SetToolTip(btn, tooltip);
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
            _tooltip.SetToolTip(btn, tooltip);
            return x + ButtonSize + Gap;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 背景已经由 BackColor + Region 圆角裁剪完成，这里只画分隔竖线
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
            // 工具(5) | 线宽(3) | 颜色(5) | 撤销/重做(2) | 操作(3)
            int x1 = Pad + (ButtonSize + Gap) * 5 - Gap + GroupGap / 2;
            int x2 = x1 + GroupGap / 2 + (SwatchSize + Gap) * 3 - Gap + GroupGap / 2;
            int x3 = x2 + GroupGap / 2 + (SwatchSize + Gap) * Theme.PaletteColors.Length - Gap + GroupGap / 2;
            int x4 = x3 + GroupGap / 2 + (ButtonSize + Gap) * 2 - Gap + GroupGap / 2;
            return new[] { x1, x2, x3, x4 };
        }
    }
}
