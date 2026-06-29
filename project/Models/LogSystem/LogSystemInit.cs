using Serilog;

namespace project.LogSystem
{
    public class LogSystemInit
    {
        public static void Init()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .WriteTo.Console()
               .WriteTo.File("logs/app_.log", rollingInterval: RollingInterval.Day)
               .CreateLogger();

            Log.Information("Логгер инициализирован.");
        }
    }
}
