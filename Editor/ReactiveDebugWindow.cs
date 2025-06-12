using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Ludo.Reactive.Editor
{
    /// <summary>
    /// Debug window for visualizing active reactive subscriptions and performance metrics.
    /// </summary>
    public class ReactiveDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showPerformanceMetrics = true;
        private bool _showActiveSubscriptions = true;
        private bool _showSubjects = true;
        private bool _showReactiveProperties = true;
        private float _refreshRate = 1.0f;
        private double _lastRefreshTime;

        private readonly List<DebugInfo> _debugInfos = new List<DebugInfo>();

        [MenuItem("Window/Ludo Reactive/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ReactiveDebugWindow>("Reactive Debug");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshRate)
            {
                RefreshDebugInfo();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_showPerformanceMetrics)
            {
                DrawPerformanceMetrics();
            }
            
            if (_showActiveSubscriptions)
            {
                DrawActiveSubscriptions();
            }
            
            if (_showSubjects)
            {
                DrawSubjects();
            }
            
            if (_showReactiveProperties)
            {
                DrawReactiveProperties();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshDebugInfo();
            }
            
            GUILayout.Space(10);
            
            _refreshRate = EditorGUILayout.Slider("Refresh Rate", _refreshRate, 0.1f, 5.0f, GUILayout.Width(200));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _debugInfos.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Toggle sections
            EditorGUILayout.BeginHorizontal();
            _showPerformanceMetrics = EditorGUILayout.ToggleLeft("Performance", _showPerformanceMetrics, GUILayout.Width(100));
            _showActiveSubscriptions = EditorGUILayout.ToggleLeft("Subscriptions", _showActiveSubscriptions, GUILayout.Width(100));
            _showSubjects = EditorGUILayout.ToggleLeft("Subjects", _showSubjects, GUILayout.Width(100));
            _showReactiveProperties = EditorGUILayout.ToggleLeft("Properties", _showReactiveProperties, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawPerformanceMetrics()
        {
            EditorGUILayout.LabelField("Performance Metrics", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField($"Total Debug Entries: {_debugInfos.Count}");
                EditorGUILayout.LabelField($"Memory Usage: {GC.GetTotalMemory(false) / 1024 / 1024:F2} MB");
                EditorGUILayout.LabelField($"Last Refresh: {DateTime.Now:HH:mm:ss}");
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField($"Frame Rate: {1.0f / Time.unscaledDeltaTime:F1} FPS");
                    EditorGUILayout.LabelField($"Time Scale: {Time.timeScale:F2}");
                }
                else
                {
                    EditorGUILayout.LabelField("Application not playing");
                }
            }
            
            EditorGUILayout.Space();
        }

        private void DrawActiveSubscriptions()
        {
            EditorGUILayout.LabelField("Active Subscriptions", EditorStyles.boldLabel);
            
            var subscriptions = _debugInfos.Where(info => info.Type == DebugInfoType.Subscription).ToList();
            
            if (subscriptions.Count == 0)
            {
                EditorGUILayout.LabelField("No active subscriptions found");
            }
            else
            {
                foreach (var subscription in subscriptions)
                {
                    DrawDebugInfo(subscription);
                }
            }
            
            EditorGUILayout.Space();
        }

        private void DrawSubjects()
        {
            EditorGUILayout.LabelField("Subjects", EditorStyles.boldLabel);
            
            var subjects = _debugInfos.Where(info => info.Type == DebugInfoType.Subject).ToList();
            
            if (subjects.Count == 0)
            {
                EditorGUILayout.LabelField("No subjects found");
            }
            else
            {
                foreach (var subject in subjects)
                {
                    DrawDebugInfo(subject);
                }
            }
            
            EditorGUILayout.Space();
        }

        private void DrawReactiveProperties()
        {
            EditorGUILayout.LabelField("Reactive Properties", EditorStyles.boldLabel);
            
            var properties = _debugInfos.Where(info => info.Type == DebugInfoType.ReactiveProperty).ToList();
            
            if (properties.Count == 0)
            {
                EditorGUILayout.LabelField("No reactive properties found");
            }
            else
            {
                foreach (var property in properties)
                {
                    DrawDebugInfo(property);
                }
            }
            
            EditorGUILayout.Space();
        }

        private void DrawDebugInfo(DebugInfo info)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(info.Name, GUILayout.Width(200));
                EditorGUILayout.LabelField(info.Details, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(info.ObserverCount.ToString(), GUILayout.Width(50));
                
                if (info.GameObject != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = info.GameObject;
                        EditorGUIUtility.PingObject(info.GameObject);
                    }
                }
            }
        }

        private void RefreshDebugInfo()
        {
            _debugInfos.Clear();
            
            if (!Application.isPlaying)
                return;
            
            // Find all GameObjects with reactive components
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<MonoBehaviour>();
                
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    try
                    {
                        AnalyzeComponent(component, go);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error analyzing component {component.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        private void AnalyzeComponent(MonoBehaviour component, GameObject gameObject)
        {
            var type = component.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | 
                                       System.Reflection.BindingFlags.NonPublic | 
                                       System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                
                // Check for ReactiveProperty<T>
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>))
                {
                    var value = field.GetValue(component);
                    if (value != null)
                    {
                        var observerCount = GetObserverCount(value);
                        var currentValue = GetCurrentValue(value);
                        
                        _debugInfos.Add(new DebugInfo
                        {
                            Type = DebugInfoType.ReactiveProperty,
                            Name = $"{component.GetType().Name}.{field.Name}",
                            Details = $"Value: {currentValue}",
                            ObserverCount = observerCount,
                            GameObject = gameObject
                        });
                    }
                }
                
                // Check for Subject<T>
                if (fieldType.IsGenericType && 
                    (fieldType.GetGenericTypeDefinition() == typeof(Subject<>) ||
                     fieldType.GetGenericTypeDefinition() == typeof(BehaviorSubject<>) ||
                     fieldType.GetGenericTypeDefinition() == typeof(ReplaySubject<>)))
                {
                    var value = field.GetValue(component);
                    if (value != null)
                    {
                        var observerCount = GetObserverCount(value);
                        var subjectType = fieldType.GetGenericTypeDefinition().Name;
                        
                        _debugInfos.Add(new DebugInfo
                        {
                            Type = DebugInfoType.Subject,
                            Name = $"{component.GetType().Name}.{field.Name}",
                            Details = $"Type: {subjectType}",
                            ObserverCount = observerCount,
                            GameObject = gameObject
                        });
                    }
                }
            }
        }

        private int GetObserverCount(object obj)
        {
            try
            {
                var property = obj.GetType().GetProperty("ObserverCount");
                if (property != null)
                {
                    return (int)property.GetValue(obj);
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            
            return 0;
        }

        private string GetCurrentValue(object obj)
        {
            try
            {
                var property = obj.GetType().GetProperty("Value");
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    return value?.ToString() ?? "null";
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            
            return "Unknown";
        }

        private struct DebugInfo
        {
            public DebugInfoType Type;
            public string Name;
            public string Details;
            public int ObserverCount;
            public GameObject GameObject;
        }

        private enum DebugInfoType
        {
            Subscription,
            Subject,
            ReactiveProperty
        }
    }
}