using System;

namespace ClipboardHistory.Models
{
    public class AppSettings
    {
        public int MaxHistoryCount { get; set; } = 500;
        public bool StartWithWindows { get; set; } = true;
        public bool MonitorClipboard { get; set; } = true;
        public string HotKey { get; set; } = "Ctrl+Shift+V";
        public bool EnableImageCapture { get; set; } = true;
        public bool EnableFileCapture { get; set; } = true;
        public int AutoCleanupDays { get; set; } = 30;
        public string DatabasePath { get; set; } = "clipboard_history.db";
        
        // 新增设置项
        public string Author { get; set; } = "何文才";
        public string Version { get; set; } = "1.0.0";
        public bool EnableAutoCleanup { get; set; } = true;
        
        public static AppSettings Default => new AppSettings();
    }
}