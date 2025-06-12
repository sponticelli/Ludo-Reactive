using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Advanced example demonstrating the newly implemented reactive features.
    /// </summary>
    public class AdvancedReactiveExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button loadButton;
        [SerializeField] private Button addItemButton;
        [SerializeField] private Button clearItemsButton;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;
        [SerializeField] private Text itemCountText;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemPrefab;

        [Header("Reactive Properties")]
        [SerializeField] private ReactiveProperty<string> status = new ReactiveProperty<string>("Ready");
        [SerializeField] private ReactiveProperty<float> progress = new ReactiveProperty<float>(0f);
        [SerializeField] private ReactiveProperty<bool> isLoading = new ReactiveProperty<bool>(false);

        private ReactiveCollection<string> items = new ReactiveCollection<string>();
        private Subject<Unit> loadRequests = new Subject<Unit>();
        private CompositeDisposable disposables = new CompositeDisposable();

        private void Start()
        {
            SetupUIBindings();
            SetupAsyncOperations();
            SetupCollectionObservation();
            SetupErrorHandling();
            SetupAdvancedOperators();
        }

        private void SetupUIBindings()
        {
            // Bind reactive properties to UI
            status.Subscribe(text => statusText.text = text).AddTo(this);
            progress.Subscribe(value => progressSlider.value = value).AddTo(this);
            
            // Bind button interactions
            loadButton.OnClickAsObservable()
                .Where(_ => !isLoading.Value)
                .Subscribe(_ => loadRequests.OnNext(Unit.Default))
                .AddTo(this);

            addItemButton.OnClickAsObservable()
                .Subscribe(_ => AddRandomItem())
                .AddTo(this);

            clearItemsButton.OnClickAsObservable()
                .Subscribe(_ => items.Clear())
                .AddTo(this);

            // Disable load button when loading
            isLoading.Subscribe(loading => loadButton.interactable = !loading).AddTo(this);
        }

        private void SetupAsyncOperations()
        {
            // Handle load requests with async operations
            loadRequests
                .Where(_ => !isLoading.Value)
                .SelectMany(_ => LoadDataAsync().ToObservable())
                .Catch<string, Exception>(ex =>
                {
                    Debug.LogError($"Load failed: {ex.Message}");
                    return Observable.Return("Load failed");
                })
                .LogError("LoadData")
                .Subscribe(result => status.Value = result)
                .AddTo(this);

            // Simulate progress updates
            loadRequests
                .SelectMany(_ => SimulateProgressAsync())
                .Subscribe(progressValue => progress.Value = progressValue)
                .AddTo(this);
        }

        private void SetupCollectionObservation()
        {
            // Observe collection changes
            items.ObserveAdd()
                .Subscribe(addEvent => 
                {
                    Debug.Log($"Item added at index {addEvent.Index}: {addEvent.Value}");
                    CreateItemUI(addEvent.Value);
                })
                .AddTo(this);

            items.ObserveRemove()
                .Subscribe(removeEvent => 
                {
                    Debug.Log($"Item removed from index {removeEvent.Index}: {removeEvent.Value}");
                    UpdateItemCount();
                })
                .AddTo(this);

            items.ObserveReset()
                .Subscribe(resetEvent => 
                {
                    Debug.Log($"Collection reset, removed {resetEvent.OldItems.Count} items");
                    ClearItemUI();
                })
                .AddTo(this);

            items.ObserveCountChanged()
                .Subscribe(count => itemCountText.text = $"Items: {count}")
                .AddTo(this);
        }

        private void SetupErrorHandling()
        {
            // Example of error handling with recovery
            Observable.Interval(TimeSpan.FromSeconds(5))
                .SelectMany(_ => SimulateRandomError())
                .Catch<string, InvalidOperationException>(ex => 
                {
                    Debug.LogWarning($"Recovered from error: {ex.Message}");
                    return Observable.Return("Recovered");
                })
                .LogError("RandomError")
                .Subscribe(result => Debug.Log($"Result: {result}"))
                .AddTo(this);
        }

        private void SetupAdvancedOperators()
        {
            // Demonstrate Buffer operator
            addItemButton.OnClickAsObservable()
                .Buffer(TimeSpan.FromSeconds(2))
                .Where(clicks => clicks.Count > 1)
                .Subscribe(clicks => 
                {
                    Debug.Log($"Rapid clicking detected: {clicks.Count} clicks in 2 seconds");
                    status.Value = $"Rapid clicking: {clicks.Count} clicks";
                })
                .AddTo(this);

            // Demonstrate Scan operator
            items.ObserveAdd()
                .Select(_ => 1)
                .Scan(0, (acc, _) => acc + 1)
                .Subscribe(totalAdded => Debug.Log($"Total items added: {totalAdded}"))
                .AddTo(this);

            // Demonstrate CombineLatest
            status.CombineLatest(progress, (s, p) => $"{s} ({p:P0})")
                .DistinctUntilChanged()
                .Subscribe(combined => Debug.Log($"Status update: {combined}"))
                .AddTo(this);

            // Demonstrate TakeUntil
            Observable.Interval(TimeSpan.FromSeconds(1))
                .TakeUntil(this.OnDestroyAsObservable())
                .Subscribe(tick => Debug.Log($"Tick: {tick}"))
                .AddTo(this);
        }

        private async Task<string> LoadDataAsync()
        {
            isLoading.Value = true;
            status.Value = "Loading...";
            
            try
            {
                // Simulate async operation
                await Task.Delay(2000);
                return "Data loaded successfully";
            }
            finally
            {
                isLoading.Value = false;
            }
        }

        private IObservable<float> SimulateProgressAsync()
        {
            return Observable.Create<float>(observer =>
            {
                var routine = StartCoroutine(ProgressCoroutine(observer));
                return Disposable.Create(() => 
                {
                    if (routine != null) StopCoroutine(routine);
                });
            });
        }

        private IEnumerator ProgressCoroutine(IObserver<float> observer)
        {
            for (float p = 0f; p <= 1f; p += 0.1f)
            {
                observer.OnNext(p);
                yield return new WaitForSeconds(0.2f);
            }
            observer.OnCompleted();
        }

        private IObservable<string> SimulateRandomError()
        {
            return Observable.Create<string>(observer =>
            {
                if (UnityEngine.Random.value < 0.3f)
                {
                    observer.OnError(new InvalidOperationException("Random error occurred"));
                }
                else
                {
                    observer.OnNext("Success");
                    observer.OnCompleted();
                }
                return Disposable.Empty;
            });
        }

        private void AddRandomItem()
        {
            var randomItem = $"Item {UnityEngine.Random.Range(1000, 9999)}";
            items.Add(randomItem);
        }

        private void CreateItemUI(string itemText)
        {
            if (itemPrefab != null && itemContainer != null)
            {
                var itemGO = Instantiate(itemPrefab, itemContainer);
                var textComponent = itemGO.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    textComponent.text = itemText;
                }
            }
            UpdateItemCount();
        }

        private void ClearItemUI()
        {
            if (itemContainer != null)
            {
                for (int i = itemContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(itemContainer.GetChild(i).gameObject);
                }
            }
            UpdateItemCount();
        }

        private void UpdateItemCount()
        {
            if (itemCountText != null)
            {
                itemCountText.text = $"Items: {items.Count}";
            }
        }

        private void OnDestroy()
        {
            disposables?.Dispose();
            items?.Dispose();
            loadRequests?.Dispose();
        }
    }
}
