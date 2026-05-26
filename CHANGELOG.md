# Changelog

本项目所有的显著变更都会记录在这里。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### Added
- 工具栏鼠标悬停 tooltip 提示
- 每工具独立记忆线宽偏好（箭头默认最粗）
- 线宽切换（3 档）
- 标注悬停即可拖动，无需先取消工具
- 选区完成后 8 手柄调整大小 + 整体拖动
- 文字标注（多行、自动调整宽高）
- macOS 风格 UI：磨砂深色卡片、矢量图标、圆形色板
- 自绘极简托盘图标

### Changed
- 工具栏改为独立 TopMost 窗口（性能优化，避免父窗口重绘）
- 截图遮罩使用预合成"暗化底图"+ 增量 invalidate（性能优化）

### Fixed
- 窗口吸附排除桌面、任务栏等系统窗口
- 文字编辑器关闭时的事件重入 NRE
- 拖动选区时尺寸提示卡顿

## [0.1.0] - 初始骨架

### Added
- 全局热键（PrintScreen / Ctrl+Alt+A）
- 系统托盘 + 单实例
- 全屏遮罩 + 鼠标拖拽框选
- 智能窗口吸附（DwmGetWindowAttribute）
- 5 个标注工具：矩形 / 箭头 / 画笔 / 文字 / 马赛克
- 撤销 / 重做
- 复制到剪贴板 / 另存为
- 多显示器 + Per-Monitor V2 DPI
