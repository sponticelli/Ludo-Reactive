using System;
using Ludo.Reactive.ErrorHandling;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Simple test to verify error handling functionality
    /// </summary>
    public static class ErrorHandlingTest
    {
        /// <summary>
        /// Run a basic test of the error handling system
        /// </summary>
        public static void RunBasicTest()
        {
            // Configure logging
            ReactiveGlobals.ConfigureLogger(new DefaultReactiveLogger(LogLevel.Debug));
            
            Console.WriteLine("=== Error Handling Test ===");
            
            // Test 1: Basic error boundary with recovery
            TestBasicErrorRecovery();
            
            // Test 2: Fallback values
            TestFallbackValues();
            
            // Test 3: Circuit breaker
            TestCircuitBreaker();
            
            Console.WriteLine("=== Test Complete ===");
        }
        
        private static void TestBasicErrorRecovery()
        {
            Console.WriteLine("\n--- Test 1: Basic Error Recovery ---");
            
            var errorBoundary = new ErrorBoundary("Test1");
            errorBoundary.AddRecoveryStrategy(new FixedRetryStrategy(maxRetries: 2));
            
            var counter = ReactiveFlow.CreateState(0);
            var computation = ReactiveFlow.CreateComputed(
                "TestComputation",
                builder =>
                {
                    var value = builder.Track(counter);
                    if (value < 3)
                        throw new ArgumentException($"Value {value} is too small");
                    return value * 2;
                },
                fallbackValue: -1,
                errorBoundary: errorBoundary
            );
            
            // Subscribe to see results
            computation.Subscribe(value => Console.WriteLine($"Result: {value}"));
            
            // Test sequence
            counter.Set(1); // Should fail and use fallback
            counter.Set(2); // Should fail and use fallback  
            counter.Set(5); // Should succeed
            
            computation.Dispose();
            errorBoundary.Dispose();
        }
        
        private static void TestFallbackValues()
        {
            Console.WriteLine("\n--- Test 2: Fallback Values ---");
            
            var input = ReactiveFlow.CreateState<string>(null);
            var processed = ReactiveFlow.CreateComputed(
                "ProcessString",
                builder =>
                {
                    var str = builder.Track(input);
                    if (string.IsNullOrEmpty(str))
                        throw new ArgumentNullException("Input is null or empty");
                    return str.ToUpper();
                },
                fallbackValue: "DEFAULT"
            );
            
            processed.Subscribe(value => Console.WriteLine($"Processed: {value}"));
            
            input.Set(null);     // Should use fallback
            input.Set("");       // Should use fallback
            input.Set("hello");  // Should work
            
            processed.Dispose();
        }
        
        private static void TestCircuitBreaker()
        {
            Console.WriteLine("\n--- Test 3: Circuit Breaker ---");
            
            var options = new ErrorBoundaryOptions
            {
                CircuitBreakerThreshold = 3,
                UseDefaultRecoveryStrategy = false
            };
            
            var errorBoundary = new ErrorBoundary("CircuitTest", options: options);
            errorBoundary.OnCircuitBreakerOpen += op => 
                Console.WriteLine($"Circuit breaker opened for: {op}");
            
            var faultyState = ReactiveFlow.CreateState(0);
            var faultyComputation = ReactiveFlow.CreateComputed(
                "FaultyComputation",
                builder =>
                {
                    var value = builder.Track(faultyState);
                    throw new InvalidOperationException($"Always fails with value {value}");
                },
                fallbackValue: -999,
                errorBoundary: errorBoundary
            );
            
            faultyComputation.Subscribe(value => Console.WriteLine($"Faulty result: {value}"));
            
            // Trigger failures to open circuit breaker
            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine($"Setting value to {i}");
                faultyState.Set(i);
            }
            
            faultyComputation.Dispose();
            errorBoundary.Dispose();
        }
    }
}
