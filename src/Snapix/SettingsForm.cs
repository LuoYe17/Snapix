using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using Snapix.Native;

namespace Snapix
{
    /// <summary>
    /// 设置面板：自定义热键、默认保存目录、开机自启。
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "Snapix";

        private readonly Settings _settings;
        private readonly HotkeyManager _hotkeyManager;

        private TextBox _hotkeyBox;
        private Button _hotkeyEditButton;
        private TextBox _saveDirBox;
        private CheckBox _autoStartCheck;

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
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(480, 280);
            this.KeyPreview = true;

            int margin = 12;
            int labelWidth = 96;
            int rowHeight = 28;
            int rowGap = 12;
            int contentLeft = margin + labelWidth + 8;
            int contentWidth = this.ClientSize.Width - contentLeft - margin;

            // ---- 热键 ----
            int y = margin + 8;
            var hotkeyLabel = new Label
            {
                Text = "截图热键：",
                Location = new Point(margin, y + 4),
                Size = new Size(labelWidth, rowHeight),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _hotkeyBox = new TextBox
            {
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth - 80 - 4, rowHeight),
                ReadOnly = true,
                Text = HotkeyManager.Format(_pendingKey, _pendingModifiers),
                TabStop = false,
            };
            _hotkeyEditButton = new Button
            {
                Location = new Point(contentLeft + contentWidth - 80, y - 1),
                Size = new Size(80, rowHeight + 2),
                Text = "修改",
            };
            _hotkeyEditButton.Click += OnHotkeyEditClick;

            // ---- 保存目录 ----
            y += rowHeight + rowGap;
            var saveDirLabel = new Label
            {
                Text = "默认保存目录：",
                Location = new Point(margin, y + 4),
                Size = new Size(labelWidth, rowHeight),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _saveDirBox = new TextBox
            {
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth - 80 - 80 - 8, rowHeight),
                ReadOnly = true,
                Text = _settings.DefaultSaveDirectory ?? "",
            };
            var browseButton = new Button
            {
                Location = new Point(contentLeft + contentWidth - 80 - 80 - 4, y - 1),
                Size = new Size(80, rowHeight + 2),
                Text = "浏览…",
            };
            browseButton.Click += OnBrowseClick;
            var clearButton = new Button
            {
                Location = new Point(contentLeft + contentWidth - 80, y - 1),
                Size = new Size(80, rowHeight + 2),
                Text = "清除",
            };
            clearButton.Click += (s, e) => _saveDirBox.Text = "";

            // 留空时的提示
            y += rowHeight + 4;
            var saveDirHint = new Label
            {
                Text = "留空则 Ctrl+S 弹出另存为对话框。",
                Location = new Point(contentLeft, y),
                Size = new Size(contentWidth, 18),
                ForeColor = SystemColors.GrayText,
            };

            // ---- 开机自启 ----
            y += 18 + rowGap;
            _autoStartCheck = new CheckBox
            {
                Text = "Windows 启动时自动运行",
                Location = new Point(margin, y),
                Size = new Size(this.ClientSize.Width - margin * 2, rowHeight),
                Checked = _settings.AutoStart,
            };

            // ---- 底部按钮 ----
            var okButton = new Button
            {
                Text = "确定",
                Size = new Size(88, 30),
                DialogResult = DialogResult.OK,
            };
            okButton.Location = new Point(
                this.ClientSize.Width - okButton.Width * 2 - margin - 8,
                this.ClientSize.Height - okButton.Height - margin);
            okButton.Click += OnOkClick;

            var cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(88, 30),
                DialogResult = DialogResult.Cancel,
            };
            cancelButton.Location = new Point(
                this.ClientSize.Width - cancelButton.Width - margin,
                this.ClientSize.Height - cancelButton.Height - margin);

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
            this.Focus();
        }

        private void EndCapture(bool committed)
        {
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_capturing)
            {
                // PrintScreen 在 WinForms 不触发 KeyDown，只能靠 ProcessCmdKey 拦
                const int WM_KEYDOWN = 0x0100;
                const int WM_SYSKEYDOWN = 0x0104;
                if (msg.Msg == WM_KEYDOWN || msg.Msg == WM_SYSKEYDOWN)
                {
                    HandleCaptureKey(keyData);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
    }
}
