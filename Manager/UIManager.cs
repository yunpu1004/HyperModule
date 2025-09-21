using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperModule
{
    public static class UIManager
    {
        private static readonly Dictionary<string, LayoutUIBehavior> layoutCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, PopupUIBehavior> popupCache = new(StringComparer.Ordinal);

        private static float layoutMinOrderLayer = 10f;
        private static float popupMinOrderLayer = 100f;
        private static readonly Stack<LayoutUIBehavior> layoutStack = new();
        private static readonly Stack<PopupUIBehavior> popupStack = new();

        // 지정한 레이아웃 UI를 찾아 활성화합니다.
        public static T ShowLayout<T>(string name = null) where T : LayoutUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            var layout = GetOrCreateBehavior<LayoutUIBehavior, T>(key, layoutCache, isPopup: false, notFoundLabel: "Layout");
            if (layout == null) return null;

            RemoveFromLayoutStack(layout);
            layoutStack.Push(layout);
            ReorderLayoutLayers();
            layout.Show();
            return layout;
        }

        // 지정한 레이아웃 UI를 찾아 비활성화합니다.
        public static T HideLayout<T>(string name = null) where T : LayoutUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            if (!TryGetExistingBehavior<LayoutUIBehavior, T>(key, layoutCache, out var layout))
                return null;

            RemoveFromLayoutStack(layout);
            layout.Hide();
            ReorderLayoutLayers();
            ResetLayoutCanvas(layout);
            return layout;
        }

        // 지정한 팝업 UI를 찾아 활성화합니다.
        public static T ShowPopup<T>(string name = null) where T : PopupUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            var popup = GetOrCreateBehavior<PopupUIBehavior, T>(key, popupCache, isPopup: true, notFoundLabel: "Popup");
            if (popup == null) return null;
            
            RemoveFromPopupStack(popup);
            popupStack.Push(popup);
            ReorderPopupLayers();
            popup.Show();
            return popup;
        }

        // 지정한 팝업 UI를 찾아 비활성화합니다.
        public static T HidePopup<T>(string name = null) where T : PopupUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            if (!TryGetExistingBehavior<PopupUIBehavior, T>(key, popupCache, out var popup))
                return null;

            RemoveFromPopupStack(popup);
            popup.Hide();
            ReorderPopupLayers();
            ResetPopupCanvas(popup);
            return popup;
        }

        // 지정한 레이아웃 UI를 찾아 Refresh를 호출합니다.
        public static T RefreshLayout<T>(string name = null) where T : LayoutUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            if (!TryGetExistingBehavior<LayoutUIBehavior, T>(key, layoutCache, out var layout))
                return null;

            layout.Refresh();
            return layout;
        }

        // 지정한 팝업 UI를 찾아 Refresh를 호출합니다.
        public static T RefreshPopup<T>(string name = null) where T : PopupUIBehavior
        {
            TryResolveKey<T>(name, out var key);

            if (!TryGetExistingBehavior<PopupUIBehavior, T>(key, popupCache, out var popup))
                return null;

            popup.Refresh();
            return popup;
        }

        // 공통: 기존 인스턴스 검색 후 없으면 생성/캐시/카메라 셋업까지 수행합니다.
        private static TBehavior GetOrCreateBehavior<TBase, TBehavior>(
            string key,
            Dictionary<string, TBase> cache,
            bool isPopup,
            string notFoundLabel)
            where TBase : BaseUIBehavior
            where TBehavior : class, TBase
        {
            if (TryGetExistingBehavior<TBase, TBehavior>(key, cache, out var behavior))
                return behavior;

            behavior = InstantiateUIBehavior<TBehavior>(key, isPopup);
            if (behavior == null)
            {
                QAUtil.LogWarning($"UIManager: {notFoundLabel} '{key}' not found.");
                return null;
            }

            cache[key] = behavior;
            SetupCanvasCamera(behavior.canvas);
            return behavior;
        }

        // 캔버스가 ScreenSpace-Camera 모드라면 메인 카메라를 지정합니다.
        private static void SetupCanvasCamera(Canvas canvas)
        {
            if (canvas == null) return;
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera) return;

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                canvas.worldCamera = mainCam;
            }
            else
            {
                QAUtil.LogWarning("UIManager: Main Camera not found for ScreenSpace-Camera Canvas.");
            }
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
        private static void TryResolveKey<T>(string name, out string key) where T : BaseUIBehavior
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                key = name.Trim();
                return;
            }

            key = typeof(T).Name;
        }

        // 씬에서 키와 일치하는 UI 컴포넌트를 찾습니다.
        private static TBehavior FindBehaviorInScene<TBehavior>(string key) where TBehavior : BaseUIBehavior
        {
            var candidates = UnityEngine.Object.FindObjectsOfType<TBehavior>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (IsNameMatch(candidate, key))
                    return candidate;
            }

            return null;
        }

        // 동일한 이름 또는 타입인지 확인합니다.
        private static bool IsNameMatch(BaseUIBehavior behavior, string key)
        {
            var t = behavior.GetType();
            return behavior.gameObject.name == key || t.Name == key || t.FullName == key;
        }

        // Addressables/Resources 경로를 폴백으로 시도하여 UI를 인스턴스화합니다.
        private static TBehavior InstantiateUIBehavior<TBehavior>(string key, bool isPopup) where TBehavior : BaseUIBehavior
        {
            string folder = isPopup ? "Popup" : "UI";

            return
                InstantiateFromAddressables<TBehavior>(key, key)
                ?? InstantiateFromAddressables<TBehavior>($"{folder}/{key}", key)
                ?? InstantiateFromResources<TBehavior>(key, key)
                ?? InstantiateFromResources<TBehavior>($"{folder}/{key}", key);
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

        // 레이아웃 스택 순서대로 캔버스 정렬값을 갱신합니다.
        private static void ReorderLayoutLayers()
        {
            CleanupLayoutStack();

            var array = layoutStack.ToArray();
            int baseOrder = Mathf.RoundToInt(layoutMinOrderLayer);

            for (int i = array.Length - 1, orderIndex = 0; i >= 0; i--, orderIndex++)
            {
                var layout = array[i];
                ApplyLayoutLayer(layout, baseOrder + orderIndex);
            }
        }

        // 레이아웃 캔버스 정렬값을 적용합니다.
        private static void ApplyLayoutLayer(LayoutUIBehavior layout, int sortingOrder)
        {
            var canvas = layout?.canvas;
            if (canvas == null) return;

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        // 레이아웃 스택에서 지정된 레이아웃을 제거합니다.
        private static bool RemoveFromLayoutStack(LayoutUIBehavior layout)
        {
            if (layoutStack.Count == 0) return false;

            var removed = false;
            var buffer = new List<LayoutUIBehavior>(layoutStack.Count);
            while (layoutStack.Count > 0)
            {
                var current = layoutStack.Pop();
                if (!removed && current == layout)
                {
                    removed = true;
                    continue;
                }

                if (current != null)
                {
                    buffer.Add(current);
                }
            }

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                layoutStack.Push(buffer[i]);
            }

            return removed;
        }

        // 레이아웃 스택에서 null 항목을 정리합니다.
        private static void CleanupLayoutStack()
        {
            if (layoutStack.Count == 0) return;

            var buffer = new List<LayoutUIBehavior>(layoutStack.Count);
            while (layoutStack.Count > 0)
            {
                var current = layoutStack.Pop();
                if (current == null) continue;
                buffer.Add(current);
            }

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                layoutStack.Push(buffer[i]);
            }
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

                if (current != null)
                {
                    buffer.Add(current);
                }
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
            CleanupPopupStack();

            var array = popupStack.ToArray();
            int baseOrder = Mathf.RoundToInt(popupMinOrderLayer);

            for (int i = array.Length - 1, orderIndex = 0; i >= 0; i--, orderIndex++)
            {
                var popup = array[i];
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

        // 레이아웃 캔버스 정렬을 원래 상태로 되돌립니다.
        private static void ResetLayoutCanvas(LayoutUIBehavior layout)
        {
            var canvas = layout?.canvas;
            if (canvas == null) return;

            if (layoutStack.Contains(layout)) return;

            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }
}
