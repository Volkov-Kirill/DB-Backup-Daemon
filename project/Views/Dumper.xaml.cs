using Microsoft.Win32;
using project.DumpSystem.DumpManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace project.Views
{
    /// <summary>
    /// Логика взаимодействия для Dumper.xaml
    /// </summary>
    public partial class Dumper : Window
    {
        Database.Database db = new Database.Database();
        private readonly DumperManager dumper = new DumperManager();
        private readonly DispatcherTimer timer = new DispatcherTimer();

        public Dumper()
        {
            InitializeComponent();
            db.InitDatabase();

            timer.Interval = TimeSpan.FromMinutes(dumper.GetIntervalMinutes());
            timer.Tick += (s, e) => CreateBackup();

            RefreshBackups();
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            CreateBackup();
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            StatusText.Text = "Фоновый режим включён. Интервал: " + dumper.GetIntervalMinutes() + " мин.";
        }

        private void StopTimer_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            StatusText.Text = "Фоновый режим остановлен.";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshBackups();
        }

        private void CreateBackup()
        {
            StatusText.Text = "Создание бэкапа...";
            var result = dumper.CreateBackup();

            if (result.Success)
            {
                db.AddBackup(Path.GetFileName(result.ZipPath), result.ZipPath);
                StatusText.Text = "Готово: " + result.ZipPath;
            }
            else
            {
                StatusText.Text = "Ошибка: " + result.Message;
                MessageBox.Show(result.Message, "Ошибка создания бэкапа");
            }

            RefreshBackups();
        }

        private void RefreshBackups()
        {
            var backups = db.GetBackups()
                .Select(b => new
                {
                    Id = b.Id,
                    DisplayInfo = b.CreatedAt.ToString("G") + " | " + b.BackupName + " | " + b.Location
                })
                .ToList();

            BackupListBox.ItemsSource = backups;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string backupDirectory = DumperManager.GetBackupDirectory();

                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                Process.Start("explorer.exe", backupDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть папку бэкапов:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
