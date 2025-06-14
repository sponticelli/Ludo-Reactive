using System;
using System.IO;
using UnityEngine;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Defines methods for persisting and loading application state.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public interface IStatePersistence<TState>
    {
        /// <summary>
        /// Saves the state to persistent storage.
        /// </summary>
        /// <param name="state">The state to save.</param>
        /// <param name="key">The key to save the state under.</param>
        void SaveState(TState state, string key = "default");

        /// <summary>
        /// Loads the state from persistent storage.
        /// </summary>
        /// <param name="key">The key to load the state from.</param>
        /// <returns>The loaded state, or default if not found.</returns>
        TState LoadState(string key = "default");

        /// <summary>
        /// Checks if a state exists in persistent storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the state exists; otherwise, false.</returns>
        bool HasState(string key = "default");

        /// <summary>
        /// Deletes the state from persistent storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        void DeleteState(string key = "default");

        /// <summary>
        /// Clears all persisted states.
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// Implements state persistence using Unity's PlayerPrefs.
    /// Suitable for simple state objects that can be serialized to JSON.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class PlayerPrefsStatePersistence<TState> : IStatePersistence<TState>
    {
        private readonly string _keyPrefix;
        private readonly TState _defaultState;

        /// <summary>
        /// Initializes a new instance of the PlayerPrefsStatePersistence class.
        /// </summary>
        /// <param name="keyPrefix">The prefix to use for PlayerPrefs keys.</param>
        /// <param name="defaultState">The default state to return when no saved state exists.</param>
        public PlayerPrefsStatePersistence(string keyPrefix = "ReactiveState", TState defaultState = default)
        {
            _keyPrefix = keyPrefix ?? throw new ArgumentNullException(nameof(keyPrefix));
            _defaultState = defaultState;
        }

        /// <inheritdoc />
        public void SaveState(TState state, string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                var json = JsonUtility.ToJson(state, true);
                var fullKey = GetFullKey(key);
                PlayerPrefs.SetString(fullKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to save state with key '{key}': {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public TState LoadState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                var fullKey = GetFullKey(key);
                if (!PlayerPrefs.HasKey(fullKey))
                {
                    return _defaultState;
                }

                var json = PlayerPrefs.GetString(fullKey);
                if (string.IsNullOrEmpty(json))
                {
                    return _defaultState;
                }

                return JsonUtility.FromJson<TState>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to load state with key '{key}': {ex.Message}");
                return _defaultState;
            }
        }

        /// <inheritdoc />
        public bool HasState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var fullKey = GetFullKey(key);
            return PlayerPrefs.HasKey(fullKey);
        }

        /// <inheritdoc />
        public void DeleteState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                return;

            var fullKey = GetFullKey(key);
            if (PlayerPrefs.HasKey(fullKey))
            {
                PlayerPrefs.DeleteKey(fullKey);
                PlayerPrefs.Save();
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            // Note: PlayerPrefs doesn't provide a way to delete keys by prefix,
            // so this is a simplified implementation
            Debug.LogWarning("[Ludo.Reactive] PlayerPrefsStatePersistence.ClearAll() only clears the default key. " +
                           "Consider using FileStatePersistence for more advanced scenarios.");
            DeleteState("default");
        }

        private string GetFullKey(string key)
        {
            return $"{_keyPrefix}_{key}";
        }
    }

    /// <summary>
    /// Implements state persistence using file system storage.
    /// Provides more flexibility than PlayerPrefs and supports complex state objects.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class FileStatePersistence<TState> : IStatePersistence<TState>
    {
        private readonly string _directoryPath;
        private readonly TState _defaultState;

        /// <summary>
        /// Initializes a new instance of the FileStatePersistence class.
        /// </summary>
        /// <param name="directoryPath">The directory path to store state files.</param>
        /// <param name="defaultState">The default state to return when no saved state exists.</param>
        public FileStatePersistence(string directoryPath = null, TState defaultState = default)
        {
            _directoryPath = directoryPath ?? Path.Combine(Application.persistentDataPath, "ReactiveState");
            _defaultState = defaultState;

            // Ensure directory exists
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        /// <inheritdoc />
        public void SaveState(TState state, string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                var json = JsonUtility.ToJson(state, true);
                var filePath = GetFilePath(key);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to save state to file '{key}': {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public TState LoadState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                var filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                {
                    return _defaultState;
                }

                var json = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(json))
                {
                    return _defaultState;
                }

                return JsonUtility.FromJson<TState>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to load state from file '{key}': {ex.Message}");
                return _defaultState;
            }
        }

        /// <inheritdoc />
        public bool HasState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        /// <inheritdoc />
        public void DeleteState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                var filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to delete state file '{key}': {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            try
            {
                if (Directory.Exists(_directoryPath))
                {
                    var files = Directory.GetFiles(_directoryPath, "*.json");
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to clear all state files: {ex.Message}");
            }
        }

        private string GetFilePath(string key)
        {
            var fileName = $"{key}.json";
            return Path.Combine(_directoryPath, fileName);
        }
    }

    /// <summary>
    /// A memory-only state persistence implementation for testing purposes.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class MemoryStatePersistence<TState> : IStatePersistence<TState>
    {
        private readonly System.Collections.Generic.Dictionary<string, TState> _states;
        private readonly TState _defaultState;

        /// <summary>
        /// Initializes a new instance of the MemoryStatePersistence class.
        /// </summary>
        /// <param name="defaultState">The default state to return when no saved state exists.</param>
        public MemoryStatePersistence(TState defaultState = default)
        {
            _states = new System.Collections.Generic.Dictionary<string, TState>();
            _defaultState = defaultState;
        }

        /// <inheritdoc />
        public void SaveState(TState state, string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _states[key] = state;
        }

        /// <inheritdoc />
        public TState LoadState(string key = "default")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            return _states.TryGetValue(key, out var state) ? state : _defaultState;
        }

        /// <inheritdoc />
        public bool HasState(string key = "default")
        {
            return !string.IsNullOrEmpty(key) && _states.ContainsKey(key);
        }

        /// <inheritdoc />
        public void DeleteState(string key = "default")
        {
            if (!string.IsNullOrEmpty(key))
            {
                _states.Remove(key);
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            _states.Clear();
        }
    }
}
