# Snapix

> 极致轻量的 Windows 截图工具。一个 exe，按下热键，框选，画箭头，Ctrl+C 走人。

[![Build](https://github.com/LuoYe17/Snapix/actions/workflows/build.yml/badge.svg)](https://github.com/LuoYe17/Snapix/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/LuoYe17/Snapix?include_prereleases&label=release)](https://github.com/LuoYe17/Snapix/releases)
[![License](https://img.shields.io/github/license/LuoYe17/Snapix)](LICENSE)
![Size](https://img.shields.io/badge/exe-~50KB-brightgreen)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)

## 为什么有 Snapix

市面上的截图工具不是太重就是太麻烦：

| 工具 | 痛点 |
|---|---|
| Windows 自带 Win+Shift+S | 标注工具贫乏、保存不方便 |
| Snipaste | 17 MB，功能太多，新手看不懂 |
| ShareX | 30 MB，UI 对普通用户劝退 |
| 微信/QQ 截图 | 强制登录，依赖客户端 |

Snapix 只做一件事：**让普通用户能在 1 秒内完成"截图 → 标注 → 复制"**。

## 特性

- 🚀 **极致轻量**：单文件便携 exe，约 50 KB，无需安装
- ⌨️ **零依赖**：基于 .NET Framework 4.8（Win10/11 系统自带）
- 🖱️ **智能框选**：拖拽框选 / 单击吸附窗口 / 8 手柄调整大小 / 整体移动
- 🎨 **5 个标注**：矩形、箭头、画笔、文字、马赛克
- 🎯 **3 档线宽 + 5 种颜色**：每个工具独立记忆线宽偏好
- ✋ **悬停即拖**：鼠标悬停在已绘制的标注上 → 直接拖动调整位置
- ↩️ **撤销/重做**：标注阶段全程可回退
- 📋 **零摩擦输出**：`Ctrl+C` 复制 / `Ctrl+S` 保存 / `Enter` 确认 / `Esc` 取消
- 🖥️ **多屏 + 高 DPI**：Per-Monitor V2 完整支持
- 🔒 **零侵入**：无安装、零注册表写入、配置只存 exe 同目录

## 截图

> *待补充：主界面 / 标注工具栏 / 多屏使用*

## 安装

### 从 Release 下载

到 [Releases](https://github.com/LuoYe17/Snapix/releases) 下载最新的 `Snapix.exe`，放到任意目录，双击运行。

> ⚠️ 由于本项目暂未购买代码签名证书，首次运行时 Windows SmartScreen 可能弹出"未知发布者"警告。点击「更多信息」→「仍要运行」即可。本项目完全开源，CI 自动构建，可审计。

### 自行构建

需要：
- Windows 10/11
- .NET SDK 8 或更高（用于构建 net48 项目）

```bash
git clone https://github.com/LuoYe17/Snapix.git
cd Snapix
dotnet build src/Snapix/Snapix.csproj -c Release
```

产物在 `src/Snapix/bin/Release/net48/Snapix.exe`。

## 使用

1. 双击运行，程序驻留系统托盘
2. 按 `PrintScreen`（被占用则降级到 `Ctrl+Alt+A`）开始截图
3. 拖拽框选区域，或单击窗口自动吸附
4. 在浮动工具栏选择标注工具，绘制后还能拖动调整位置
5. `Ctrl+C` 复制 / `Ctrl+S` 保存 / `Enter` 确认 / `Esc` 取消

### 全部快捷键

| 快捷键 | 功能 |
|--------|------|
| `PrintScreen` / `Ctrl+Alt+A` | 唤起截图 |
| 鼠标拖拽 | 框选区域 |
| 单击窗口 | 自动吸附为该窗口边界 |
| 拖拽手柄 / 选区内部 | 调整 / 移动选区 |
| `Esc` | 取消截图 |
| `Enter` / 双击 | 确认并复制到剪贴板 |
| `Ctrl+C` | 复制到剪贴板 |
| `Ctrl+S` | 另存为图片文件 |
| `Ctrl+Z` | 撤销标注 |
| `Ctrl+Y` | 重做标注 |

## 系统要求

- Windows 10 1903+ / Windows 11
- .NET Framework 4.8（Win10 1903+ 系统已内置）

## 路线图

- [x] 核心截图 + 5 标注工具
- [x] 选区调整 + 标注拖拽
- [x] 线宽切换
- [ ] 设置面板（自定义热键、保存路径、开机自启）
- [ ] 贴图功能（v2 视用户呼声决定）
- [ ] 录屏（暂无计划）

## 贡献

欢迎 PR 和 Issue。详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 协议

[MIT](LICENSE) © 2026 LuoYe17
