using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperModule
{
    public static class UIManager
    {
        private static readonly Dictionary<string, LayoutUIBehavior> layoutCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, PopupUIBehavior> popupCache = new(StringComparer.Ordinal);

        private static float popupMinOrderLayer = 100f;
        private static readonly Stack<PopupUIBehavior> popupStack = new();

        // 지정한 레이아웃 UI를 찾아 활성화합니다.
        public static T ShowLayout<T>(string name = null) where T : LayoutUIBehavior
        {
            if (!TryResolveKey<T>(name, out var key)) return null;

            var layout = GetOrCreateBehavior<LayoutUIBehavior, T>(key, layoutCache, isPopup: false);
            if (layout == null)
            {
                QAUtil.LogWarning($"UIManager: Layout '{key}' not found.");
                return null;
            }

            layout.Show();
            return layout;
        }

        // 지정한 레이아웃 UI 인스턴스를 숨깁니다.
        public static void HideLayout<T>(string name = null) where T : LayoutUIBehavior
        {
            if (!TryResolveKey<T>(name, out var key)) return;

            if (!TryGetExistingBehavior<LayoutUIBehavior, T>(key, layoutCache, out var layout))
                return;

            layout.Hide();
        }

        // 지정한 팝업 UI를 활성화하고 스택에 추가합니다.
        public static T ShowPopup<T>(string name = null) where T : PopupUIBehavior
        {
            if (!TryResolveKey<T>(name, out var key)) return null;

            var popup = GetOrCreateBehavior<PopupUIBehavior, T>(key, popupCache, isPopup: true);
            if (popup == null)
            {
                QAUtil.LogWarning($"UIManager: Popup '{key}' not found.");
                return null;
            }

            RemoveFromPopupStack(popup);
            popupStack.Push(popup);
            ReorderPopupLayers();
            popup.Show();
            return popup;
        }

        // 스택에서 제거한 팝업 UI를 숨깁니다.
        public static void HidePopup<T>(string name = null) where T : PopupUIBehavior
        {
            if (!TryResolveKey<T>(name, out var key)) return;

            if (!TryGetExistingBehavior<PopupUIBehavior, T>(key, popupCache, out var popup))
                return;

            HidePopupInternal(popup);
        }

        // 팝업 숨김 공통 로직을 처리합니다.
        private static void HidePopupInternal(PopupUIBehavior popup)
        {
            bool removed = RemoveFromPopupStack(popup);
            popup.Hide();
            if (removed)
            {
                ReorderPopupLayers();
            }
            ResetPopupCanvas(popup);
        }

        // 캐시에서 찾거나 없으면 로드하여 캐시에 등록합니다.
        private static TBehavior GetOrCreateBehavior<TBase, TBehavior>(string key, Dictionary<string, TBase> cache, bool isPopup)
            where TBase : BaseUIBehavior
            where TBehavior : class, TBase
        {
            if (TryGetExistingBehavior<TBase, TBehavior>(key, cache, out var existing))
            {
                return existing;
            }

            var loaded = LoadBehavior<TBase, TBehavior>(key, isPopup);
            if (loaded != null)
            {
                cache[key] = loaded;
            }

            return loaded;
        }

        // 캐시 또는 씬에서 기존 UI 인스턴스를 찾습니다.
        private static bool TryGetExistingBehavior<TBase, TBehavior>(string key, Dictionary<string, TBase> cache, out TBehavior behavior)
            where TBase : BaseUIBehavior
            where TBehavior : class, TBase
        {
            if (cache.TryGetValue(key, out var cached))
            {
                if (cached is TBehavior typed && typed != null)
                {
                    behavior = typed;
                    return true;
                }

                cache.Remove(key);

                if (cached != null)
                {
                    QAUtil.LogWarning($"UIManager: Cached '{key}' is '{cached.GetType().Name}', expected '{typeof(TBehavior).Name}'.");
                }
            }

            var located = FindBehaviorInScene<TBehavior>(key);
            if (located != null)
            {
                cache[key] = located;
                behavior = located;
                return true;
            }

            behavior = null;
            return false;
        }

        // 이름이 없으면 타입명으로 키를 결정합니다.
        private static bool TryResolveKey<T>(string name, out string key) where T : BaseUIBehavior
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                key = name.Trim();
                return true;
            }

            key = typeof(T).Name;
            if (string.IsNullOrEmpty(key))
            {
                QAUtil.LogWarning($"UIManager: Could not resolve key for type '{typeof(T).FullName}'.");
                return false;
            }

            return true;
        }

        // 씬에서 키와 일치하는 UI 컴포넌트를 찾습니다.
        private static TBehavior FindBehaviorInScene<TBehavior>(string key) where TBehavior : BaseUIBehavior
        {
            var candidates = UnityEngine.Object.FindObjectsOfType<TBehavior>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null) continue;

                if (IsNameMatch(candidate, key))
                    return candidate;
            }

            return null;
        }

        // 동일한 이름 또는 타입인지 확인합니다.
        private static bool IsNameMatch(BaseUIBehavior behavior, string key)
        {
            if (behavior == null) return false;

            var goName = behavior.gameObject.name;
            if (!string.IsNullOrEmpty(goName) && string.Equals(goName, key, StringComparison.Ordinal))
                return true;

            var typeName = behavior.GetType().Name;
            if (!string.IsNullOrEmpty(typeName) && string.Equals(typeName, key, StringComparison.Ordinal))
                return true;

            var fullTypeName = behavior.GetType().FullName;
            if (!string.IsNullOrEmpty(fullTypeName) && string.Equals(fullTypeName, key, StringComparison.Ordinal))
                return true;

            return false;
        }

        // 어드레서블 또는 리소스에서 UI 프리팹을 로드합니다.
        private static TBehavior LoadBehavior<TBase, TBehavior>(string key, bool isPopup)
            where TBase : BaseUIBehavior
            where TBehavior : class, TBase
        {
            string folder = isPopup ? "Popup" : "UI";

            var behavior = InstantiateFromAddressables<TBehavior>(key, key);
            if (behavior != null) return behavior;

            behavior = InstantiateFromAddressables<TBehavior>($"{folder}/{key}", key);
            if (behavior != null) return behavior;

            behavior = InstantiateFromResources<TBehavior>(key, key);
            if (behavior != null) return behavior;

            return InstantiateFromResources<TBehavior>($"{folder}/{key}", key);
        }

        // 어드레서블 프리팹을 인스턴스화합니다.
        private static TBehavior InstantiateFromAddressables<TBehavior>(string address, string instanceName) where TBehavior : BaseUIBehavior
        {
            if (string.IsNullOrEmpty(address)) return null;

            if (AddressablesManager.TryGet<GameObject>(address, out var prefab) && prefab != null)
            {
                return InstantiateBehavior<TBehavior>(prefab, address, instanceName);
            }

            return null;
        }

        // Resources 프리팹을 인스턴스화합니다.
        private static TBehavior InstantiateFromResources<TBehavior>(string path, string instanceName) where TBehavior : BaseUIBehavior
        {
            if (string.IsNullOrEmpty(path)) return null;

            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;

            return InstantiateBehavior<TBehavior>(prefab, path, instanceName);
        }

        // 프리팹에서 요청한 UI 컴포넌트를 추출해 반환합니다.
        private static TBehavior InstantiateBehavior<TBehavior>(GameObject prefab, string source, string instanceName) where TBehavior : BaseUIBehavior
        {
            if (prefab == null) return null;

            var instance = UnityEngine.Object.Instantiate(prefab);
            if (!string.IsNullOrEmpty(instanceName))
            {
                instance.name = instanceName;
            }

            var behavior = instance.GetComponent<TBehavior>() ?? instance.GetComponentInChildren<TBehavior>(true);
            if (behavior == null)
            {
                QAUtil.LogWarning($"UIManager: '{source}' does not contain a {typeof(TBehavior).Name} component.");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            return behavior;
        }

        // 팝업 스택에서 지정된 팝업을 제거합니다.
        private static bool RemoveFromPopupStack(PopupUIBehavior popup)
        {
            if (popupStack.Count == 0) return false;

            var removed = false;
            var buffer = new List<PopupUIBehavior>(popupStack.Count);
            while (popupStack.Count > 0)
            {
                var current = popupStack.Pop();
                if (!removed && current == popup)
                {
                    removed = true;
                    continue;
                }

                if (current == null)
                {
                    removed = true;
                    continue;
                }

                buffer.Add(current);
            }

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                popupStack.Push(buffer[i]);
            }

            return removed;
        }

        // 스택 순서대로 팝업 캔버스 정렬값을 갱신합니다.
        private static void ReorderPopupLayers()
        {
            if (popupStack.Count == 0) return;

            CleanupPopupStack();

            var array = popupStack.ToArray();
            int baseOrder = Mathf.RoundToInt(popupMinOrderLayer);

            for (int i = array.Length - 1, orderIndex = 0; i >= 0; i--, orderIndex++)
            {
                var popup = array[i];
                if (popup == null) continue;

                var canvas = popup.canvas;
                if (canvas == null) continue;

                canvas.overrideSorting = true;
                canvas.sortingOrder = baseOrder + orderIndex;
            }
        }

        // 팝업 스택에서 null 항목을 정리합니다.
        private static void CleanupPopupStack()
        {
            if (popupStack.Count == 0) return;

            var buffer = new List<PopupUIBehavior>(popupStack.Count);
            while (popupStack.Count > 0)
            {
                var current = popupStack.Pop();
                if (current == null) continue;
                buffer.Add(current);
            }

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                popupStack.Push(buffer[i]);
            }
        }

        // 스택에서 빠진 팝업의 캔버스 정렬을 초기화합니다.
        private static void ResetPopupCanvas(PopupUIBehavior popup)
        {
            var canvas = popup?.canvas;
            if (canvas == null) return;

            if (popupStack.Contains(popup)) return;

            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }
}









