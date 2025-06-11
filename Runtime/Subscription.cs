using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Subscription information for observables
    /// </summary>
    internal struct Subscription
    {
        public long Id { get; set; }
        public Action Callback { get; set; }
        public Action<object> TypedCallback { get; set; }
    }
}