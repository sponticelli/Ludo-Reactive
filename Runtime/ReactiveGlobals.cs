using Ludo.Reactive.Logging;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Global configuration and services for the reactive framework
    /// </summary>
    public static class ReactiveGlobals
    {
        private static IReactiveLogger _logger;
        private static ErrorBoundary _globalErrorBoundary;

        /// <summary>
        /// Global logger instance for the reactive framework
        /// </summary>
        public static IReactiveLogger Logger
        {
            get => _logger ??= new DefaultReactiveLogger();
            set => _logger = value;
        }

        /// <summary>
        /// Global error boundary for unhandled reactive errors
        /// </summary>
        public static ErrorBoundary GlobalErrorBoundary
        {
            get => _globalErrorBoundary ??= new ErrorBoundary("Global", Logger);
            set => _globalErrorBoundary = value;
        }

        /// <summary>
        /// Configures the global logger
        /// </summary>
        public static void ConfigureLogger(IReactiveLogger logger)
        {
            Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Configures the global error boundary
        /// </summary>
        public static void ConfigureGlobalErrorBoundary(ErrorBoundary errorBoundary)
        {
            GlobalErrorBoundary?.Dispose();
            GlobalErrorBoundary = errorBoundary ?? throw new System.ArgumentNullException(nameof(errorBoundary));
        }

        /// <summary>
        /// Resets all global configuration to defaults
        /// </summary>
        public static void Reset()
        {
            _globalErrorBoundary?.Dispose();
            _globalErrorBoundary = null;
            _logger = null;
        }
    }
}
