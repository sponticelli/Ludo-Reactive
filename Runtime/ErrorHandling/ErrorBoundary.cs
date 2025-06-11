using System;
using System.Collections.Generic;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive.ErrorHandling
{
    /// <summary>
    /// Provides error isolation and recovery for reactive computations
    /// </summary>
    public class ErrorBoundary : IDisposable
    {
        private readonly string _name;
        private readonly IReactiveLogger _logger;
        private readonly List<IErrorRecoveryStrategy> _recoveryStrategies;
        private readonly Dictionary<string, ReactiveErrorInfo> _errorHistory;
        private readonly ErrorBoundaryOptions _options;
        private bool _isDisposed;

        public event Action<ReactiveErrorInfo> OnError;
        public event Action<ReactiveErrorInfo> OnRecovery;
        public event Action<string> OnCircuitBreakerOpen;

        public ErrorBoundary(string name, IReactiveLogger logger = null, ErrorBoundaryOptions options = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger ?? ReactiveGlobals.Logger;
            _options = options ?? new ErrorBoundaryOptions();
            _recoveryStrategies = new List<IErrorRecoveryStrategy>();
            _errorHistory = new Dictionary<string, ReactiveErrorInfo>();

            // Add default recovery strategy
            if (_options.UseDefaultRecoveryStrategy)
            {
                AddRecoveryStrategy(new ExponentialBackoffStrategy());
            }
        }

        public string Name => _name;
        public bool IsCircuitBreakerOpen { get; private set; }
        public int ErrorCount => _errorHistory.Count;

        /// <summary>
        /// Adds a recovery strategy to this error boundary
        /// </summary>
        public void AddRecoveryStrategy(IErrorRecoveryStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            _recoveryStrategies.Add(strategy);
        }

        /// <summary>
        /// Executes an action within this error boundary
        /// </summary>
        public T Execute<T>(string operationName, Func<T> operation, T fallbackValue = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ErrorBoundary));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            if (IsCircuitBreakerOpen)
            {
                _logger?.LogWarning($"Circuit breaker is open for boundary '{_name}', returning fallback value");
                return fallbackValue;
            }

            var errorInfo = GetOrCreateErrorInfo(operationName);

            try
            {
                var result = operation();
                
                // Clear error history on successful execution
                if (_errorHistory.ContainsKey(operationName))
                {
                    _errorHistory.Remove(operationName);
                    _logger?.LogInfo($"Operation '{operationName}' recovered successfully in boundary '{_name}'");
                    OnRecovery?.Invoke(errorInfo);
                }

                return result;
            }
            catch (Exception ex)
            {
                return HandleError(operationName, ex, () => operation(), fallbackValue);
            }
        }

        /// <summary>
        /// Executes an action within this error boundary
        /// </summary>
        public void Execute(string operationName, Action operation)
        {
            Execute(operationName, () => { operation(); return true; });
        }

        /// <summary>
        /// Resets the circuit breaker and clears error history
        /// </summary>
        public void Reset()
        {
            IsCircuitBreakerOpen = false;
            _errorHistory.Clear();
            _logger?.LogInfo($"Error boundary '{_name}' has been reset");
        }

        /// <summary>
        /// Gets error information for a specific operation
        /// </summary>
        public ReactiveErrorInfo GetErrorInfo(string operationName)
        {
            return _errorHistory.TryGetValue(operationName, out var info) ? info : null;
        }

        private T HandleError<T>(string operationName, Exception exception, Func<T> retryOperation, T fallbackValue)
        {
            var errorInfo = GetOrCreateErrorInfo(operationName);
            errorInfo.Exception = exception;
            errorInfo.Timestamp = DateTime.UtcNow;

            _logger?.LogException(exception, $"Error in operation '{operationName}' within boundary '{_name}'", errorInfo);
            OnError?.Invoke(errorInfo);

            // Try recovery strategies
            foreach (var strategy in _recoveryStrategies)
            {
                if (strategy.CanHandle(errorInfo))
                {
                    _logger?.LogInfo($"Attempting recovery for '{operationName}' using {strategy.GetType().Name}");
                    
                    if (strategy.TryRecover(errorInfo, () =>
                    {
                        try
                        {
                            retryOperation();
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }))
                    {
                        _logger?.LogInfo($"Recovery successful for '{operationName}' in boundary '{_name}'");
                        OnRecovery?.Invoke(errorInfo);
                        
                        // Execute the operation again after successful recovery
                        try
                        {
                            return retryOperation();
                        }
                        catch (Exception retryEx)
                        {
                            // If retry fails, continue to next strategy or fallback
                            errorInfo.Exception = retryEx;
                        }
                    }
                }
            }

            // Check if we should open the circuit breaker
            CheckCircuitBreaker(operationName, errorInfo);

            // All recovery attempts failed, return fallback value
            _logger?.LogError($"All recovery attempts failed for '{operationName}' in boundary '{_name}', using fallback value");
            return fallbackValue;
        }

        private ReactiveErrorInfo GetOrCreateErrorInfo(string operationName)
        {
            if (!_errorHistory.TryGetValue(operationName, out var errorInfo))
            {
                errorInfo = new ReactiveErrorInfo { ComputationName = operationName };
                _errorHistory[operationName] = errorInfo;
            }
            return errorInfo;
        }

        private void CheckCircuitBreaker(string operationName, ReactiveErrorInfo errorInfo)
        {
            if (_options.CircuitBreakerThreshold <= 0) return;

            if (errorInfo.RetryCount >= _options.CircuitBreakerThreshold)
            {
                IsCircuitBreakerOpen = true;
                _logger?.LogError($"Circuit breaker opened for boundary '{_name}' due to repeated failures in '{operationName}'");
                OnCircuitBreakerOpen?.Invoke(operationName);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _errorHistory.Clear();
                _recoveryStrategies.Clear();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Configuration options for error boundaries
    /// </summary>
    public class ErrorBoundaryOptions
    {
        /// <summary>
        /// Whether to use the default exponential backoff recovery strategy
        /// </summary>
        public bool UseDefaultRecoveryStrategy { get; set; } = true;

        /// <summary>
        /// Number of failures before opening the circuit breaker (0 = disabled)
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// Whether to log all errors or only critical ones
        /// </summary>
        public bool LogAllErrors { get; set; } = true;
    }
}
