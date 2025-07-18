using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ClipboardHistory.Models;
using ClipboardHistory.Services;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace ClipboardHistory
{
    public partial class App : Application
    {
        private TaskbarIcon? _trayIcon;
        private ClipboardMonitor? _clipboardMonitor;
        private HotkeyService? _hotkeyService;
        private StorageService? _storageService;
        private MainWindow? _mainWindow;
        private AppSettings _settings = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Console.WriteLine("正在启动应用...");
                await LoadSettingsAsync();
                Console.WriteLine("设置已加载");
                
                InitializeServices();
                Console.WriteLine("服务已初始化");
                
                InitializeTrayIcon();
                Console.WriteLine("托盘图标已创建");
                
                InitializeMainWindow();
                Console.WriteLine("主窗口已创建");
                
                SetupHotkey();
                Console.WriteLine("热键已设置");
                
                StartClipboardMonitoring();
                Console.WriteLine("剪贴板监听已启动");
                
                Console.WriteLine("应用启动完成！请复制一些文本然后按 Ctrl+Shift+V 测试");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动时出错: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    await SaveSettingsAsync();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            _storageService = new StorageService(_settings);
            _hotkeyService = new HotkeyService();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "剪贴板历史工具"
            };

            // 创建简单的图标或使用默认图标
            try
            {
                if (File.Exists("icon.ico"))
                {
                    _trayIcon.Icon = new System.Drawing.Icon("icon.ico");
                }
                else
                {
                    // 使用系统默认图标
                    _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            var showHistoryItem = new System.Windows.Controls.MenuItem { Header = "显示历史" };
            showHistoryItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(showHistoryItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var settingsItem = new System.Windows.Controls.MenuItem { Header = "设置" };
            settingsItem.Click += (s, e) => MessageBox.Show("设置功能开发中...", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
            contextMenu.Items.Add(settingsItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
            exitItem.Click += async (s, e) => { await SaveSettingsAsync(); Current.Shutdown(); };
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;
            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
        }

        private void InitializeMainWindow()
        {
            _mainWindow = new MainWindow(_storageService!, _hotkeyService!);
        }

        private void SetupHotkey()
        {
            if (_mainWindow != null)
            {
                var windowHandle = new WindowInteropHelper(_mainWindow).EnsureHandle();
                var source = HwndSource.FromHwnd(windowHandle);
                source?.AddHook(WndProc);

                if (_hotkeyService!.RegisterHotkey(windowHandle, _settings.HotKey))
                {
                    _hotkeyService.HotkeyPressed += ShowMainWindow;
                    Console.WriteLine("全局热键注册成功");
                }
                else
                {
                    Console.WriteLine("全局热键注册失败");
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotkeyService?.ProcessHotkeyMessage(hwnd, msg, wParam, lParam) ?? false;
            return IntPtr.Zero;
        }

        private void StartClipboardMonitoring()
        {
            if (_mainWindow != null)
            {
                _clipboardMonitor = new ClipboardMonitor(_settings);
                _clipboardMonitor.ClipboardChanged += OnClipboardChanged;
                _clipboardMonitor.StartMonitoring(_mainWindow);
                Console.WriteLine("剪贴板监听器已启动");
            }
        }

        private async void OnClipboardChanged(ClipboardItem item)
        {
            try
            {
                Console.WriteLine($"检测到剪贴板变化: {item.Preview}");
                await _storageService!.AddItemAsync(item);
                Console.WriteLine("项目已保存到数据库");
                
                // 无论窗口是否可见都标记需要刷新
                if (_mainWindow != null)
                {
                    _mainWindow.NeedsRefresh = true;
                    
                    // 如果窗口当前可见，立即刷新
                    if (_mainWindow.IsVisible)
                    {
                        await _mainWindow.LoadHistoryAsync();
                        Console.WriteLine("界面已立即更新");
                    }
                    else
                    {
                        Console.WriteLine("窗口未显示，标记为需要刷新");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存剪贴板项目失败: {ex.Message}");
            }
        }

        private void ShowMainWindow()
        {
            Console.WriteLine("显示主窗口");
            if (_mainWindow != null)
            {
                if (!_mainWindow.IsVisible)
                {
                    var workingArea = SystemParameters.WorkArea;
                    _mainWindow.Left = (workingArea.Width - _mainWindow.Width) / 2;
                    _mainWindow.Top = (workingArea.Height - _mainWindow.Height) / 2;
                    
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    
                    // 显示窗口时总是刷新数据，确保显示最新内容
                    _ = _mainWindow.LoadHistoryAsync();
                }
                else
                {
                    _mainWindow.Activate();
                    
                    // 如果窗口已显示但有新数据，也要刷新
                    if (_mainWindow.NeedsRefresh)
                    {
                        _ = _mainWindow.LoadHistoryAsync();
                        _mainWindow.NeedsRefresh = false;
                    }
                }
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _clipboardMonitor?.Dispose();
            _hotkeyService?.Dispose();
            _trayIcon?.Dispose();
            await SaveSettingsAsync();
            base.OnExit(e);
        }
    }
}