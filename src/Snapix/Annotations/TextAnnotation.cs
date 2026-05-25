using System.Drawing;
using System.Windows.Forms;

namespace Snapix
{
    internal sealed class TextAnnotation : AnnotationBase
    {
        private Point _position;
        private readonly Color _color;
        private string _text = string.Empty;

        public TextAnnotation(Point position, Color color)
        {
            _position = position;
            _color = color;
        }

        public override void Update(Point currentPoint)
        {
            // 文字标注不需要拖拽更新
        }

        public override void Draw(Graphics g, Point offset)
        {
            if (string.IsNullOrEmpty(_text)) return;

            using (var font = new Font("Microsoft YaHei", 14f))
            using (var brush = new SolidBrush(_color))
            {
                g.DrawString(_text, font, brush, _position.X + offset.X, _position.Y + offset.Y);
            }
        }

        public void SetText(string text)
        {
            _text = text;
        }
    }
}
