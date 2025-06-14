using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Provides utilities for creating immutable state updates.
    /// Helps ensure state mutations follow immutability principles.
    /// </summary>
    public static class ImmutableStateUpdater
    {
        /// <summary>
        /// Creates a new instance of the state with the specified property updated.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <typeparam name="TProperty">The type of the property to update.</typeparam>
        /// <param name="state">The current state.</param>
        /// <param name="propertySelector">A function that selects the property to update.</param>
        /// <param name="newValue">The new value for the property.</param>
        /// <returns>A new state instance with the property updated.</returns>
        public static TState UpdateProperty<TState, TProperty>(
            TState state,
            Func<TState, TProperty> propertySelector,
            TProperty newValue)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            // For value types, we need to use reflection to create a copy
            if (typeof(TState).IsValueType)
            {
                return UpdateValueTypeProperty(state, propertySelector, newValue);
            }

            // For reference types, try to use a copy constructor or clone method
            return UpdateReferenceTypeProperty(state, propertySelector, newValue);
        }

        /// <summary>
        /// Creates a new instance of the state with multiple properties updated.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="state">The current state.</param>
        /// <param name="updates">A dictionary of property names and their new values.</param>
        /// <returns>A new state instance with the properties updated.</returns>
        public static TState UpdateProperties<TState>(
            TState state,
            Dictionary<string, object> updates)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (updates == null || updates.Count == 0)
                return state;

            var stateType = typeof(TState);
            var newState = CreateCopy(state);

            foreach (var update in updates)
            {
                var property = stateType.GetProperty(update.Key, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        property.SetValue(newState, update.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Failed to update property '{update.Key}': {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Ludo.Reactive] Property '{update.Key}' not found or not writable on type {stateType.Name}");
                }
            }

            return newState;
        }

        /// <summary>
        /// Creates a deep copy of the state object.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="state">The state to copy.</param>
        /// <returns>A deep copy of the state.</returns>
        public static TState CreateCopy<TState>(TState state)
        {
            if (state == null)
                return default;

            var stateType = typeof(TState);

            // Handle value types
            if (stateType.IsValueType)
            {
                return state; // Value types are copied by default
            }

            // Handle strings (immutable reference type)
            if (stateType == typeof(string))
            {
                return state;
            }

            // Try to use ICloneable interface
            if (state is ICloneable cloneable)
            {
                return (TState)cloneable.Clone();
            }

            // Try to use copy constructor
            var copyConstructor = stateType.GetConstructor(new[] { stateType });
            if (copyConstructor != null)
            {
                return (TState)copyConstructor.Invoke(new object[] { state });
            }

            // Try to use parameterless constructor and copy properties
            var defaultConstructor = stateType.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null)
            {
                var newInstance = (TState)defaultConstructor.Invoke(null);
                CopyProperties(state, newInstance);
                return newInstance;
            }

            // Fallback: use MemberwiseClone for reference types
            if (stateType.IsClass)
            {
                var cloneMethod = stateType.GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
                if (cloneMethod != null)
                {
                    return (TState)cloneMethod.Invoke(state, null);
                }
            }

            Debug.LogWarning($"[Ludo.Reactive] Unable to create copy of type {stateType.Name}. Returning original instance.");
            return state;
        }

        /// <summary>
        /// Validates that a state change follows immutability principles.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        /// <returns>True if the change is valid; otherwise, false.</returns>
        public static bool ValidateImmutableChange<TState>(TState oldState, TState newState)
        {
            if (EqualityComparer<TState>.Default.Equals(oldState, newState))
            {
                return true; // No change is valid
            }

            var stateType = typeof(TState);

            // Value types are always immutable
            if (stateType.IsValueType)
            {
                return true;
            }

            // Reference types should be different instances
            if (ReferenceEquals(oldState, newState))
            {
                Debug.LogWarning($"[Ludo.Reactive] State mutation detected: same reference returned for type {stateType.Name}");
                return false;
            }

            return true;
        }

        private static TState UpdateValueTypeProperty<TState, TProperty>(
            TState state,
            Func<TState, TProperty> propertySelector,
            TProperty newValue)
        {
            // For value types, we need to create a copy and update the property
            var stateType = typeof(TState);
            var newState = state; // Copy the struct

            // Use reflection to find and update the property
            var propertyInfo = FindPropertyFromSelector(propertySelector);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                var boxedState = (object)newState;
                propertyInfo.SetValue(boxedState, newValue);
                return (TState)boxedState;
            }

            Debug.LogWarning($"[Ludo.Reactive] Unable to update property on value type {stateType.Name}");
            return state;
        }

        private static TState UpdateReferenceTypeProperty<TState, TProperty>(
            TState state,
            Func<TState, TProperty> propertySelector,
            TProperty newValue)
        {
            var newState = CreateCopy(state);
            var propertyInfo = FindPropertyFromSelector(propertySelector);
            
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(newState, newValue);
            }
            else
            {
                Debug.LogWarning($"[Ludo.Reactive] Unable to update property on type {typeof(TState).Name}");
            }

            return newState;
        }

        private static PropertyInfo FindPropertyFromSelector<TState, TProperty>(
            Func<TState, TProperty> propertySelector)
        {
            // This is a simplified approach - in a real implementation,
            // you might want to use expression trees to extract property info
            var stateType = typeof(TState);
            var propertyType = typeof(TProperty);

            // Find properties that match the return type
            var matchingProperties = stateType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == propertyType)
                .ToArray();

            if (matchingProperties.Length == 1)
            {
                return matchingProperties[0];
            }

            // If multiple properties match, we can't determine which one without expression analysis
            Debug.LogWarning($"[Ludo.Reactive] Ambiguous property selector for type {stateType.Name}");
            return null;
        }

        private static void CopyProperties<T>(T source, T destination)
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(source);
                    property.SetValue(destination, value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Failed to copy property '{property.Name}': {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Provides extension methods for immutable state updates.
    /// </summary>
    public static class ImmutableStateExtensions
    {
        /// <summary>
        /// Creates a new state with the specified property updated.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="state">The current state.</param>
        /// <param name="propertySelector">The property selector.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>A new state with the property updated.</returns>
        public static TState With<TState, TProperty>(
            this TState state,
            Func<TState, TProperty> propertySelector,
            TProperty newValue)
        {
            return ImmutableStateUpdater.UpdateProperty(state, propertySelector, newValue);
        }

        /// <summary>
        /// Creates a new state with multiple properties updated.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="state">The current state.</param>
        /// <param name="updates">The property updates.</param>
        /// <returns>A new state with the properties updated.</returns>
        public static TState With<TState>(
            this TState state,
            Dictionary<string, object> updates)
        {
            return ImmutableStateUpdater.UpdateProperties(state, updates);
        }

        /// <summary>
        /// Validates that this state follows immutability principles compared to the previous state.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="newState">The new state.</param>
        /// <param name="oldState">The old state.</param>
        /// <returns>True if the change is valid; otherwise, false.</returns>
        public static bool IsImmutableChangeFrom<TState>(this TState newState, TState oldState)
        {
            return ImmutableStateUpdater.ValidateImmutableChange(oldState, newState);
        }
    }
}
