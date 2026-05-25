using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Snapix.Native;

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

            // 创建隐藏主窗口用于接收热键消息
            var mainForm = new MainForm();
            Application.Run(mainForm);
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

        public MainForm()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0;
            this.Size = new Size(0, 0);

            InitTray();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Visible = false;

            _hotkeyManager = new HotkeyManager(this.Handle);
            if (!_hotkeyManager.Register(Keys.PrintScreen))
            {
                // PrintScreen 被占用时尝试 Ctrl+Alt+A
                _hotkeyManager.Register(Keys.A, 0x0002 | 0x0001); // MOD_CONTROL | MOD_ALT
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
                Icon = SystemIcons.Application, // 后续替换为自定义图标
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
