using System.Drawing;

namespace Snapix
{
    internal sealed class TextAnnotation : AnnotationBase
    {
        private Point _position;
        private readonly Color _color;
        private readonly string _text;
        private readonly float _fontSize;
        private Size _measuredSize = Size.Empty;

        public TextAnnotation(Point position, Color color, string text, float fontSize = 16f)
        {
            _position = position;
            _color = color;
            _text = text ?? string.Empty;
            _fontSize = fontSize;
        }

        public override void Update(Point currentPoint)
        {
            // 文字标注不支持拖拽更新
        }

        public override Rectangle Bounds
        {
            get
            {
                if (_measuredSize == Size.Empty) MeasureLazy();
                return new Rectangle(_position.X - 2, _position.Y - 2,
                    _measuredSize.Width + 4, _measuredSize.Height + 4);
            }
        }

        public override void Translate(int dx, int dy)
        {
            _position = new Point(_position.X + dx, _position.Y + dy);
        }

        private void MeasureLazy()
        {
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font("Microsoft YaHei UI", _fontSize))
            {
                var sz = g.MeasureString(string.IsNullOrEmpty(_text) ? "M" : _text, font);
                _measuredSize = new Size((int)System.Math.Ceiling(sz.Width), (int)System.Math.Ceiling(sz.Height));
            }
        }

        public override void Draw(Graphics g, Point offset)
        {
            if (string.IsNullOrEmpty(_text)) return;

            using (var font = new Font("Microsoft YaHei UI", _fontSize))
            using (var brush = new SolidBrush(_color))
            {
                g.DrawString(_text, font, brush, _position.X + offset.X, _position.Y + offset.Y);
                if (_measuredSize == Size.Empty)
                {
                    var sz = g.MeasureString(_text, font);
                    _measuredSize = new Size((int)System.Math.Ceiling(sz.Width), (int)System.Math.Ceiling(sz.Height));
                }
            }
        }
    }
}
