using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Snapix.Native;
using Snapix.UI;

namespace Snapix
{
    /// <summary>
    /// 全屏遮罩窗口：负责框选区域、智能窗口吸附、标注、输出。
    /// </summary>
    internal sealed class CaptureForm : Form
    {
        // 截屏底图
        private Bitmap _screenshot;
        private Bitmap _dimmedScreenshot; // 预合成的暗化底图（性能优化）
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
        private Color _currentColor = Color.FromArgb(255, 255, 69, 58); // 与色板首项一致

        // 每个工具独立记忆线宽
        private readonly Dictionary<ToolType, int> _toolThickness = new Dictionary<ToolType, int>
        {
            { ToolType.Rectangle, 4 },
            { ToolType.Arrow,     6 }, // 箭头默认最粗
            { ToolType.Pen,       4 },
            { ToolType.Mosaic,    4 },
        };
        private int CurrentThickness => _toolThickness.TryGetValue(_currentTool, out var t) ? t : 4;
        private const float TextFontSize = 16f;

        // 文字编辑器
        private TextBox _textEditor;
        private Point _textEditorOrigin; // 选区局部坐标

        // 选区调整 / 移动
        private enum ResizeHandle
        {
            None,
            TopLeft, Top, TopRight,
            Left, Right,
            BottomLeft, Bottom, BottomRight,
            Move, // 拖动整体
        }
        private ResizeHandle _activeHandle = ResizeHandle.None;
        private Point _dragStartPoint;
        private Rectangle _dragStartRect;
        private const int HandleSize = 8;
        private const int HandleHitPadding = 4;

        // 标注拖拽
        private AnnotationBase _draggingAnnotation;
        private Point _annotationDragStart;

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

            // 预合成"暗化版底图"，避免每次重绘都做昂贵的 Region.Exclude + FillRegion
            _dimmedScreenshot = new Bitmap(_screenshot.Width, _screenshot.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(_dimmedScreenshot))
            {
                g.DrawImage(_screenshot, 0, 0);
                using (var dim = new SolidBrush(Theme.OverlayDim))
                    g.FillRectangle(dim, 0, 0, _dimmedScreenshot.Width, _dimmedScreenshot.Height);
            }

            // 覆盖整个虚拟桌面
            this.Location = new Point(_virtualBounds.Left, _virtualBounds.Top);
            this.Size = new Size(_virtualBounds.Width, _virtualBounds.Height);

            // 获取窗口列表用于吸附（排除自己 + 全屏窗口）
            _windowRects = WindowFinder.GetVisibleWindowRects(this.Handle, _virtualBounds);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            // 关键：关闭抗锯齿和插值，截图重绘根本不需要平滑——大幅提升大图绘制速度
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode.SourceCopy; // 不需要 alpha blend

            var clip = e.ClipRectangle;

            // 用预合成的"暗化底图"做背景（一次 DrawImage，不再 Region.Exclude）
            if (_dimmedScreenshot != null)
                g.DrawImage(_dimmedScreenshot, clip, clip, GraphicsUnit.Pixel);

            g.CompositingMode = CompositingMode.SourceOver;

            // 选区内贴回原图（亮的）
            if (_selectionDone || _isSelecting)
            {
                if (_selectionRect.Width > 0 && _selectionRect.Height > 0)
                {
                    // 仅绘制选区与 clip 的交集部分
                    var visible = Rectangle.Intersect(_selectionRect, clip);
                    if (visible.Width > 0 && visible.Height > 0 && _screenshot != null)
                    {
                        g.DrawImage(_screenshot, visible, visible, GraphicsUnit.Pixel);
                    }
                }
            }

            // 仅在绘制装饰元素时再开抗锯齿
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                // 选区边框
                using (var pen = new Pen(Theme.SelectionBorder, 1.5f))
                    g.DrawRectangle(pen, _selectionRect);

                // 选中后绘制 8 个手柄（仅当未激活标注工具时显示，避免标注时干扰）
                if (_selectionDone && _currentTool == ToolType.None)
                    DrawHandles(g);

                // 尺寸提示
                DrawSizeInfo(g);
            }
            else if (!_isSelecting && _hoveredWindow != Rectangle.Empty)
            {
                // 窗口吸附高亮
                var localRect = ScreenToLocal(_hoveredWindow);
                using (var pen = new Pen(Theme.SelectionBorder, 2f))
                    g.DrawRectangle(pen, localRect);
            }

            // 绘制已有标注
            foreach (var ann in _annotations)
                ann.Draw(g, _selectionRect.Location);

