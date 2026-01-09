using System;
using System.IO;

namespace LicenceController.Core.Helpers
{
    public static class LogHelper
    {
        public static void LogToFile(string message)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var logPath = Path.Combine(baseDir, "logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
                var logFile = Path.Combine(logPath, $"LicenceLog_{DateTime.Now:yyyy-MM-dd}.txt");
                using (var writer = new StreamWriter(logFile, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch { /* Log hatası olursa sessizce geç */ }
        }
    }
} 