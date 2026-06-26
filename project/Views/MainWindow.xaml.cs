using project.LogSystem;
using project.Views;
using Serilog;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
namespace project
{

    public partial class MainWindow : Window
    {
        private readonly Database.Database db = new Database.Database();
        public MainWindow()
        {
            InitializeComponent();
            LogSystemInit.Init();
            Log.Information("Приложение запущено.");
            db.InitDatabase();
        }

        private void Login_bth(object sender, RoutedEventArgs e)
        {
            if (db.LoginUser(LoginName.Text, Password.Text) == true)
            {
                Dumper dumper = new Dumper();
                dumper.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль. По умолчанию: admin / admin", "Ошибка входа");
            }
        }
    }
}
