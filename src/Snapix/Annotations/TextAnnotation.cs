using System.Drawing;

namespace Snapix
{
    internal sealed class TextAnnotation : AnnotationBase
    {
        private readonly Point _position;
        private readonly Color _color;
        private readonly string _text;
        private readonly float _fontSize;

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

        public override void Draw(Graphics g, Point offset)
        {
            if (string.IsNullOrEmpty(_text)) return;

            using (var font = new Font("Microsoft YaHei UI", _fontSize, GraphicsUnit.Point))
            using (var brush = new SolidBrush(_color))
            {
                g.DrawString(_text, font, brush, _position.X + offset.X, _position.Y + offset.Y);
            }
        }
    }
}
