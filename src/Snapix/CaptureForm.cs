using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Snapix.Native;

namespace Snapix
{
    /// <summary>
    /// 全屏遮罩窗口：负责框选区域、智能窗口吸附、标注、输出。
    /// </summary>
    internal sealed class CaptureForm : Form
    {
        // 截屏底图
        private Bitmap _screenshot;
        private Rectangle _virtualBounds;

        // 框选状态
        private bool _isSelecting;
        private bool _selectionDone;
        private Point _startPoint;
        private Point _endPoint;
        private Rectangle _selectionRect;

        // 窗口吸附
        private List<Rectangle> _windowRects;
        private Rectangle _hoveredWindow = Rectangle.Empty;

        // 标注
        private readonly List<AnnotationBase> _annotations = new List<AnnotationBase>();
        private readonly List<AnnotationBase> _redoStack = new List<AnnotationBase>();
        private AnnotationBase _currentAnnotation;
        private ToolType _currentTool = ToolType.None;
        private Color _currentColor = Color.Red;
        private int _currentThickness = 2;

        // 工具栏
        private AnnotationToolbar _toolbar;

        public CaptureForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Cursor = Cursors.Cross;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResumeLayout(false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 捕获全屏
            _virtualBounds = ScreenCapture.GetVirtualScreenBounds();
            _screenshot = ScreenCapture.CaptureAllScreens();

            // 覆盖整个虚拟桌面
            this.Location = new Point(_virtualBounds.Left, _virtualBounds.Top);
            this.Size = new Size(_virtualBounds.Width, _virtualBounds.Height);

            // 获取窗口列表用于吸附（排除自己 + 全屏窗口）
            _windowRects = WindowFinder.GetVisibleWindowRects(this.Handle, _virtualBounds);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制截屏底图
            if (_screenshot != null)
                g.DrawImage(_screenshot, 0, 0);

            // 暗色遮罩
            using (var overlay = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                if (_selectionRect.Width > 0 && _selectionRect.Height > 0)
                {
                    // 遮罩选区外的区域
                    var region = new Region(new Rectangle(0, 0, Width, Height));
                    region.Exclude(_selectionRect);
                    g.FillRegion(overlay, region);
                    region.Dispose();

                    // 选区边框
                    using (var pen = new Pen(Color.FromArgb(0, 174, 255), 1))
                        g.DrawRectangle(pen, _selectionRect);

                    // 尺寸提示
                    DrawSizeInfo(g);
                }
                else if (!_isSelecting)
                {
                    // 未开始选择时全屏遮罩
                    g.FillRectangle(overlay, 0, 0, Width, Height);

                    // 窗口吸附高亮
                    if (_hoveredWindow != Rectangle.Empty)
                    {
                        var localRect = ScreenToLocal(_hoveredWindow);
                        using (var pen = new Pen(Color.FromArgb(0, 174, 255), 2))
                            g.DrawRectangle(pen, localRect);
                    }
                }
                else
                {
                    g.FillRectangle(overlay, 0, 0, Width, Height);
                }
            }

            // 绘制已有标注
            foreach (var ann in _annotations)
                ann.Draw(g, _selectionRect.Location);

            // 绘制当前正在画的标注
            _currentAnnotation?.Draw(g, _selectionRect.Location);
        }

        private void DrawSizeInfo(Graphics g)
        {
            string text = $"{_selectionRect.Width} × {_selectionRect.Height}";
            using (var font = new Font("Segoe UI", 9f))
            using (var brush = new SolidBrush(Color.White))
            using (var bg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                var size = g.MeasureString(text, font);
                var pt = new PointF(_selectionRect.Left, _selectionRect.Top - size.Height - 4);
                if (pt.Y < 0) pt.Y = _selectionRect.Top + 4;

                g.FillRectangle(bg, pt.X, pt.Y, size.Width + 8, size.Height + 2);
                g.DrawString(text, font, brush, pt.X + 4, pt.Y + 1);
            }
        }

        #region Mouse Events

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (!_selectionDone)
            {
                // 总是先进入手动框选模式；MouseUp 时若几乎没拖动再回退到窗口吸附
                _isSelecting = true;
                _startPoint = e.Location;
                _endPoint = e.Location;
            }
            else if (_currentTool != ToolType.None)
            {
                // 开始标注
                var relativePoint = new Point(e.X - _selectionRect.X, e.Y - _selectionRect.Y);
                _currentAnnotation = CreateAnnotation(_currentTool, relativePoint);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isSelecting)
            {
                _endPoint = e.Location;
                _selectionRect = GetRectangle(_startPoint, _endPoint);
                Invalidate();
            }
            else if (!_selectionDone)
            {
                // 窗口吸附检测
                var screenPt = PointToScreen(e.Location);
                var found = Rectangle.Empty;
                foreach (var wr in _windowRects)
                {
                    if (wr.Contains(screenPt))
                    {
                        // 选最小的包含鼠标的窗口
                        if (found == Rectangle.Empty || (wr.Width * wr.Height < found.Width * found.Height))
                            found = wr;
                    }
                }

                if (found != _hoveredWindow)
                {
                    _hoveredWindow = found;
                    Invalidate();
                }
            }
            else if (_currentAnnotation != null)
            {
                var relativePoint = new Point(e.X - _selectionRect.X, e.Y - _selectionRect.Y);
                _currentAnnotation.Update(relativePoint);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            if (_isSelecting)
            {
                _isSelecting = false;
                _selectionRect = GetRectangle(_startPoint, _endPoint);

                // 拖动距离过小视为单击：回退到窗口吸附
                int dx = Math.Abs(_endPoint.X - _startPoint.X);
                int dy = Math.Abs(_endPoint.Y - _startPoint.Y);
                if (dx < 4 && dy < 4 && _hoveredWindow != Rectangle.Empty)
                {
                    _selectionRect = ScreenToLocal(_hoveredWindow);
                }

                if (_selectionRect.Width > 3 && _selectionRect.Height > 3)
                {
                    _selectionDone = true;
                    ShowToolbar();
                }

                Invalidate();
            }
            else if (_currentAnnotation != null)
            {
                _annotations.Add(_currentAnnotation);
                _redoStack.Clear();
                _currentAnnotation = null;
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (_selectionDone && _currentTool == ToolType.None)
            {
                ConfirmCapture();
            }
        }

        #endregion

        #region Keyboard

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
            {
                CancelCapture();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmCapture();
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopyToClipboard();
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveToFile();
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                Redo();
            }
        }

