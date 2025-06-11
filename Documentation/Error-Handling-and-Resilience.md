# Error Handling and Resilience

This guide covers the comprehensive error handling and resilience features in Ludo.Reactive, designed to make reactive applications more robust and fault-tolerant.

## Overview

Ludo.Reactive provides a multi-layered approach to error handling:

1. **Structured Logging** - Comprehensive logging system with configurable levels
2. **Error Recovery Strategies** - Automatic retry mechanisms with various strategies
3. **Error Boundaries** - Isolation of failing computations from healthy ones
4. **Circuit Breaker Pattern** - Automatic failure detection and recovery
5. **Fallback Values** - Graceful degradation when computations fail

## Structured Logging

### IReactiveLogger Interface

The framework uses a structured logging approach through the `IReactiveLogger` interface:

```csharp
// Configure logging
ReactiveGlobals.ConfigureLogger(new DefaultReactiveLogger(
    minimumLevel: LogLevel.Debug,
    includeTimestamp: true,
    includeContext: true
));

// Logs are automatically generated for:
// - Computation execution times
// - Dependency tracking
// - Scheduler events
// - Error occurrences
```

### Unity Integration

For Unity projects, use the Unity-specific logger:

```csharp
// Automatically configured in Unity
var unityLogger = new UnityReactiveLogger(
    minimumLevel: LogLevel.Warning,
    includeStackTrace: false
);
ReactiveGlobals.ConfigureLogger(unityLogger);
```

## Error Recovery Strategies

### Built-in Strategies

#### Exponential Backoff Strategy
```csharp
var strategy = new ExponentialBackoffStrategy(
    maxRetries: 3,
    baseDelay: TimeSpan.FromMilliseconds(100),
    backoffMultiplier: 2.0,
    shouldRetryPredicate: ex => ex is TimeoutException
);
```

#### Fixed Retry Strategy
```csharp
var strategy = new FixedRetryStrategy(
    maxRetries: 3,
    delay: TimeSpan.FromMilliseconds(500)
);
```

#### Fail Fast Strategy
```csharp
var strategy = new FailFastStrategy(); // No retries, immediate failure
```

### Custom Recovery Strategies

Implement `IErrorRecoveryStrategy` for custom behavior:

```csharp
public class CustomRecoveryStrategy : IErrorRecoveryStrategy
{
    public bool CanHandle(ReactiveErrorInfo errorInfo)
    {
        // Custom logic to determine if this strategy applies
        return errorInfo.Exception is MyCustomException;
    }

    public bool TryRecover(ReactiveErrorInfo errorInfo, Func<bool> retryAction)
    {
        // Custom recovery logic
        if (errorInfo.RetryCount < MaxRetryAttempts)
        {
            // Perform custom recovery actions
            return retryAction();
        }
        return false;
    }

    public int MaxRetryAttempts => 5;
    
    public TimeSpan GetRetryDelay(int attemptNumber)
    {
        // Custom delay calculation
        return TimeSpan.FromSeconds(attemptNumber);
    }
}
```

## Error Boundaries

Error boundaries provide isolation and recovery for reactive computations:

### Basic Usage

```csharp
// Create an error boundary
var errorBoundary = new ErrorBoundary("UserInterface");
errorBoundary.AddRecoveryStrategy(new ExponentialBackoffStrategy());

// Use with computations
var computation = ReactiveFlow.CreateComputed(
    "RiskyComputation",
    builder => {
        // Computation that might fail
        var value = builder.Track(someState);
        if (value < 0) throw new ArgumentException("Negative value");
        return Math.Sqrt(value);
    },
    fallbackValue: 0.0,
    errorBoundary: errorBoundary
);
```

### Error Boundary Events

```csharp
errorBoundary.OnError += errorInfo =>
{
    Console.WriteLine($"Error in {errorInfo.ComputationName}: {errorInfo.Exception.Message}");
};

errorBoundary.OnRecovery += errorInfo =>
{
    Console.WriteLine($"Recovered: {errorInfo.ComputationName}");
};

errorBoundary.OnCircuitBreakerOpen += operationName =>
{
    Console.WriteLine($"Circuit breaker opened for: {operationName}");
};
```

### Circuit Breaker Pattern

