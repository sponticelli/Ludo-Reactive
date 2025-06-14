# Changelog

All notable changes to the Ludo.Reactive package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.2] - 2025-06-14

## Added

### Test Coverage
- MonoBehaviour lifecycle integration
- Coroutine-based operations
- Unity event system integration
- Comprehensive async testing
- DisposableTracker component tests
- ObservableTrigger component tests
- Unity schedulers integration


## [1.2.1] - 2025-06-14

### Added

#### Universal Reactive Programming Terminology Aliases
- **Map()** - Alias for Select() to provide universal reactive programming terminology compatibility
  - Identical functionality to Select() for transforming observable values
  - Familiar to developers from RxJS, RxJava, and other reactive frameworks
  - Maintains 100% backward compatibility with existing Select() usage
- **FlatMap()** - Alias for SelectMany() to provide universal reactive programming terminology compatibility
  - Identical functionality to SelectMany() for flattening nested observables
  - Common terminology across reactive programming ecosystems
  - Maintains 100% backward compatibility with existing SelectMany() usage
- **Fold()** - Alias for Scan() to provide universal reactive programming terminology compatibility
  - Identical functionality to Scan() for accumulating values with intermediate results
  - Familiar terminology from functional programming languages
  - Maintains 100% backward compatibility with existing Scan() usage
- **Filter()** - Alias for Where() to provide universal reactive programming terminology compatibility
  - Identical functionality to Where() for filtering observable sequences based on predicates
  - Standard terminology across RxJS, RxJava, RxSwift, and most functional programming languages
  - Maintains 100% backward compatibility with existing Where() usage
- **Tap()** - Alias for Do() to provide universal reactive programming terminology compatibility
  - Identical functionality to Do() for performing side effects without affecting the stream
  - Standard terminology in RxJS, RxJava, RxSwift, and Angular reactive programming
  - Supports both single-action and full lifecycle (onNext, onError, onCompleted) overloads
  - Maintains 100% backward compatibility with existing Do() usage
- **Reduce()** - Alias for Aggregate() to provide universal reactive programming terminology compatibility
  - Identical functionality to Aggregate() for applying accumulator functions and returning final results
  - Standard terminology in RxJS, JavaScript Array methods, and most functional programming languages
  - Supports both seeded and non-seeded reduction operations
  - Maintains 100% backward compatibility with existing Aggregate() usage
- **Of()** - Alias for Return() to provide universal reactive programming terminology compatibility
  - Identical functionality to Return() for creating observables with single elements
  - Standard terminology in RxJS, RxJava, and RxSwift for single-value observable creation
  - Maintains 100% backward compatibility with existing Return() usage
- **Debounce()** - Alias for Throttle() to provide universal reactive programming terminology compatibility
  - Identical functionality to Throttle() for limiting emission rate after periods of silence
  - Standard terminology in RxJS, web development, and Angular reactive programming
  - Supports both TimeSpan-only and TimeSpan+Scheduler overloads
  - Maintains 100% backward compatibility with existing Throttle() usage
- **Distinct()** - Alias for DistinctUntilChanged() to provide universal reactive programming terminology compatibility
  - Identical functionality to DistinctUntilChanged() for skipping consecutive duplicate values
  - Simplified terminology commonly used in RxJava and functional programming contexts
  - Supports both default equality comparer and custom comparer overloads
  - Maintains 100% backward compatibility with existing DistinctUntilChanged() usage
- **Void()** - Alias for AsUnitObservable() to provide universal reactive programming terminology compatibility
  - Identical functionality to AsUnitObservable() for converting sequences to Unit type
  - Standard terminology in RxSwift and RxJava for discarding values while preserving events
  - Maintains 100% backward compatibility with existing AsUnitObservable() usage

### Enhanced Features
- **Developer Experience** - Improved approachability for developers from other reactive ecosystems
- **Cross-Platform Familiarity** - Universal terminology reduces learning curve for new users
- **Documentation** - Comprehensive XML documentation with cross-references to original methods

## [1.2.0] - 2025-06-13

### Added

#### State Management System
- **ReactiveStore<TState>** - Redux-like state container with immutable state updates
  - Action dispatching with middleware support
  - Reactive state observation with change events
  - Thread-safe operations with proper locking
  - Built-in logging middleware for debugging
- **IAction & ActionBase** - Action system for describing state changes
  - Timestamp tracking for all actions
  - Support for payload actions with typed data
  - Reversible actions for undo/redo functionality
- **IReducer & ReducerBase** - Pure functions for state transformations
  - Composite reducers for handling multiple action types
  - Functional reducers for simple use cases
  - Action filtering and routing capabilities
- **StateSelector & MemoizedSelector** - Efficient state slicing and caching
  - Memoization for performance optimization
  - Reactive selectors that emit on changes
  - Performance metrics and cache hit tracking
- **ImmutableStateUpdater** - Utilities for immutable state updates
  - Property update helpers with fluent API
  - Deep copying for reference types
  - Immutability validation and change detection

#### Command History & Undo/Redo
- **CommandHistory<TState>** - Complete undo/redo system
  - Configurable history size limits
  - Command execution tracking with timestamps
  - Branching support for complex undo scenarios
  - Observable history change events
- **IReversibleCommand** - Commands that can be executed and undone
  - Base implementations for common patterns
  - Command naming and metadata support
  - Conditional undo capabilities

