using Serilog;
using project.DumpSystem.DumpManager;

namespace project.DumpSystem.ArchiverManager
{
    public class ArchiverManager
    {
        private readonly DumperManager _dumper = new DumperManager();

        public BackupResult Init(string url)
        {
            Log.Information("Запуск бэкапа");
            return _dumper.CreateBackup();
        }
    }
}
