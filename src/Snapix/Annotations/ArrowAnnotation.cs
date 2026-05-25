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
