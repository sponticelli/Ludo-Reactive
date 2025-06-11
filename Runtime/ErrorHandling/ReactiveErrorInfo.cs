using System;
using System.Collections.Generic;

namespace Ludo.Reactive.ErrorHandling
{
    /// <summary>
    /// Contains information about a reactive computation error
    /// </summary>
    public class ReactiveErrorInfo
    {
        public string ComputationName { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public string StackTrace { get; set; }
        public ErrorSeverity Severity { get; set; }

        public ReactiveErrorInfo()
        {
            Context = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }

        public ReactiveErrorInfo(string computationName, Exception exception, ErrorSeverity severity = ErrorSeverity.Error)
            : this()
        {
            ComputationName = computationName;
            Exception = exception;
            Severity = severity;
            StackTrace = exception?.StackTrace;
        }

        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }

        public T GetContext<T>(string key, T defaultValue = default)
        {
            if (Context.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public override string ToString()
        {
            var contextStr = Context.Count > 0 ? $" Context: {string.Join(", ", Context)}" : "";
            return $"[{Severity}] {ComputationName}: {Exception?.Message} (Retry: {RetryCount}){contextStr}";
        }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Warning,
        Error,
        Critical
    }
}
