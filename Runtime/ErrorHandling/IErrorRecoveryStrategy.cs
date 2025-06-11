using System;
using System.Threading.Tasks;

namespace Ludo.Reactive.ErrorHandling
{
    /// <summary>
    /// Interface for error recovery strategies
    /// </summary>
    public interface IErrorRecoveryStrategy
    {
        /// <summary>
        /// Determines if this strategy can handle the given error
        /// </summary>
        bool CanHandle(ReactiveErrorInfo errorInfo);

        /// <summary>
        /// Attempts to recover from the error
        /// </summary>
        /// <returns>True if recovery was successful, false otherwise</returns>
        bool TryRecover(ReactiveErrorInfo errorInfo, Func<bool> retryAction);

        /// <summary>
        /// Gets the maximum number of retry attempts for this strategy
        /// </summary>
        int MaxRetryAttempts { get; }

        /// <summary>
        /// Gets the delay between retry attempts
        /// </summary>
        TimeSpan GetRetryDelay(int attemptNumber);
    }

    /// <summary>
    /// Simple retry strategy with exponential backoff
    /// </summary>
    public class ExponentialBackoffStrategy : IErrorRecoveryStrategy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;
        private readonly double _backoffMultiplier;
        private readonly Func<Exception, bool> _shouldRetryPredicate;

        public ExponentialBackoffStrategy(
            int maxRetries = 3,
            TimeSpan? baseDelay = null,
            double backoffMultiplier = 2.0,
            Func<Exception, bool> shouldRetryPredicate = null)
        {
            _maxRetries = maxRetries;
            _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(100);
            _backoffMultiplier = backoffMultiplier;
            _shouldRetryPredicate = shouldRetryPredicate ?? (_ => true);
        }

        public int MaxRetryAttempts => _maxRetries;

        public bool CanHandle(ReactiveErrorInfo errorInfo)
        {
            return errorInfo.RetryCount < _maxRetries && 
                   _shouldRetryPredicate(errorInfo.Exception);
        }

        public bool TryRecover(ReactiveErrorInfo errorInfo, Func<bool> retryAction)
        {
            if (!CanHandle(errorInfo))
                return false;

            try
            {
                // Apply delay if this is a retry
                if (errorInfo.RetryCount > 0)
                {
                    var delay = GetRetryDelay(errorInfo.RetryCount);
                    if (delay > TimeSpan.Zero)
                    {
                        // For now, we'll use a simple Thread.Sleep
                        // In a real implementation, you might want async delays
                        System.Threading.Thread.Sleep(delay);
                    }
                }

                errorInfo.RetryCount++;
                return retryAction();
            }
            catch
            {
                return false;
            }
        }

        public TimeSpan GetRetryDelay(int attemptNumber)
        {
            var delay = TimeSpan.FromTicks((long)(_baseDelay.Ticks * Math.Pow(_backoffMultiplier, attemptNumber - 1)));
            return delay;
        }
    }

    /// <summary>
    /// Strategy that immediately fails without retry
    /// </summary>
    public class FailFastStrategy : IErrorRecoveryStrategy
    {
        public int MaxRetryAttempts => 0;

        public bool CanHandle(ReactiveErrorInfo errorInfo) => false;

        public bool TryRecover(ReactiveErrorInfo errorInfo, Func<bool> retryAction) => false;

        public TimeSpan GetRetryDelay(int attemptNumber) => TimeSpan.Zero;
    }

    /// <summary>
    /// Strategy that retries a fixed number of times with fixed delay
    /// </summary>
    public class FixedRetryStrategy : IErrorRecoveryStrategy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _delay;

        public FixedRetryStrategy(int maxRetries = 3, TimeSpan? delay = null)
        {
            _maxRetries = maxRetries;
            _delay = delay ?? TimeSpan.FromMilliseconds(100);
        }

        public int MaxRetryAttempts => _maxRetries;

        public bool CanHandle(ReactiveErrorInfo errorInfo)
        {
            return errorInfo.RetryCount < _maxRetries;
        }

        public bool TryRecover(ReactiveErrorInfo errorInfo, Func<bool> retryAction)
        {
            if (!CanHandle(errorInfo))
                return false;

            try
            {
                if (errorInfo.RetryCount > 0 && _delay > TimeSpan.Zero)
                {
                    System.Threading.Thread.Sleep(_delay);
                }

                errorInfo.RetryCount++;
                return retryAction();
            }
            catch
            {
                return false;
            }
        }

        public TimeSpan GetRetryDelay(int attemptNumber) => _delay;
    }
}
