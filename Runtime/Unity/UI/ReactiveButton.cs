using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Reactive Button that manages click events reactively
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ReactiveButton : ReactiveMonoBehaviour
    {
        private Button _button;
        private ReactiveState<bool> _isInteractable;
        private ReactiveState<int> _clickCount;

        public IReadOnlyReactiveValue<bool> IsInteractable => _isInteractable;
        public IReadOnlyReactiveValue<int> ClickCount => _clickCount;

        protected override void InitializeReactive()
        {
            _button = GetComponent<Button>();
            _isInteractable = CreateState(_button.interactable);
            _clickCount = CreateState(0);

            // Update button interactable state
            CreateEffect(builder => { _button.interactable = builder.Track(_isInteractable); });

            // Handle button clicks
            _button.onClick.AddListener(() => _clickCount.Update(count => count + 1));
        }

        public void SetInteractable(bool interactable) => _isInteractable.Set(interactable);

        protected override void OnDestroy()
        {
            _button?.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}