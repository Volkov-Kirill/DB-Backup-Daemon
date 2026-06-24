using Microsoft.Win32;
using project.DumpSystem.DumpManager;
using System;
using System.Collections.Generic;
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
        public Dumper()
        {
            InitializeComponent();
            var backups = db.GetBackups()
     .Select(b => new
     {
         Id = b.Id,
         DisplayInfo = $"{b.BackupName} | {b.CreatedAt:G} | {b.Location}"
     }).ToList();

            BackupListBox.ItemsSource = backups;
            BackupListBox.DisplayMemberPath = "DisplayInfo";
        }
    }
}
