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
