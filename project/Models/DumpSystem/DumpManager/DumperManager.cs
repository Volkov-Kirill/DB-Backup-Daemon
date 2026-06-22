using Microsoft.Win32;
using Serilog;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using ZipArchive = System.IO.Compression.ZipArchive;

namespace project.DumpSystem.DumpManager
{
    public class DumperManager
    {
        public void IntallDumper(string zipPath,string folderPath) 
        {
            try
            {
                using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    var sqlFiles = Directory.GetFiles(folderPath, "*.sql");
                    foreach (var file in sqlFiles)
                    {
                        zip.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                    Log.Information("Дамп готов");
                }

            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
            }


        }
       
    }
}