#### State Persistence
- **IStatePersistence<TState>** - Flexible state persistence interface
- **PlayerPrefsStatePersistence** - Unity PlayerPrefs-based persistence
- **FileStatePersistence** - File system-based persistence with JSON
- **MemoryStatePersistence** - In-memory persistence for testing
- Cross-session state management with automatic serialization

#### Enhanced Reactive Collections
- **ObservableList<T>** - List with granular change tracking
  - Batch operations with AddRange support
  - Detailed change events with timestamps
  - Collection change sets for efficient updates
  - Unity serialization support
- **ObservableDictionary<TKey, TValue>** - Dictionary with key/value change events
  - Separate observables for add/remove/replace operations
  - Conflict detection and resolution
  - Thread-safe operations
- **ObservableSet<T>** - Set with add/remove notifications
  - Complete set operation support (Union, Intersect, Except)
  - Uniqueness enforcement with custom comparers
  - Efficient membership testing

#### Collection Synchronization
- **CollectionSynchronizer<T>** - Multi-collection synchronization
  - Conflict resolution strategies (Source/Target/MostRecent/Custom)
  - Bidirectional synchronization support
  - Performance statistics and monitoring
  - Automatic cleanup and disposal management
- **CollectionDiffer<T>** - Efficient diff algorithms
  - Myers' diff algorithm for optimal performance
  - Simple diff for append/remove scenarios
  - Longest Common Subsequence computation
  - Diff application with error handling

#### Collection Change Tracking
- **CollectionChangeSet<T>** - Batch change representation
  - Granular change type tracking (Add/Remove/Replace/Move/Reset)
  - Change summarization and statistics
  - Timestamp tracking for all changes
- **DiffOperation<T>** - Individual diff operations
  - Insert/Delete/Replace operation types
  - Index-based change tracking
  - Efficient batch application

### Enhanced Features
- **Fluent API Extensions** - Chainable methods for state updates
- **Performance Optimizations** - Object pooling for change events
- **Memory Management** - Automatic disposal and cleanup
- **Error Handling** - Robust exception handling with logging
- **Unity Integration** - Seamless Unity component lifecycle support

### Examples & Documentation
- **ReactiveV12Example** - Comprehensive demonstration of all v1.2.0 features
- **State Management Patterns** - Best practices and common patterns
- **Collection Synchronization Examples** - Multi-collection scenarios
- **Undo/Redo Implementation Guide** - Complete command pattern examples

## [1.1.0] - 2025-06-13

### Added

#### Core Features
- **MessageBroker System** - Global event system for decoupled communication between components
  - Dictionary-based O(1) subscription lookups for performance
  - Message filtering capabilities with predicate support
  - Automatic cleanup with weak references to prevent memory leaks
  - Thread-safe operations with proper locking mechanisms
  - Extension methods for Unity component integration

- **TestScheduler** - Deterministic scheduler for unit testing
  - Manual time control with virtual time advancement
  - Predictable testing of time-based reactive operations
  - Integration with existing IScheduler interface
  - Support for scheduled, delayed, and periodic operations
  - Comprehensive test scenario support

- **Object Pooling System** - Reduces garbage collection pressure
  - Observable instance pooling for frequently created objects
  - Subscription object pooling for memory efficiency
  - Event argument object pooling to minimize allocations
  - Thread-safe concurrent queue implementation
  - Configurable pool sizes and automatic cleanup

#### WebGL Optimizations
- **WebGL-Specific Performance Optimizations**
  - Reduced garbage collection pressure for browser environments
  - Optimized coroutine usage for WebGL threading limitations
  - Memory-efficient observable chains with caching
  - Batched operations to reduce emission frequency
  - Browser-optimized timing mechanisms

#### Enhanced UI Integrations
- **Advanced Slider Reactive Bindings**
  - Two-way reactive property binding with change detection
  - Value transformation support for custom data types
  - Precision-based change detection to prevent feedback loops

- **Toggle/Checkbox Reactive State Management**
  - Bidirectional binding with ReactiveProperty<bool>
  - Automatic state synchronization
  - Change detection to prevent unnecessary updates

- **Enhanced Dropdown Selection Streams**
  - Rich selection data including index, text, and image
  - Enum-based reactive property binding
  - Type-safe dropdown value management

- **ScrollRect Position and Content Change Observables**
  - Comprehensive scroll data including position, velocity, and bounds
  - Near-end detection for infinite scrolling scenarios
  - Viewport and content size tracking

### Performance Improvements
- **Dictionary-based Subscription Lookups** - O(1) performance for message broker operations
- **Weak References** - Automatic cleanup prevents memory leaks in long-running applications
- **Dirty Checking** - Change detection optimizations reduce unnecessary notifications
- **Computation Caching** - Memoization for frequently accessed values
- **Object Pooling** - Reuse of frequently created instances reduces GC pressure

### Technical Enhancements
- **Thread-Safe Operations** - Proper locking mechanisms for concurrent access
- **Exception Handling** - Robust error handling with proper logging
- **Memory Management** - Automatic disposal tracking and cleanup
- **Performance Monitoring** - Built-in metrics for subscription counts and pool usage

## [1.0.0] - 2024-12-19

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



For more information about each feature, see the [Documentation](Documentation/) folder.
