using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Snapix.UI
{
    /// <summary>
    /// 线宽指示色块：不同直径的小圆点表示不同粗细。
    /// 视觉本身就是粗细的可视化，无需文字说明。
    /// </summary>
    internal sealed class ThicknessSwatch : Control
    {
        public int Thickness { get; }
        public bool IsActive { get; private set; }

        private bool _hover;

        public ThicknessSwatch(int thickness)
        {
            Thickness = thickness;
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

            // 选中态外圈白圈
            if (IsActive)
            {
                var ringRect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
                using (var pen = new Pen(Color.White, 1.5f))
                    g.DrawEllipse(pen, ringRect);
            }

            // 中心点：直径与 thickness 联动（线宽越大点越大）
            // thickness 2/4/6 → 直径 5/9/13
            float diameter = 1 + Thickness * 2;
            float pad = (Width - diameter) / 2f;
            var dotRect = new RectangleF(pad, pad, diameter, diameter);

            Color fill = _hover ? Theme.IconColorHover : Theme.IconColor;
            using (var brush = new SolidBrush(fill))
                g.FillEllipse(brush, dotRect);
        }
    }
}
