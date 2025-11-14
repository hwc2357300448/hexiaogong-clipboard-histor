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
        private int _insertCountSinceCleanup = 0;
        private const int CleanupInterval = 10; // 每10次插入清理一次
        private const int CurrentDatabaseVersion = 1; // 当前数据库版本

        public StorageService(AppSettings settings)
        {
            _settings = settings;

            // 使用 AppContext.BaseDirectory 对单文件发布更友好
            var baseDir = AppContext.BaseDirectory;

            // 如果是相对路径，则使用程序目录；如果是绝对路径，直接使用
            var dbPath = Path.IsPathRooted(settings.DatabasePath)
                ? settings.DatabasePath
                : Path.Combine(baseDir, settings.DatabasePath);

            // 确保数据库文件所在目录存在
            var dbDirectory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                try
                {
                    Directory.CreateDirectory(dbDirectory);
                    Console.WriteLine($"✓ 创建数据库目录: {dbDirectory}");
                }
                catch (Exception ex)
                {
                    var errorMsg = $"无法创建数据库目录: {dbDirectory}\n错误: {ex.Message}";
                    Console.WriteLine(errorMsg);
                    System.Windows.MessageBox.Show(errorMsg, "目录创建失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    throw;
                }
            }

            Console.WriteLine($"✓ 基础目录: {baseDir}");
            Console.WriteLine($"✓ 数据库路径: {dbPath}");
            Console.WriteLine($"✓ 数据库目录: {dbDirectory}");

            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

            // 首先创建版本表
            CreateVersionTableIfNotExists(connection);

            // 获取当前数据库版本
            int currentVersion = GetDatabaseVersion(connection);

            // 如果是新数据库，直接创建最新结构
            if (currentVersion == 0)
            {
                CreateTablesForNewDatabase(connection);
                SetDatabaseVersion(connection, CurrentDatabaseVersion);
                Console.WriteLine($"新数据库已创建，版本: {CurrentDatabaseVersion}");
            }
            // 如果是旧版本数据库，执行迁移
            else if (currentVersion < CurrentDatabaseVersion)
            {
                Console.WriteLine($"检测到旧版本数据库 (v{currentVersion})，开始迁移到 v{CurrentDatabaseVersion}...");
                MigrateDatabase(connection, currentVersion, CurrentDatabaseVersion);
                Console.WriteLine("数据库迁移完成！");
            }
            else
            {
                Console.WriteLine($"数据库版本正常: v{currentVersion}");
            }

            // 无论版本如何，都检查并添加可能缺失的列（防止版本号正确但列不完整的情况）
            EnsureRequiredColumnsExist(connection);
            }
            catch (SqliteException ex)
            {
                var errorMsg = $"数据库初始化失败: {ex.Message}\n连接字符串: {_connectionString}\n错误代码: {ex.SqliteErrorCode}";
                Console.WriteLine(errorMsg);
                System.Windows.MessageBox.Show(errorMsg, "数据库错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                var errorMsg = $"数据库初始化失败: {ex.Message}\n连接字符串: {_connectionString}";
                Console.WriteLine(errorMsg);
                System.Windows.MessageBox.Show(errorMsg, "数据库错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private void CreateVersionTableIfNotExists(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DatabaseVersion (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    Version INTEGER NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        private int GetDatabaseVersion(SqliteConnection connection)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Version FROM DatabaseVersion WHERE Id = 1";
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private void SetDatabaseVersion(SqliteConnection connection, int version)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO DatabaseVersion (Id, Version, UpdatedAt)
                VALUES (1, @version, @updatedAt)";
            command.Parameters.AddWithValue("@version", version);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private void CreateTablesForNewDatabase(SqliteConnection connection)
        {
            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClipboardItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT NOT NULL,
                    DataType INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    IsFavorite INTEGER NOT NULL DEFAULT 0,
                    FavoriteSortOrder INTEGER,
                    FilePath TEXT,
                    ImageData BLOB
                )";
            createTableCommand.ExecuteNonQuery();

            // 创建CreatedAt索引，用于时间排序
            var createIndexCommand = connection.CreateCommand();
            createIndexCommand.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt
                ON ClipboardItems(CreatedAt)";
            createIndexCommand.ExecuteNonQuery();

            // 创建Content索引，用于快速查找重复项和搜索
            // SQLite不支持部分索引语法，但会自动优化长文本索引
            var createContentIndexCommand = connection.CreateCommand();
            createContentIndexCommand.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ClipboardItems_Content
                ON ClipboardItems(Content)";
            createContentIndexCommand.ExecuteNonQuery();

            // 创建复合索引，用于重复检测查询
            var createCompositeIndexCommand = connection.CreateCommand();
            createCompositeIndexCommand.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ClipboardItems_Content_DataType
                ON ClipboardItems(Content, DataType)";
            createCompositeIndexCommand.ExecuteNonQuery();
        }

        private void MigrateDatabase(SqliteConnection connection, int fromVersion, int toVersion)
        {
            Console.WriteLine($"开始数据库迁移: v{fromVersion} -> v{toVersion}");

            using var transaction = connection.BeginTransaction();
            try
            {
                // v0 -> v1: 从v1.0.0升级到v1.1.0
                if (fromVersion == 0 && toVersion >= 1)
                {
                    Console.WriteLine("执行迁移: v0 -> v1");

                    // 检查表是否存在
                    var checkTableCommand = connection.CreateCommand();
                    checkTableCommand.CommandText = @"
                        SELECT name FROM sqlite_master
                        WHERE type='table' AND name='ClipboardItems'";
                    var tableExists = checkTableCommand.ExecuteScalar() != null;

                    if (tableExists)
                    {
                        Console.WriteLine("发现v1.0.0数据库，检查需要的列...");

                        // 检查每个列是否存在，不存在则添加
                        var columns = new Dictionary<string, string>
                        {
                            { "IsFavorite", "INTEGER NOT NULL DEFAULT 0" },
                            { "FavoriteSortOrder", "INTEGER" },
                            { "FilePath", "TEXT" },
                            { "ImageData", "BLOB" }
                        };

                        foreach (var column in columns)
                        {
                            if (!ColumnExists(connection, "ClipboardItems", column.Key))
                            {
                                Console.WriteLine($"添加缺失的列: {column.Key}");
                                var addColumnCommand = connection.CreateCommand();
                                addColumnCommand.CommandText = $"ALTER TABLE ClipboardItems ADD COLUMN {column.Key} {column.Value}";
                                addColumnCommand.ExecuteNonQuery();
                            }
                            else
                            {
                                Console.WriteLine($"列已存在: {column.Key}");
                            }
                        }

                        // 创建索引（如果不存在）
                        var createIndexCommand = connection.CreateCommand();
                        createIndexCommand.CommandText = @"
                            CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt
                            ON ClipboardItems(CreatedAt)";
                        createIndexCommand.ExecuteNonQuery();

                        var createContentIndexCommand = connection.CreateCommand();
                        createContentIndexCommand.CommandText = @"
                            CREATE INDEX IF NOT EXISTS IX_ClipboardItems_Content
                            ON ClipboardItems(Content)";
                        createContentIndexCommand.ExecuteNonQuery();

                        var createCompositeIndexCommand = connection.CreateCommand();
                        createCompositeIndexCommand.CommandText = @"
                            CREATE INDEX IF NOT EXISTS IX_ClipboardItems_Content_DataType
                            ON ClipboardItems(Content, DataType)";
                        createCompositeIndexCommand.ExecuteNonQuery();

                        Console.WriteLine("数据库结构已更新，所有历史数据已保留");
                    }
                    else
                    {
                        Console.WriteLine("未发现旧表，创建新表结构");
                        CreateTablesForNewDatabase(connection);
                    }
                }

                // 更新版本号
                SetDatabaseVersion(connection, toVersion);
                transaction.Commit();
                Console.WriteLine($"迁移成功完成: 数据库版本已更新为 v{toVersion}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"迁移失败: {ex.Message}");
                throw new Exception($"数据库迁移失败: {ex.Message}", ex);
            }
        }

        private void EnsureRequiredColumnsExist(SqliteConnection connection)
        {
            Console.WriteLine("检查并确保所有必需的列都存在...");

            var requiredColumns = new Dictionary<string, string>
            {
                { "IsFavorite", "INTEGER NOT NULL DEFAULT 0" },
                { "FavoriteSortOrder", "INTEGER" },
                { "FilePath", "TEXT" },
                { "ImageData", "BLOB" }
            };

            foreach (var column in requiredColumns)
            {
                if (!ColumnExists(connection, "ClipboardItems", column.Key))
                {
                    try
                    {
                        var addColumnCommand = connection.CreateCommand();
                        addColumnCommand.CommandText = $"ALTER TABLE ClipboardItems ADD COLUMN {column.Key} {column.Value}";
                        addColumnCommand.ExecuteNonQuery();
                        Console.WriteLine($"✓ 添加缺失的列: {column.Key}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ 添加列 {column.Key} 失败: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine($"✓ 列已存在: {column.Key}");
                }
            }

            Console.WriteLine("列检查完成");
        }

        private bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName})";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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

            // 性能优化：每10次插入才清理一次旧记录，而不是每次插入都清理
            _insertCountSinceCleanup++;
            if (_insertCountSinceCleanup >= CleanupInterval)
            {
                Console.WriteLine($"达到清理阈值（{CleanupInterval}次插入），执行清理");
                await CleanupOldItemsAsync();
                _insertCountSinceCleanup = 0;
            }

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
                SELECT Id, Content, DataType, CreatedAt, IsFavorite, FilePath, ImageData, FavoriteSortOrder
                FROM ClipboardItems
                {whereClause}
                ORDER BY IsFavorite DESC,
                         CASE WHEN IsFavorite = 1 THEN COALESCE(FavoriteSortOrder, 999999) ELSE 0 END,
                         CreatedAt DESC
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

            // 检查当前状态
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT IsFavorite FROM ClipboardItems WHERE Id = @id";
            checkCommand.Parameters.AddWithValue("@id", itemId);
            var currentFavorite = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            var command = connection.CreateCommand();
            if (currentFavorite == 0)
            {
                // 设置为收藏，分配最大排序号+1
                command.CommandText = @"
                    UPDATE ClipboardItems
                    SET IsFavorite = 1,
                        FavoriteSortOrder = (SELECT COALESCE(MAX(FavoriteSortOrder), 0) + 1 FROM ClipboardItems WHERE IsFavorite = 1)
                    WHERE Id = @id";
            }
            else
            {
                // 取消收藏，清空排序号
                command.CommandText = @"
                    UPDATE ClipboardItems
                    SET IsFavorite = 0,
                        FavoriteSortOrder = NULL
                    WHERE Id = @id";
            }

            command.Parameters.AddWithValue("@id", itemId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task MoveFavoriteUpAsync(int itemId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                WITH CurrentItem AS (
                    SELECT Id, FavoriteSortOrder
                    FROM ClipboardItems
                    WHERE Id = @id AND IsFavorite = 1
                ),
                PrevItem AS (
                    SELECT Id, FavoriteSortOrder
                    FROM ClipboardItems
                    WHERE IsFavorite = 1
                      AND FavoriteSortOrder < (SELECT FavoriteSortOrder FROM CurrentItem)
                    ORDER BY FavoriteSortOrder DESC
                    LIMIT 1
                )
                UPDATE ClipboardItems
                SET FavoriteSortOrder = CASE
                    WHEN Id = (SELECT Id FROM CurrentItem) THEN (SELECT FavoriteSortOrder FROM PrevItem)
                    WHEN Id = (SELECT Id FROM PrevItem) THEN (SELECT FavoriteSortOrder FROM CurrentItem)
                    ELSE FavoriteSortOrder
                END
                WHERE Id IN (SELECT Id FROM CurrentItem UNION SELECT Id FROM PrevItem)";

            command.Parameters.AddWithValue("@id", itemId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task MoveFavoriteDownAsync(int itemId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                WITH CurrentItem AS (
                    SELECT Id, FavoriteSortOrder
                    FROM ClipboardItems
                    WHERE Id = @id AND IsFavorite = 1
                ),
                NextItem AS (
                    SELECT Id, FavoriteSortOrder
                    FROM ClipboardItems
                    WHERE IsFavorite = 1
                      AND FavoriteSortOrder > (SELECT FavoriteSortOrder FROM CurrentItem)
                    ORDER BY FavoriteSortOrder ASC
                    LIMIT 1
                )
                UPDATE ClipboardItems
                SET FavoriteSortOrder = CASE
                    WHEN Id = (SELECT Id FROM CurrentItem) THEN (SELECT FavoriteSortOrder FROM NextItem)
                    WHEN Id = (SELECT Id FROM NextItem) THEN (SELECT FavoriteSortOrder FROM CurrentItem)
                    ELSE FavoriteSortOrder
                END
                WHERE Id IN (SELECT Id FROM CurrentItem UNION SELECT Id FROM NextItem)";

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