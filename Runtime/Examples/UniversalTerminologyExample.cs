using System;
using UnityEngine;
using Ludo.Reactive;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Demonstrates the universal reactive programming terminology aliases (Map, FlatMap, Fold)
    /// alongside their original counterparts (Select, SelectMany, Scan) to show compatibility
    /// and provide familiar terminology for developers from other reactive ecosystems.
    /// </summary>
    public class UniversalTerminologyExample : MonoBehaviour
    {
        [Header("Universal Terminology Demo")]
        [SerializeField] private bool useUniversalTerminology = true;

        private ReactiveProperty<int> counter = new ReactiveProperty<int>(0);
        private ReactiveProperty<string> status = new ReactiveProperty<string>("Ready");
        private bool isRunning = true;

        private void Start()
        {
            Debug.Log("=== Universal Reactive Programming Terminology Demo ===");
            
            if (useUniversalTerminology)
            {
                DemonstrateUniversalTerminology();
            }
            else
            {
                DemonstrateOriginalTerminology();
            }
            
            // Start the counter
            InvokeRepeating(nameof(IncrementCounter), 1f, 1f);
        }

        private void DemonstrateUniversalTerminology()
        {
            Debug.Log("Using Universal Terminology (Map, FlatMap, Fold, Filter, Tap, Reduce, Of, Debounce, Distinct, Void):");

            // Map example - Transform values (equivalent to Select)
            counter
                .Map(value => $"Count: {value}")
                .Subscribe(text =>
                {
                    status.Value = text;
                    Debug.Log($"[Map] {text}");
                })
                .AddTo(this);

            // Filter example - Filter values (equivalent to Where)
            counter
                .Filter(value => value % 2 == 0) // Even numbers only
                .Tap(value => Debug.Log($"[Tap] Side effect for even: {value}")) // Side effects
                .Subscribe(evenValue => Debug.Log($"[Filter] Even number: {evenValue}"))
                .AddTo(this);

            // FlatMap example - Flatten observables (equivalent to SelectMany)
            counter
                .Filter(value => value % 3 == 0) // Every 3rd count (using Filter instead of Where)
                .FlatMap(value => Observable.Of(value).Delay(TimeSpan.FromSeconds(0.2f))) // Using Of instead of Return
                .Subscribe(flatValue => Debug.Log($"[FlatMap + Of] Delayed: {flatValue}"))
                .AddTo(this);

            // Fold example - Accumulate values (equivalent to Scan)
            counter
                .Take(5) // Only first 5 values
                .Fold(0, (accumulator, current) => accumulator + current)
                .Subscribe(sum => Debug.Log($"[Fold] Running sum: {sum}"))
                .AddTo(this);

            // Reduce example - Final aggregation (equivalent to Aggregate)
            counter
                .Take(4) // Only first 4 values
                .Reduce(1, (acc, x) => acc * x, acc => $"Product: {acc}") // Calculate product
                .Subscribe(result => Debug.Log($"[Reduce] {result}"))
                .AddTo(this);

            // Phase 2 aliases demonstration
            counter
                .Map(x => x % 3)                                    // Transform to modulo 3: 1,2,0,1,2,0...
                .Distinct()                                         // Remove consecutive duplicates
                .Debounce(TimeSpan.FromSeconds(0.1f))               // Debounce rapid changes
                .Tap(x => Debug.Log($"[Phase 2] Distinct value: {x}"))
                .Void()                                             // Convert to Unit
                .Subscribe(_ => Debug.Log($"[Void] Unit received"))
                .AddTo(this);

            // Comprehensive chaining with all universal terminology (Phase 1 + Phase 2)
            counter
                .Filter(x => x > 2)                                 // Phase 1: Filter values > 2
                .Map(x => x * 2)                                    // Phase 1: Double the value
                .Distinct()                                         // Phase 2: Remove consecutive duplicates
                .Tap(x => Debug.Log($"[Tap] Processing: {x}"))      // Phase 1: Side effect
                .Debounce(TimeSpan.FromSeconds(0.2f))               // Phase 2: Debounce
                .FlatMap(x => Observable.Of(x).Delay(TimeSpan.FromSeconds(0.1f))) // Phase 1: Delay with Of
                .Fold("Values: ", (acc, x) => acc + x + " ")        // Phase 1: Accumulate as string
                .Subscribe(result => Debug.Log($"[Universal Chain] {result}"))
                .AddTo(this);
        }

        private void DemonstrateOriginalTerminology()
        {
            Debug.Log("Using Original Terminology (Select, SelectMany, Scan):");
            
            // Select example - Transform values
            counter
                .Select(value => $"Count: {value}")
                .Subscribe(text => 
                {
                    status.Value = text;
                    Debug.Log($"[Select] {text}");
                })
                .AddTo(this);

            // SelectMany example - Flatten observables
            counter
                .Where(value => value % 3 == 0) // Every 3rd count
                .SelectMany(value => Observable.Range(value, 2)) // Emit value and value+1
                .Subscribe(flatValue => Debug.Log($"[SelectMany] Flattened: {flatValue}"))
                .AddTo(this);

            // Scan example - Accumulate values
            counter
                .Take(5) // Only first 5 values
                .Scan(0, (accumulator, current) => accumulator + current)
                .Subscribe(sum => Debug.Log($"[Scan] Running sum: {sum}"))
                .AddTo(this);

            // Chaining original terminology
            counter
                .Select(x => x * 2)                                    // Double the value
                .SelectMany(x => Observable.Return(x).Delay(TimeSpan.FromSeconds(0.5f))) // Delay emission
                .Scan("Values: ", (acc, x) => acc + x + " ")           // Accumulate as string
                .Subscribe(result => Debug.Log($"[Chained] {result}"))
                .AddTo(this);
        }

        private void IncrementCounter()
        {
            if (!isRunning) return;

            counter.Value++;

            // Stop after 10 increments
            if (counter.Value >= 10)
            {
                isRunning = false;
                Debug.Log("=== Demo Complete ===");
            }
        }

        private void OnDestroy()
        {
            counter?.Dispose();
            status?.Dispose();
        }

        [ContextMenu("Toggle Terminology")]
        private void ToggleTerminology()
        {
            useUniversalTerminology = !useUniversalTerminology;
            Debug.Log($"Switched to {(useUniversalTerminology ? "Universal" : "Original")} terminology. Restart to see changes.");
        }

        [ContextMenu("Demonstrate Compatibility")]
        private void DemonstrateCompatibility()
        {
            Debug.Log("=== Compatibility Demo ===");
            Debug.Log("Both terminologies can be used interchangeably:");

            // Mix universal and original terminology in the same chain
            Observable.Range(1, 6)
                .Filter(x => x % 2 == 0)   // Universal: Filter (even numbers)
                .Map(x => x * 2)           // Universal: Map
                .Tap(x => Debug.Log($"Processing: {x}")) // Universal: Tap
                .Where(x => x > 4)         // Original: Where
                .FlatMap(x => Observable.Of(x)) // Universal: FlatMap + Of
                .Take(2)                   // Original: Take
                .Reduce((acc, x) => acc + x) // Universal: Reduce
                .Subscribe(result => Debug.Log($"Mixed terminology result: {result}"));

            // Demonstrate all universal aliases together (Phase 1 + Phase 2)
            Observable.Of(10)              // Phase 1: Of
                .Filter(x => x > 5)        // Phase 1: Filter
                .Map(x => x * 3)           // Phase 1: Map (30)
                .Tap(x => Debug.Log($"Intermediate: {x}")) // Phase 1: Tap
                .FlatMap(x => Observable.Range(1, 4)) // Phase 1: FlatMap (1,2,3,4)
                .Distinct()                // Phase 2: Distinct (1,2,3,4 - no duplicates)
                .Debounce(TimeSpan.FromMilliseconds(50)) // Phase 2: Debounce
                .Reduce(0, (acc, x) => acc + x, acc => $"Sum: {acc}") // Phase 1: Reduce (Sum: 10)
                .Tap(result => Debug.Log($"Before Void: {result}")) // Phase 1: Tap
                .Void()                    // Phase 2: Void
                .Subscribe(_ => Debug.Log($"All universal aliases completed with Unit"));
        }
    }
}
