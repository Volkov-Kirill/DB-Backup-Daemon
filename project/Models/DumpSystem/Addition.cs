using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace project.DumpSystem
{
    public class Addition
    {
        public bool CheckUrl(string url)
        {
            if (File.Exists(url))
            {
                return true;
            }
            return false;
        }
    }
}
