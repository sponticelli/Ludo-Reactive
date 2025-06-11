using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Reactive Text component that automatically updates
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ReactiveText : ReactiveMonoBehaviour
    {
        private Text _textComponent;
        private ReactiveState<string> _text;

        public IReactiveValue<string> Text => _text;

        protected override void InitializeReactive()
        {
            _textComponent = GetComponent<Text>();
            _text = CreateState(_textComponent.text);

            // Update Unity Text when reactive state changes
            CreateEffect(builder => { _textComponent.text = builder.Track(_text); });
        }

        public void SetText(string text) => _text.Set(text);
    }
}