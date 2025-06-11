using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Handle for managing subscriptions
    /// </summary>
    public class SubscriptionHandle : IDisposable
    {
        private Action _unsubscribeAction;
        private bool _isDisposed;

        public SubscriptionHandle(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _unsubscribeAction?.Invoke();
                _unsubscribeAction = null;
                _isDisposed = true;
            }
        }
    }
}