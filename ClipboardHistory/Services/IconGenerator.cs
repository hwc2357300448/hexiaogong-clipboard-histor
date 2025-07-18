using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ClipboardHistory.Services
{
    public static class IconGenerator
    {
        public static Icon CreateClipboardIcon()
        {
            // 创建一个 32x32 的位图
            using (var bitmap = new Bitmap(32, 32))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // 设置高质量渲染
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    
                    // 清除背景
                    graphics.Clear(Color.Transparent);
                    
                    // 绘制剪贴板图标
                    using (var brush = new SolidBrush(Color.FromArgb(52, 152, 219))) // 蓝色
                    {
                        // 绘制剪贴板主体
                        graphics.FillRectangle(brush, 6, 8, 20, 20);
                        
                        // 绘制边框
                        using (var pen = new Pen(Color.FromArgb(41, 128, 185), 2))
                        {
                            graphics.DrawRectangle(pen, 6, 8, 20, 20);
                        }
                        
                        // 绘制剪贴板夹子
                        using (var clipBrush = new SolidBrush(Color.FromArgb(149, 165, 166))) // 灰色
                        {
                            graphics.FillRectangle(clipBrush, 10, 4, 12, 6);
                        }
                        
                        // 绘制文本线条
                        using (var textBrush = new SolidBrush(Color.White))
                        {
                            graphics.FillRectangle(textBrush, 9, 12, 14, 2);
                            graphics.FillRectangle(textBrush, 9, 16, 10, 2);
                            graphics.FillRectangle(textBrush, 9, 20, 12, 2);
                        }
                    }
                }
                
                // 转换为图标
                IntPtr hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
        
        public static void SaveIconToFile(string filePath)
        {
            using (var icon = CreateClipboardIcon())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    icon.Save(fileStream);
                }
            }
        }
    }
}