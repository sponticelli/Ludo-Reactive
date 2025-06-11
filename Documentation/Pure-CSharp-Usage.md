# Pure C# Usage

This guide shows how to use Ludo.Reactive in pure C# environments without Unity dependencies.

## Getting Started

Ludo.Reactive can be used in any .NET application, including console applications, web applications, WPF, WinForms, and more.

### Basic Setup

```csharp
using Ludo.Reactive;

class Program
{
    static void Main()
    {
        // Create reactive state
        var counter = ReactiveFlow.CreateState(0);
        
        // Create computed value
        var doubled = ReactiveFlow.CreateComputed("doubled", builder =>
        {
            return builder.Track(counter) * 2;
        });
        
        // Create effect
        var logger = ReactiveFlow.CreateEffect("logger", builder =>
        {
            var value = builder.Track(doubled);
            Console.WriteLine($"Doubled: {value}");
        });
        
        // Update state
        counter.Set(5); // Prints: "Doubled: 10"
        counter.Set(10); // Prints: "Doubled: 20"
        
        // Cleanup
        logger.Dispose();
        doubled.Dispose();
    }
}
```

## Console Application Examples

### Simple Calculator

```csharp
using System;
using Ludo.Reactive;

public class ReactiveCalculator
{
    private readonly ReactiveState<double> _operandA;
    private readonly ReactiveState<double> _operandB;
    private readonly ReactiveState<string> _operation;
    private readonly ComputedValue<double> _result;
    private readonly ReactiveEffect _displayEffect;
    
    public ReactiveCalculator()
    {
        _operandA = ReactiveFlow.CreateState(0.0);
        _operandB = ReactiveFlow.CreateState(0.0);
        _operation = ReactiveFlow.CreateState("+");
        
        _result = ReactiveFlow.CreateComputed("result", builder =>
        {
            var a = builder.Track(_operandA);
            var b = builder.Track(_operandB);
            var op = builder.Track(_operation);
            
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b != 0 ? a / b : double.NaN,
                _ => double.NaN
            };
        });
        
        _displayEffect = ReactiveFlow.CreateEffect("display", builder =>
        {
            var a = builder.Track(_operandA);
            var b = builder.Track(_operandB);
            var op = builder.Track(_operation);
            var result = builder.Track(_result);
            
            Console.WriteLine($"{a} {op} {b} = {result}");
        });
    }
    
    public void SetOperandA(double value) => _operandA.Set(value);
    public void SetOperandB(double value) => _operandB.Set(value);
    public void SetOperation(string operation) => _operation.Set(operation);
    public double GetResult() => _result.Current;
    
    public void Dispose()
    {
        _displayEffect?.Dispose();
        _result?.Dispose();
    }
}

// Usage
var calculator = new ReactiveCalculator();
calculator.SetOperandA(10);
calculator.SetOperandB(5);
calculator.SetOperation("*"); // Prints: "10 * 5 = 50"
```

### Data Processing Pipeline

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Ludo.Reactive;

public class DataProcessor
{
    private readonly ReactiveState<List<int>> _rawData;
    private readonly ReactiveState<bool> _enableFiltering;
    private readonly ReactiveState<int> _filterThreshold;
    
    private readonly ComputedValue<List<int>> _filteredData;
    private readonly ComputedValue<double> _average;
    private readonly ComputedValue<int> _sum;
    private readonly ComputedValue<string> _statistics;
    
    public DataProcessor()
    {
        _rawData = ReactiveFlow.CreateState(new List<int>());
        _enableFiltering = ReactiveFlow.CreateState(false);
        _filterThreshold = ReactiveFlow.CreateState(0);
        
        _filteredData = ReactiveFlow.CreateComputed("filteredData", builder =>
        {
            var data = builder.Track(_rawData);
            var enableFilter = builder.Track(_enableFiltering);
            var threshold = builder.Track(_filterThreshold);
            
            if (enableFilter)
                return data.Where(x => x >= threshold).ToList();
            else
                return data;
        });
        
        _average = ReactiveFlow.CreateComputed("average", builder =>
        {
            var data = builder.Track(_filteredData);
            return data.Count > 0 ? data.Average() : 0.0;
        });
        
        _sum = ReactiveFlow.CreateComputed("sum", builder =>
        {
            var data = builder.Track(_filteredData);
            return data.Sum();
        });
        
        _statistics = ReactiveFlow.CreateComputed("statistics", builder =>
        {
            var data = builder.Track(_filteredData);
            var avg = builder.Track(_average);
            var sum = builder.Track(_sum);
            
            return $"Count: {data.Count}, Sum: {sum}, Average: {avg:F2}";
        });
        
        // Auto-display statistics when they change
        ReactiveFlow.CreateEffect("statsDisplay", builder =>
        {
            var stats = builder.Track(_statistics);
            Console.WriteLine($"Statistics: {stats}");
        });
    }
    
