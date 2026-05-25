using System;
using System.Drawing;

namespace Snapix
{
    internal sealed class MosaicAnnotation : AnnotationBase
    {
        private Point _start;
        private Point _end;
        private readonly Bitmap _source;
        private readonly Rectangle _selectionRect;
        private const int BlockSize = 10;

        public MosaicAnnotation(Point start, Bitmap source, Rectangle selectionRect)
        {
            _start = start;
            _end = start;
            _source = source;
            _selectionRect = selectionRect;
        }

        public override void Update(Point currentPoint)
        {
            _end = currentPoint;
        }

        public override void Draw(Graphics g, Point offset)
        {
            int x = Math.Min(_start.X, _end.X);
            int y = Math.Min(_start.Y, _end.Y);
            int w = Math.Abs(_start.X - _end.X);
            int h = Math.Abs(_start.Y - _end.Y);

            if (w < 1 || h < 1) return;

            // 对选区内的像素做马赛克
            for (int bx = x; bx < x + w; bx += BlockSize)
            {
                for (int by = y; by < y + h; by += BlockSize)
                {
                    int blockW = Math.Min(BlockSize, x + w - bx);
                    int blockH = Math.Min(BlockSize, y + h - by);

                    // 从原图取色（选区坐标 → 截屏坐标）
                    int srcX = _selectionRect.X + bx;
                    int srcY = _selectionRect.Y + by;

                    if (srcX >= 0 && srcX < _source.Width && srcY >= 0 && srcY < _source.Height)
                    {
                        var color = _source.GetPixel(srcX, srcY);
                        using (var brush = new SolidBrush(color))
                            g.FillRectangle(brush, bx + offset.X, by + offset.Y, blockW, blockH);
                    }
                }
            }
        }
    }
}
