using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ludo.Reactive
{
    /// <summary>
    /// Performance monitoring for reactive computations
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Dictionary<string, ComputationStats> _computationStats = new Dictionary<string, ComputationStats>();
        private readonly Dictionary<string, SubscriptionStats> _subscriptionStats = new Dictionary<string, SubscriptionStats>();
        private readonly object _lock = new object();
        
        public static PerformanceMonitor Instance { get; } = new PerformanceMonitor();

        /// <summary>
        /// Records computation execution metrics
        /// </summary>
        public void RecordComputationExecution(string computationName, TimeSpan executionTime, bool success, int dependencyCount = 0)
        {
            lock (_lock)
            {
                if (!_computationStats.TryGetValue(computationName, out var stats))
                {
                    stats = new ComputationStats { Name = computationName };
                    _computationStats[computationName] = stats;
                }

                stats.ExecutionCount++;
                stats.TotalExecutionTime += executionTime;
                stats.LastExecutionTime = executionTime;
                stats.LastExecutedAt = DateTime.UtcNow;
                
                if (success)
                {
                    stats.SuccessCount++;
                }
                else
                {
                    stats.ErrorCount++;
                }

                if (executionTime > stats.MaxExecutionTime)
                {
                    stats.MaxExecutionTime = executionTime;
                }

                if (stats.MinExecutionTime == TimeSpan.Zero || executionTime < stats.MinExecutionTime)
                {
                    stats.MinExecutionTime = executionTime;
                }

                stats.AverageExecutionTime = TimeSpan.FromTicks(stats.TotalExecutionTime.Ticks / stats.ExecutionCount);
                stats.DependencyCount = dependencyCount;
            }
        }

        /// <summary>
        /// Records subscription metrics
        /// </summary>
        public void RecordSubscriptionMetrics(string sourceName, int subscriptionCount, int weakSubscriptionCount = 0)
        {
            lock (_lock)
            {
                if (!_subscriptionStats.TryGetValue(sourceName, out var stats))
                {
                    stats = new SubscriptionStats { SourceName = sourceName };
                    _subscriptionStats[sourceName] = stats;
                }

                stats.SubscriptionCount = subscriptionCount;
                stats.WeakSubscriptionCount = weakSubscriptionCount;
                stats.LastUpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets performance statistics for a computation
        /// </summary>
        public ComputationStats GetComputationStats(string computationName)
        {
            lock (_lock)
            {
                return _computationStats.TryGetValue(computationName, out var stats) ? stats : null;
            }
        }

        /// <summary>
        /// Gets all computation statistics
        /// </summary>
        public Dictionary<string, ComputationStats> GetAllComputationStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, ComputationStats>(_computationStats);
            }
        }

        /// <summary>
        /// Gets all subscription statistics
        /// </summary>
        public Dictionary<string, SubscriptionStats> GetAllSubscriptionStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, SubscriptionStats>(_subscriptionStats);
            }
        }

        /// <summary>
        /// Gets computations that are performing poorly
        /// </summary>
        public List<ComputationStats> GetSlowComputations(TimeSpan threshold)
        {
            var slowComputations = new List<ComputationStats>();
            
            lock (_lock)
            {
                foreach (var stats in _computationStats.Values)
                {
                    if (stats.AverageExecutionTime > threshold || stats.MaxExecutionTime > threshold * 2)
                    {
                        slowComputations.Add(stats);
                    }
                }
            }

            slowComputations.Sort((a, b) => b.AverageExecutionTime.CompareTo(a.AverageExecutionTime));
            return slowComputations;
        }

        /// <summary>
        /// Gets memory usage statistics
        /// </summary>
        public MemoryStats GetMemoryStats()
        {
            var totalComputations = 0;
            var totalSubscriptions = 0;
            var totalWeakSubscriptions = 0;

            lock (_lock)
            {
                totalComputations = _computationStats.Count;
                
                foreach (var stats in _subscriptionStats.Values)
                {
                    totalSubscriptions += stats.SubscriptionCount;
                    totalWeakSubscriptions += stats.WeakSubscriptionCount;
                }
            }

            return new MemoryStats
            {
                TotalComputations = totalComputations,
                TotalSubscriptions = totalSubscriptions,
                TotalWeakSubscriptions = totalWeakSubscriptions,
                EstimatedMemoryUsage = EstimateMemoryUsage(totalComputations, totalSubscriptions, totalWeakSubscriptions)
            };
        }

        /// <summary>
        /// Clears all performance statistics
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _computationStats.Clear();
                _subscriptionStats.Clear();
            }
        }

        private long EstimateMemoryUsage(int computations, int subscriptions, int weakSubscriptions)
        {
            // Rough estimation in bytes
            const int computationOverhead = 200; // Approximate overhead per computation
            const int subscriptionOverhead = 50;  // Approximate overhead per subscription
            const int weakSubscriptionOverhead = 30; // Weak references are smaller
            
            return (computations * computationOverhead) + 
                   (subscriptions * subscriptionOverhead) + 
                   (weakSubscriptions * weakSubscriptionOverhead);
        }
    }

    /// <summary>
    /// Statistics for a computation
    /// </summary>
    public class ComputationStats
    {
        public string Name { get; set; }
        public int ExecutionCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MinExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public TimeSpan LastExecutionTime { get; set; }
        public DateTime LastExecutedAt { get; set; }
        public int DependencyCount { get; set; }
        
        public double SuccessRate => ExecutionCount > 0 ? (double)SuccessCount / ExecutionCount : 0.0;
        public double ErrorRate => ExecutionCount > 0 ? (double)ErrorCount / ExecutionCount : 0.0;
    }

    /// <summary>
    /// Statistics for subscriptions
    /// </summary>
    public class SubscriptionStats
    {
        public string SourceName { get; set; }
        public int SubscriptionCount { get; set; }
        public int WeakSubscriptionCount { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        
        public int TotalSubscriptions => SubscriptionCount + WeakSubscriptionCount;
    }

    /// <summary>
    /// Memory usage statistics
    /// </summary>
    public class MemoryStats
    {
        public int TotalComputations { get; set; }
        public int TotalSubscriptions { get; set; }
        public int TotalWeakSubscriptions { get; set; }
        public long EstimatedMemoryUsage { get; set; }
    }
}