    public void SetData(List<int> data) => _rawData.Set(data);
    public void AddData(int value) => _rawData.Update(list => 
    {
        var newList = new List<int>(list) { value };
        return newList;
    });
    public void EnableFiltering(bool enable) => _enableFiltering.Set(enable);
    public void SetFilterThreshold(int threshold) => _filterThreshold.Set(threshold);
    
    public string GetStatistics() => _statistics.Current;
}

// Usage
var processor = new DataProcessor();
processor.SetData(new List<int> { 1, 5, 10, 15, 20, 25 });
// Prints: "Statistics: Count: 6, Sum: 76, Average: 12.67"

processor.EnableFiltering(true);
processor.SetFilterThreshold(10);
// Prints: "Statistics: Count: 4, Sum: 70, Average: 17.50"
```

## Web Application Integration

### ASP.NET Core Example

```csharp
using Microsoft.AspNetCore.Mvc;
using Ludo.Reactive;

[ApiController]
[Route("api/[controller]")]
public class CounterController : ControllerBase
{
    private static readonly ReactiveState<int> _counter = ReactiveFlow.CreateState(0);
    private static readonly ComputedValue<string> _status = ReactiveFlow.CreateComputed("status", builder =>
    {
        var count = builder.Track(_counter);
        return count switch
        {
            0 => "Starting",
            < 10 => "Low",
            < 100 => "Medium",
            _ => "High"
        };
    });
    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            Count = _counter.Current, 
            Status = _status.Current 
        });
    }
    
    [HttpPost("increment")]
    public IActionResult Increment()
    {
        _counter.Update(x => x + 1);
        return Ok(new { 
            Count = _counter.Current, 
            Status = _status.Current 
        });
    }
    
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _counter.Set(0);
        return Ok(new { 
            Count = _counter.Current, 
            Status = _status.Current 
        });
    }
}
```

## WPF Integration

### ViewModel with Reactive Properties

```csharp
using System.ComponentModel;
using System.Windows.Input;
using Ludo.Reactive;

public class ReactiveViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ReactiveState<string> _firstName;
    private readonly ReactiveState<string> _lastName;
    private readonly ComputedValue<string> _fullName;
    private readonly ComputedValue<bool> _canSave;
    private readonly List<IDisposable> _disposables = new();
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public ReactiveViewModel()
    {
        _firstName = ReactiveFlow.CreateState("");
        _lastName = ReactiveFlow.CreateState("");
        
        _fullName = ReactiveFlow.CreateComputed("fullName", builder =>
        {
            var first = builder.Track(_firstName);
            var last = builder.Track(_lastName);
            return $"{first} {last}".Trim();
        });
        
        _canSave = ReactiveFlow.CreateComputed("canSave", builder =>
        {
            var first = builder.Track(_firstName);
            var last = builder.Track(_lastName);
            return !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last);
        });
        
        // Bridge reactive values to WPF property change notifications
        _disposables.Add(_firstName.Subscribe(_ => OnPropertyChanged(nameof(FirstName))));
        _disposables.Add(_lastName.Subscribe(_ => OnPropertyChanged(nameof(LastName))));
        _disposables.Add(_fullName.Subscribe(_ => OnPropertyChanged(nameof(FullName))));
        _disposables.Add(_canSave.Subscribe(_ => OnPropertyChanged(nameof(CanSave))));
        
        _disposables.Add(_fullName);
        _disposables.Add(_canSave);
    }
    
    public string FirstName
    {
        get => _firstName.Current;
        set => _firstName.Set(value);
    }
    
    public string LastName
    {
        get => _lastName.Current;
        set => _lastName.Set(value);
    }
    
    public string FullName => _fullName.Current;
    public bool CanSave => _canSave.Current;
    
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable?.Dispose();
        _disposables.Clear();
    }
}
```

## Advanced Patterns

### State Machine

```csharp
public enum ConnectionState { Disconnected, Connecting, Connected, Error }

