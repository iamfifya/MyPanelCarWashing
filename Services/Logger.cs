using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace MyPanelCarWashing.Services
{
    public static class Logger
    {
        // Внутренний движок NLog
        private static readonly NLog.Logger _nlog = LogManager.GetCurrentClassLogger();

        public static string SessionId { get; private set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        private static string _currentUser = "System";
        private static string _currentUserId = "?";

        static Logger()
        {
            // Настраиваем NLog программно, без XML-файлов
            var config = new LoggingConfiguration();

            // Папка в AppData (чтобы не было проблем с правами Администратора в Windows)
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyCarWashing", "Logs");

            var logfile = new FileTarget("logfile")
            {
                FileName = Path.Combine(logFolder, "app_${shortdate}.log"),
                Layout = "${longdate} | [${level:uppercase=true}] | [${event-properties:item=Category}] | ${event-properties:item=UserContext} | ${message} ${event-properties:item=Details} | Th:${threadid}",

                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 30,

                // Убрали ConcurrentWrites, оставили только эти две:
                KeepFileOpen = true,
                AutoFlush = true
            };

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
        }

        public static void SetUserContext(string fullName, int? id = null)
        {
            _currentUser = fullName ?? "Неизвестный";
            _currentUserId = id?.ToString() ?? "?";
        }

        // Твои старые методы остались без изменений! Ничего переписывать не надо.
        public static void Info(string message, string category = "APP", string details = "") =>
            Log(LogLevel.Info, message, category, details);

        public static void Warn(string message, string category = "APP", string details = "") =>
            Log(LogLevel.Warn, message, category, details);

        public static void Error(string message, Exception ex, string category = "APP", string details = "")
        {
            string exDetails = $"Исключение: {ex?.GetType().Name} | Сообщение: {ex?.Message} | Стек: {ex?.StackTrace}";
            string fullDetails = string.IsNullOrEmpty(details) ? exDetails : $"{exDetails} | Доп: {details}";
            Log(LogLevel.Error, message, category, fullDetails);
        }

        private static void Log(LogLevel level, string message, string category, string details)
        {
            // Передаем твои кастомные параметры в NLog
            var logEvent = new LogEventInfo(level, "CarWashLogger", message);
            logEvent.Properties["Category"] = category;
            logEvent.Properties["UserContext"] = $"{_currentUser}(ID:{_currentUserId})";

            string detailStr = string.IsNullOrEmpty(details) ? "" : $"| Детали: {details.Replace(Environment.NewLine, " ")}";
            logEvent.Properties["Details"] = detailStr;

            // NLog записывает всё в отдельном сверхбыстром потоке
            _nlog.Log(logEvent);
        }
    }
}
