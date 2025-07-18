using System;
using System.IO;
using System.Text.Json;
using ClipboardHistory.Models;

namespace ClipboardHistory.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings;
        
        public SettingsService()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipboardHistory",
                "settings.json"
            );
            
            LoadSettings();
        }
        
        public AppSettings Settings => _settings;
        
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.Default;
                }
                else
                {
                    _settings = AppSettings.Default;
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
                _settings = AppSettings.Default;
            }
        }
        
        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                File.WriteAllText(_settingsPath, json);
                Console.WriteLine("设置已保存");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }
        
        public void UpdateMaxHistoryCount(int count)
        {
            _settings.MaxHistoryCount = Math.Max(10, Math.Min(10000, count));
            SaveSettings();
        }
        
        public void UpdateAutoCleanupDays(int days)
        {
            _settings.AutoCleanupDays = Math.Max(1, Math.Min(365, days));
            SaveSettings();
        }
        
        public void UpdateStartWithWindows(bool startWithWindows)
        {
            _settings.StartWithWindows = startWithWindows;
            SaveSettings();
        }
        
        public void UpdateEnableAutoCleanup(bool enabled)
        {
            _settings.EnableAutoCleanup = enabled;
            SaveSettings();
        }
        
        public void ResetToDefault()
        {
            _settings = AppSettings.Default;
            SaveSettings();
        }
    }
}