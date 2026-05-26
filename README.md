# Snapix

一个轻量的 Windows 截图工具。按下 PrintScreen 框选，标注，复制。

[![Build](https://github.com/LuoYe17/Snapix/actions/workflows/build.yml/badge.svg)](https://github.com/LuoYe17/Snapix/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/LuoYe17/Snapix?include_prereleases&label=release)](https://github.com/LuoYe17/Snapix/releases)
[![License](https://img.shields.io/github/license/LuoYe17/Snapix)](LICENSE)

## 起因

写这个是因为我自己每天截图都不顺手。Win+Shift+S 标注弱、保存麻烦；Snipaste 功能太多按错；微信截图要登录。所以做了一个只管截图、按 PrintScreen 就能用的工具，单文件可以拷到 U 盘里到处带。

## 功能

- 全局热键唤起（PrintScreen，被占用时降级为 Ctrl+Alt+A）
- 拖拽框选，或单击窗口自动吸附边界
- 选区完成后 8 个手柄调整大小、内部拖动整体
- 5 种标注：矩形、箭头、画笔、文字、马赛克
- 3 档线宽 + 5 种颜色，每个工具独立记忆线宽
- 鼠标悬停在已绘标注上可直接拖动调整位置
- 撤销 / 重做
- Ctrl+C 复制到剪贴板、Ctrl+S 另存为
- 多显示器、Per-Monitor V2 高 DPI

## 安装

到 [Releases](https://github.com/LuoYe17/Snapix/releases) 下载 `Snapix.exe`，放到任意目录双击运行。无需安装。

> 没有代码签名证书，首次运行 SmartScreen 会弹"未知发布者"。点「更多信息」→「仍要运行」。源码全开，CI 自动构建，可自行核对。

## 自行构建

需要 .NET SDK 8 或更高（用来编译 net48 项目）。

```bash
git clone https://github.com/LuoYe17/Snapix.git
cd Snapix
dotnet build src/Snapix/Snapix.csproj -c Release
```

产物在 `src/Snapix/bin/Release/net48/Snapix.exe`。

## 快捷键

| 键位 | 作用 |
|---|---|
| PrintScreen / Ctrl+Alt+A | 唤起截图 |
| 鼠标拖拽 | 框选 |
| 单击窗口 | 吸附为该窗口边界 |
| Esc | 取消 |
| Enter / 双击 | 确认并复制到剪贴板 |
| Ctrl+C | 复制 |
| Ctrl+S | 另存为 |
| Ctrl+Z / Ctrl+Y | 撤销 / 重做 |

## 系统要求

Windows 10 1903+ 或 Windows 11。系统已自带 .NET Framework 4.8，无需另装运行时。

## 路线图

已完成：核心截图、标注、选区调整、撤销重做。

下一步打算做：
- 设置面板（自定义热键、保存路径、开机自启）
- 首次启动的简短引导

明确不做（v1 范围）：录屏、GIF、OCR、云上传。

贴图（类似 Snipaste 的 F3）暂不做，看后续需求再定。

## 贡献

欢迎 PR 和 Issue。详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 协议

[MIT](LICENSE)
