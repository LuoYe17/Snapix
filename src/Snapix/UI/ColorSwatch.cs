using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Snapix.UI
{
    /// <summary>
    /// 圆形颜色色块。选中时外圈白边。
    /// </summary>
    internal sealed class ColorSwatch : Control
    {
        public Color SwatchColor { get; }
        public bool IsActive { get; set; }

        private bool _hover;

        public ColorSwatch(Color color)
        {
            SwatchColor = color;
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

            float pad = IsActive ? 3f : (_hover ? 2f : 2.5f);
            var dotRect = new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2);

            // 选中态外圈白圈
            if (IsActive)
            {
                var ringRect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
                using (var pen = new Pen(Color.White, 1.5f))
                    g.DrawEllipse(pen, ringRect);
            }

            using (var brush = new SolidBrush(SwatchColor))
                g.FillEllipse(brush, dotRect);
        }
    }
}
