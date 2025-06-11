using System;

namespace Ludo.Reactive.Logging
{
    /// <summary>
    /// Default implementation of IReactiveLogger that outputs to console
    /// </summary>
    public class DefaultReactiveLogger : IReactiveLogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly bool _includeTimestamp;
        private readonly bool _includeContext;

        public DefaultReactiveLogger(LogLevel minimumLevel = LogLevel.Warning, bool includeTimestamp = true, bool includeContext = true)
        {
            _minimumLevel = minimumLevel;
            _includeTimestamp = includeTimestamp;
            _includeContext = includeContext;
        }

        public void LogDebug(string message, object context = null)
        {
            Log(LogLevel.Debug, message, context);
        }

        public void LogInfo(string message, object context = null)
        {
            Log(LogLevel.Info, message, context);
        }

        public void LogWarning(string message, object context = null)
        {
            Log(LogLevel.Warning, message, context);
        }

        public void LogError(string message, object context = null)
        {
            Log(LogLevel.Error, message, context);
        }

        public void LogException(Exception exception, string context = null, object additionalData = null)
        {
            var message = $"Exception: {exception.Message}";
            if (!string.IsNullOrEmpty(context))
            {
                message = $"{context} - {message}";
            }
            
            if (additionalData != null)
            {
                message += $" | Additional Data: {additionalData}";
            }
            
            message += $"\nStack Trace: {exception.StackTrace}";
            
            Log(LogLevel.Error, message);
        }

        public void LogComputationExecution(string computationName, TimeSpan duration, bool success, Exception error = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message = $"Computation '{computationName}' {status} in {duration.TotalMilliseconds:F2}ms";
            
            if (!success && error != null)
            {
                message += $" - Error: {error.Message}";
                Log(LogLevel.Error, message);
            }
            else
            {
                Log(LogLevel.Debug, message);
            }
        }

        public void LogDependencyTracking(string computationName, string dependencyInfo)
        {
            Log(LogLevel.Debug, $"Dependency tracking for '{computationName}': {dependencyInfo}");
        }

        public void LogSchedulerEvent(string eventType, string details)
        {
            Log(LogLevel.Debug, $"Scheduler {eventType}: {details}");
        }

        private void Log(LogLevel level, string message, object context = null)
        {
            if (level < _minimumLevel) return;

            var prefix = GetLogPrefix(level);
            var formattedMessage = $"{prefix}{message}";

            if (_includeContext && context != null)
            {
                formattedMessage += $" [Context: {context}]";
            }

            Console.WriteLine(formattedMessage);
        }

        private string GetLogPrefix(LogLevel level)
        {
            var timestamp = _includeTimestamp ? $"[{DateTime.Now:HH:mm:ss.fff}] " : "";
            var levelStr = level switch
            {
                LogLevel.Debug => "[DEBUG] ",
                LogLevel.Info => "[INFO] ",
                LogLevel.Warning => "[WARN] ",
                LogLevel.Error => "[ERROR] ",
                _ => ""
            };

            return $"{timestamp}[Reactive]{levelStr}";
        }
    }

    /// <summary>
    /// Log levels for reactive framework
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