            // 绘制当前正在画的标注
            _currentAnnotation?.Draw(g, _selectionRect.Location);
        }

        private void DrawHandles(Graphics g)
        {
            var r = _selectionRect;
            var pts = new[]
            {
                new Point(r.Left, r.Top),
                new Point(r.Left + r.Width / 2, r.Top),
                new Point(r.Right, r.Top),
                new Point(r.Left, r.Top + r.Height / 2),
                new Point(r.Right, r.Top + r.Height / 2),
                new Point(r.Left, r.Bottom),
                new Point(r.Left + r.Width / 2, r.Bottom),
                new Point(r.Right, r.Bottom),
            };

            int s = HandleSize;
            using (var fill = new SolidBrush(Color.White))
            using (var border = new Pen(Theme.SelectionBorder, 1.2f))
            {
                foreach (var p in pts)
                {
                    var rect = new Rectangle(p.X - s / 2, p.Y - s / 2, s, s);
                    g.FillRectangle(fill, rect);
                    g.DrawRectangle(border, rect);
                }
            }
        }

        private void DrawSizeInfo(Graphics g)
        {
            string text = $"{_selectionRect.Width} × {_selectionRect.Height}";
            using (var font = Theme.UiFont(9.5f))
            using (var brush = new SolidBrush(Theme.SizeBadgeText))
            using (var bg = new SolidBrush(Theme.SizeBadgeBackground))
            {
                var size = g.MeasureString(text, font);
                float padX = 8, padY = 3;
                float bgW = size.Width + padX * 2;
                float bgH = size.Height + padY * 2;

                float bx = _selectionRect.Left;
                float by = _selectionRect.Top - bgH - 6;
                if (by < 0) by = _selectionRect.Top + 6;
                if (bx + bgW > Width) bx = Width - bgW - 4;

                using (var path = GraphicsHelpers.RoundRect(new RectangleF(bx, by, bgW, bgH), 6f))
                    g.FillPath(bg, path);
                g.DrawString(text, font, brush, bx + padX, by + padY);
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
                return;
            }

            // 1) 选区边缘手柄 → resize（即使有标注工具激活也优先生效）
            var handle = HitTestHandleEdge(e.Location);
            if (handle != ResizeHandle.None)
            {
                _activeHandle = handle;
                _dragStartPoint = e.Location;
                _dragStartRect = _selectionRect;
                return;
            }

            // 2) 命中已有标注 → 拖动该标注（无论当前是什么工具）
            var hit = HitTestAnnotation(e.Location);
            if (hit != null)
            {
                _draggingAnnotation = hit;
                _annotationDragStart = e.Location;
                return;
            }

            // 3) 在选区外点击 → 啥都不做（防止误操作）
            if (!_selectionRect.Contains(e.Location)) return;

            // 4) 选区内空白 + 文字工具 → 弹输入框
            if (_currentTool == ToolType.Text)
            {
                CommitTextEditor();
                BeginTextEditor(e.Location);
                return;
            }

            // 5) 选区内空白 + 其他标注工具 → 画新标注
            if (_currentTool != ToolType.None)
            {
                var relativePoint = new Point(e.X - _selectionRect.X, e.Y - _selectionRect.Y);
                _currentAnnotation = CreateAnnotation(_currentTool, relativePoint);
                return;
            }

            // 6) 选区内空白 + 无工具 → 整体移动选区
            _activeHandle = ResizeHandle.Move;
            _dragStartPoint = e.Location;
            _dragStartRect = _selectionRect;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isSelecting)
            {
                var oldRect = _selectionRect;
                _endPoint = e.Location;
                _selectionRect = GetRectangle(_startPoint, _endPoint);

                var union = Rectangle.Union(
                    InflateForDecorations(oldRect),
                    InflateForDecorations(_selectionRect));
                Invalidate(union);
            }
            else if (_activeHandle != ResizeHandle.None)
            {
                var oldRect = _selectionRect;
                ApplyResize(e.Location);
                RepositionToolbar();

                // 重绘新旧选区 + 装饰区域（手柄 + 尺寸 badge 在选区外侧，需要更大边距）
                var union = Rectangle.Union(
                    InflateForDecorations(oldRect),
                    InflateForDecorations(_selectionRect));
                Invalidate(union);
            }
            else if (_draggingAnnotation != null)
            {
                var oldBounds = _draggingAnnotation.Bounds;
                int dx = e.X - _annotationDragStart.X;
                int dy = e.Y - _annotationDragStart.Y;
                _draggingAnnotation.Translate(dx, dy);
                _annotationDragStart = e.Location;

                // 增量重绘：合并旧+新 bounds，并换算到屏幕坐标
                var newBounds = _draggingAnnotation.Bounds;
                var dirty = Rectangle.Union(oldBounds, newBounds);
                dirty.Offset(_selectionRect.X, _selectionRect.Y);
                dirty.Inflate(4, 4);
                Invalidate(dirty);
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
            else if (_selectionDone)
            {
                // 鼠标悬停光标提示，让"标注可拖动"自然可发现
                var handle = HitTestHandleEdge(e.Location);
                if (handle != ResizeHandle.None)
                {
                    this.Cursor = CursorForHandle(handle);
                }
                else if (HitTestAnnotation(e.Location) != null)
                {
                    this.Cursor = Cursors.SizeAll;
                }
                else if (_currentTool == ToolType.Text)
                {
                    this.Cursor = Cursors.IBeam;
                }
                else if (_currentTool != ToolType.None)
                {
                    this.Cursor = Cursors.Default;
                }
                else if (_selectionRect.Contains(e.Location))
                {
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    this.Cursor = Cursors.Cross;
                }
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
            else if (_activeHandle != ResizeHandle.None)
            {
                _activeHandle = ResizeHandle.None;
                Invalidate();
            }
            else if (_draggingAnnotation != null)
            {
                _draggingAnnotation = null;
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

            // 文字编辑器持有焦点时，让它自己处理键盘
            if (_textEditor != null && _textEditor.Focused) return;

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
            CommitTextEditor();
            CopyToClipboard();
            Close();
        }

        private void CancelCapture()
        {
            DisposeTextEditor();
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

            var settings = Settings.Load();
            string defaultDir = settings.DefaultSaveDirectory;
            if (!string.IsNullOrEmpty(defaultDir) && System.IO.Directory.Exists(defaultDir))
            {
                string path = System.IO.Path.Combine(defaultDir, $"Snapix_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                try
                {
                    using (var result = RenderResult())
                    {
                        result.Save(path, ImageFormat.Png);
                    }
                    Close();
                    return;
                }
                catch
                {
                    // 失败回退到对话框
                }
            }

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
        private const int SW_SHOWNOACTIVATE = 4;

        private void ShowToolbar()
        {
            if (_toolbar != null) return;

            _toolbar = new AnnotationToolbar();
            _toolbar.ToolSelected += (tool) =>
            {
                CommitTextEditor(); // 切换工具前先提交未完成的文字
                _currentTool = tool;
                this.Cursor = tool == ToolType.None ? Cursors.Cross : (tool == ToolType.Text ? Cursors.IBeam : Cursors.Default);

                // 工具栏线宽 swatch 同步到当前工具的偏好
                if (_toolThickness.TryGetValue(tool, out var t))
                    _toolbar.SetThicknessVisual(t);
            };
            _toolbar.ColorSelected += (color) => _currentColor = color;
            _toolbar.ThicknessSelected += (t) =>
            {
                if (_currentTool != ToolType.None && _toolThickness.ContainsKey(_currentTool))
                    _toolThickness[_currentTool] = t;
            };
            _toolbar.ConfirmClicked += () => ConfirmCapture();
            _toolbar.CancelClicked += () => CancelCapture();
            _toolbar.SaveClicked += () => SaveToFile();
            _toolbar.UndoClicked += () => Undo();
            _toolbar.RedoClicked += () => Redo();

            RepositionToolbar();
            // 用 SW_SHOWNOACTIVATE 显示，避免抢走 CaptureForm 的键盘焦点
            _toolbar.Owner = this;
            ShowWindow(_toolbar.Handle, SW_SHOWNOACTIVATE);
            _toolbar.Visible = true; // 同步 .NET 状态

            // 显式抢回键盘焦点
            this.Activate();
            this.Focus();
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

        /// <summary>
        /// 把选区扩展到包含所有装饰元素（边框、8 手柄、尺寸 badge）的最小区域。
        /// 尺寸 badge 高度约 22px，放在选区上方，所以顶部要多留空间。
        /// </summary>
        private Rectangle InflateForDecorations(Rectangle r)
        {
            const int sideMargin = HandleSize + 4;
            const int topMargin = 30; // 容纳尺寸 badge
            return new Rectangle(
                r.X - sideMargin,
                r.Y - topMargin,
                r.Width + sideMargin * 2,
                r.Height + topMargin + sideMargin);
        }

        // ---------- 选区调整/移动 ----------

        private ResizeHandle HitTestHandleEdge(Point p)
        {
            if (!_selectionDone) return ResizeHandle.None;

            int half = HandleSize / 2 + HandleHitPadding;
            var r = _selectionRect;

            // 8 手柄中心点
            var pts = new[]
            {
                (ResizeHandle.TopLeft,    new Point(r.Left,           r.Top)),
                (ResizeHandle.Top,        new Point(r.Left + r.Width/2, r.Top)),
                (ResizeHandle.TopRight,   new Point(r.Right,          r.Top)),
                (ResizeHandle.Left,       new Point(r.Left,           r.Top + r.Height/2)),
                (ResizeHandle.Right,      new Point(r.Right,          r.Top + r.Height/2)),
                (ResizeHandle.BottomLeft, new Point(r.Left,           r.Bottom)),
                (ResizeHandle.Bottom,     new Point(r.Left + r.Width/2, r.Bottom)),
                (ResizeHandle.BottomRight,new Point(r.Right,          r.Bottom)),
            };

            foreach (var (h, c) in pts)
            {
                if (Math.Abs(p.X - c.X) <= half && Math.Abs(p.Y - c.Y) <= half)
                    return h;
            }

            return ResizeHandle.None;
        }

        /// <summary>给"鼠标移动到选区时改光标"用：把内部空白也认为是 Move。</summary>
        private ResizeHandle HitTestHandle(Point p)
        {
            var edge = HitTestHandleEdge(p);
            if (edge != ResizeHandle.None) return edge;
            if (_selectionRect.Contains(p)) return ResizeHandle.Move;
            return ResizeHandle.None;
        }

        /// <summary>从顶到底找命中的标注。</summary>
        private AnnotationBase HitTestAnnotation(Point screenLocalPoint)
        {
            // 标注存储的坐标是相对于 _selectionRect.Location 的局部坐标
            var local = new Point(screenLocalPoint.X - _selectionRect.X, screenLocalPoint.Y - _selectionRect.Y);
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                if (_annotations[i].HitTest(local)) return _annotations[i];
            }
            return null;
        }

        private Cursor CursorForHandle(ResizeHandle h)
        {
            switch (h)
            {
                case ResizeHandle.TopLeft:
                case ResizeHandle.BottomRight:
                    return Cursors.SizeNWSE;
                case ResizeHandle.TopRight:
                case ResizeHandle.BottomLeft:
                    return Cursors.SizeNESW;
                case ResizeHandle.Top:
                case ResizeHandle.Bottom:
                    return Cursors.SizeNS;
                case ResizeHandle.Left:
                case ResizeHandle.Right:
                    return Cursors.SizeWE;
                case ResizeHandle.Move:
                    return Cursors.SizeAll;
                default:
                    return Cursors.Cross;
            }
        }

        private void ApplyResize(Point current)
        {
            int dx = current.X - _dragStartPoint.X;
            int dy = current.Y - _dragStartPoint.Y;
            var r = _dragStartRect;

            int left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom;

            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft: left += dx; top += dy; break;
                case ResizeHandle.Top: top += dy; break;
                case ResizeHandle.TopRight: right += dx; top += dy; break;
                case ResizeHandle.Left: left += dx; break;
                case ResizeHandle.Right: right += dx; break;
                case ResizeHandle.BottomLeft: left += dx; bottom += dy; break;
                case ResizeHandle.Bottom: bottom += dy; break;
                case ResizeHandle.BottomRight: right += dx; bottom += dy; break;
                case ResizeHandle.Move:
                    left += dx; right += dx; top += dy; bottom += dy;
                    // 防止移动越界
                    if (left < 0) { right -= left; left = 0; }
                    if (top < 0) { bottom -= top; top = 0; }
                    if (right > Width) { left -= (right - Width); right = Width; }
                    if (bottom > Height) { top -= (bottom - Height); bottom = Height; }
                    break;
            }

            // 边界夹紧 + 最小尺寸
            left = Math.Max(0, Math.Min(left, Width));
            right = Math.Max(0, Math.Min(right, Width));
            top = Math.Max(0, Math.Min(top, Height));
            bottom = Math.Max(0, Math.Min(bottom, Height));

            int x = Math.Min(left, right);
            int y = Math.Min(top, bottom);
            int w = Math.Max(8, Math.Abs(right - left));
            int h = Math.Max(8, Math.Abs(bottom - top));

            _selectionRect = new Rectangle(x, y, w, h);
        }

        private void RepositionToolbar()
        {
            if (_toolbar == null) return;

            // _selectionRect 是 CaptureForm 局部坐标，需要换算到屏幕坐标
            int x = _selectionRect.Left;
            int y = _selectionRect.Bottom + 8;
            if (y + _toolbar.Height > Height)
                y = _selectionRect.Top - _toolbar.Height - 8;
            if (y < 0) y = Math.Max(0, _selectionRect.Bottom - _toolbar.Height - 8);

            if (x + _toolbar.Width > Width) x = Math.Max(0, Width - _toolbar.Width - 4);
            if (x < 0) x = 4;

            // 转换为屏幕坐标
            var screenPt = this.PointToScreen(new Point(x, y));
            _toolbar.Location = screenPt;
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
                    return new RectangleAnnotation(start, _currentColor, CurrentThickness);
                case ToolType.Arrow:
                    return new ArrowAnnotation(start, _currentColor, CurrentThickness);
                case ToolType.Pen:
                    return new PenAnnotation(start, _currentColor, CurrentThickness);
                case ToolType.Mosaic:
                    return new MosaicAnnotation(start, _screenshot, _selectionRect);
                case ToolType.Text:
                    // 文字工具不通过 CreateAnnotation 走鼠标拖拽流程，直接返回 null
                    return null;
                default:
                    return null;
            }
        }

        private void BeginTextEditor(Point clickPoint)
        {
            // 用 TextBox 作为编辑器；提交后转 TextAnnotation
            _textEditorOrigin = new Point(clickPoint.X - _selectionRect.X, clickPoint.Y - _selectionRect.Y);

            _textEditor = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 30, 32),
                ForeColor = _currentColor,
                Font = new Font("Microsoft YaHei UI", TextFontSize),
                AcceptsReturn = false, // 单行 Enter 用作提交，多行用 Shift+Enter
                Location = clickPoint,
                Size = new Size(200, 36),
                Cursor = Cursors.IBeam,
            };

            _textEditor.KeyDown += TextEditor_KeyDown;
            _textEditor.LostFocus += TextEditor_LostFocus;
            _textEditor.TextChanged += TextEditor_AutoResize;

            this.Controls.Add(_textEditor);
            _textEditor.BringToFront();
            _textEditor.Focus();
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CancelTextEditor();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (e.Shift)
                {
                    // 多行：手动插入换行
                    e.SuppressKeyPress = true;
                    int pos = _textEditor.SelectionStart;
                    _textEditor.Text = _textEditor.Text.Insert(pos, Environment.NewLine);
                    _textEditor.SelectionStart = pos + Environment.NewLine.Length;
                }
                else
                {
                    e.SuppressKeyPress = true;
                    CommitTextEditor();
                }
            }
        }

        private void TextEditor_AutoResize(object sender, EventArgs e)
        {
            if (_textEditor == null) return;

            using (var g = _textEditor.CreateGraphics())
            {
                var size = g.MeasureString(
                    string.IsNullOrEmpty(_textEditor.Text) ? "M" : _textEditor.Text,
                    _textEditor.Font);
                int newW = Math.Max(80, (int)Math.Ceiling(size.Width) + 16);
                int newH = Math.Max(32, (int)Math.Ceiling(size.Height) + 12);
                _textEditor.Size = new Size(newW, newH);
            }
        }

        private void CommitTextEditor()
        {
            if (_textEditor == null) return;

            var text = _textEditor.Text;
            if (!string.IsNullOrEmpty(text))
            {
                _annotations.Add(new TextAnnotation(_textEditorOrigin, _currentColor, text, TextFontSize));
                _redoStack.Clear();
            }

            DisposeTextEditor();
            Invalidate();
        }

        private void CancelTextEditor()
        {
            DisposeTextEditor();
            Invalidate();
        }

        private void TextEditor_LostFocus(object sender, EventArgs e)
        {
            // 字段已被 DisposeTextEditor 清空时不再处理
            if (_textEditor == null) return;
            CommitTextEditor();
        }

        private void DisposeTextEditor()
        {
            var editor = _textEditor;
            if (editor == null) return;

            // 先置空字段并解除事件，防止 Controls.Remove 触发 LostFocus 导致重入
            _textEditor = null;
            editor.KeyDown -= TextEditor_KeyDown;
            editor.LostFocus -= TextEditor_LostFocus;
            editor.TextChanged -= TextEditor_AutoResize;

            this.Controls.Remove(editor);
            editor.Dispose();
        }

        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _screenshot?.Dispose();
            _dimmedScreenshot?.Dispose();
            if (_toolbar != null && !_toolbar.IsDisposed)
            {
                _toolbar.Close();
                _toolbar.Dispose();
            }
        }
    }
}
