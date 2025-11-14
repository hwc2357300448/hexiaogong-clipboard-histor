using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ClipboardHistory.Models;

namespace ClipboardHistory.Services
{
    public class ClipboardMonitor : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr _hwnd;
        private HwndSource? _hwndSource;
        private readonly AppSettings _settings;
        
        public event Action<ClipboardItem>? ClipboardChanged;

        public ClipboardMonitor(AppSettings settings)
        {
            _settings = settings;
        }

        public void StartMonitoring(Window window)
        {
            Console.WriteLine("开始设置剪贴板监听...");
            
            // 确保窗口句柄已创建
            var windowInteropHelper = new WindowInteropHelper(window);
            _hwnd = windowInteropHelper.EnsureHandle();
            
            Console.WriteLine($"窗口句柄: {_hwnd}");
            
            // 获取HwndSource
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            if (_hwndSource == null)
            {
                Console.WriteLine("无法获取HwndSource");
                return;
            }
            
            // 添加消息钩子
            _hwndSource.AddHook(WndProc);
            Console.WriteLine("消息钩子已添加");
            
            // 注册剪贴板格式监听器
            bool success = NativeMethods.AddClipboardFormatListener(_hwnd);
            Console.WriteLine($"剪贴板监听注册结果: {success}");
            
            if (!success)
            {
                var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Console.WriteLine($"注册失败，错误代码: {error}");
            }
            else
            {
                // 立即检查一次剪贴板内容作为测试
                Console.WriteLine("立即检查剪贴板内容作为测试...");
                ProcessClipboardChange();
            }
        }

        public void StopMonitoring()
        {
            Console.WriteLine("停止剪贴板监听");
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.RemoveClipboardFormatListener(_hwnd);
                _hwndSource?.RemoveHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 调试：记录所有消息
            if (msg == WM_CLIPBOARDUPDATE)
            {
                Console.WriteLine($"收到剪贴板更新消息 (msg={msg:X})");
                if (_settings.MonitorClipboard)
                {
                    Console.WriteLine("开始处理剪贴板变化");
                    ProcessClipboardChange();
                }
                else
                {
                    Console.WriteLine("剪贴板监听被禁用");
                }
            }
            return IntPtr.Zero;
        }

        private void ProcessClipboardChange()
        {
            try
            {
                Console.WriteLine("处理剪贴板变化...");
                
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    Console.WriteLine($"检测到文本: {text.Substring(0, Math.Min(50, text.Length))}...");

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var item = new ClipboardItem
                        {
                            Content = text,
                            DataType = ClipboardDataType.Text,
                            CreatedAt = DateTime.Now
                        };
                        Console.WriteLine("触发剪贴板变化事件");
                        ClipboardChanged?.Invoke(item);
                    }
                    else
                    {
                        Console.WriteLine("文本被过滤或为空");
                    }
                }
                else if (Clipboard.ContainsImage() && _settings.EnableImageCapture)
                {
                    Console.WriteLine("检测到图片");
                    var image = Clipboard.GetImage();
                    if (image != null)
                    {
                        var item = new ClipboardItem
                        {
                            Content = "图片",
                            DataType = ClipboardDataType.Image,
                            CreatedAt = DateTime.Now,
                            ImageData = BitmapSourceToByteArray(image)
                        };
                        ClipboardChanged?.Invoke(item);
                    }
                }
                else if (Clipboard.ContainsFileDropList() && _settings.EnableFileCapture)
                {
                    Console.WriteLine("检测到文件");
                    var files = Clipboard.GetFileDropList();
                    if (files.Count > 0)
                    {
                        var fileList = string.Join(", ", files.Cast<string>().Select(Path.GetFileName));
                        var item = new ClipboardItem
                        {
                            Content = fileList,
                            DataType = ClipboardDataType.Files,
                            CreatedAt = DateTime.Now,
                            FilePath = string.Join(";", files.Cast<string>())
                        };
                        ClipboardChanged?.Invoke(item);
                    }
                }
                else
                {
                    Console.WriteLine("剪贴板不包含支持的内容类型");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理剪贴板变化时出错: {ex.Message}");
            }
        }

        private static byte[] BitmapSourceToByteArray(BitmapSource bitmapSource)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        public void Dispose()
        {
            StopMonitoring();
            _hwndSource?.Dispose();
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}