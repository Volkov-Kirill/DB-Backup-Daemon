namespace project.DumpSystem
{
    public class BackupResult
    {
        public bool Success { get; set; }
        public string SourcePath { get; set; }
        public string DumpPath { get; set; }
        public string ZipPath { get; set; }
        public string Message { get; set; }
    }
}
