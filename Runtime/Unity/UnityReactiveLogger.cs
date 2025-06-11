using System;
using UnityEngine;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Unity-specific implementation of IReactiveLogger that uses Unity's Debug.Log system
    /// </summary>
    public class UnityReactiveLogger : IReactiveLogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly bool _includeStackTrace;
        private readonly string _logPrefix;

        public UnityReactiveLogger(LogLevel minimumLevel = LogLevel.Warning, bool includeStackTrace = false, string logPrefix = "[Reactive]")
        {
            _minimumLevel = minimumLevel;
            _includeStackTrace = includeStackTrace;
            _logPrefix = logPrefix;
        }

        public void LogDebug(string message, object context = null)
        {
            if (_minimumLevel <= LogLevel.Debug)
            {
                var formattedMessage = FormatMessage(message, context);
                Debug.Log($"{_logPrefix}[DEBUG] {formattedMessage}");
            }
        }

        public void LogInfo(string message, object context = null)
        {
            if (_minimumLevel <= LogLevel.Info)
            {
                var formattedMessage = FormatMessage(message, context);
                Debug.Log($"{_logPrefix}[INFO] {formattedMessage}");
            }
        }

        public void LogWarning(string message, object context = null)
        {
            if (_minimumLevel <= LogLevel.Warning)
            {
                var formattedMessage = FormatMessage(message, context);
                Debug.LogWarning($"{_logPrefix}[WARN] {formattedMessage}");
            }
        }

        public void LogError(string message, object context = null)
        {
            if (_minimumLevel <= LogLevel.Error)
            {
                var formattedMessage = FormatMessage(message, context);
                Debug.LogError($"{_logPrefix}[ERROR] {formattedMessage}");
            }
        }

        public void LogException(Exception exception, string context = null, object additionalData = null)
        {
            var message = $"{_logPrefix}[EXCEPTION]";
            
            if (!string.IsNullOrEmpty(context))
            {
                message += $" {context}:";
            }
            
            message += $" {exception.Message}";
            
            if (additionalData != null)
            {
                message += $" | Data: {additionalData}";
            }

            if (_includeStackTrace)
            {
                Debug.LogException(exception);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public void LogComputationExecution(string computationName, TimeSpan duration, bool success, Exception error = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message = $"Computation '{computationName}' {status} in {duration.TotalMilliseconds:F2}ms";
            
            if (!success && error != null)
            {
                message += $" - Error: {error.Message}";
                LogError(message);
            }
            else
            {
                LogDebug(message);
            }
        }

        public void LogDependencyTracking(string computationName, string dependencyInfo)
        {
            LogDebug($"Dependency tracking for '{computationName}': {dependencyInfo}");
        }

        public void LogSchedulerEvent(string eventType, string details)
        {
            LogDebug($"Scheduler {eventType}: {details}");
        }

        private string FormatMessage(string message, object context)
        {
            if (context == null) return message;
            
            return $"{message} [Context: {context}]";
        }
    }
}
