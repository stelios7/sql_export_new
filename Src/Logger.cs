using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQL_Export.Src
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logDirectory;
        private static readonly string _logFilePath;

        static Logger()
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _logDirectory = Path.Combine(baseDir, appName, "Logs");
            Directory.CreateDirectory(_logDirectory);

            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            _logFilePath = Path.Combine(_logDirectory, logFileName);
        }

        public static void Info(string message) => WriteLog("INFO", message);
        public static void Warn(string message) => WriteLog("WARN", message);
        public static void Error(string message) => WriteLog("ERROR", message);

        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                var logLine = new StringBuilder();
                logLine.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ");
                logLine.Append($"[{level}] ");
                logLine.AppendLine(message);
                logLine.AppendLine();

                File.AppendAllText(_logFilePath, logLine.ToString());
            }
        }

        public static void Error (Exception ex)
        {
            Error($"{ex.Message}\n{ex.StackTrace}");
        }
    }
}
