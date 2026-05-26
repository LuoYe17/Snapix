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

        /// <summary>外接矩形（局部坐标，用于命中测试）。</summary>
        public abstract Rectangle Bounds { get; }

        /// <summary>整体平移。</summary>
        public abstract void Translate(int dx, int dy);

        /// <summary>命中测试，默认实现：在 Bounds 内即命中。子类可重写。</summary>
        public virtual bool HitTest(Point p) => Bounds.Contains(p);
    }
}
