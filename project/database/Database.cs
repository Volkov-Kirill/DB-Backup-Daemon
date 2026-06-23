using System;
using System.Collections.Generic;
using Serilog;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Windows;

namespace project.Database
{
    public class Database
    {
        private const string _connectionString = "Data Source=test.db";
        private long _currentUserId;

        public Database()
        {
            Batteries.Init();

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new Exception("Переменная окружения BACKUP_DB_PATH не установлена");
            }
        }


        public void InitDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL
                );";

                string createBackupsTable = @"
                CREATE TABLE IF NOT EXISTS Backups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    BackupName TEXT,
                    Location TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );";

                using (var command = new SqliteCommand(createUsersTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createBackupsTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                Log.Information("База данных инициализирована");
            }
        }

        // Регистрация нового пользователя
        public bool RegisterUser(string username, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string insertUser = "INSERT INTO Users (Name, Password) VALUES (@name, @password);";

                try
                {
                    using (var cmd = new SqliteCommand(insertUser, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.ExecuteNonQuery();
                    }
                    return true; // успешно
                }
                catch (SqliteException ex)
                {
                    Log.Error($"Ошибка регистрации: {ex.Message}");
                    return false; // ошибка (например, дублирование имени)
                }
            }
        }

        // Авторизация пользователя
        public bool LoginUser(string username, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id FROM Users WHERE Name = @name AND Password = @password;";
                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        _currentUserId = (long)result;
                        MessageBox.Show("oket");
                        return true; // вошел
                    }
                }
            }
            return false; // не найден или неверный пароль
        }

        // Получить текущего пользователя
        public long GetCurrentUserId()
        {
            return _currentUserId;
        }

        // Добавить бэкап
        public bool AddBackup(string backupName, string location)
        {
            if (_currentUserId == 0)
                return false;

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string insertBackup = @"
                INSERT INTO Backups (UserId, BackupName, Location)
                VALUES (@userId, @backupName, @location);";

                try
                {
                    using (var cmd = new SqliteCommand(insertBackup, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", GetCurrentUserId());
                        cmd.Parameters.AddWithValue("@backupName", backupName);
                        cmd.Parameters.AddWithValue("@location", location);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Ошибка добавления бэкапа: {ex.Message}");
                    return false;
                }
            }
        }

        // Получить список бэкапов текущего пользователя
        public List<(long Id, string BackupName, string Location, DateTime CreatedAt)> GetBackups()
        {
            var list = new List<(long, string, string, DateTime)>();
            if (_currentUserId == 0)
                return list;

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT Id, BackupName, Location, CreatedAt
                FROM Backups WHERE UserId = @userId
                ORDER BY CreatedAt DESC;";

                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", _currentUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);

                            string backupName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            string location = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            DateTime createdAt;

                            if (reader.IsDBNull(3))
                                createdAt = DateTime.MinValue; 
                            else
                                createdAt = reader.GetDateTime(3);

                            list.Add((id, backupName, location, createdAt));
                        }
                    }
                }
            }
            return list;
        }
    }
}
