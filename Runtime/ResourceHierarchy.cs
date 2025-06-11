using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Base class for hierarchical resource management with automatic cleanup
    /// </summary>
    public abstract class ResourceHierarchy : IDisposable, IComparable<ResourceHierarchy>
    {
        private Dictionary<Type, object> _contextStorage = new Dictionary<Type, object>();
        private List<IDisposable> _childDisposables = new List<IDisposable>();
        private List<SubscriptionHandle> _childHandles = new List<SubscriptionHandle>();
        private List<Action> _cleanupActions = new List<Action>();
        private List<long> _hierarchyPath = new List<long>();
        private long _nextChildId = 0;
        private bool _isDisposed = false;

        public ResourceHierarchy Parent { get; private set; }
        public ResourceHierarchy Root => Parent?.Root ?? this;

        internal T ManageResource<T>(T disposable) where T : IDisposable
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);
            
            _childDisposables.Add(disposable);
            if (disposable is ResourceHierarchy child)
            {
                child.Parent = this;
                child.UpdateHierarchyPath(_hierarchyPath, _nextChildId++);
            }
            return disposable;
        }

        internal SubscriptionHandle ManageResource(SubscriptionHandle handle)
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);
            
            _childHandles.Add(handle);
            return handle;
        }

        internal void RegisterCleanup(Action cleanupAction)
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);
            _cleanupActions.Add(cleanupAction);
        }

        public void SetContext<TContext>(TContext value)
        {
            _contextStorage[typeof(TContext)] = value;
        }

        public TContext GetContext<TContext>()
        {
            for (var current = this; current != null; current = current.Parent)
            {
                if (current._contextStorage.TryGetValue(typeof(TContext), out var value))
                {
                    return (TContext)value;
                }
            }
            return default(TContext);
        }

        private void UpdateHierarchyPath(List<long> parentPath, long childId)
        {
            _hierarchyPath.Clear();
            _hierarchyPath.AddRange(parentPath);
            _hierarchyPath.Add(childId);
        }

        public int CompareTo(ResourceHierarchy other)
        {
            if (other == null) return 1;
            
            // Compare hierarchy depth (deeper nodes execute first)
            var depthComparison = other._hierarchyPath.Count.CompareTo(_hierarchyPath.Count);
            if (depthComparison != 0) return depthComparison;
            
            // Compare path lexicographically
            for (int i = 0; i < Math.Min(_hierarchyPath.Count, other._hierarchyPath.Count); i++)
            {
                var pathComparison = _hierarchyPath[i].CompareTo(other._hierarchyPath[i]);
                if (pathComparison != 0) return pathComparison;
            }
            
            return 0;
        }

        public virtual void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            
            // Execute cleanup actions in reverse order
            for (int i = _cleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _cleanupActions[i]();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in cleanup action: {ex}");
                }
            }
            
            // Dispose child handles
            foreach (var handle in _childHandles)
            {
                try
                {
                    handle?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception disposing handle: {ex}");
                }
            }
            
            // Dispose child disposables
            foreach (var disposable in _childDisposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception disposing child: {ex}");
                }
            }
            
            _cleanupActions.Clear();
            _childHandles.Clear();
            _childDisposables.Clear();
            _contextStorage.Clear();
        }
    }
}