public class ConnectionManager
{
    private readonly ReactiveState<ConnectionState> _state;
    private readonly ReactiveState<string> _errorMessage;
    private readonly ComputedValue<bool> _canConnect;
    private readonly ComputedValue<bool> _canDisconnect;
    
    public ConnectionManager()
    {
        _state = ReactiveFlow.CreateState(ConnectionState.Disconnected);
        _errorMessage = ReactiveFlow.CreateState("");
        
        _canConnect = ReactiveFlow.CreateComputed("canConnect", builder =>
        {
            var state = builder.Track(_state);
            return state == ConnectionState.Disconnected || state == ConnectionState.Error;
        });
        
        _canDisconnect = ReactiveFlow.CreateComputed("canDisconnect", builder =>
        {
            var state = builder.Track(_state);
            return state == ConnectionState.Connected;
        });
        
        // Log state changes
        ReactiveFlow.CreateEffect("stateLogger", builder =>
        {
            var state = builder.Track(_state);
            var error = builder.Track(_errorMessage);
            
            Console.WriteLine($"Connection state: {state}");
            if (state == ConnectionState.Error && !string.IsNullOrEmpty(error))
                Console.WriteLine($"Error: {error}");
        });
    }
    
    public async Task ConnectAsync()
    {
        if (!_canConnect.Current) return;
        
        _state.Set(ConnectionState.Connecting);
        _errorMessage.Set("");
        
        try
        {
            // Simulate connection
            await Task.Delay(1000);
            _state.Set(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _state.Set(ConnectionState.Error);
            _errorMessage.Set(ex.Message);
        }
    }
    
    public void Disconnect()
    {
        if (!_canDisconnect.Current) return;
        _state.Set(ConnectionState.Disconnected);
        _errorMessage.Set("");
    }
    
    public ConnectionState CurrentState => _state.Current;
    public bool CanConnect => _canConnect.Current;
    public bool CanDisconnect => _canDisconnect.Current;
}
```

## Performance Considerations

### Batching Updates

```csharp
var items = ReactiveFlow.CreateState(new List<string>());
var count = ReactiveFlow.CreateComputed("count", builder => builder.Track(items).Count);
var summary = ReactiveFlow.CreateComputed("summary", builder => 
    $"Items: {builder.Track(count)}, First: {builder.Track(items).FirstOrDefault()}");

// Without batching - triggers multiple recalculations
items.Set(new List<string> { "A" });
items.Update(list => list.Concat(new[] { "B" }).ToList());

// With batching - single recalculation
ReactiveFlow.ExecuteBatch(() =>
{
    items.Set(new List<string> { "A" });
    items.Update(list => list.Concat(new[] { "B" }).ToList());
});
```

### Memory Management

```csharp
public class ResourceManager : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    
    public T Track<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }
    
    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable?.Dispose();
        _disposables.Clear();
    }
}

// Usage
using var resources = new ResourceManager();
var state = resources.Track(ReactiveFlow.CreateState(0));
var computed = resources.Track(ReactiveFlow.CreateComputed("doubled", builder => builder.Track(state) * 2));
var effect = resources.Track(ReactiveFlow.CreateEffect("logger", builder => Console.WriteLine(builder.Track(computed))));
// All resources automatically disposed when 'using' scope ends
```

## Testing

### Unit Testing Reactive Logic

```csharp
[Test]
public void TestReactiveCalculation()
{
    // Arrange
    var input = ReactiveFlow.CreateState(5);
    var doubled = ReactiveFlow.CreateComputed("doubled", builder => builder.Track(input) * 2);
    
    // Act & Assert
    Assert.AreEqual(10, doubled.Current);
    
    input.Set(7);
    Assert.AreEqual(14, doubled.Current);
    
    // Cleanup
    doubled.Dispose();
}
```
