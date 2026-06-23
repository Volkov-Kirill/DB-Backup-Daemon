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
        public Dumper()
        {
            InitializeComponent();
        }
        DumperManager dumperManager = new DumperManager();
        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDlg = new FolderBrowserDialog();
            if (folderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = folderDlg.SelectedPath;
                txtFolderPath.Text = folderPath;
            }
        }

        private void btnArchive_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = txtFolderPath.Text;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "ZIP файлы (*.zip)|*.zip",
                FileName = "sql_files_backup.zip"
            };

            if (saveDlg.ShowDialog() == true)
            {
                string zipPath = saveDlg.FileName;
                dumperManager.IntallDumper(zipPath, folderPath);
            }
        }


    }
}
