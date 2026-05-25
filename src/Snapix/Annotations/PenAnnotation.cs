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
