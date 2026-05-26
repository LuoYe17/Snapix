using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Snapix
{
    internal sealed class PenAnnotation : AnnotationBase
    {
        private readonly List<Point> _points = new List<Point>();
        private readonly Color _color;
        private readonly int _thickness;

        public PenAnnotation(Point start, Color color, int thickness)
        {
            _points.Add(start);
            _color = color;
            _thickness = thickness;
        }

        public override void Update(Point currentPoint)
        {
            _points.Add(currentPoint);
        }

        public override Rectangle Bounds
        {
            get
            {
                if (_points.Count == 0) return Rectangle.Empty;
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var p in _points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
                int pad = _thickness + 4;
                return new Rectangle(minX - pad, minY - pad, maxX - minX + pad * 2, maxY - minY + pad * 2);
            }
        }

        public override void Translate(int dx, int dy)
        {
            for (int i = 0; i < _points.Count; i++)
                _points[i] = new Point(_points[i].X + dx, _points[i].Y + dy);
        }

        public override bool HitTest(Point p)
        {
            if (!Bounds.Contains(p)) return false;
            // 点到任意线段距离
            double tol = _thickness + 5;
            for (int i = 1; i < _points.Count; i++)
            {
                if (DistanceToSegment(p, _points[i - 1], _points[i]) <= tol) return true;
            }
            return false;
        }

        private static double DistanceToSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001) { dx = p.X - a.X; dy = p.Y - a.Y; return Math.Sqrt(dx * dx + dy * dy); }
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));
            double px = a.X + t * dx, py = a.Y + t * dy;
            double ddx = p.X - px, ddy = p.Y - py;
            return Math.Sqrt(ddx * ddx + ddy * ddy);
        }

        public override void Draw(Graphics g, Point offset)
        {
            if (_points.Count < 2) return;

            using (var pen = new Pen(_color, _thickness))
            {
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                for (int i = 1; i < _points.Count; i++)
                {
                    var p1 = new Point(_points[i - 1].X + offset.X, _points[i - 1].Y + offset.Y);
                    var p2 = new Point(_points[i].X + offset.X, _points[i].Y + offset.Y);
                    g.DrawLine(pen, p1, p2);
                }
            }
        }
    }
}
