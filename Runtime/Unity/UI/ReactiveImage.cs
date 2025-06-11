using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Reactive Image component with animated properties
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ReactiveImage : ReactiveMonoBehaviour
    {
        private Image _image;
        private ReactiveState<Color> _color;
        private ReactiveState<float> _alpha;

        public IReactiveValue<Color> Color => _color;
        public IReactiveValue<float> Alpha => _alpha;

        protected override void InitializeReactive()
        {
            _image = GetComponent<Image>();
            _color = CreateState(_image.color);
            _alpha = CreateState(_image.color.a);

            // Update image color
            CreateEffect(builder =>
            {
                var color = builder.Track(_color);
                var alpha = builder.Track(_alpha);
                _image.color = new Color(color.r, color.g, color.b, alpha);
            });
        }

        public void SetColor(Color color) => _color.Set(color);
        public void SetAlpha(float alpha) => _alpha.Set(alpha);
    }
}