using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using ClipboardHistory.Models;

namespace ClipboardHistory
{
    public partial class PreviewWindow : Window
    {
        private readonly ClipboardItem _item;

        public PreviewWindow(ClipboardItem item)
        {
            InitializeComponent();
            _item = item;
            LoadContent();
        }

        private void LoadContent()
        {
            if (_item.DataType == ClipboardDataType.Image)
            {
                LoadImageContent();
            }
            else if (_item.DataType == ClipboardDataType.Text)
            {
                LoadTextContent();
            }
            else
            {
                TitleTextBlock.Text = "不支持的预览类型";
                ContentTextBox.Text = "该类型暂不支持预览";
                ContentTextBox.Visibility = Visibility.Visible;
            }
        }

        private void LoadImageContent()
        {
            try
            {
                // 设置标题
                TitleTextBlock.Text = $"图片预览 - {_item.CreatedAt:yyyy-MM-dd HH:mm:ss}";

                // 加载图片
                if (_item.ImageData != null && _item.ImageData.Length > 0)
                {
                    using (var stream = new MemoryStream(_item.ImageData))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        PreviewImage.Source = bitmap;

                        // 显示图片尺寸信息
                        TitleTextBlock.Text += $" ({bitmap.PixelWidth}×{bitmap.PixelHeight})";
                    }

                    ImageScrollViewer.Visibility = Visibility.Visible;
                    ContentTextBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    throw new Exception("图片数据为空");
                }
            }
            catch (Exception ex)
            {
                TitleTextBlock.Text = "图片加载失败";
                ContentTextBox.Text = $"无法加载图片: {ex.Message}";
                ContentTextBox.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadTextContent()
        {
            // 设置标题
            TitleTextBlock.Text = $"文本预览 - {_item.CreatedAt:yyyy-MM-dd HH:mm:ss}";

            // 加载内容
            string content = _item.Content;

            // 尝试格式化 JSON
            if (TryFormatJson(content, out string formattedJson))
            {
                ContentTextBox.Text = formattedJson;
                TitleTextBlock.Text += " (JSON)";
            }
            // 尝试格式化 XML
            else if (TryFormatXml(content, out string formattedXml))
            {
                ContentTextBox.Text = formattedXml;
                TitleTextBlock.Text += " (XML)";
            }
            else
            {
                // 默认显示原始内容
                ContentTextBox.Text = content;
            }

            ContentTextBox.Visibility = Visibility.Visible;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
        }

        private bool TryFormatJson(string content, out string formatted)
        {
            formatted = string.Empty;
            try
            {
                // 去除前后空白
                content = content.Trim();

                // 检查是否以 { 或 [ 开头
                if (!content.StartsWith("{") && !content.StartsWith("["))
                {
                    return false;
                }

                // 尝试解析并格式化
                using var document = JsonDocument.Parse(content);
                formatted = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryFormatXml(string content, out string formatted)
        {
            formatted = string.Empty;
            try
            {
                // 去除前后空白
                content = content.Trim();

                // 检查是否以 < 开头
                if (!content.StartsWith("<"))
                {
                    return false;
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);

                // 格式化输出
                using var stringWriter = new System.IO.StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace
                });

                xmlDoc.Save(xmlWriter);
                formatted = stringWriter.ToString();
                return true;
            }
            catch
            {
                return false;
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
            Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ContentTextBox.Text);
                MessageBox.Show("内容已复制到剪贴板", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Esc 键关闭窗口
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
