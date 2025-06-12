using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Memoization utility for expensive computations with same inputs
    /// </summary>
    public class ComputationMemoizer<TInput, TOutput>
    {
        private readonly Dictionary<TInput, CacheEntry> _cache;
        private readonly IEqualityComparer<TInput> _inputComparer;
        private readonly IEqualityComparer<TOutput> _outputComparer;
        private readonly int _maxCacheSize;
        private readonly TimeSpan _maxAge;

        public ComputationMemoizer(
            int maxCacheSize = 100,
            TimeSpan? maxAge = null,
            IEqualityComparer<TInput> inputComparer = null,
            IEqualityComparer<TOutput> outputComparer = null)
        {
            _cache = new Dictionary<TInput, CacheEntry>(inputComparer ?? EqualityComparer<TInput>.Default);
            _inputComparer = inputComparer ?? EqualityComparer<TInput>.Default;
            _outputComparer = outputComparer ?? EqualityComparer<TOutput>.Default;
            _maxCacheSize = maxCacheSize;
            _maxAge = maxAge ?? TimeSpan.FromMinutes(10);
        }

        public TOutput GetOrCompute(TInput input, Func<TInput, TOutput> computation)
        {
            if (computation == null) throw new ArgumentNullException(nameof(computation));

            // Clean expired entries periodically
            if (_cache.Count > _maxCacheSize * 0.8)
            {
                CleanExpiredEntries();
            }

            if (_cache.TryGetValue(input, out var entry))
            {
                // Check if entry is still valid
                if (DateTime.UtcNow - entry.CreatedAt <= _maxAge)
                {
                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;
                    return entry.Output;
                }
                else
                {
                    // Entry expired, remove it
                    _cache.Remove(input);
                }
            }

            // Compute new value
            var output = computation(input);
            
            // Add to cache if we have space
            if (_cache.Count < _maxCacheSize)
            {
                _cache[input] = new CacheEntry
                {
                    Output = output,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    AccessCount = 1
                };
            }
            else
            {
                // Cache is full, remove least recently used entry
                RemoveLeastRecentlyUsed();
                _cache[input] = new CacheEntry
                {
                    Output = output,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    AccessCount = 1
                };
            }

            return output;
        }

        public void InvalidateAll()
        {
            _cache.Clear();
        }

        public void Invalidate(TInput input)
        {
            _cache.Remove(input);
        }

        public bool TryGetCached(TInput input, out TOutput output)
        {
            if (_cache.TryGetValue(input, out var entry) && 
                DateTime.UtcNow - entry.CreatedAt <= _maxAge)
            {
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;
                output = entry.Output;
                return true;
            }

            output = default;
            return false;
        }

        public int CacheSize => _cache.Count;
        public int MaxCacheSize => _maxCacheSize;

        private void CleanExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var toRemove = new List<TInput>();

            foreach (var kvp in _cache)
            {
                if (now - kvp.Value.CreatedAt > _maxAge)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _cache.Remove(key);
            }
        }

        private void RemoveLeastRecentlyUsed()
        {
            if (_cache.Count == 0) return;

            TInput lruKey = default;
            DateTime oldestAccess = DateTime.MaxValue;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessedAt < oldestAccess)
                {
                    oldestAccess = kvp.Value.LastAccessedAt;
                    lruKey = kvp.Key;
                }
            }

            if (lruKey != null)
            {
                _cache.Remove(lruKey);
            }
        }

        private class CacheEntry
        {
            public TOutput Output { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public int AccessCount { get; set; }
        }
    }

    /// <summary>
    /// Static helper for creating memoized functions
    /// </summary>
    public static class Memoizer
    {
        public static Func<TInput, TOutput> Create<TInput, TOutput>(
            Func<TInput, TOutput> function,
            int maxCacheSize = 100,
            TimeSpan? maxAge = null,
            IEqualityComparer<TInput> inputComparer = null)
        {
            var memoizer = new ComputationMemoizer<TInput, TOutput>(
                maxCacheSize, maxAge, inputComparer);
            
            return input => memoizer.GetOrCompute(input, function);
        }
    }
}
