using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Snapix.Native;
using Snapix.UI;

namespace Snapix
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        static void Main()
        {
            // 单实例检测
            _mutex = new Mutex(true, "Snapix_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Snapix 已在运行中。", "Snapix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 全局异常拦截，避免静默崩溃
            Application.ThreadException += (s, e) => ShowFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex) ShowFatal(ex);
            };

            // 创建隐藏主窗口用于接收热键消息
            var mainForm = new MainForm();
            Application.Run(mainForm);
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Snapix 出错了",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch { }
        }
    }

    /// <summary>
    /// 隐藏的主窗口，负责托盘图标和热键消息循环。
    /// </summary>
    internal sealed class MainForm : Form
    {
        private NotifyIcon _trayIcon;
        private HotkeyManager _hotkeyManager;
        private bool _capturing;
        private string _hotkeyLabel = "PrintScreen";
        private Settings _settings;

        public MainForm()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0;
            this.Size = new Size(0, 0);

            _settings = Settings.Load();
            InitTray();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Visible = false;

            _hotkeyManager = new HotkeyManager(this.Handle);
            if (_hotkeyManager.Register(Keys.PrintScreen))
            {
                _hotkeyLabel = "PrintScreen";
            }
            else if (_hotkeyManager.Register(Keys.A, 0x0002 | 0x0001)) // MOD_CONTROL | MOD_ALT
            {
                _hotkeyLabel = "Ctrl+Alt+A";
            }
            else
            {
                _hotkeyLabel = "(热键被占用)";
            }

            // 托盘菜单第一项跟随实际热键
            if (_trayIcon?.ContextMenuStrip?.Items.Count > 0)
                _trayIcon.ContextMenuStrip.Items[0].Text = $"截图 ({_hotkeyLabel})";

            // 首次启动引导：用 MessageBox 而非托盘气泡（系统通知设置可能屏蔽气泡）
            if (!_settings.FirstRunCompleted)
            {
                _settings.FirstRunCompleted = true;
                _settings.Save();

                // 延迟弹出，避免抢在窗口创建前
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        $"Snapix 已驻留在系统托盘。\n\n按 {_hotkeyLabel} 即可开始截图。\n右键托盘图标可以退出。",
                        "Snapix",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }));
            }
        }

        private void InitTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("截图 (PrintScreen)", null, (s, e) => StartCapture());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon
            {
                Text = "Snapix - 轻量截图",
                Icon = AppIcon.Create(32, darkBackground: true),
                Visible = true,
                ContextMenuStrip = menu
            };

            _trayIcon.DoubleClick += (s, e) => StartCapture();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                StartCapture();
            }
            base.WndProc(ref m);
        }

        private void StartCapture()
        {
            if (_capturing) return;
            _capturing = true;

            var captureForm = new CaptureForm();
            captureForm.FormClosed += (s, e) => _capturing = false;
            captureForm.Show();
            // 确保获得键盘焦点，使 Esc/Enter/Ctrl+S 等快捷键生效
            captureForm.Activate();
            captureForm.Focus();
        }

        private void ExitApp()
        {
            _hotkeyManager?.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _hotkeyManager?.Dispose();
            _trayIcon?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
