using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Serilog;
using Microsoft.Data.Sqlite;
using DotNetEnv;

namespace project.Database
{
    public class Database
    {

        public void installDatabase()цпукц
        {
            Env.Load();

            string? databaseName = Environment.GetEnvironmentVariable("BACKUP_DB_PATH");

            using (var connection = new SqliteConnection(databaseName))
                {
                    connection.Open();

                    string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Password TEXT NOT NULL
                );
            ";

                    string createBackupsTable = @"
                CREATE TABLE IF NOT EXISTS Backups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    BackupName TEXT,
                    Location TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            ";

                    using (var command = new SqliteCommand(createUsersTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SqliteCommand(createBackupsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    Log.Information("База данных создана");
                }
            
        }
    }
}
