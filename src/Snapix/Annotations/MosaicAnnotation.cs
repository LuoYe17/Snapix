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

        public override Rectangle Bounds
        {
            get
            {
                int x = Math.Min(_start.X, _end.X);
                int y = Math.Min(_start.Y, _end.Y);
                int w = Math.Abs(_start.X - _end.X);
                int h = Math.Abs(_start.Y - _end.Y);
                return new Rectangle(x, y, w, h);
            }
        }

        public override void Translate(int dx, int dy)
        {
            _start = new Point(_start.X + dx, _start.Y + dy);
            _end = new Point(_end.X + dx, _end.Y + dy);
        }

        public override void Draw(Graphics g, Point offset)
        {
            int x = Math.Min(_start.X, _end.X);
            int y = Math.Min(_start.Y, _end.Y);
            int w = Math.Abs(_start.X - _end.X);
            int h = Math.Abs(_start.Y - _end.Y);

            if (w < 1 || h < 1) return;

            // 对马赛克区域内的像素做马赛克。注意：马赛克的视觉位置是 (x,y)（已被 Translate 改过）
            // 但取色源仍来自原始截屏中的"未平移"位置。为了让平移后的马赛克持续覆盖新位置的内容，
            // 这里始终从原图当前可视位置取色（即 _selectionRect.X + x），以便看起来像"贴在那"。
            for (int bx = x; bx < x + w; bx += BlockSize)
            {
                for (int by = y; by < y + h; by += BlockSize)
                {
                    int blockW = Math.Min(BlockSize, x + w - bx);
                    int blockH = Math.Min(BlockSize, y + h - by);

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
