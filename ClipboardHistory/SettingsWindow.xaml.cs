using ClipboardHistory.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClipboardHistory
{
    public partial class SettingsWindow : Window
    {
        private readonly StartupService _startupService;
        private readonly SettingsService _settingsService;
        
        public SettingsWindow()
        {
            InitializeComponent();
            _startupService = new StartupService();
            _settingsService = new SettingsService();
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            var settings = _settingsService.Settings;
            
            // 加载开机自启设置
            AutoStartCheckBox.IsChecked = _startupService.IsStartupEnabled();
            
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
            if (!_startupService.SetStartupEnabled(true))
            {
                MessageBox.Show("设置开机自启失败，请以管理员权限运行程序。", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                AutoStartCheckBox.IsChecked = false;
            }
        }
        
        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_startupService.SetStartupEnabled(false))
            {
                MessageBox.Show("取消开机自启失败。", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                AutoStartCheckBox.IsChecked = true;
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