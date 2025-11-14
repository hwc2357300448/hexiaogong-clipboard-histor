using ClipboardHistory.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClipboardHistory
{
    public partial class SettingsWindow : Window
    {
        private readonly TaskSchedulerService _taskSchedulerService;
        private readonly SettingsService _settingsService;
        private bool _isProcessingStartup = false; // 防止重复触发

        public SettingsWindow()
        {
            InitializeComponent();
            _taskSchedulerService = new TaskSchedulerService();
            _settingsService = new SettingsService();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Settings;

            // 加载开机自启设置
            AutoStartCheckBox.IsChecked = _taskSchedulerService.IsStartupTaskEnabled();
            
            // 加载历史记录设置
            MaxHistoryCountTextBox.Text = settings.MaxHistoryCount.ToString();
            AutoCleanupDaysTextBox.Text = settings.AutoCleanupDays.ToString();
            EnableAutoCleanupCheckBox.IsChecked = settings.EnableAutoCleanup;
        }
        
        private void SaveSettings()
        {
            var settings = _settingsService.Settings;
            
            // 保存历史记录设置
            if (int.TryParse(MaxHistoryCountTextBox.Text, out int maxCount))
            {
                _settingsService.UpdateMaxHistoryCount(maxCount);
            }
            
            if (int.TryParse(AutoCleanupDaysTextBox.Text, out int cleanupDays))
            {
                _settingsService.UpdateAutoCleanupDays(cleanupDays);
            }
            
            _settingsService.UpdateEnableAutoCleanup(EnableAutoCleanupCheckBox.IsChecked ?? false);
        }
        
        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isProcessingStartup) return;
            _isProcessingStartup = true;
            try
            {
                if (!_taskSchedulerService.CreateStartupTask())
                {
                    MessageBox.Show("创建开机自启任务失败。可能原因：\n1. 用户取消了UAC权限请求\n2. 系统权限不足\n\n请允许UAC提示以启用此功能。", "设置失败",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AutoStartCheckBox.IsChecked = false;
                }
                else
                {
                    MessageBox.Show("开机自启任务已创建成功！\n程序将在下次登录时以管理员权限自动启动。", "设置成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                _isProcessingStartup = false;
            }
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isProcessingStartup) return;
            _isProcessingStartup = true;
            try
            {
                if (!_taskSchedulerService.DeleteStartupTask())
                {
                    MessageBox.Show("删除开机自启任务失败。可能原因：\n1. 用户取消了UAC权限请求\n2. 任务不存在\n\n请允许UAC提示以取消此功能。", "删除失败",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AutoStartCheckBox.IsChecked = true;
                }
                else
                {
                    MessageBox.Show("开机自启任务已删除。", "删除成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                _isProcessingStartup = false;
            }
        }
        
        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许数字输入
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要重置所有设置为默认值吗？", "确认重置", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _settingsService.ResetToDefault();
                LoadSettings();
                MessageBox.Show("设置已重置为默认值。", "重置完成", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}