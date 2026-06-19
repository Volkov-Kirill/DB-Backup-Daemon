using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.IO;

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
