using project.DumpSystem;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace project.Database
{
    public class BackupRecord
    {
        public long Id { get; set; }
        public string BackupName { get; set; }
        public string Location { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Database
    {
        private static long _currentUserId;
        private readonly string _dataPath;

        public Database()
        {
            _dataPath = ProjectConfig.ResolvePath(ProjectConfig.Get("APP_DATA_PATH", "appdata.db"));
        }

        public void InitDatabase()
        {
            try
            {
                string directory = Path.GetDirectoryName(_dataPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_dataPath))
                {
                    File.WriteAllText(_dataPath, string.Empty, Encoding.UTF8);
                }

                RegisterUser("admin", "admin");
                Log.Information("Локальная база приложения инициализирована: {Path}", _dataPath);
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка инициализации локальной базы: {Message}", ex.Message);
            }
        }

        public bool RegisterUser(string username, string password)
        {
            var lines = ReadLines();
            bool exists = lines.Any(l => l.StartsWith("USER|", StringComparison.OrdinalIgnoreCase) && Decode(GetPart(l, 2)) == username);
            if (exists)
            {
                return true;
            }

            long nextId = GetNextId(lines, "USER");
            lines.Add("USER|" + nextId + "|" + Encode(username) + "|" + Encode(password));
            WriteLines(lines);
            return true;
        }

        public bool LoginUser(string username, string password)
        {
            foreach (string line in ReadLines())
            {
                if (!line.StartsWith("USER|", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string name = Decode(GetPart(line, 2));
                string pass = Decode(GetPart(line, 3));

                if (name == username && pass == password)
                {
                    long id;
                    if (!long.TryParse(GetPart(line, 1), out id))
                    {
                        id = 1;
                    }

                    _currentUserId = id;
                    return true;
                }
            }

            return false;
        }

        public long GetCurrentUserId()
        {
            if (_currentUserId == 0)
            {
                _currentUserId = 1;
            }

            return _currentUserId;
        }

        public bool AddBackup(string backupName, string location)
        {
            try
            {
                var lines = ReadLines();
                long nextId = GetNextId(lines, "BACKUP");
                long userId = GetCurrentUserId();
                long ticks = DateTime.Now.Ticks;

                lines.Add("BACKUP|" + nextId + "|" + userId + "|" + Encode(backupName) + "|" + Encode(location) + "|" + ticks);
                WriteLines(lines);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка добавления записи о бэкапе: {Message}", ex.Message);
                return false;
            }
        }

        public List<BackupRecord> GetBackups()
        {
            long userId = GetCurrentUserId();
            var result = new List<BackupRecord>();

            foreach (string line in ReadLines())
            {
                if (!line.StartsWith("BACKUP|", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                long lineUserId;
                if (!long.TryParse(GetPart(line, 2), out lineUserId) || lineUserId != userId)
                {
                    continue;
                }

                long id;
                long ticks;
                long.TryParse(GetPart(line, 1), out id);
                long.TryParse(GetPart(line, 5), out ticks);

                result.Add(new BackupRecord
                {
                    Id = id,
                    BackupName = Decode(GetPart(line, 3)),
                    Location = Decode(GetPart(line, 4)),
                    CreatedAt = ticks > 0 ? new DateTime(ticks) : DateTime.MinValue
                });
            }

            return result.OrderByDescending(x => x.CreatedAt).ToList();
        }

        private List<string> ReadLines()
        {
            if (!File.Exists(_dataPath))
            {
                File.WriteAllText(_dataPath, string.Empty, Encoding.UTF8);
            }

            return File.ReadAllLines(_dataPath, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        }

        private void WriteLines(List<string> lines)
        {
            File.WriteAllLines(_dataPath, lines.ToArray(), Encoding.UTF8);
        }

        private static long GetNextId(List<string> lines, string type)
        {
            long max = 0;
            foreach (string line in lines)
            {
                if (!line.StartsWith(type + "|", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                long id;
                if (long.TryParse(GetPart(line, 1), out id) && id > max)
                {
                    max = id;
                }
            }

            return max + 1;
        }

        private static string GetPart(string line, int index)
        {
            string[] parts = line.Split('|');
            return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string Decode(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
