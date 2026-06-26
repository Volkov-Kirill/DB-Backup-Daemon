using System;
using System.Collections.Generic;
using System.IO;

namespace project.DumpSystem
{
    public static class ProjectConfig
    {
        private static Dictionary<string, string> _values;

        public static string Get(string key, string defaultValue)
        {
            EnsureLoaded();
            string value;
            if (_values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            return defaultValue;
        }

        public static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
        }

        private static void EnsureLoaded()
        {
            if (_values != null)
            {
                return;
            }

            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string envPath = FindEnvFile();
            if (envPath == null)
            {
                return;
            }

            foreach (string rawLine in File.ReadAllLines(envPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int index = line.IndexOf('=');
                if (index <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim().Trim('"');
                _values[key] = value;
            }
        }

        private static string FindEnvFile()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && directory != null; i++)
            {
                string path = Path.Combine(directory.FullName, ".env");
                if (File.Exists(path))
                {
                    return path;
                }

                directory = directory.Parent;
            }

            string currentPath = Path.Combine(Environment.CurrentDirectory, ".env");
            return File.Exists(currentPath) ? currentPath : null;
        }
    }
}
