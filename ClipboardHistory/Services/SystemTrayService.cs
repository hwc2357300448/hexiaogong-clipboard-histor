using System;
using System.Drawing;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace ClipboardHistory.Services
{
    public class SystemTrayService : IDisposable
    {
        private TaskbarIcon _taskbarIcon;
        private readonly MainWindow _mainWindow;
        private Icon _trayIcon;
        
        public SystemTrayService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeTrayIcon();
        }
        
        private void InitializeTrayIcon()
        {
            try
            {
                // 创建托盘图标
                _trayIcon = IconGenerator.CreateClipboardIcon();
                
                _taskbarIcon = new TaskbarIcon
                {
                    Icon = _trayIcon,
                    ToolTipText = "剪贴板历史 - 双击打开"
                };
                
                // 添加事件处理
                _taskbarIcon.TrayLeftMouseDown += OnTrayIconClick;
                _taskbarIcon.TrayMouseDoubleClick += OnTrayIconDoubleClick;
                
                // 创建右键菜单
                CreateContextMenu();
                
                Console.WriteLine("系统托盘图标已创建");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建托盘图标失败: {ex.Message}");
            }
        }
        
        private void CreateContextMenu()
        {
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            // 打开/隐藏菜单项
            var toggleMenuItem = new System.Windows.Controls.MenuItem { Header = "打开剪贴板历史" };
            toggleMenuItem.Click += (s, e) => ToggleMainWindow();
            contextMenu.Items.Add(toggleMenuItem);
            
            // 分隔线
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            // 设置菜单项
            var settingsMenuItem = new System.Windows.Controls.MenuItem { Header = "设置" };
            settingsMenuItem.Click += (s, e) => OpenSettings();
            contextMenu.Items.Add(settingsMenuItem);
            
            // 分隔线
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            // 退出菜单项
            var exitMenuItem = new System.Windows.Controls.MenuItem { Header = "退出" };
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitMenuItem);
            
            _taskbarIcon.ContextMenu = contextMenu;
        }
        
        private void OnTrayIconDoubleClick(object sender, RoutedEventArgs e)
        {
            ToggleMainWindow();
        }
        
        private void OnTrayIconClick(object sender, RoutedEventArgs e)
        {
            ToggleMainWindow();
        }
        
        private void ToggleMainWindow()
        {
            if (_mainWindow.IsVisible)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }
        
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = _mainWindow;
            settingsWindow.ShowDialog();
        }
        
        private void ExitApplication()
        {
            // 确认退出
            var result = System.Windows.MessageBox.Show(
                "确定要退出剪贴板历史吗？", 
                "确认退出", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _mainWindow.ForceClose = true;
                System.Windows.Application.Current.Shutdown();
            }
        }
        
        public void ShowBalloonTip(string title, string text)
        {
            _taskbarIcon?.ShowBalloonTip(title, text, BalloonIcon.Info);
        }
        
        public void Dispose()
        {
            _taskbarIcon?.Dispose();
            _trayIcon?.Dispose();
        }
    }
}