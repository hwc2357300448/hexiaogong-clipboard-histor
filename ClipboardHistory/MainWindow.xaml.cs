using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using ClipboardHistory.Models;
using ClipboardHistory.Services;

namespace ClipboardHistory
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Windows API for sending keystrokes
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;

        private readonly StorageService _storageService;
        private readonly HotkeyService _hotkeyService;
        private readonly SystemTrayService _trayService;
        private ClipboardItem? _selectedItem;
        private string _searchText = string.Empty;
        private string _statusText = "就绪";
        
        public bool ForceClose { get; set; } = false;

        public ObservableCollection<ClipboardItem> ClipboardItems { get; } = new();
        public bool NeedsRefresh { get; set; } = false;

        public ClipboardItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = LoadHistoryAsync();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public MainWindow(StorageService storageService, HotkeyService hotkeyService)
        {
            InitializeComponent();
            _storageService = storageService;
            _hotkeyService = hotkeyService;
            _trayService = new SystemTrayService(this);
            DataContext = this;
            
            Loaded += MainWindow_Loaded;
            IsVisibleChanged += MainWindow_IsVisibleChanged;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ForceClose)
            {
                // 最小化到托盘而不是真正关闭
                e.Cancel = true;
                Hide();
                
                // 首次最小化时显示提示
                if (!_hasShownTrayTip)
                {
                    _trayService.ShowBalloonTip("剪贴板历史", "程序已最小化到系统托盘，双击图标可重新打开");
                    _hasShownTrayTip = true;
                }
            }
            else
            {
                // 真正关闭程序时清理资源
                _trayService?.Dispose();
            }
        }
        
        private bool _hasShownTrayTip = false;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
            
            // 设置焦点到列表的第一项
            if (ClipboardItems.Count > 0)
            {
                HistoryListBox.SelectedIndex = 0;
                HistoryListBox.Focus();
                
                // 确保选中的项目获得焦点
                var container = HistoryListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                container?.Focus();
            }
            else
            {
                SearchTextBox.Focus();
            }
        }

        private async void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 当窗口变为可见且需要刷新时，重新加载数据
            if (IsVisible && NeedsRefresh)
            {
                await LoadHistoryAsync();
                NeedsRefresh = false;
            }
            
            // 每次窗口变为可见时，设置焦点到列表
            if (IsVisible)
            {
                // 延迟设置焦点，确保窗口完全显示
                await Task.Delay(50);
                if (ClipboardItems.Count > 0)
                {
                    HistoryListBox.SelectedIndex = 0;
                    HistoryListBox.Focus();
                    
                    // 确保选中的项目获得焦点
                    var container = HistoryListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    container?.Focus();
                }
            }
        }

        public async Task LoadHistoryAsync()
        {
            try
            {
                Console.WriteLine("开始加载历史记录...");
                var items = await _storageService.GetItemsAsync(100, 
                    string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
                
                Console.WriteLine($"从数据库获取到 {items.Count} 条记录");
                
                ClipboardItems.Clear();
                foreach (var item in items)
                {
                    ClipboardItems.Add(item);
                }

                // 自动选择第一项
                if (ClipboardItems.Count > 0 && SelectedItem == null)
                {
                    HistoryListBox.SelectedIndex = 0;
                }

                var count = await _storageService.GetItemCountAsync();
                StatusText = $"共 {count} 条记录，显示 {ClipboardItems.Count} 条";
                Console.WriteLine($"界面状态更新: {StatusText}");
            }
            catch (Exception ex)
            {
                StatusText = $"加载失败: {ex.Message}";
                Console.WriteLine($"加载历史记录失败: {ex.Message}");
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                // 如果搜索框有焦点，将焦点转移到列表
                if (SearchTextBox.IsFocused)
                {
                    HistoryListBox.Focus();
                    if (ClipboardItems.Count > 0)
                    {
                        HistoryListBox.SelectedIndex = 0;
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                // 如果在搜索框中按回车，转移焦点到列表并选择第一项
                if (SearchTextBox.IsFocused && ClipboardItems.Count > 0)
                {
                    HistoryListBox.Focus();
                    HistoryListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Home)
            {
                // Home 键：跳转到第一项
                if (ClipboardItems.Count > 0)
                {
                    HistoryListBox.SelectedIndex = 0;
                    HistoryListBox.ScrollIntoView(ClipboardItems[0]);
                    HistoryListBox.Focus();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.End)
            {
                // End 键：跳转到最后一项
                if (ClipboardItems.Count > 0)
                {
                    HistoryListBox.SelectedIndex = ClipboardItems.Count - 1;
                    HistoryListBox.ScrollIntoView(ClipboardItems[ClipboardItems.Count - 1]);
                    HistoryListBox.Focus();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.PageUp)
            {
                // PageUp：向上翻页
                if (ClipboardItems.Count > 0)
                {
                    int newIndex = Math.Max(0, HistoryListBox.SelectedIndex - 10);
                    HistoryListBox.SelectedIndex = newIndex;
                    HistoryListBox.ScrollIntoView(ClipboardItems[newIndex]);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.PageDown)
            {
                // PageDown：向下翻页
                if (ClipboardItems.Count > 0)
                {
                    int newIndex = Math.Min(ClipboardItems.Count - 1, HistoryListBox.SelectedIndex + 10);
                    HistoryListBox.SelectedIndex = newIndex;
                    HistoryListBox.ScrollIntoView(ClipboardItems[newIndex]);
                    e.Handled = true;
                }
            }
        }

        private async void HistoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 确保双击的是列表项而不是空白区域
            if (e.OriginalSource is FrameworkElement element)
            {
                var listBoxItem = FindParent<ListBoxItem>(element);
                if (listBoxItem != null && listBoxItem.DataContext is ClipboardItem)
                {
                    await PasteSelectedItemAsync();
                }
            }
        }

        private async void HistoryListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SelectedItem != null)
            {
                await PasteSelectedItemAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && SelectedItem != null)
            {
                await DeleteSelectedItemAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.Space && SelectedItem != null)
            {
                // Space 键：显示预览窗口
                ShowPreview();
                e.Handled = true;
            }
            else if (e.Key == Key.F)
            {
                // Ctrl+F: 聚焦搜索框
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
                // F: 切换收藏状态
                else if (SelectedItem != null)
                {
                    await _storageService.ToggleFavoriteAsync(SelectedItem.Id);
                    await LoadHistoryAsync();
                    StatusText = SelectedItem.IsFavorite ? "已取消收藏" : "已收藏";
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                // Ctrl+↑: 上移收藏项
                if (SelectedItem != null && SelectedItem.IsFavorite)
                {
                    var currentIndex = HistoryListBox.SelectedIndex;
                    await _storageService.MoveFavoriteUpAsync(SelectedItem.Id);
                    await LoadHistoryAsync();

                    // 保持选中同一项（索引可能会变）
                    if (currentIndex > 0)
                    {
                        HistoryListBox.SelectedIndex = currentIndex - 1;
                    }
                    StatusText = "已上移";
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                // Ctrl+↓: 下移收藏项
                if (SelectedItem != null && SelectedItem.IsFavorite)
                {
                    var currentIndex = HistoryListBox.SelectedIndex;
                    await _storageService.MoveFavoriteDownAsync(SelectedItem.Id);
                    await LoadHistoryAsync();

                    // 保持选中同一项（索引可能会变）
                    if (currentIndex < ClipboardItems.Count - 1)
                    {
                        HistoryListBox.SelectedIndex = currentIndex + 1;
                    }
                    StatusText = "已下移";
                    e.Handled = true;
                }
            }
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                await _storageService.ToggleFavoriteAsync(SelectedItem.Id);
                await LoadHistoryAsync();
                StatusText = "收藏状态已切换";
            }
        }

        private async void FavoriteIconButton_Click(object sender, RoutedEventArgs e)
        {
            // 从按钮获取数据上下文中的 ClipboardItem
            if (sender is Button button && button.DataContext is ClipboardItem item)
            {
                await _storageService.ToggleFavoriteAsync(item.Id);
                await LoadHistoryAsync();
                StatusText = item.IsFavorite ? "已取消收藏" : "已收藏";

                // 阻止事件冒泡到ListBoxItem，避免触发选择
                e.Handled = true;
            }
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要清空所有非收藏的历史记录吗？", "确认清空", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await _storageService.ClearAllItemsAsync();
                await LoadHistoryAsync();
                StatusText = "历史记录已清空";
            }
        }

        private async Task PasteSelectedItemAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                // 先设置剪贴板内容
                switch (SelectedItem.DataType)
                {
                    case ClipboardDataType.Text:
                        Clipboard.SetText(SelectedItem.Content);
                        break;
                    case ClipboardDataType.Image when SelectedItem.ImageData != null:
                        var image = ImageFromByteArray(SelectedItem.ImageData);
                        Clipboard.SetImage(image);
                        break;
                    case ClipboardDataType.Files when !string.IsNullOrEmpty(SelectedItem.FilePath):
                        var files = SelectedItem.FilePath.Split(';');
                        var fileDropList = new System.Collections.Specialized.StringCollection();
                        fileDropList.AddRange(files);
                        Clipboard.SetFileDropList(fileDropList);
                        break;
                }

                StatusText = "正在粘贴...";
                Console.WriteLine($"已设置剪贴板内容: {SelectedItem.Preview}");
                
                // 先隐藏窗口
                Hide();
                
                // 等待窗口完全隐藏和焦点切换
                await Task.Delay(200);
                
                // 自动发送 Ctrl+V
                SendCtrlV();
                
                StatusText = "粘贴完成";
            }
            catch (Exception ex)
            {
                StatusText = $"粘贴失败: {ex.Message}";
                Console.WriteLine($"粘贴失败: {ex.Message}");
            }
        }

        private void SendCtrlV()
        {
            try
            {
                // 确保有足够的时间切换到目标窗口
                System.Threading.Thread.Sleep(50);
                
                // 按下 Ctrl
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(10);
                
                // 按下 V
                keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(10);
                
                // 释放 V
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                System.Threading.Thread.Sleep(10);
                
                // 释放 Ctrl
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                Console.WriteLine("已发送 Ctrl+V 组合键");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送按键失败: {ex.Message}");
            }
        }

        private async Task DeleteSelectedItemAsync()
        {
            if (SelectedItem == null) return;

            await _storageService.DeleteItemAsync(SelectedItem.Id);
            await LoadHistoryAsync();
            StatusText = "项目已删除";
        }

        private void ShowPreview()
        {
            if (SelectedItem == null) return;

            // 支持文本和图片预览
            if (SelectedItem.DataType == ClipboardDataType.Files)
            {
                StatusText = "文件列表暂不支持预览";
                return;
            }

            try
            {
                var previewWindow = new PreviewWindow(SelectedItem);
                previewWindow.Owner = this;
                previewWindow.ShowDialog();
                StatusText = "预览已关闭";
            }
            catch (Exception ex)
            {
                StatusText = $"打开预览失败: {ex.Message}";
                Console.WriteLine($"打开预览失败: {ex.Message}");
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource ImageFromByteArray(byte[] data)
        {
            using var stream = new System.IO.MemoryStream(data);
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                stream, 
                System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat, 
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            return decoder.Frames[0];
        }

        // 辅助方法：查找父元素
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            
            if (parentObject == null) return null;
            
            if (parentObject is T parent)
                return parent;
            
            return FindParent<T>(parentObject);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DataTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ClipboardDataType.Text => "文本",
                ClipboardDataType.Image => "图片",
                ClipboardDataType.Files => "文件",
                _ => "未知"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FavoriteTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? "取消收藏 (F)" : "收藏 (F)";
            }
            return "收藏";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}