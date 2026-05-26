using System;
using System.Drawing;

namespace Snapix
{
    internal sealed class RectangleAnnotation : AnnotationBase
    {
        private Point _start;
        private Point _end;
        private readonly Color _color;
        private readonly int _thickness;

        public RectangleAnnotation(Point start, Color color, int thickness)
        {
            _start = start;
            _end = start;
            _color = color;
            _thickness = thickness;
        }

        public override void Update(Point currentPoint)
        {
            _end = currentPoint;
        }

        public override Rectangle Bounds
        {
            get
            {
                int x = Math.Min(_start.X, _end.X);
                int y = Math.Min(_start.Y, _end.Y);
                int w = Math.Abs(_start.X - _end.X);
                int h = Math.Abs(_start.Y - _end.Y);
                return new Rectangle(x - _thickness, y - _thickness, w + _thickness * 2, h + _thickness * 2);
            }
        }

        public override void Translate(int dx, int dy)
        {
            _start = new Point(_start.X + dx, _start.Y + dy);
            _end = new Point(_end.X + dx, _end.Y + dy);
        }

        public override bool HitTest(Point p)
        {
            // 矩形为空心，命中测试仅检查靠近边框的区域，避免点中间穿透到下层
            if (!Bounds.Contains(p)) return false;
            int x = Math.Min(_start.X, _end.X);
            int y = Math.Min(_start.Y, _end.Y);
            int w = Math.Abs(_start.X - _end.X);
            int h = Math.Abs(_start.Y - _end.Y);
            int tol = _thickness + 4;
            // 在边框附近
            return (p.X >= x - tol && p.X <= x + tol)
                || (p.X >= x + w - tol && p.X <= x + w + tol)
                || (p.Y >= y - tol && p.Y <= y + tol)
                || (p.Y >= y + h - tol && p.Y <= y + h + tol);
        }

        public override void Draw(Graphics g, Point offset)
        {
            int x = Math.Min(_start.X, _end.X) + offset.X;
            int y = Math.Min(_start.Y, _end.Y) + offset.Y;
            int w = Math.Abs(_start.X - _end.X);
            int h = Math.Abs(_start.Y - _end.Y);

            if (w < 1 || h < 1) return;

            using (var pen = new Pen(_color, _thickness))
                g.DrawRectangle(pen, x, y, w, h);
        }
    }
}
