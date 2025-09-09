using System;
using System.Collections.Generic;

namespace HyperModule
{
    public static class DelegateManager
    {
        private static Dictionary<string, Delegate> delegateDictionary = new Dictionary<string, Delegate>();

        public static void AddDelegate<T>(string key, T del) where T : Delegate
        {
            delegateDictionary[key] = del;
        }

        public static T GetDelegate<T>(string key) where T : Delegate
        {
            if (delegateDictionary.TryGetValue(key, out var del))
            {
                return del as T;
            }
            return null;
        }

        public static void RemoveDelegate(string key)
        {
            if (delegateDictionary.ContainsKey(key))
            {
                delegateDictionary.Remove(key);
            }
        }

        public static bool ContainsDelegate(string key)
        {
            return delegateDictionary.ContainsKey(key);
        }
    }
}