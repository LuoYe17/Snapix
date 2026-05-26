using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Snapix.UI;

namespace Snapix
{
    /// <summary>
    /// 设置面板：自定义热键、默认保存目录、开机自启。
    /// 视觉风格与 AnnotationToolbar 一致：深色磨砂卡片、12px 圆角、Theme.Accent 强调色。
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "Snapix";

        private const int CornerRadius = 12;
        private const int TitleBarHeight = 36;

        private readonly Settings _settings;
        private readonly HotkeyManager _hotkeyManager;

        // 字段名保持不变以便最小改动；类型升级为主题化控件
        private ThemedTextBox _hotkeyBox;
        private TextButton _hotkeyEditButton;
        private ThemedTextBox _saveDirBox;
        private ThemedCheckBox _autoStartCheck;

        // 标题栏关闭按钮（命中测试时排除拖动区域）
        private IconButton _closeButton;

        private bool _capturing;
        private Keys _capturedKey;
        private uint _capturedModifiers;
        private Keys _pendingKey;
        private uint _pendingModifiers;

        public SettingsForm(Settings settings, HotkeyManager hotkeyManager)
        {
            _settings = settings;
            _hotkeyManager = hotkeyManager;
            _pendingKey = (Keys)_settings.HotkeyKey;
            _pendingModifiers = (uint)_settings.HotkeyModifiers;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Snapix 设置";
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(480, 320);
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(40, 40, 42);
            this.Font = Theme.UiFont(9.5f);
            this.ForeColor = Theme.IconColorActive;
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            UpdateRegion();

            // ---- 布局参数 ----
            int margin = 20;
            int labelWidth = 96;
            int rowHeight = 30;
            int rowGap = 14;
            int contentLeft = margin + labelWidth + 8;
            int contentWidth = this.ClientSize.Width - contentLeft - margin;

            int contentTop = TitleBarHeight + 12;

            // ---- 标题栏关闭按钮 ----
            _closeButton = new IconButton
            {
                Icon = Icons.IconKind.Close,
                Size = new Size(28, 28),
                Location = new Point(this.ClientSize.Width - 28 - 6, (TitleBarHeight - 28) / 2),
                TabStop = false,
            };
            _closeButton.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(_closeButton);

            // ---- 热键 ----
            int y = contentTop;
            var hotkeyLabel = MakeLabel("截图热键", margin, y, labelWidth, rowHeight);

            _hotkeyBox = new ThemedTextBox
            {
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth - 80 - 8, rowHeight),
                ReadOnly = true,
                TabStopInner = false,
                Text = HotkeyManager.Format(_pendingKey, _pendingModifiers),
            };
            _hotkeyEditButton = new TextButton
            {
                Style = TextButton.ButtonStyle.Ghost,
                Location = new Point(contentLeft + contentWidth - 80, y),
                Size = new Size(80, rowHeight),
                Text = "修改",
            };
            _hotkeyEditButton.Click += OnHotkeyEditClick;

            // ---- 保存目录 ----
            y += rowHeight + rowGap;
            var saveDirLabel = MakeLabel("默认保存目录", margin, y, labelWidth, rowHeight);

