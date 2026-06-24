using project.DumpSystem.ArchiverManager;
using System.Windows.Input;

namespace project.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _statusText = "Готов к работе";
        private readonly ArchiverManager _archiver;

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ICommand StartBackupCommand { get; }

        public MainViewModel()
        {
            _archiver = new ArchiverManager();
            StartBackupCommand = new RelayCommand(obj => ExecuteBackup());
        }

        private void ExecuteBackup()
        {
            StatusText = "Выполняется дамп и архивация...";

            _archiver.Init("https://example-db-url.com");

            StatusText = "Бэкап успешно завершен!";
        }
    }
}
