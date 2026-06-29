using project.LogSystem;
using project.Views;
using Serilog;
using System.Windows;
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
                MessageBox.Show("Неверный логин или пароль.", "Ошибка входа");
            }
        }
    }
}
