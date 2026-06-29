using project.DumpSystem.DumpManager;
using Serilog;

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
