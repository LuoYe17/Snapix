using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Snapix
{
    internal sealed class ArrowAnnotation : AnnotationBase
    {
        private Point _start;
        private Point _end;
        private readonly Color _color;
        private readonly int _thickness;

        public ArrowAnnotation(Point start, Color color, int thickness)
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
                int pad = _thickness + 6;
                return new Rectangle(x - pad, y - pad, w + pad * 2, h + pad * 2);
            }
        }

        public override void Translate(int dx, int dy)
        {
            _start = new Point(_start.X + dx, _start.Y + dy);
            _end = new Point(_end.X + dx, _end.Y + dy);
        }

        public override bool HitTest(Point p)
        {
            // 点到线段的距离
            return DistanceToSegment(p, _start, _end) <= _thickness + 5;
        }

        private static double DistanceToSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001) return Distance(p, a);
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));
            double px = a.X + t * dx;
            double py = a.Y + t * dy;
            double ddx = p.X - px;
            double ddy = p.Y - py;
            return Math.Sqrt(ddx * ddx + ddy * ddy);
        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override void Draw(Graphics g, Point offset)
        {
            var p1 = new Point(_start.X + offset.X, _start.Y + offset.Y);
            var p2 = new Point(_end.X + offset.X, _end.Y + offset.Y);

            using (var pen = new Pen(_color, _thickness))
            {
                pen.CustomEndCap = new AdjustableArrowCap(4, 4);
                g.DrawLine(pen, p1, p2);
            }
        }
    }
}
