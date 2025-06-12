# Changelog

All notable changes to the Ludo.Reactive package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-12

### Added

#### Core Framework
- **ReactiveProperty<T>** - Mutable observable container with change notifications
- **Subject<T>** - Bridge between imperative and reactive code
- **BehaviorSubject<T>** - Subject that stores and replays the latest value
- **ReplaySubject<T>** - Subject that buffers and replays multiple values
- **IReadOnlyReactiveProperty<T>** - Read-only interface for reactive properties

#### Observable Creation
- **Observable.Return<T>()** - Create observable with single value
- **Observable.Empty<T>()** - Create empty observable
- **Observable.Never<T>()** - Create non-terminating observable
- **Observable.Throw<T>()** - Create observable that terminates with error
- **Observable.Create<T>()** - Create custom observable
- **Observable.Interval()** - Create time-based observable sequence
- **Observable.Timer()** - Create delayed observable
- **Observable.Generate()** - Create observable from state-driven loop
- **Observable.FromCoroutine()** - Convert Unity coroutines to observables
- **Observable.FromUnityEvent()** - Convert UnityEvents to observables
- **Observable.FromAsyncOperation()** - Convert AsyncOperations to observables
- **Observable.Range()** - Generate sequence of integers

#### Transformation Operators
- **Select()** - Transform values using projection function
- **SelectMany()** - Flatten nested observables
- **Where()** - Filter values using predicate
- **DistinctUntilChanged()** - Skip consecutive duplicate values
- **Take()** - Take first N values
- **Buffer()** - Collect values over time or count
- **Scan()** - Accumulate values with running total
- **Aggregate()** - Apply accumulator function and return final result
- **Cast()** - Convert elements to specified type
- **OfType()** - Filter elements by type
- **AsUnitObservable()** - Convert to Unit observable

#### Combination Operators
- **CombineLatest()** - Combine latest values from multiple sources
- **Merge()** - Merge multiple observable streams
- **Zip()** - Pair values by index

#### Utility Operators
- **Do()** - Perform side effects without affecting the stream
- **Delay()** - Time-shift emissions
- **Throttle()** - Limit emission rate
- **Timeout()** - Apply timeout policy
- **TakeUntil()** - Take values until another observable emits
- **ObserveOn()** - Observe notifications on specified scheduler
- **SubscribeOn()** - Subscribe on specified scheduler
- **Publish()** - Create connectable observable for multicasting
- **Replay()** - Create connectable observable with replay buffer
- **RefCount()** - Automatically connect/disconnect based on subscriptions

#### Unity Integration
- **UnitySchedulers** - Unity-specific schedulers for different execution contexts
  - MainThread scheduler for Update loop
  - FixedUpdate scheduler for physics updates
  - LateUpdate scheduler for post-update operations
  - EndOfFrame scheduler for end-of-frame operations
- **AddTo()** extension - Automatic disposal with GameObject lifecycle
- **DisposableTracker** - Component for managing disposable lifetimes
- **ObservableTrigger** - Component for Unity lifecycle observables

#### Unity Lifecycle Observables
- **OnDestroyAsObservable()** - Observe GameObject destruction
- **OnEnableAsObservable()** - Observe GameObject enable events
- **OnDisableAsObservable()** - Observe GameObject disable events
- **UpdateAsObservable()** - Observe Update loop
- **FixedUpdateAsObservable()** - Observe FixedUpdate loop
- **LateUpdateAsObservable()** - Observe LateUpdate loop
- **EveryUpdate()** - Global Update observable
- **EveryFixedUpdate()** - Global FixedUpdate observable
- **EveryLateUpdate()** - Global LateUpdate observable

