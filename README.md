# 📋 剪贴板历史工具

一个功能强大、现代化的 Windows 剪贴板历史记录工具，支持文本、图片和文件的历史记录管理。

![GitHub release](https://img.shields.io/badge/release-v1.0.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ 主要功能

### 🎯 核心功能
- ✅ **历史记录存储** - 保存最多 10,000 条记录（可配置）
- ✅ **多类型支持** - 文本、图片、文件路径
- ✅ **持久化存储** - SQLite 数据库，重启后不丢失
- ✅ **快捷键呼出** - 默认 Ctrl+Shift+V
- ✅ **自动粘贴** - 选择后自动粘贴到光标位置

### 🎨 现代化界面
- ✅ **现代化设计** - 基于现代设计系统的美观界面
- ✅ **响应式布局** - 支持多分辨率，无变形问题
- ✅ **搜索功能** - 实时搜索历史记录
- ✅ **预览显示** - 内容摘要和时间戳
- ✅ **键盘导航** - 支持上下键选择

### 🔧 高级功能
- ✅ **收藏功能** - 标记重要内容，永久保存
- ✅ **自动清理** - 按时间和数量智能清理旧记录
- ✅ **敏感数据过滤** - 自动过滤密码等敏感信息
- ✅ **系统托盘** - 后台运行，资源占用低
- ✅ **开机自启** - 可选择开机自动启动
- ✅ **设置界面** - 完整的配置选项

## 🖥️ 系统要求

- **操作系统**: Windows 10/11 (x64)
- **运行时**: .NET 8.0 Runtime
- **内存**: 最低 50MB 可用内存
- **磁盘**: 最低 100MB 可用空间

## 📦 安装和使用

### 快速安装
1. 从 [Releases](https://github.com/hwc2357300448/hexiaogong-clipboard-histor/releases) 页面下载最新版本的 `ClipboardHistory-Setup.exe`
2. 右键以管理员身份运行安装程序
3. 按照安装向导完成安装
4. 程序将自动启动并在系统托盘运行

### 从源码编译
```bash
# 克隆项目
git clone https://github.com/hwc2357300448/hexiaogong-clipboard-histor.git
cd hexiaogong-clipboard-histor

# 还原依赖包
dotnet restore

# 编译项目
dotnet build

# 运行应用
dotnet run --project ClipboardHistory
```

### 发布版本
```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## 🎮 使用说明

### 基本操作
1. **启动应用** - 程序会在系统托盘运行
2. **呼出历史** - 按 `Ctrl+Shift+V` 快捷键
3. **搜索内容** - 在搜索框输入关键词
4. **选择记录** - 双击或按回车键自动粘贴
5. **收藏项目** - 选中后点击"⭐ 收藏"按钮

### 快捷键
- `Ctrl+Shift+V` - 呼出剪贴板历史
- `↑/↓` - 上下选择记录
- `Enter` - 粘贴选中项目
- `Delete` - 删除选中项目
- `Esc` - 关闭窗口

### 托盘菜单
- **显示历史** - 打开历史记录窗口
- **设置** - 配置应用选项
- **退出** - 关闭应用

## ⚙️ 配置说明

应用配置保存在 `%APPDATA%\ClipboardHistory\settings.json` 文件中：

```json
{
  "MaxHistoryCount": 500,
  "StartWithWindows": true,
  "MonitorClipboard": true,
  "HotKey": "Ctrl+Shift+V",
  "EnableImageCapture": true,
  "EnableFileCapture": true,
  "AutoCleanupDays": 30,
  "EnableAutoCleanup": true,
  "FilterSensitiveData": true,
  "DatabasePath": "clipboard_history.db",
  "Author": "何小工",
  "Version": "1.0.0"
}
```

### 配置项说明
- `MaxHistoryCount`: 最大历史记录数量（10-10000）
- `AutoCleanupDays`: 自动清理天数（1-365）
- `EnableAutoCleanup`: 是否启用自动清理
- `StartWithWindows`: 是否开机自启动
- `FilterSensitiveData`: 是否过滤敏感数据

## 🏗️ 技术架构

### 主要技术栈
- **框架**: .NET 8.0 + WPF
- **数据库**: SQLite with Entity Framework Core
- **系统集成**: Win32 API (User32, Kernel32)
- **托盘图标**: Hardcodet.NotifyIcon.Wpf
- **安装包**: NSIS 3.x

### 项目结构
```
ClipboardHistory/
├── Models/                 # 数据模型
│   ├── ClipboardItem.cs   # 剪贴板项目模型
│   └── AppSettings.cs     # 应用设置模型
├── Services/              # 业务服务
│   ├── ClipboardMonitor.cs # 剪贴板监听
│   ├── StorageService.cs   # 数据存储
│   ├── HotkeyService.cs    # 全局热键
│   ├── SystemTrayService.cs # 系统托盘
│   ├── SettingsService.cs  # 设置管理
│   └── StartupService.cs   # 开机自启
├── MainWindow.xaml        # 主窗口界面
├── MainWindow.xaml.cs     # 主窗口逻辑
├── SettingsWindow.xaml    # 设置窗口界面
├── SettingsWindow.xaml.cs # 设置窗口逻辑
├── App.xaml              # 应用程序入口
└── App.xaml.cs           # 应用程序逻辑
```

## 📊 性能特点

- **内存占用**: < 50MB 运行时内存
- **启动时间**: < 2秒 冷启动
- **实时监听**: 无感知的剪贴板监控
- **数据索引**: 优化的数据库查询性能
- **响应速度**: < 100ms 界面响应时间

## 🔒 安全特性

- **敏感数据过滤** - 自动识别并过滤密码、银行卡号等
- **本地存储** - 数据完全保存在本地，不上传云端
- **权限最小化** - 仅请求必要的系统权限
- **数据加密** - 敏感配置采用加密存储

## 🚀 发布说明

### v1.0.0 (2025-01-18)
- ✅ 完整的剪贴板历史记录功能
- ✅ 现代化的用户界面设计
- ✅ 完善的系统集成和托盘支持
- ✅ 全面的配置选项和设置界面
- ✅ 专业的Windows安装包
- ✅ 完整的中文本地化支持

## 🛠️ 开发计划

### 已完成 ✅
- ✅ 设置界面
- ✅ 开机自启
- ✅ 现代化UI设计
- ✅ 自动粘贴功能
- ✅ 收藏功能
- ✅ 自动清理
- ✅ 系统托盘
- ✅ 全局热键
- ✅ 安装包制作

### 未来计划 📋
- [ ] 主题切换（深色/浅色）
- [ ] 导入导出功能
- [ ] 插件系统
- [ ] 云同步（可选）
- [ ] 多语言支持

## 📄 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 贡献指南
1. Fork 本仓库
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交你的改动 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 👨‍💻 作者

**何小工** - 项目作者和维护者

## 💬 联系方式

- **微信**: hwc19970111
- **邮箱**: 2357300448@qq.com

## 💬 支持

如果你觉得这个项目有用，请给它一个 ⭐️！

如有问题或建议，请创建 [Issue](https://github.com/hwc2357300448/hexiaogong-clipboard-histor/issues)。

---

*最后更新: 2025年1月18日*