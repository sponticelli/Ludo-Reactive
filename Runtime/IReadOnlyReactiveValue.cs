namespace Ludo.Reactive
{
    /// <summary>
    /// Read-only reactive value interface
    /// </summary>
    public interface IReadOnlyReactiveValue<T> : IObservable<T>
    {
        T Current { get; }
    }
}