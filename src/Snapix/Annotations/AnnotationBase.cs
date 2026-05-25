using System.Drawing;

namespace Snapix
{
    /// <summary>
    /// 标注基类。所有坐标相对于选区左上角。
    /// </summary>
    internal abstract class AnnotationBase
    {
        public abstract void Update(Point currentPoint);
        public abstract void Draw(Graphics g, Point offset);
    }
}
