using System.IO;

namespace WinFlipped.Helpers
{
    internal static class DebugLog
    {
        private static readonly object LockObject = new();
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinFlipped",
            "Logs"
        );

        private static readonly string LogPath = Path.Combine(LogDirectory, "debug.log");

        internal static string GetPath()
        {
            Directory.CreateDirectory(LogDirectory);
            return LogPath;
        }

        internal static void Write(string message)
        {
            var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";
            lock (LockObject)
            {
                File.AppendAllText(GetPath(), line);
            }
        }
    }
}
