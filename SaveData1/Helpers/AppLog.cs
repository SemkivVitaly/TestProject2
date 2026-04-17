using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SaveData1.Helpers
{
    /// <summary>Простой файловый логгер: %LOCALAPPDATA%\SaveData1\logs\app-YYYYMMDD.log. Потокобезопасный, без внешних зависимостей.</summary>
    public static class AppLog
    {
        private static readonly object _sync = new object();
        private static readonly string _logDir;

        static AppLog()
        {
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDir = Path.Combine(root, "SaveData1", "logs");
                Directory.CreateDirectory(_logDir);
            }
            catch
            {
                _logDir = null;
            }
        }

        public static void Info(string message) => Write("INFO", message, null);
        public static void Warn(string message, Exception ex = null) => Write("WARN", message, ex);
        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception ex)
        {
            if (_logDir == null) return;
            try
            {
                string fileName = "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                string path = Path.Combine(_logDir, fileName);

                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" [").Append(level).Append("] T").Append(Thread.CurrentThread.ManagedThreadId).Append(' ');
                sb.Append(message ?? "");
                if (ex != null)
                {
                    sb.AppendLine();
                    sb.Append("  Exception: ").Append(ex.GetType().FullName).Append(": ").Append(ex.Message);
                    for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    {
                        sb.AppendLine();
                        sb.Append("  -> ").Append(inner.GetType().FullName).Append(": ").Append(inner.Message);
                    }
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        sb.AppendLine();
                        sb.Append(ex.StackTrace);
                    }
                }
                sb.AppendLine();

                lock (_sync)
                {
                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // логгер не должен ломать приложение
            }
        }

        public static string LogDirectory => _logDir;
    }
}
