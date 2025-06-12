using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace Ludo.Reactive.Editor
{
    /// <summary>
    /// Custom property drawer for ReactiveProperty<T> that displays the current value and subscription information.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReactiveProperty<>))]
    public class ReactivePropertyDrawer : PropertyDrawer
    {
        private const float BUTTON_WIDTH = 60f;
        private const float SPACING = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                var valueProperty = property.FindPropertyRelative("_value");
                if (valueProperty == null)
                {
                    EditorGUI.LabelField(position, label.text, "ReactiveProperty (no value field found)");
                    return;
                }

                // Get the actual ReactiveProperty instance
                var reactiveProperty = GetReactivePropertyInstance(property);
                
                // Calculate rects
                var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
                var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                    position.width - EditorGUIUtility.labelWidth - BUTTON_WIDTH - SPACING, position.height);
                var buttonRect = new Rect(position.x + position.width - BUTTON_WIDTH, position.y, 
                    BUTTON_WIDTH, position.height);

                // Draw label with subscription info
                var labelText = label.text;
                if (reactiveProperty != null)
                {
                    var observerCount = GetObserverCount(reactiveProperty);
                    if (observerCount > 0)
                    {
                        labelText += $" ({observerCount})";
                    }
                }
                
                EditorGUI.LabelField(labelRect, labelText);

                // Draw value field
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
                
                if (EditorGUI.EndChangeCheck() && reactiveProperty != null)
                {
                    // Force notify when value changes in inspector
                    property.serializedObject.ApplyModifiedProperties();
                    ForceNotify(reactiveProperty);
                }

                // Draw debug button
                if (Application.isPlaying && reactiveProperty != null)
                {
                    if (GUI.Button(buttonRect, "Notify"))
                    {
                        ForceNotify(reactiveProperty);
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUI.Button(buttonRect, "Notify");
                    GUI.enabled = true;
                }
            }
            catch (Exception ex)
            {
                EditorGUI.LabelField(position, label.text, $"Error: {ex.Message}");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("_value");
            if (valueProperty != null)
            {
                return EditorGUI.GetPropertyHeight(valueProperty);
            }
            return EditorGUIUtility.singleLineHeight;
        }

        private object GetReactivePropertyInstance(SerializedProperty property)
        {
            try
            {
                var target = property.serializedObject.targetObject;
                var path = property.propertyPath;
                
                // Handle array elements
                if (path.Contains("[") && path.Contains("]"))
                {
                    var arrayPath = path.Substring(0, path.IndexOf('['));
                    var indexStart = path.IndexOf('[') + 1;
                    var indexEnd = path.IndexOf(']');
                    var indexStr = path.Substring(indexStart, indexEnd - indexStart);
                    
                    if (int.TryParse(indexStr, out int index))
                    {
                        var arrayField = target.GetType().GetField(arrayPath, 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (arrayField != null)
                        {
                            var array = arrayField.GetValue(target) as Array;
                            if (array != null && index < array.Length)
                            {
                                return array.GetValue(index);
                            }
                        }
                    }
                }
                else
                {
                    // Simple field access
                    var field = target.GetType().GetField(path, 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        return field.GetValue(target);
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            
            return null;
        }

        private int GetObserverCount(object reactiveProperty)
        {
            try
            {
                var observerCountProperty = reactiveProperty.GetType().GetProperty("ObserverCount");
                if (observerCountProperty != null)
                {
                    return (int)observerCountProperty.GetValue(reactiveProperty);
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            
            return 0;
        }

        private void ForceNotify(object reactiveProperty)
        {
            try
            {
                var forceNotifyMethod = reactiveProperty.GetType().GetMethod("ForceNotify");
                if (forceNotifyMethod != null)
                {
                    forceNotifyMethod.Invoke(reactiveProperty, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to force notify ReactiveProperty: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Custom property drawer for IReadOnlyReactiveProperty<T> that displays the current value as read-only.
    /// </summary>
    [CustomPropertyDrawer(typeof(IReadOnlyReactiveProperty<>))]
    public class ReadOnlyReactivePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                var valueProperty = property.FindPropertyRelative("_value");
                if (valueProperty == null)
                {
                    EditorGUI.LabelField(position, label.text, "ReadOnlyReactiveProperty (no value field found)");
                    return;
                }

                // Draw as read-only
                GUI.enabled = false;
                EditorGUI.PropertyField(position, valueProperty, label);
                GUI.enabled = true;
            }
            catch (Exception ex)
            {
                EditorGUI.LabelField(position, label.text, $"Error: {ex.Message}");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("_value");
            if (valueProperty != null)
            {
                return EditorGUI.GetPropertyHeight(valueProperty);
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
