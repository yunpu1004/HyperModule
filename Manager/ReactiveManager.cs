using System;
using System.Collections.Generic;
using R3;

namespace HyperModule
{
    public static class ReactiveManager
    {
        private abstract class ReactiveEntry
        {
            public abstract object ReactiveProperty { get; }
            public abstract object GetCurrentValue();
            public abstract void Dispose();
        }

        private sealed class ReactiveEntry<T> : ReactiveEntry
        {
            private readonly ReactiveProperty<T> reactiveProperty;

            public ReactiveEntry(ReactiveProperty<T> reactiveProperty)
            {
                this.reactiveProperty = reactiveProperty;
            }

            public override object ReactiveProperty => reactiveProperty;
            public override object GetCurrentValue() => reactiveProperty.CurrentValue;
            public override void Dispose()
            {
                if (reactiveProperty is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static readonly Dictionary<string, ReactiveEntry> reactiveDictionary = new Dictionary<string, ReactiveEntry>();

        public static ReactiveProperty<T> CreateReactiveProperty<T>(string key, T initialValue)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var reactiveProperty = new ReactiveProperty<T>(initialValue);

            if (reactiveDictionary.TryGetValue(key, out var existingEntry))
            {
                existingEntry.Dispose();
            }

            reactiveDictionary[key] = new ReactiveEntry<T>(reactiveProperty);

            return reactiveProperty;
        }

        public static ReactiveProperty<T> GetReactiveProperty<T>(string key)
        {
            return TryGetReactiveProperty(key, out ReactiveProperty<T> reactiveProperty) ? reactiveProperty : null;
        }

        public static bool TryGetReactiveProperty<T>(string key, out ReactiveProperty<T> reactiveProperty)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (reactiveDictionary.TryGetValue(key, out var entry) && entry.ReactiveProperty is ReactiveProperty<T> typedReactiveProperty)
            {
                reactiveProperty = typedReactiveProperty;
                return true;
            }

            reactiveProperty = null;
            return false;
        }

        public static T GetReactiveValue<T>(string key)
        {
            return TryGetReactiveValue(key, out T value) ? value : default;
        }

        public static bool TryGetReactiveValue<T>(string key, out T value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (TryGetReactiveProperty(key, out ReactiveProperty<T> reactiveProperty))
            {
                value = reactiveProperty.CurrentValue;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetReactiveValue(string key, out object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (reactiveDictionary.TryGetValue(key, out var entry))
            {
                value = entry.GetCurrentValue();
                return true;
            }

            value = null;
            return false;
        }

        public static bool DisposeReactiveProperty(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (reactiveDictionary.TryGetValue(key, out var entry))
            {
                reactiveDictionary.Remove(key);
                entry.Dispose();
                return true;
            }

            return false;
        }

        public static bool ContainsReactiveProperty(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return reactiveDictionary.ContainsKey(key);
        }
    }
}
