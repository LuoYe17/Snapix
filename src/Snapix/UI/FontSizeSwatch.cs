using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Snapix.UI
{
    /// <summary>
    /// 字号档位指示色块：不同大小的字母 "T" 表示不同字号档位。
    /// 视觉本身就是字号的可视化（小/中/大），无需文字说明。
    /// </summary>
    internal sealed class FontSizeSwatch : Control
    {
        public float FontSize { get; }
        public bool IsActive { get; private set; }

        // 字母 T 的绘制 em 像素（≈视觉高度 / 0.72）；字母实际高度大约为该值的 72%。
        private readonly float _emPixels;

        private bool _hover;

        public FontSizeSwatch(float fontSize, float emPixels)
        {
            FontSize = fontSize;
            _emPixels = emPixels;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(22, 22);
            this.TabStop = false;
        }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // 选中态外圈白圈（与线宽/颜色 swatch 一致）
            if (IsActive)
                using (var pen = new Pen(Color.White, 1.5f))
                    g.DrawEllipse(pen, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1));

            Color fill = _hover ? Theme.IconColorHover : Theme.IconColor;
            using (var font = new Font(Theme.FontFamilyFallback, _emPixels, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(fill))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString("T", font, brush, new RectangleF(0, 0, Width, Height), sf);
        }
    }
}
