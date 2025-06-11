using System;

namespace Ludo.Reactive.Logging
{
    /// <summary>
    /// Interface for logging reactive framework events and errors
    /// </summary>
    public interface IReactiveLogger
    {
        /// <summary>
        /// Log a debug message
        /// </summary>
        void LogDebug(string message, object context = null);
        
        /// <summary>
        /// Log an informational message
        /// </summary>
        void LogInfo(string message, object context = null);
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        void LogWarning(string message, object context = null);
        
        /// <summary>
        /// Log an error message
        /// </summary>
        void LogError(string message, object context = null);
        
        /// <summary>
        /// Log an exception with context
        /// </summary>
        void LogException(Exception exception, string context = null, object additionalData = null);
        
        /// <summary>
        /// Log computation execution details
        /// </summary>
        void LogComputationExecution(string computationName, TimeSpan duration, bool success, Exception error = null);
        
        /// <summary>
        /// Log dependency tracking information
        /// </summary>
        void LogDependencyTracking(string computationName, string dependencyInfo);
        
        /// <summary>
        /// Log scheduler events
        /// </summary>
        void LogSchedulerEvent(string eventType, string details);
    }
}