        #endregion

        #region Actions

        private void ConfirmCapture()
        {
            if (!_selectionDone) return;
            CopyToClipboard();
            Close();
        }

        private void CancelCapture()
        {
            Close();
        }

        private void CopyToClipboard()
        {
            if (!_selectionDone) return;
            using (var result = RenderResult())
            {
                Clipboard.SetImage(result);
            }
        }

        private void SaveToFile()
        {
            if (!_selectionDone) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp";
                dlg.DefaultExt = "png";
                dlg.FileName = $"Snapix_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (var result = RenderResult())
                    {
                        var format = ImageFormat.Png;
                        if (dlg.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                            format = ImageFormat.Jpeg;
                        else if (dlg.FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                            format = ImageFormat.Bmp;

                        result.Save(dlg.FileName, format);
                    }
                    Close();
                }
            }
        }

        private Bitmap RenderResult()
        {
            var bmp = new Bitmap(_selectionRect.Width, _selectionRect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 从截屏底图裁剪选区
                g.DrawImage(_screenshot,
                    new Rectangle(0, 0, _selectionRect.Width, _selectionRect.Height),
                    _selectionRect, GraphicsUnit.Pixel);

                // 绘制标注
                foreach (var ann in _annotations)
                    ann.Draw(g, Point.Empty);
            }
            return bmp;
        }

        private void Undo()
        {
            if (_annotations.Count > 0)
            {
                var last = _annotations[_annotations.Count - 1];
                _annotations.RemoveAt(_annotations.Count - 1);
                _redoStack.Add(last);
                Invalidate();
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var last = _redoStack[_redoStack.Count - 1];
                _redoStack.RemoveAt(_redoStack.Count - 1);
                _annotations.Add(last);
                Invalidate();
            }
        }

        #endregion

        #region Toolbar

        private void ShowToolbar()
        {
            if (_toolbar != null) return;

            _toolbar = new AnnotationToolbar();
            _toolbar.ToolSelected += (tool) =>
            {
                _currentTool = tool;
                this.Cursor = tool == ToolType.None ? Cursors.Cross : Cursors.Default;
            };
            _toolbar.ColorSelected += (color) => _currentColor = color;
            _toolbar.ThicknessSelected += (t) => _currentThickness = t;
            _toolbar.ConfirmClicked += () => ConfirmCapture();
            _toolbar.CancelClicked += () => CancelCapture();
            _toolbar.SaveClicked += () => SaveToFile();

            // 定位在选区下方，超出则放上方，仍超出则贴在选区内右下角
            int x = _selectionRect.Left;
            int y = _selectionRect.Bottom + 8;
            if (y + _toolbar.Height > Height)
                y = _selectionRect.Top - _toolbar.Height - 8;
            if (y < 0)
                y = Math.Max(0, _selectionRect.Bottom - _toolbar.Height - 8);

            // x 也要夹回屏幕内
            if (x + _toolbar.Width > Width)
                x = Math.Max(0, Width - _toolbar.Width - 4);
            if (x < 0) x = 4;

            _toolbar.Location = new Point(x, y);
            this.Controls.Add(_toolbar);
            _toolbar.BringToFront();
        }

        #endregion

        #region Helpers

        private Rectangle GetRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p1.X - p2.X);
            int h = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, w, h);
        }

        private Rectangle ScreenToLocal(Rectangle screenRect)
        {
            return new Rectangle(
                screenRect.X - _virtualBounds.X,
                screenRect.Y - _virtualBounds.Y,
                screenRect.Width,
                screenRect.Height);
        }

        private AnnotationBase CreateAnnotation(ToolType tool, Point start)
        {
            switch (tool)
            {
                case ToolType.Rectangle:
                    return new RectangleAnnotation(start, _currentColor, _currentThickness);
                case ToolType.Arrow:
                    return new ArrowAnnotation(start, _currentColor, _currentThickness);
                case ToolType.Pen:
                    return new PenAnnotation(start, _currentColor, _currentThickness);
                case ToolType.Mosaic:
                    return new MosaicAnnotation(start, _screenshot, _selectionRect);
                case ToolType.Text:
                    return new TextAnnotation(start, _currentColor);
                default:
                    return null;
            }
        }

        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _screenshot?.Dispose();
            _toolbar?.Dispose();
        }
    }
}
