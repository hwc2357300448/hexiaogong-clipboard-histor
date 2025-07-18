using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClipboardHistory.Models;
using Microsoft.Data.Sqlite;

namespace ClipboardHistory.Services
{
    public class StorageService
    {
        private readonly string _connectionString;
        private readonly AppSettings _settings;

        public StorageService(AppSettings settings)
        {
            _settings = settings;
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.DatabasePath);
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClipboardItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT NOT NULL,
                    DataType INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    IsFavorite INTEGER NOT NULL DEFAULT 0,
                    FilePath TEXT,
                    ImageData BLOB
                )";
            createTableCommand.ExecuteNonQuery();

            var createIndexCommand = connection.CreateCommand();
            createIndexCommand.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt 
                ON ClipboardItems(CreatedAt)";
            createIndexCommand.ExecuteNonQuery();
        }

        public async Task<int> AddItemAsync(ClipboardItem item)
        {
            Console.WriteLine($"尝试保存项目到数据库: {item.Content.Substring(0, Math.Min(50, item.Content.Length))}...");
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            Console.WriteLine("数据库连接已打开");

            // 检查是否已存在相同内容且时间很近（比如10秒内）
            var recentItem = await GetRecentItemByContentAsync(item.Content, item.DataType);
            if (recentItem != null && (DateTime.Now - recentItem.CreatedAt).TotalSeconds < 10)
            {
                Console.WriteLine("发现10秒内的重复项目，更新时间戳");
                // 更新时间
                await UpdateItemTimestampAsync(recentItem.Id);
                return recentItem.Id;
            }

            // 清理旧记录
            await CleanupOldItemsAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ClipboardItems (Content, DataType, CreatedAt, IsFavorite, FilePath, ImageData)
                VALUES (@content, @dataType, @createdAt, @isFavorite, @filePath, @imageData)";
            
            command.Parameters.AddWithValue("@content", item.Content);
            command.Parameters.AddWithValue("@dataType", (int)item.DataType);
            command.Parameters.AddWithValue("@createdAt", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@isFavorite", item.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("@filePath", item.FilePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imageData", item.ImageData ?? (object)DBNull.Value);

            Console.WriteLine("执行插入命令");
            await command.ExecuteNonQueryAsync();

            command.CommandText = "SELECT last_insert_rowid()";
            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);
            Console.WriteLine($"新项目已保存，ID: {id}");
            return id;
        }

        public async Task<List<ClipboardItem>> GetItemsAsync(int limit = 100, string? searchTerm = null)
        {
            Console.WriteLine($"GetItemsAsync: 获取记录，限制: {limit}, 搜索词: '{searchTerm}'");
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            Console.WriteLine("数据库连接已打开");

            var command = connection.CreateCommand();
            var whereClause = string.IsNullOrWhiteSpace(searchTerm) 
                ? "" 
                : "WHERE Content LIKE @searchTerm";
            
            command.CommandText = $@"
                SELECT Id, Content, DataType, CreatedAt, IsFavorite, FilePath, ImageData
                FROM ClipboardItems
                {whereClause}
                ORDER BY IsFavorite DESC, CreatedAt DESC
                LIMIT @limit";

            command.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
            }

            Console.WriteLine($"执行查询: {command.CommandText}");
            var items = new List<ClipboardItem>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                items.Add(new ClipboardItem
                {
                    Id = reader.GetInt32("Id"),
                    Content = reader.GetString("Content"),
                    DataType = (ClipboardDataType)reader.GetInt32("DataType"),
                    CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                    IsFavorite = reader.GetInt32("IsFavorite") == 1,
                    FilePath = reader.IsDBNull("FilePath") ? null : reader.GetString("FilePath"),
                    ImageData = reader.IsDBNull("ImageData") ? null : (byte[])reader["ImageData"]
                });
            }

            Console.WriteLine($"查询结果: 找到 {items.Count} 条记录");
            return items;
        }

        public async Task<ClipboardItem?> GetRecentItemByContentAsync(string content, ClipboardDataType dataType)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Content, DataType, CreatedAt, IsFavorite, FilePath, ImageData
                FROM ClipboardItems
                WHERE Content = @content AND DataType = @dataType
                ORDER BY CreatedAt DESC
                LIMIT 1";

            command.Parameters.AddWithValue("@content", content);
            command.Parameters.AddWithValue("@dataType", (int)dataType);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ClipboardItem
                {
                    Id = reader.GetInt32("Id"),
                    Content = reader.GetString("Content"),
                    DataType = (ClipboardDataType)reader.GetInt32("DataType"),
                    CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                    IsFavorite = reader.GetInt32("IsFavorite") == 1,
                    FilePath = reader.IsDBNull("FilePath") ? null : reader.GetString("FilePath"),
                    ImageData = reader.IsDBNull("ImageData") ? null : (byte[])reader["ImageData"]
                };
            }

            return null;
        }

        public async Task<ClipboardItem?> GetItemByContentAsync(string content, ClipboardDataType dataType)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Content, DataType, CreatedAt, IsFavorite, FilePath, ImageData
                FROM ClipboardItems
                WHERE Content = @content AND DataType = @dataType
                ORDER BY CreatedAt DESC
                LIMIT 1";

            command.Parameters.AddWithValue("@content", content);
            command.Parameters.AddWithValue("@dataType", (int)dataType);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ClipboardItem
                {
                    Id = reader.GetInt32("Id"),
                    Content = reader.GetString("Content"),
                    DataType = (ClipboardDataType)reader.GetInt32("DataType"),
                    CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                    IsFavorite = reader.GetInt32("IsFavorite") == 1,
                    FilePath = reader.IsDBNull("FilePath") ? null : reader.GetString("FilePath"),
                    ImageData = reader.IsDBNull("ImageData") ? null : (byte[])reader["ImageData"]
                };
            }

            return null;
        }

        public async Task ToggleFavoriteAsync(int itemId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ClipboardItems 
                SET IsFavorite = CASE WHEN IsFavorite = 1 THEN 0 ELSE 1 END
                WHERE Id = @id";
            
            command.Parameters.AddWithValue("@id", itemId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ClipboardItems WHERE Id = @id";
            command.Parameters.AddWithValue("@id", itemId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task ClearAllItemsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ClipboardItems WHERE IsFavorite = 0";
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> CleanupOldItemsAsync(int maxCount, int maxDays)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                // 清理超过最大数量的记录（保留最新的）
                var deleteByCountCommand = connection.CreateCommand();
                deleteByCountCommand.CommandText = @"
                    DELETE FROM ClipboardItems 
                    WHERE Id NOT IN (
                        SELECT Id FROM ClipboardItems 
                        ORDER BY CreatedAt DESC 
                        LIMIT @MaxCount
                    )";
                deleteByCountCommand.Parameters.AddWithValue("@MaxCount", maxCount);
                var deletedByCount = await deleteByCountCommand.ExecuteNonQueryAsync();
                
                // 清理超过指定天数的记录
                var deleteByDateCommand = connection.CreateCommand();
                deleteByDateCommand.CommandText = @"
                    DELETE FROM ClipboardItems 
                    WHERE CreatedAt < @CutoffDate AND IsFavorite = 0";
                deleteByDateCommand.Parameters.AddWithValue("@CutoffDate", DateTime.Now.AddDays(-maxDays));
                var deletedByDate = await deleteByDateCommand.ExecuteNonQueryAsync();
                
                Console.WriteLine($"清理完成：按数量删除 {deletedByCount} 条，按日期删除 {deletedByDate} 条");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理历史记录失败: {ex.Message}");
                return false;
            }
        }
        
        public async Task<DateTime> GetOldestItemDateAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MIN(CreatedAt) FROM ClipboardItems";
                var result = await command.ExecuteScalarAsync();
                
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDateTime(result);
                }
                
                return DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最旧记录日期失败: {ex.Message}");
                return DateTime.Now;
            }
        }

        private async Task UpdateItemTimestampAsync(int itemId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ClipboardItems 
                SET CreatedAt = @createdAt 
                WHERE Id = @id";
            
            command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id", itemId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CleanupOldItemsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // 删除超过最大数量的非收藏项
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ClipboardItems 
                WHERE Id NOT IN (
                    SELECT Id FROM ClipboardItems WHERE IsFavorite = 1
                    UNION ALL
                    SELECT Id FROM (
                        SELECT Id FROM ClipboardItems 
                        WHERE IsFavorite = 0
                        ORDER BY CreatedAt DESC 
                        LIMIT @maxCount
                    )
                )";
            
            command.Parameters.AddWithValue("@maxCount", _settings.MaxHistoryCount);
            await command.ExecuteNonQueryAsync();

            // 删除超过指定天数的非收藏项
            var cutoffDate = DateTime.Now.AddDays(-_settings.AutoCleanupDays);
            command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ClipboardItems 
                WHERE IsFavorite = 0 AND CreatedAt < @cutoffDate";
            
            command.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("yyyy-MM-dd HH:mm:ss"));
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> GetItemCountAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ClipboardItems";
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}