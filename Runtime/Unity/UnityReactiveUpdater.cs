using UnityEngine;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// MonoBehaviour that drives the Unity reactive scheduler
    /// </summary>
    internal class UnityReactiveUpdater : MonoBehaviour
    {
        private void Update()
        {
            UnityReactiveScheduler.Instance.ProcessMainThreadActions();
        }
    }
}