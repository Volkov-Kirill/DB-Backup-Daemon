using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Serilog;
using project.DumpSystem.DumpManager;

namespace project.DumpSystem.ArchiverManager
{
    
    public class ArchiverManager
    {
        Addition addition = new Addition();
        DumperManager Dumper = new DumperManager();

        public void Init(string url)
        {
            if (addition.CheckUrl(url))
            {
                Log.Information("Начинаем дамп");
                Dumper.IntallDumper();
            }
        }

        


    }
}