```csharp
var options = new ErrorBoundaryOptions
{
    CircuitBreakerThreshold = 5, // Open after 5 consecutive failures
    UseDefaultRecoveryStrategy = true
};

var errorBoundary = new ErrorBoundary("NetworkOperations", options: options);

// The circuit breaker will automatically open after threshold failures
// and prevent further execution until manually reset
errorBoundary.Reset(); // Manually reset the circuit breaker
```

## Fallback Values

Computed values can specify fallback values for graceful degradation:

```csharp
var computation = ReactiveFlow.CreateComputed(
    "NetworkData",
    builder => {
        var response = builder.Track(networkResponse);
        if (response == null) throw new TimeoutException();
        return ProcessResponse(response);
    },
    fallbackValue: "Offline Mode" // Used when computation fails
);
```

## Global Error Handling

Configure global error handling for unhandled errors:

```csharp
// Configure global error boundary
var globalBoundary = new ErrorBoundary("Global");
globalBoundary.AddRecoveryStrategy(new ExponentialBackoffStrategy());
ReactiveGlobals.ConfigureGlobalErrorBoundary(globalBoundary);

// All computations without explicit error boundaries will use the global one
```

## Best Practices

### 1. Layer Error Boundaries

Create hierarchical error boundaries for different application layers:

```csharp
var dataBoundary = new ErrorBoundary("DataLayer");
var businessBoundary = new ErrorBoundary("BusinessLogic");
var uiBoundary = new ErrorBoundary("UserInterface");

// Configure different strategies for each layer
dataBoundary.AddRecoveryStrategy(new ExponentialBackoffStrategy(maxRetries: 5));
businessBoundary.AddRecoveryStrategy(new FixedRetryStrategy(maxRetries: 2));
uiBoundary.AddRecoveryStrategy(new FailFastStrategy());
```

### 2. Use Appropriate Fallback Values

Choose meaningful fallback values that maintain application functionality:

```csharp
var userProfile = ReactiveFlow.CreateComputed(
    "UserProfile",
    builder => LoadUserProfile(builder.Track(userId)),
    fallbackValue: new UserProfile { Name = "Guest", IsGuest = true }
);
```

### 3. Monitor Error Patterns

Use error boundary events to monitor and respond to error patterns:

```csharp
errorBoundary.OnError += errorInfo =>
{
    // Log to analytics
    Analytics.TrackError(errorInfo.ComputationName, errorInfo.Exception);
    
    // Alert on critical errors
    if (errorInfo.Severity == ErrorSeverity.Critical)
    {
        AlertingService.SendAlert(errorInfo);
    }
};
```

### 4. Configure Logging Appropriately

Use different log levels for different environments:

```csharp
#if DEBUG
var logger = new DefaultReactiveLogger(LogLevel.Debug);
#else
var logger = new DefaultReactiveLogger(LogLevel.Warning);
#endif
ReactiveGlobals.ConfigureLogger(logger);
```

## Unity-Specific Considerations

### Main Thread Safety

Unity computations automatically handle main thread requirements:

```csharp
var unityComputation = UnityReactiveFlow.CreateMainThreadComputed(
    "UIUpdate",
    builder => {
        // This runs on Unity's main thread
        var data = builder.Track(gameData);
        return UpdateUI(data);
    },
    fallbackValue: "Error State",
    errorBoundary: uiErrorBoundary
);
```

### Performance Monitoring

Monitor reactive performance in Unity:

```csharp
errorBoundary.OnError += errorInfo =>
{
    // Log to Unity Console
    Debug.LogError($"Reactive Error: {errorInfo}");
    
    // Track in Unity Analytics
    if (errorInfo.GetContext<float>("ExecutionTime") > 16.0f)
    {
        Debug.LogWarning("Slow reactive computation detected");
    }
};
```

## Troubleshooting

### Common Issues

1. **Memory Leaks**: Always dispose error boundaries and computations
2. **Infinite Loops**: Configure appropriate max iterations in scheduler
3. **Thread Safety**: Use Unity-specific schedulers for Unity computations
4. **Performance**: Monitor computation execution times through logging

### Debugging

Enable debug logging to trace reactive execution:

```csharp
ReactiveGlobals.ConfigureLogger(new DefaultReactiveLogger(LogLevel.Debug));

// This will log:
// - Computation scheduling
// - Dependency tracking
// - Error recovery attempts
// - Performance metrics
```
