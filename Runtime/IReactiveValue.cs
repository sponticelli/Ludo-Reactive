using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Mutable reactive value interface
    /// </summary>
    public interface IReactiveValue<T> : IReadOnlyReactiveValue<T>
    {
        void Set(T value);
        void Update(Func<T, T> updater);
    }
}