using System;
using Ludo.Reactive.ErrorHandling;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Examples demonstrating error handling and resilience features
    /// </summary>
    public static class ErrorHandlingExamples
    {
        /// <summary>
        /// Basic error boundary usage
        /// </summary>
        public static void BasicErrorBoundaryExample()
        {
            // Create a custom error boundary with specific recovery strategies
            var errorBoundary = new ErrorBoundary("UserInterface");
            errorBoundary.AddRecoveryStrategy(new ExponentialBackoffStrategy(maxRetries: 3));
            
            // Create reactive state that might fail
            var userInput = ReactiveFlow.CreateState("");
            var processedInput = ReactiveFlow.CreateComputed(
                "ProcessInput",
                builder =>
                {
                    var input = builder.Track(userInput);
                    if (string.IsNullOrEmpty(input))
                        throw new ArgumentException("Input cannot be empty");
                    return input.ToUpper();
                },
                fallbackValue: "DEFAULT",
                errorBoundary: errorBoundary
            );

            // Create an effect that handles errors gracefully
            var displayEffect = ReactiveFlow.CreateEffect(
                "DisplayEffect",
                builder =>
                {
                    var result = builder.Track(processedInput);
                    Console.WriteLine($"Processed: {result}");
                },
                errorBoundary: errorBoundary
            );

            // Test with invalid input
            userInput.Set(""); // This will trigger error handling
            userInput.Set("hello"); // This should recover

            // Cleanup
            displayEffect.Dispose();
            processedInput.Dispose();
            errorBoundary.Dispose();
        }

        /// <summary>
        /// Custom recovery strategy example
        /// </summary>
        public static void CustomRecoveryStrategyExample()
        {
            // Create a custom recovery strategy for network operations
            var networkRecovery = new ExponentialBackoffStrategy(
                maxRetries: 5,
                baseDelay: TimeSpan.FromSeconds(1),
                backoffMultiplier: 1.5,
                shouldRetryPredicate: ex => ex is TimeoutException || ex is System.Net.NetworkInformation.NetworkInformationException
            );

            var errorBoundary = new ErrorBoundary("NetworkOperations");
            errorBoundary.AddRecoveryStrategy(networkRecovery);

            // Simulate a network-dependent computation
            var networkData = ReactiveFlow.CreateState<string>(null);
            var processedData = ReactiveFlow.CreateComputed(
                "ProcessNetworkData",
                builder =>
                {
                    var data = builder.Track(networkData);
                    if (data == null)
                        throw new TimeoutException("Network timeout");
                    return $"Processed: {data}";
                },
                fallbackValue: "Offline Mode",
                errorBoundary: errorBoundary
            );

            // Test network failure and recovery
            networkData.Set(null); // Triggers timeout
            networkData.Set("network response"); // Should recover

            processedData.Dispose();
            errorBoundary.Dispose();
        }

        /// <summary>
        /// Circuit breaker pattern example
        /// </summary>
        public static void CircuitBreakerExample()
        {
            var options = new ErrorBoundaryOptions
            {
                CircuitBreakerThreshold = 3, // Open circuit after 3 failures
                UseDefaultRecoveryStrategy = true
            };

            var errorBoundary = new ErrorBoundary("CircuitBreakerDemo", options: options);
            
            // Subscribe to circuit breaker events
            errorBoundary.OnCircuitBreakerOpen += operationName =>
            {
                Console.WriteLine($"Circuit breaker opened for operation: {operationName}");
            };

            var faultyInput = ReactiveFlow.CreateState(0);
            var faultyComputation = ReactiveFlow.CreateComputed(
                "FaultyOperation",
                builder =>
                {
                    var value = builder.Track(faultyInput);
                    if (value < 10)
                        throw new InvalidOperationException("Value too small");
                    return value * 2;
                },
                fallbackValue: -1,
                errorBoundary: errorBoundary
            );

            // Trigger multiple failures to open circuit breaker
            for (int i = 0; i < 5; i++)
            {
                faultyInput.Set(i); // Will fail for values < 10
            }

            // Reset and try again
            errorBoundary.Reset();
            faultyInput.Set(15); // Should work now

            faultyComputation.Dispose();
            errorBoundary.Dispose();
        }

        /// <summary>
        /// Logging configuration example
        /// </summary>
        public static void LoggingConfigurationExample()
        {
            // Configure custom logger
            var customLogger = new DefaultReactiveLogger(
                minimumLevel: LogLevel.Debug,
                includeTimestamp: true,
                includeContext: true
            );

            ReactiveGlobals.ConfigureLogger(customLogger);

            // Create computations that will generate log messages
            var state = ReactiveFlow.CreateState(1);
            var computed = ReactiveFlow.CreateComputed(
                "LoggedComputation",
                builder =>
                {
                    var value = builder.Track(state);
                    if (value == 0)
                        throw new DivideByZeroException("Cannot divide by zero");
                    return 100 / value;
                },
                fallbackValue: 0
            );

            // This will generate debug logs
            state.Set(5);
            state.Set(2);
            state.Set(0); // This will generate error logs

            computed.Dispose();
        }

        /// <summary>
        /// Error boundary hierarchy example
        /// </summary>
        public static void ErrorBoundaryHierarchyExample()
        {
            // Create a hierarchy of error boundaries
            var globalBoundary = new ErrorBoundary("Global");
            var uiBoundary = new ErrorBoundary("UI");
            var dataBoundary = new ErrorBoundary("Data");

            // Configure different strategies for different boundaries
            globalBoundary.AddRecoveryStrategy(new FailFastStrategy());
            uiBoundary.AddRecoveryStrategy(new FixedRetryStrategy(maxRetries: 2));
            dataBoundary.AddRecoveryStrategy(new ExponentialBackoffStrategy(maxRetries: 5));

            // Create computations with different error boundaries
            var dataState = ReactiveFlow.CreateState("valid");
            var dataComputation = ReactiveFlow.CreateComputed(
                "DataProcessing",
                builder =>
                {
                    var data = builder.Track(dataState);
                    if (data == "invalid")
                        throw new ArgumentException("Invalid data");
                    return data.ToUpper();
                },
                fallbackValue: "FALLBACK",
                errorBoundary: dataBoundary
            );

            var uiComputation = ReactiveFlow.CreateComputed(
                "UIRendering",
                builder =>
                {
                    var processedData = builder.Track(dataComputation);
                    return $"UI: {processedData}";
                },
                fallbackValue: "UI: ERROR",
                errorBoundary: uiBoundary
            );

            // Test error propagation
            dataState.Set("invalid"); // Should be handled by data boundary
            dataState.Set("valid"); // Should recover

            // Cleanup
            uiComputation.Dispose();
            dataComputation.Dispose();
            dataBoundary.Dispose();
            uiBoundary.Dispose();
            globalBoundary.Dispose();
        }
    }
}
