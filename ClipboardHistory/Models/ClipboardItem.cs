using System;

namespace ClipboardHistory.Models
{
    public enum ClipboardDataType
    {
        Text,
        Image,
        Files
    }

    public class ClipboardItem
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public ClipboardDataType DataType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFavorite { get; set; }
        public string? FilePath { get; set; }
        public byte[]? ImageData { get; set; }
        
        public string Preview => GetPreview();
        
        private string GetPreview()
        {
            return DataType switch
            {
                ClipboardDataType.Text => Content.Length > 100 ? Content[..100] + "..." : Content,
                ClipboardDataType.Image => $"图片 ({CreatedAt:HH:mm:ss})",
                ClipboardDataType.Files => $"文件: {Content}",
                _ => Content
            };
        }
    }
}