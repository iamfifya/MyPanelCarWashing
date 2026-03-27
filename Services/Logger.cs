using System;
using System.IO;
using System.Threading.Tasks;

namespace MyPanelCarWashing.Services
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "error.log");

        public static async Task LogErrorAsync(Exception ex, string additionalInfo = null)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(_logPath);
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n" +
                                      $"Stack Trace: {ex.StackTrace}\n" +
                                      $"Additional Info: {additionalInfo}\n" +
                                      new string('-', 50) + "\n";

                        File.AppendAllText(_logPath, logEntry);
                    }
                    catch { }
                }
            });
        }

        public static void LogError(Exception ex, string additionalInfo = null)
        {
            try
            {
                var directory = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n" +
                              $"Stack Trace: {ex.StackTrace}\n" +
                              $"Additional Info: {additionalInfo}\n" +
                              new string('-', 50) + "\n";

                File.AppendAllText(_logPath, logEntry);
            }
            catch { }
        }
    }
}
