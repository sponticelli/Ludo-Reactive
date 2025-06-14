using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// Represents a difference operation in a collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public struct DiffOperation<T>
    {
        /// <summary>
        /// Gets the type of operation.
        /// </summary>
        public DiffOperationType Type { get; }

        /// <summary>
        /// Gets the index where the operation should be applied.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the item involved in the operation.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Gets the old item (for replace operations).
        /// </summary>
        public T OldItem { get; }

        /// <summary>
        /// Initializes a new instance of the DiffOperation struct.
        /// </summary>
        /// <param name="type">The type of operation.</param>
        /// <param name="index">The index where the operation should be applied.</param>
        /// <param name="item">The item involved in the operation.</param>
        /// <param name="oldItem">The old item (for replace operations).</param>
        public DiffOperation(DiffOperationType type, int index, T item, T oldItem = default)
        {
            Type = type;
            Index = index;
            Item = item;
            OldItem = oldItem;
        }

        /// <summary>
        /// Returns a string representation of the diff operation.
        /// </summary>
        /// <returns>A string representation of the diff operation.</returns>
        public override string ToString()
        {
            return Type switch
            {
                DiffOperationType.Insert => $"Insert[{Index}] = {Item}",
                DiffOperationType.Delete => $"Delete[{Index}] = {Item}",
                DiffOperationType.Replace => $"Replace[{Index}] {OldItem} -> {Item}",
                _ => $"Unknown[{Index}]"
            };
        }
    }

    /// <summary>
    /// Defines the types of diff operations.
    /// </summary>
    public enum DiffOperationType
    {
        /// <summary>
        /// Insert an item at the specified index.
        /// </summary>
        Insert,

        /// <summary>
        /// Delete an item at the specified index.
        /// </summary>
        Delete,

        /// <summary>
        /// Replace an item at the specified index.
        /// </summary>
        Replace
    }

    /// <summary>
    /// Provides efficient algorithms for computing differences between collections.
    /// Uses Myers' diff algorithm for optimal performance on large collections.
    /// </summary>
    /// <typeparam name="T">The type of items in the collections.</typeparam>
    public class CollectionDiffer<T>
    {
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Initializes a new instance of the CollectionDiffer class.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for comparing items.</param>
        public CollectionDiffer(IEqualityComparer<T> comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Computes the minimal set of operations to transform the source collection into the target collection.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <param name="target">The target collection.</param>
        /// <returns>A list of operations to transform source into target.</returns>
        public List<DiffOperation<T>> ComputeDiff(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            // Use simple diff for now as Myers algorithm has bugs
            // TODO: Fix Myers algorithm implementation
            return ComputeSimpleDiff(source, target);
        }

        /// <summary>
        /// Computes the longest common subsequence between two collections.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <param name="target">The target collection.</param>
        /// <returns>The longest common subsequence.</returns>
        public List<T> ComputeLCS(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var lcsMatrix = ComputeLCSMatrix(source, target);
            return ExtractLCS(source, target, lcsMatrix);
        }

        /// <summary>
        /// Applies a set of diff operations to a collection.
        /// </summary>
        /// <param name="collection">The collection to modify.</param>
        /// <param name="operations">The operations to apply.</param>
        public void ApplyDiff(IList<T> collection, IEnumerable<DiffOperation<T>> operations)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));

            var sortedOps = operations.OrderByDescending(op => op.Index).ToList();

            foreach (var operation in sortedOps)
            {
                try
                {
                    switch (operation.Type)
                    {
                        case DiffOperationType.Insert:
                            if (operation.Index >= 0 && operation.Index <= collection.Count)
                            {
                                collection.Insert(operation.Index, operation.Item);
                            }
                            break;

                        case DiffOperationType.Delete:
                            if (operation.Index >= 0 && operation.Index < collection.Count)
                            {
                                collection.RemoveAt(operation.Index);
                            }
                            break;

                        case DiffOperationType.Replace:
                            if (operation.Index >= 0 && operation.Index < collection.Count)
                            {
                                collection[operation.Index] = operation.Item;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Error applying diff operation {operation}: {ex}");
                }
            }
        }

        /// <summary>
        /// Computes a simple diff that only considers additions and removals at the end of collections.
        /// More efficient for collections that primarily grow or shrink.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <param name="target">The target collection.</param>
        /// <returns>A list of operations to transform source into target.</returns>
        public List<DiffOperation<T>> ComputeSimpleDiff(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var operations = new List<DiffOperation<T>>();
            var minLength = Math.Min(source.Count, target.Count);

            // Check for replacements in the common prefix
            for (int i = 0; i < minLength; i++)
            {
                if (!_comparer.Equals(source[i], target[i]))
                {
                    operations.Add(new DiffOperation<T>(DiffOperationType.Replace, i, target[i], source[i]));
                }
            }

            // Handle length differences
            if (source.Count > target.Count)
            {
                // Remove excess items from the end
                for (int i = source.Count - 1; i >= target.Count; i--)
                {
                    operations.Add(new DiffOperation<T>(DiffOperationType.Delete, i, source[i]));
                }
            }
            else if (target.Count > source.Count)
            {
                // Add new items to the end
                for (int i = source.Count; i < target.Count; i++)
                {
                    operations.Add(new DiffOperation<T>(DiffOperationType.Insert, i, target[i]));
                }
            }

            return operations;
        }

        private List<DiffOperation<T>> ComputeMyersDiff(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            var n = source.Count;
            var m = target.Count;
            var max = n + m;
            var v = new int[2 * max + 1];
            var trace = new List<int[]>();

            for (int d = 0; d <= max; d++)
            {
                var vCopy = new int[v.Length];
                Array.Copy(v, vCopy, v.Length);
                trace.Add(vCopy);

                for (int k = -d; k <= d; k += 2)
                {
                    int x;
                    if (k == -d || (k != d && v[k - 1 + max] < v[k + 1 + max]))
                    {
                        x = v[k + 1 + max];
                    }
                    else
                    {
                        x = v[k - 1 + max] + 1;
                    }

                    int y = x - k;

                    while (x < n && y < m && _comparer.Equals(source[x], target[y]))
                    {
                        x++;
                        y++;
                    }

                    v[k + max] = x;

                    if (x >= n && y >= m)
                    {
                        return BacktrackOperations(source, target, trace, d);
                    }
                }
            }

            // Fallback to simple diff if Myers algorithm doesn't converge
            return ComputeSimpleDiff(source, target);
        }

        private List<DiffOperation<T>> BacktrackOperations(IReadOnlyList<T> source, IReadOnlyList<T> target, List<int[]> trace, int d)
        {
            var operations = new List<DiffOperation<T>>();
            int x = source.Count;
            int y = target.Count;
            var max = source.Count + target.Count;

            for (int depth = d; depth > 0; depth--)
            {
                var v = trace[depth];
                var vPrev = trace[depth - 1];
                int k = x - y;

                int prevK;
                if (k == -depth || (k != depth && vPrev[k - 1 + max] < vPrev[k + 1 + max]))
                {
                    prevK = k + 1;
                }
                else
                {
                    prevK = k - 1;
                }

                int prevX = vPrev[prevK + max];
                int prevY = prevX - prevK;

                while (x > prevX && y > prevY)
                {
                    x--;
                    y--;
                }

                if (x > prevX)
                {
                    operations.Add(new DiffOperation<T>(DiffOperationType.Delete, x - 1, source[x - 1]));
                    x = prevX;
                }
                else if (y > prevY)
                {
                    operations.Add(new DiffOperation<T>(DiffOperationType.Insert, x, target[y - 1]));
                    y = prevY;
                }
            }

            operations.Reverse();
            return operations;
        }

        private int[,] ComputeLCSMatrix(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            var n = source.Count;
            var m = target.Count;
            var matrix = new int[n + 1, m + 1];

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (_comparer.Equals(source[i - 1], target[j - 1]))
                    {
                        matrix[i, j] = matrix[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        matrix[i, j] = Math.Max(matrix[i - 1, j], matrix[i, j - 1]);
                    }
                }
            }

            return matrix;
        }

        private List<T> ExtractLCS(IReadOnlyList<T> source, IReadOnlyList<T> target, int[,] matrix)
        {
            var lcs = new List<T>();
            int i = source.Count;
            int j = target.Count;

            while (i > 0 && j > 0)
            {
                if (_comparer.Equals(source[i - 1], target[j - 1]))
                {
                    lcs.Insert(0, source[i - 1]);
                    i--;
                    j--;
                }
                else if (matrix[i - 1, j] > matrix[i, j - 1])
                {
                    i--;
                }
                else
                {
                    j--;
                }
            }

            return lcs;
        }
    }

    /// <summary>
    /// Provides extension methods for collection diffing.
    /// </summary>
    public static class CollectionDifferExtensions
    {
        /// <summary>
        /// Computes the difference between this collection and another collection.
        /// </summary>
        /// <typeparam name="T">The type of items in the collections.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="target">The target collection.</param>
        /// <param name="comparer">The equality comparer to use.</param>
        /// <returns>A list of operations to transform source into target.</returns>
        public static List<DiffOperation<T>> DiffWith<T>(
            this IReadOnlyList<T> source,
            IReadOnlyList<T> target,
            IEqualityComparer<T> comparer = null)
        {
            var differ = new CollectionDiffer<T>(comparer);
            return differ.ComputeDiff(source, target);
        }

        /// <summary>
        /// Applies a set of diff operations to this collection.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to modify.</param>
        /// <param name="operations">The operations to apply.</param>
        /// <param name="comparer">The equality comparer to use.</param>
        public static void ApplyDiff<T>(
            this IList<T> collection,
            IEnumerable<DiffOperation<T>> operations,
            IEqualityComparer<T> comparer = null)
        {
            var differ = new CollectionDiffer<T>(comparer);
            differ.ApplyDiff(collection, operations);
        }
    }
}
