# Snapix

极致轻量的 Windows 截图工具。一个 exe，按下热键，框选，画箭头，Ctrl+C 走人。

## 特性

- 🚀 单文件便携 exe（< 500 KB），无需安装，双击即用
- ⌨️ 全局热键唤起（默认 `PrintScreen`）
- 🖱️ 鼠标拖拽框选 + 智能窗口吸附
- 🎨 5 个标注工具：矩形、箭头、画笔、文字、马赛克
- 📋 `Ctrl+C` 复制到剪贴板 / `Ctrl+S` 另存为 / `Enter` 确认 / `Esc` 取消
- 🖥️ 多显示器 + 高 DPI 完美支持
- 🔒 零注册表写入，配置存 exe 同目录

## 系统要求

- Windows 10 1903+ / Windows 11（系统自带 .NET Framework 4.8）

## 使用方法

1. 下载 `Snapix.exe`，放到任意目录
2. 双击运行，程序驻留系统托盘
3. 按 `PrintScreen` 开始截图
4. 拖拽框选区域，使用工具栏标注
5. `Ctrl+C` 复制 / `Ctrl+S` 保存 / `Enter` 确认 / `Esc` 取消

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `PrintScreen` | 开始截图 |
| `Esc` | 取消截图 |
| `Enter` / 双击 | 确认截图并复制到剪贴板 |
| `Ctrl+C` | 复制到剪贴板 |
| `Ctrl+S` | 另存为文件 |
| `Ctrl+Z` | 撤销标注 |
| `Ctrl+Y` | 重做标注 |

## 构建

```bash
dotnet build src/Snapix/Snapix.csproj -c Release
```

输出位于 `src/Snapix/bin/Release/net48/Snapix.exe`

## 首次运行说明

由于本项目暂未购买代码签名证书，首次运行时 Windows SmartScreen 可能弹出"未知发布者"警告。
点击「更多信息」→「仍要运行」即可。本项目完全开源，构建流程透明可审计。

## License

[MIT](LICENSE)

## 贡献

欢迎 PR 和 Issue！请确保代码风格与现有代码一致。
