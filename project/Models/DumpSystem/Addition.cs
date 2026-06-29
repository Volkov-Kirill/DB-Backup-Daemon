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
