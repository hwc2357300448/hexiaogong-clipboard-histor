using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace ClipboardHistory.Services
{
    public class StartupService
    {
        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "ClipboardHistory";
        
        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查开机自启状态失败: {ex.Message}");
                return false;
            }
        }
        
        public bool SetStartupEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                if (key == null)
                {
                    return false;
                }
                
                if (enabled)
                {
                    var exePath = Assembly.GetExecutingAssembly().Location;
                    if (exePath.EndsWith(".dll"))
                    {
                        // 如果是 .dll 文件，需要获取 .exe 文件路径
                        exePath = Path.ChangeExtension(exePath, ".exe");
                    }
                    
                    key.SetValue(AppName, $"\"{exePath}\"");
                    Console.WriteLine($"已启用开机自启: {exePath}");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Console.WriteLine("已禁用开机自启");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置开机自启失败: {ex.Message}");
                return false;
            }
        }
    }
}