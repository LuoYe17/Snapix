# Changelog

本项目所有的显著变更都会记录在这里。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [1.0.0] - 2026-05

第一个稳定版本。

### Added
- 设置面板：自定义截图热键、默认保存目录、Windows 启动时自动运行
- 文字标注字号档位（小 / 中 / 大），与文字工具联动
- 复制 / 保存成功后右下角 toast 反馈
- README 演示 GIF
- 启动速度埋点确认稳态 ~75ms（4K 多屏实测）

### Changed
- Ctrl+C 现在等同于 Enter：复制并关闭遮罩
- 设置面板使用与项目一致的深色磨砂 UI（圆角、自定义控件）

### Fixed
- 系统级抢占的热键（如 Win11 上 Snipping Tool 占用的 PrintScreen）现在能在设置中重新捕获，使用 low-level 键盘钩子绕过应用消息泵
- 切换工具时正确同步线宽 / 字号档位的视觉选中态

## [0.1.0] - 2026-05

第一个发布版本。

### Added
- 全局热键唤起截图（PrintScreen，被占用时降级 Ctrl+Alt+A）
- 系统托盘 + 单实例
- 全屏遮罩 + 鼠标拖拽框选
- 智能窗口吸附（基于 DwmGetWindowAttribute，排除桌面/任务栏等系统窗口）
- 选区完成后 8 个手柄调整大小 + 内部拖动整体
- 5 种标注工具：矩形、箭头、画笔、文字、马赛克
- 文字标注：多行、自动调整宽高、Enter 提交、Shift+Enter 换行
- 鼠标悬停在已绘标注上可直接拖动调整位置（无需先取消工具）
- 3 档线宽 + 5 种颜色，每个工具独立记忆线宽偏好（箭头默认最粗）
- 撤销 / 重做
- 复制到剪贴板 / 另存为图片
- 多显示器 + Per-Monitor V2 高 DPI
- macOS 风格 UI：磨砂深色卡片、矢量图标、圆形色板
- 自绘极简托盘图标
- 首次启动欢迎对话框
- 配置持久化（exe 同目录 config.ini）
- GitHub Actions CI 自动构建 + tag 触发自动发版
