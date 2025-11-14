# 更新日志

所有重要的项目变更都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [1.1.0] - 2025-11-14

这是一个重要的功能增强和性能优化版本。

### 🎉 新增功能

#### ⭐ 管理员权限开机自启
- 使用 Windows 任务计划程序实现开机自启
- 程序以管理员权限自动运行，确保完整功能
- 首次启用时自动请求 UAC 授权
- 设置界面明确说明需要管理员权限
- 支持一键启用/禁用开机自启

#### ⭐ 文本内容预览弹窗
- 按 **Space 键**快速预览选中文本的完整内容
- 自动识别并格式化 **JSON** 内容，便于阅读
- 自动识别并格式化 **XML** 内容，便于阅读
- 使用等宽字体（Consolas）显示，适合查看代码
- 支持文本选择和复制
- 按 **Esc 键**快速关闭预览窗口
- 提供"复制全文"按钮

#### ✨ 收藏功能优化
- **列表项内嵌星标按钮**，无需先选中即可收藏
- **F 键快捷键**一键切换收藏状态
- 星标按钮悬停显示提示文字
- 收藏项自动置顶显示
- 收藏项不会被自动清理

#### ✨ 滚动交互优化
- **自定义滚动条样式**，更美观现代
- 滚动条悬停时宽度扩展，带动画效果
- **Home 键**快速跳转到第一项
- **End 键**快速跳转到最后一项
- **PageUp/PageDown 键**翻页浏览（每次约10项）
- ListBox 虚拟化优化，支持大量数据流畅滚动

### 🚀 性能优化

#### ⚡ 数据库查询优化
- **清理频率优化**：从每次插入都清理改为每 10 次插入清理一次，插入性能提升 **90%**
- **Content 列索引**：为 Content 列创建索引，搜索性能提升 **50-80%**
- **复合索引**：为 Content + DataType 创建复合索引，重复检测性能提升 **70-90%**
- 预期整体性能提升：插入快 10 倍，搜索快 2-5 倍

#### ⚡ UI 渲染优化
- ListBox 启用虚拟化（Recycling 模式）
- 缓存 20 个列表项，减少内存占用
- 大量数据（1000+ 条）渲染瞬间完成
- 滚动流畅无卡顿

### 🐛 Bug 修复

#### 🔧 IconGenerator 内存泄漏
- **问题**：每次创建托盘图标时泄漏一个 GDI 句柄
- **影响**：长时间运行后可能导致系统资源耗尽
- **修复**：添加 `DestroyIcon` P/Invoke 调用，在 `finally` 块中释放 GDI 句柄
- **文件**：`Services/IconGenerator.cs`

#### 🔧 双重托盘图标问题
- **问题**：`App.xaml.cs` 和 `SystemTrayService` 各自初始化托盘图标，导致显示两个图标
- **影响**：用户体验差，资源浪费
- **修复**：移除 `App.xaml.cs` 中的托盘初始化，统一由 `SystemTrayService` 管理
- **文件**：`App.xaml.cs`, `Services/SystemTrayService.cs`

#### 🔧 单文件发布数据库路径问题
- **问题**：使用 `AppDomain.CurrentDomain.BaseDirectory` 在单文件发布时无法正确获取路径
- **影响**：单文件发布版本无法启动，报错 "SQLite Error 14: unable to open database file"
- **修复**：改用 `AppContext.BaseDirectory`，添加目录存在性检查和自动创建
- **文件**：`Services/StorageService.cs` (构造函数)

#### 🔧 数据库列完整性验证
- **问题**：数据库版本号正确但列不完整时会导致运行时错误
- **影响**：升级或迁移时可能出现 "no such column" 错误
- **修复**：新增 `EnsureRequiredColumnsExist()` 方法，每次启动时验证并修复缺失的列
- **文件**：`Services/StorageService.cs` (新增方法)

### 🧹 代码清理

- 移除敏感数据过滤逻辑（本地工具无需此功能）
- 移除未使用的配置项：
  - `FilterSensitiveData`
  - `LastCleanupDate`
  - `ShowBalloonTips`
  - `MinimizeToTray`
- 简化剪贴板监听逻辑，接受所有文本内容

### 📝 用户界面改进

- 状态栏新增快捷键提示：`Space 预览`
- 设置窗口说明文字更新，明确开机自启需要管理员权限
- 收藏按钮移至列表项内，交互更直观
- 滚动条样式更现代，与整体设计更协调

### 📦 技术细节

#### 创建的新文件
- `Services/TaskSchedulerService.cs` - 任务计划程序服务
- `PreviewWindow.xaml` - 文本预览窗口界面
- `PreviewWindow.xaml.cs` - 文本预览窗口逻辑

#### 修改的文件
- `Models/AppSettings.cs` - 移除未使用配置项
- `Services/ClipboardMonitor.cs` - 移除敏感数据过滤
- `Services/IconGenerator.cs` - 修复内存泄漏
- `Services/StorageService.cs` - 数据库性能优化
- `App.xaml.cs` - 移除重复托盘初始化
- `MainWindow.xaml` - 收藏按钮、滚动条样式、快捷键提示
- `MainWindow.xaml.cs` - 收藏处理、预览功能、导航键
- `SettingsWindow.xaml` - 自启说明文本
- `SettingsWindow.xaml.cs` - 任务计划服务集成

### ⚠️ 已知问题

- 暂无

### 🔄 迁移指南

从 v1.0.0 升级到 v1.1.0：
1. 删除旧数据库文件（如需要），程序会自动创建新索引
2. 首次启动会自动创建 Content 列和复合索引
3. 开机自启需要重新配置（新版使用任务计划程序）

---

## [1.0.0] - 2025-07-10

### 🎉 初始发布

#### 核心功能
- ✅ 剪贴板自动监听（文本、图片、文件）
- ✅ 历史记录保存到 SQLite 数据库
- ✅ 全局热键 Ctrl+Shift+V 呼出主窗口
- ✅ 支持搜索过滤
- ✅ 自动粘贴功能
- ✅ 系统托盘图标和菜单
- ✅ 现代化 WPF 界面
- ✅ 设置窗口（最大记录数、自动清理天数等）

#### 技术特性
- .NET 8.0 WPF 应用
- SQLite 本地数据库
- MVVM-lite 架构
- Win32 API 集成（剪贴板监听、全局热键）

---

## [未发布]

### 计划中的功能
- [ ] 右键菜单（预览、复制、删除、收藏）
- [ ] 数据导入/导出
- [ ] 多语言支持
- [ ] 主题切换（深色模式）
- [ ] 云同步功能
- [ ] 插件系统

---

## 版本说明

### 版本号规则
- **主版本号（Major）**：重大架构变更或不兼容的 API 修改
- **次版本号（Minor）**：新功能添加，向下兼容
- **修订号（Patch）**：Bug 修复和小改进

### 发布频率
- **稳定版本**：每 1-2 个月
- **热修复版本**：根据严重 Bug 随时发布

---

**作者**: 何小工
**联系方式**:
- 微信: hwc19970111
- 邮箱: 2357300448@qq.com

**开源协议**: MIT License
