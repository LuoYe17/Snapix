using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Snapix
{
    /// <summary>
    /// 极简 INI 风格配置，存于 exe 同目录。
    /// v1 只用于"是否首次启动"标记，后续设置面板会扩展。
    /// </summary>
    internal sealed class Settings
    {
        private const string FileName = "config.ini";

        public bool FirstRunCompleted { get; set; }

        public static string ConfigPath
        {
            get
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(exeDir, FileName);
            }
        }

        public static Settings Load()
        {
            var s = new Settings();
            if (!File.Exists(ConfigPath)) return s;

            try
            {
                foreach (var line in File.ReadAllLines(ConfigPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                        continue;

                    int eq = trimmed.IndexOf('=');
                    if (eq <= 0) continue;

                    var key = trimmed.Substring(0, eq).Trim();
                    var value = trimmed.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "FirstRunCompleted":
                            s.FirstRunCompleted = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                            break;
                    }
                }
            }
            catch
            {
                // 配置损坏视为不存在
            }

            return s;
        }

        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Snapix configuration");
                sb.AppendLine("FirstRunCompleted=" + (FirstRunCompleted ? "true" : "false"));
                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // exe 所在目录无写权限时静默失败（U 盘只读、Program Files 等情况）
            }
        }
    }
}
