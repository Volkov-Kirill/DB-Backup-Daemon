using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Globalization;
using Serilog;
using project.DumpSystem.Zip;

namespace project.DumpSystem.DumpManager
{
    public class DumperManager
    {
        public string GetSourcePath()
        {
            return ProjectConfig.ResolvePath(ProjectConfig.Get("BACKUP_DB_PATH", "database.db"));
        }

        public string GetBackupDirectory()
        {
            return ProjectConfig.ResolvePath(ProjectConfig.Get("BACKUP_DIRECTORY", "backups"));
        }

        public string GetPassword()
        {
            return ProjectConfig.Get("BACKUP_PASSWORD", "12345");
        }

        public int GetIntervalMinutes()
        {
            int minutes;
            if (!int.TryParse(ProjectConfig.Get("BACKUP_INTERVAL_MINUTES", "1440"), out minutes) || minutes < 1)
            {
                minutes = 1440;
            }

            return minutes;
        }

        public BackupResult CreateBackup()
        {
            try
            {
                string sourcePath = GetSourcePath();
                string backupDirectory = GetBackupDirectory();
                string password = GetPassword();

                Directory.CreateDirectory(backupDirectory);
                EnsureSourceDatabaseExists(sourcePath);

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dumpsDirectory = Path.Combine(backupDirectory, "dumps");
                Directory.CreateDirectory(dumpsDirectory);

                string dumpPath = Path.Combine(dumpsDirectory, "dump_" + stamp + "_" + Path.GetFileName(sourcePath));
                File.Copy(sourcePath, dumpPath, true);

                string zipPath = Path.Combine(backupDirectory, "backup_" + stamp + ".zip");
                PasswordZipWriter.CreateEncryptedZip(zipPath, dumpPath, Path.GetFileName(dumpPath), password);

                bool valid = PasswordZipWriter.ValidateEncryptedZip(zipPath, password);
                if (!valid)
                {
                    throw new InvalidDataException("Архив создан, но проверка паролем не прошла.");
                }

                ApplyRotation(backupDirectory);
                Log.Information("Бэкап создан: {ZipPath}", zipPath);

                return new BackupResult
                {
                    Success = true,
                    SourcePath = sourcePath,
                    DumpPath = dumpPath,
                    ZipPath = zipPath,
                    Message = "Бэкап создан и проверен. Архив защищен паролем."
                };
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка создания бэкапа: {Message}", ex.Message);
                return new BackupResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // Старый метод оставлен, чтобы не ломать структуру проекта.
        public void IntallDumper(string zipPath, string folderPath)
        {
            try
            {
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.GetFiles(folderPath, "*.sql"))
                    {
                        zip.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }

                Log.Information("Обычный ZIP с SQL-файлами создан: {Path}", zipPath);
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка старого метода архивации: {Message}", ex.Message);
            }
        }

        private static void EnsureSourceDatabaseExists(string sourcePath)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(sourcePath))
            {
                return;
            }

            File.WriteAllText(sourcePath,
                "DB-Backup-Daemon demo database\r\n" +
                "This file is used as BACKUP_DB_PATH.\r\n" +
                "CreatedAt=" + DateTime.Now.ToString("G") + "\r\n");
        }

        private static void ApplyRotation(string backupDirectory)
        {
            var files = new DirectoryInfo(backupDirectory)
                .GetFiles("backup_*.zip")
                .OrderByDescending(f => GetBackupTime(f))
                .ToArray();

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 7 ежедневных бэкапов: оставляем самый свежий архив за каждый день.
            foreach (var file in files
                .GroupBy(f => GetBackupTime(f).Date)
                .OrderByDescending(g => g.Key)
                .Take(7)
                .Select(g => g.OrderByDescending(GetBackupTime).First()))
            {
                keep.Add(file.FullName);
            }

            // 4 еженедельных бэкапа: оставляем самый свежий архив за каждую календарную неделю.
            foreach (var file in files
                .GroupBy(f => GetWeekKey(GetBackupTime(f)))
                .OrderByDescending(g => g.Max(GetBackupTime))
                .Take(4)
                .Select(g => g.OrderByDescending(GetBackupTime).First()))
            {
                keep.Add(file.FullName);
            }

            // 1 ежемесячный бэкап: оставляем самый свежий архив за последний месяц, где есть архивы.
            foreach (var file in files
                .GroupBy(f => GetBackupTime(f).ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .OrderByDescending(g => g.Max(GetBackupTime))
                .Take(1)
                .Select(g => g.OrderByDescending(GetBackupTime).First()))
            {
                keep.Add(file.FullName);
            }

            foreach (var file in files)
            {
                if (keep.Contains(file.FullName))
                {
                    continue;
                }

                try
                {
                    file.Delete();
                    Log.Information("Старый архив удален ротацией 7/4/1: {Path}", file.FullName);
                }
                catch (Exception ex)
                {
                    Log.Warning("Не удалось удалить старый архив {Path}: {Message}", file.FullName, ex.Message);
                }
            }
        }

        private static DateTime GetBackupTime(FileInfo file)
        {
            DateTime parsed;
            string name = Path.GetFileNameWithoutExtension(file.Name);

            if (name.StartsWith("backup_", StringComparison.OrdinalIgnoreCase))
            {
                string stamp = name.Substring("backup_".Length);
                if (DateTime.TryParseExact(
                    stamp,
                    "yyyyMMdd_HHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
                {
                    return parsed;
                }
            }

            return file.CreationTime;
        }

        private static string GetWeekKey(DateTime date)
        {
            Calendar calendar = CultureInfo.InvariantCulture.Calendar;
            int week = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return date.Year.ToString("0000", CultureInfo.InvariantCulture) + "-W" + week.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
