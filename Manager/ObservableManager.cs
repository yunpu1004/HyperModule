using System;
using System.Collections.Generic;
using R3;

namespace HyperModule
{
    public static class ObservableManager
    {
        private sealed class ObservableEntry
        {
            public ObservableEntry(object observable, Observable<object> boxedObservable)
            {
                Observable = observable;
                BoxedObservable = boxedObservable;
            }

            public object Observable { get; }
            public Observable<object> BoxedObservable { get; }
        }

        private static readonly Dictionary<string, ObservableEntry> observableDictionary = new Dictionary<string, ObservableEntry>();

        public static void AddObservable<T>(string key, Observable<T> observable)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (observable == null) throw new ArgumentNullException(nameof(observable));

            var boxedObservable = observable.Select(value => (object)value);
            observableDictionary[key] = new ObservableEntry(observable, boxedObservable);
        }

        public static Observable<T> GetObservable<T>(string key)
        {
            return TryGetObservable(key, out Observable<T> observable) ? observable : null;
        }

        public static bool TryGetObservable<T>(string key, out Observable<T> observable)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (observableDictionary.TryGetValue(key, out var entry) && entry.Observable is Observable<T> typedObservable)
            {
                observable = typedObservable;
                return true;
            }

            observable = null;
            return false;
        }

        public static bool TryGetObservable(string key, out Observable<object> observable)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (observableDictionary.TryGetValue(key, out var entry))
            {
                observable = entry.BoxedObservable;
                return true;
            }

            observable = null;
            return false;
        }

        public static void RemoveObservable(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            observableDictionary.Remove(key);
        }

        public static bool ContainsObservable(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return observableDictionary.ContainsKey(key);
        }
    }
}