            _saveDirBox = new ThemedTextBox
            {
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth - 80 - 80 - 16, rowHeight),
                ReadOnly = true,
                Text = _settings.DefaultSaveDirectory ?? "",
            };
            var browseButton = new TextButton
            {
                Style = TextButton.ButtonStyle.Ghost,
                Location = new Point(contentLeft + contentWidth - 80 - 80 - 8, y),
                Size = new Size(80, rowHeight),
                Text = "浏览…",
            };
            browseButton.Click += OnBrowseClick;
            var clearButton = new TextButton
            {
                Style = TextButton.ButtonStyle.Ghost,
                Location = new Point(contentLeft + contentWidth - 80, y),
                Size = new Size(80, rowHeight),
                Text = "清除",
            };
            clearButton.Click += (s, e) => _saveDirBox.Text = "";

            // 留空时的提示
            y += rowHeight + 6;
            var saveDirHint = new Label
            {
                Text = "留空则 Ctrl+S 弹出另存为对话框。",
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth, 18),
                ForeColor = Color.FromArgb(160, 255, 255, 255),
                BackColor = Color.Transparent,
                Font = Theme.UiFont(8.5f),
                AutoSize = false,
            };

            // ---- 开机自启 ----
            y += 18 + rowGap + 4;
            _autoStartCheck = new ThemedCheckBox
            {
                Text = "Windows 启动时自动运行",
                Location = new Point(margin, y),
                Size = new Size(this.ClientSize.Width - margin * 2, 24),
                Checked = _settings.AutoStart,
            };

            // ---- 底部按钮 ----
            int btnW = 88;
            int btnH = 32;
            int btnY = this.ClientSize.Height - btnH - margin;

            var okButton = new TextButton
            {
                Style = TextButton.ButtonStyle.Accent,
                Text = "确定",
                Size = new Size(btnW, btnH),
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - btnW * 2 - margin - 8, btnY),
            };
            okButton.Click += OnOkClick;

            var cancelButton = new TextButton
            {
                Style = TextButton.ButtonStyle.Ghost,
                Text = "取消",
                Size = new Size(btnW, btnH),
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - btnW - margin, btnY),
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.Controls.AddRange(new Control[]
            {
                hotkeyLabel, _hotkeyBox, _hotkeyEditButton,
                saveDirLabel, _saveDirBox, browseButton, clearButton,
                saveDirHint,
                _autoStartCheck,
                okButton, cancelButton,
            });
        }

        private static Label MakeLabel(string text, int x, int y, int w, int rowH)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, rowH),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.IconColor,
                BackColor = Color.Transparent,
                Font = Theme.UiFont(9.5f),
            };
        }

        // ---------- 自绘窗口外形 ----------

        private void UpdateRegion()
        {
            using (var path = GraphicsHelpers.RoundRect(new RectangleF(0, 0, Width, Height), CornerRadius))
                this.Region = new Region(path);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 标题栏文字
            var titleRect = new Rectangle(16, 0, this.ClientSize.Width - 16 - 40, TitleBarHeight);
            TextRenderer.DrawText(g, this.Text ?? string.Empty, Theme.UiFont(10f, FontStyle.Regular),
                titleRect, Theme.IconColorActive,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            // 标题栏底部分隔线
            using (var pen = new Pen(Theme.Divider, 1f))
                g.DrawLine(pen, 0, TitleBarHeight, this.ClientSize.Width, TitleBarHeight);

            // 外圈细边框，让圆角更精致
            var border = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var path = GraphicsHelpers.RoundRect(border, CornerRadius))
            using (var pen = new Pen(Theme.ToolbarBorder, 1f))
                g.DrawPath(pen, path);
        }

        // 标题栏拖动：把标题栏区域报告为标题栏（HTCAPTION），让系统接管拖动
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCAPTION = 2;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST)
            {
                int lParam = m.LParam.ToInt32();
                int x = (short)(lParam & 0xFFFF);
                int y = (short)((lParam >> 16) & 0xFFFF);
                var pt = this.PointToClient(new Point(x, y));

                // 关闭按钮区域不算标题栏
                if (_closeButton != null && _closeButton.Bounds.Contains(pt)) return;

                if (pt.Y >= 0 && pt.Y < TitleBarHeight && pt.X >= 0 && pt.X < this.ClientSize.Width)
                {
                    m.Result = (IntPtr)HTCAPTION;
                }
            }
        }

        // ---------- 热键捕获 ----------

        private void OnHotkeyEditClick(object sender, EventArgs e)
        {
            BeginCapture();
        }

        private void BeginCapture()
        {
            _capturing = true;
            _capturedKey = Keys.None;
            _capturedModifiers = 0;
            _hotkeyBox.Text = "请按下新热键（Esc 取消）…";
            _hotkeyEditButton.Enabled = false;
            // 暂停全局热键，避免捕获 PrintScreen 时触发截图
            _hotkeyManager?.Unregister();
            // 装 LL 键盘 hook：PrintScreen 在 Win11 被 Snipping Tool 占住，
            // 普通消息泵（含 ProcessCmdKey）拦不到，必须走全局钩子
            LowLevelKeyboardHook.Install(OnHookKey);
            this.Focus();
        }

        private void EndCapture(bool committed)
        {
            // 先卸 hook，避免 EndCapture 流程里再被回调进来
            LowLevelKeyboardHook.Uninstall();

            _capturing = false;
            _hotkeyEditButton.Enabled = true;

            if (committed && _capturedKey != Keys.None)
            {
                _pendingKey = _capturedKey;
                _pendingModifiers = _capturedModifiers;
            }

            _hotkeyBox.Text = HotkeyManager.Format(_pendingKey, _pendingModifiers);
            // 恢复原热键，确保对话框打开期间热键仍可用（按 OK 时会再次替换）
            _hotkeyManager?.Register((Keys)_settings.HotkeyKey, (uint)_settings.HotkeyModifiers);
        }

        /// <summary>
        /// LL 键盘 hook 的回调：在非 UI 线程被调用，须 BeginInvoke 切回 UI。
        /// 返回 true 表示拦下消息（不再下传），用于阻止 PrintScreen 触发系统 Snipping Tool。
        /// </summary>
        private bool OnHookKey(int vkCode)
        {
            if (!_capturing) return false;

            // 在 hook 线程里读修饰键状态，UI 线程读会有时序差
            uint mods = 0;
            if ((LowLevelKeyboardHook.GetAsyncKeyState(LowLevelKeyboardHook.VK_CONTROL) & 0x8000) != 0)
                mods |= HotkeyManager.MOD_CONTROL;
            if ((LowLevelKeyboardHook.GetAsyncKeyState(LowLevelKeyboardHook.VK_MENU) & 0x8000) != 0)
                mods |= HotkeyManager.MOD_ALT;
            if ((LowLevelKeyboardHook.GetAsyncKeyState(LowLevelKeyboardHook.VK_SHIFT) & 0x8000) != 0)
                mods |= HotkeyManager.MOD_SHIFT;

            // 拼成 WinForms Keys：低位是 KeyCode，高位是修饰位
            Keys keyData = (Keys)vkCode;
            if ((mods & HotkeyManager.MOD_CONTROL) != 0) keyData |= Keys.Control;
            if ((mods & HotkeyManager.MOD_ALT) != 0) keyData |= Keys.Alt;
            if ((mods & HotkeyManager.MOD_SHIFT) != 0) keyData |= Keys.Shift;

            // 切回 UI 线程处理；BeginInvoke 不阻塞 hook 线程
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((Action)(() =>
                {
                    if (_capturing) HandleCaptureKey(keyData);
                }));
            }

            // 阻断消息继续投递，避免 Snipping Tool 弹出
            return true;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 兜底：无论 OK / Cancel / 直接关闭，都不能留下 hook
            LowLevelKeyboardHook.Uninstall();
            base.OnFormClosed(e);
        }

        private void HandleCaptureKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;

            if (key == Keys.Escape)
            {
                EndCapture(false);
                return;
            }

            // 只是按住修饰键不算完成
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                key == Keys.LControlKey || key == Keys.RControlKey ||
                key == Keys.LShiftKey || key == Keys.RShiftKey ||
                key == Keys.LMenu || key == Keys.RMenu ||
                key == Keys.None)
            {
                return;
            }

            uint mods = 0;
            if ((keyData & Keys.Control) == Keys.Control) mods |= HotkeyManager.MOD_CONTROL;
            if ((keyData & Keys.Alt) == Keys.Alt) mods |= HotkeyManager.MOD_ALT;
            if ((keyData & Keys.Shift) == Keys.Shift) mods |= HotkeyManager.MOD_SHIFT;

            // 接受：单键（PrintScreen / F1-F12）或 任意修饰 + 任意键
            bool isStandaloneAllowed =
                key == Keys.PrintScreen ||
                (key >= Keys.F1 && key <= Keys.F12);

            if (mods == 0 && !isStandaloneAllowed)
            {
                // 单字母无修饰不算合法，继续等待
                return;
            }

            _capturedKey = key;
            _capturedModifiers = mods;
            EndCapture(true);
        }

        // ---------- 浏览目录 ----------

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择截图默认保存目录";
                if (!string.IsNullOrEmpty(_saveDirBox.Text) && Directory.Exists(_saveDirBox.Text))
                    dlg.SelectedPath = _saveDirBox.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _saveDirBox.Text = dlg.SelectedPath;
                }
            }
        }

        // ---------- 确定 ----------

        private void OnOkClick(object sender, EventArgs e)
        {
            // 1) 校验新热键能否成功注册
            bool hotkeyChanged =
                _pendingKey != (Keys)_settings.HotkeyKey ||
                _pendingModifiers != (uint)_settings.HotkeyModifiers;

            if (hotkeyChanged && _hotkeyManager != null)
            {
                if (!_hotkeyManager.Register(_pendingKey, _pendingModifiers))
                {
                    MessageBox.Show(this, "该热键已被占用，请换一个。", "Snapix",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 恢复原热键
                    _hotkeyManager.Register((Keys)_settings.HotkeyKey, (uint)_settings.HotkeyModifiers);
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // 2) 持久化设置
            _settings.HotkeyKey = (int)_pendingKey;
            _settings.HotkeyModifiers = (int)_pendingModifiers;
            _settings.DefaultSaveDirectory = _saveDirBox.Text?.Trim() ?? "";
            _settings.AutoStart = _autoStartCheck.Checked;
            _settings.Save();

            // 3) 同步注册表 Run 项
            ApplyAutoStart(_settings.AutoStart);
        }

        private static void ApplyAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = Assembly.GetEntryAssembly()?.Location;
                        if (string.IsNullOrEmpty(exePath)) return;
                        key.SetValue(RunValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        if (key.GetValue(RunValueName) != null)
                            key.DeleteValue(RunValueName, throwOnMissingValue: false);
                    }
                }
            }
            catch
            {
                // 注册表失败静默处理：不阻断设置保存
            }
        }

        // ---------- LL 键盘钩子（仅服务于本对话框的热键捕获） ----------

        /// <summary>
        /// 低级键盘钩子。装上后能拦到 PrintScreen 等被系统占用的按键。
        /// 静态字段必须长期持有 delegate，否则 GC 回收后回调时会崩。
        /// </summary>
        private static class LowLevelKeyboardHook
        {
            private const int WH_KEYBOARD_LL = 13;
            private const int HC_ACTION = 0;
            private const int WM_KEYDOWN = 0x0100;
            private const int WM_SYSKEYDOWN = 0x0104;

            public const int VK_SHIFT = 0x10;
            public const int VK_CONTROL = 0x11;
            public const int VK_MENU = 0x12;

            // 静态字段持有 delegate 防 GC——SetWindowsHookEx 最经典的坑
            private static HookProc _proc;
            private static IntPtr _hook = IntPtr.Zero;
            private static Func<int, bool> _onKey;

            public static void Install(Func<int, bool> onKey)
            {
                if (_hook != IntPtr.Zero) return; // 已装则跳过
                _onKey = onKey;
                _proc = HookCallback;

                // .NET Framework 4.8 上 hMod 必须是有效模块句柄；用主模块名取
                IntPtr hMod;
                using (var proc = Process.GetCurrentProcess())
                {
                    hMod = GetModuleHandle(proc.MainModule.ModuleName);
                }

                _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
            }

            public static void Uninstall()
            {
                if (_hook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hook);
                    _hook = IntPtr.Zero;
                }
                _proc = null;
                _onKey = null;
            }

            private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode == HC_ACTION)
                {
                    int msg = wParam.ToInt32();
                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        var data = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        var cb = _onKey;
                        if (cb != null && cb((int)data.vkCode))
                        {
                            // 返回 1 阻断消息继续传递（Snipping Tool 收不到 PrintScreen）
                            return (IntPtr)1;
                        }
                    }
                }
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

            [StructLayout(LayoutKind.Sequential)]
            private struct KBDLLHOOKSTRUCT
            {
                public uint vkCode;
                public uint scanCode;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int vKey);
        }
    }
}