#### Unity Event Integration
- **UnityEvent.AsObservable()** - Convert UnityEvents to observables
- **Button.OnClickAsObservable()** - Observe button clicks
- **Toggle.OnValueChangedAsObservable()** - Observe toggle changes
- **Slider.OnValueChangedAsObservable()** - Observe slider changes
- **InputField.OnValueChangedAsObservable()** - Observe input field changes
- **InputField.OnEndEditAsObservable()** - Observe input field end edit
- **Dropdown.OnValueChangedAsObservable()** - Observe dropdown changes
- **Scrollbar.OnValueChangedAsObservable()** - Observe scrollbar changes
- **ScrollRect.OnValueChangedAsObservable()** - Observe scroll rect changes

#### Memory Management
- **CompositeDisposable** - Manage multiple disposables as a group
- **Disposable.Create()** - Create custom disposables
- **Disposable.Empty** - No-op disposable
- Automatic cleanup integration with Unity lifecycle
- Weak reference support to prevent memory leaks

#### Performance Optimizations
- Object pooling for frequently allocated observers
- Dictionary-based O(1) subscription lookups
- Lazy evaluation throughout the framework
- Minimal garbage collection design
- WebGL and mobile platform optimizations

#### Error Handling
- **Catch()** - Handle errors and continue with recovery observable
- **LogError()** - Log errors using Unity Debug.LogError
- Graceful error propagation through observable chains
- Unity-specific error logging integration
- Exception isolation to prevent cascade failures

#### Async/Await Integration
- **ToTask()** - Convert observable to Task
- **ToArrayAsync()** - Convert observable to Task<T[]>
- **ToObservable()** - Convert Task to observable
- **ObserveOnMainThread()** - Observe on Unity main thread
- **SubscribeOnMainThread()** - Subscribe on Unity main thread

#### Reactive Collections
- **ReactiveCollection<T>** - Observable collection with change notifications
- **ObserveAdd()** - Observe item additions
- **ObserveRemove()** - Observe item removals
- **ObserveReplace()** - Observe item replacements
- **ObserveMove()** - Observe item moves
- **ObserveReset()** - Observe collection resets
- **ObserveCountChanged()** - Observe count changes

#### Editor Integration
- **ReactivePropertyDrawer** - Custom property drawer for ReactiveProperty<T>
- **ReadOnlyReactivePropertyDrawer** - Property drawer for read-only properties
- **ReactiveDebugWindow** - Debug window for visualizing subscriptions
- Real-time subscription monitoring
- Performance metrics display
- GameObject selection integration

#### Examples and Documentation
- **ReactiveExample** - Comprehensive example script demonstrating usage patterns
- **Quick Start Guide** - Getting started documentation
- **Architecture Design Document** - Detailed system design documentation
- **README** - Package overview and basic usage
- **CHANGELOG** - Version history and changes

### Technical Details

#### Supported Unity Versions
- Unity 6000.0 or later
- .NET Standard 2.1 compatibility

#### Platform Support
- All Unity-supported platforms
- WebGL optimizations for browser environments
- Mobile-specific performance optimizations
- Editor integration for development workflow

#### Dependencies
- Unity Editor Coroutines (1.0.0) for editor-time coroutine support

### Performance Characteristics
- Zero allocation for most common operations
- O(1) subscription and unsubscription performance
- Minimal memory footprint
- Efficient batching for high-frequency events
- Automatic disposal prevents memory leaks

### Design Principles
- Unity-first approach with seamless integration
- SOLID principles for maintainable architecture
- Fluent API design for readable code
- Performance-oriented implementation
- Developer experience focused

### Breaking Changes
- None (initial release)

### Known Issues
- None at release

### Migration Guide
- None required (initial release)

---

## Future Roadmap

### Planned for v1.1.0
- MessageBroker for global event system
- TestScheduler for deterministic testing
- Additional UI component integrations
- Performance profiling tools
- WebGL-specific optimizations
- Object pooling implementation

### Planned for v1.2.0
- Custom scheduler implementations
- Advanced animation integration
- Reactive state management patterns
- Additional platform optimizations
- Performance monitoring tools

---

For more information about each feature, see the [Documentation](Documentation/) folder.
