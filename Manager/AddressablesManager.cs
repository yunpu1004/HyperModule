using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace HyperModule
{
    public static class AddressablesManager
    {
        private static AddressablesSettings _settings;

        // address(PrimaryKey) -> loaded asset
        private static readonly Dictionary<string, object> _assetMap = new Dictionary<string, object>();

        // 나중에 해제하기 위한 핸들 보관
        private static readonly List<AsyncOperationHandle> _assetHandles = new List<AsyncOperationHandle>();

        private static bool _isLoading;

        /// <summary>
        /// Resources/Settings/AddressablesSettings 를 로드하여, labels 기준으로
        /// Addressables 에셋을 모두 로드하고 Dictionary<string, object> 에 저장합니다.
        /// 키는 에셋의 Address(PrimaryKey) 입니다.
        /// </summary>
        public static async UniTask Init()
        {
            if (_isLoading)
            {
                QAUtil.LogWarning("[AddressablesManager] Init already in progress.");
                return;
            }
            _isLoading = true;

            // 1) SO 로드
            _settings = Resources.Load<AddressablesSettings>("Settings/AddressablesSettings");
            if (_settings == null)
            {
                QAUtil.LogError("[AddressablesManager] AddressablesSettings is not found in Resources/Settings/");
                _isLoading = false;
                return;
            }

            string[] labels = _settings.labels;
            // 2) labels 검증
            if (labels == null || labels.Length == 0)
            {
                QAUtil.LogWarning("[AddressablesManager] AddressablesSettings.labels is null or empty.");
                _isLoading = false;
                return;
            }

            try
            {
                // 3) labels 로 매칭되는 모든 리소스 로케이션 수집 (Union: 라벨들 중 하나라도 매칭되면 포함)
                var labelKeys = new List<object>(labels);
                var locHandle = Addressables.LoadResourceLocationsAsync(labelKeys, Addressables.MergeMode.Union, typeof(object));
                var locations = await locHandle.Task;

                if (locHandle.Status != AsyncOperationStatus.Succeeded || locations == null)
                {
                    QAUtil.LogError("[AddressablesManager] Failed to load resource locations for labels.");
                    Addressables.Release(locHandle);
                    _isLoading = false;
                    return;
                }

                // 4) PrimaryKey 기준으로 중복 제거
                var uniqueLocations = new Dictionary<string, IResourceLocation>(capacity: locations.Count);
                foreach (var loc in locations)
                {
                    if (!uniqueLocations.ContainsKey(loc.PrimaryKey))
                        uniqueLocations.Add(loc.PrimaryKey, loc);
                }

                QAUtil.Log($"[AddressablesManager] Found {uniqueLocations.Count} assets for labels: {string.Join(", ", labels)}");

                // 5) 각 로케이션의 에셋 로드 후 Dictionary<string, object> 에 저장
                int loadedCount = 0;
                foreach (var kv in uniqueLocations)
                {
                    string address = kv.Key;
                    IResourceLocation loc = kv.Value;

                    var handle = Addressables.LoadAssetAsync<object>(loc);
                    await handle.Task;

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _assetMap[address] = handle.Result;
                        _assetHandles.Add(handle);
                        loadedCount++;
                        QAUtil.Log($"[AddressablesManager] Loaded: {address} -> {handle.Result?.GetType().Name ?? "null"}");
                    }
                    else
                    {
                        QAUtil.LogWarning($"[AddressablesManager] Failed to load asset: {address}");
                        Addressables.Release(handle);
                    }
                }

                Addressables.Release(locHandle);
                QAUtil.Log($"[AddressablesManager] Loaded {loadedCount} / {uniqueLocations.Count} assets into dictionary.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// address(PrimaryKey) 로 특정 타입으로 가져오기
        /// </summary>
        public static bool TryGet<T>(string address, out T asset) where T : class
        {
            if (_assetMap.TryGetValue(address, out var obj) && obj is T typed)
            {
                asset = typed;
                return true;
            }
            asset = null;
            return false;
        }

        /// <summary>
        /// address(PrimaryKey) 로 가져오되 실패 시 null 반환
        /// </summary>
        public static T Get<T>(string address) where T : class
        {
            return TryGet<T>(address, out var a) ? a : null;
        }

        /// <summary>
        /// 로드된 모든 에셋 해제 및 딕셔너리 정리
        /// </summary>
        public static async UniTask ReleaseAll()
        {
            // Init 진행 중이면 안전하게 중단
            if (_isLoading)
            {
                QAUtil.LogWarning("[AddressablesManager] ReleaseAll skipped: Init is in progress.");
                return;
            }

            // Unity API 호출은 메인스레드 보장
            await UniTask.SwitchToMainThread();

            int total = _assetHandles.Count;
            if (total == 0)
            {
                _assetMap.Clear();
                QAUtil.Log("[AddressablesManager] No loaded assets to release. Cleared dictionary.");
                return;
            }

            int released = 0;
            // 역순 해제를 권장(의존 역순 가능성, 큰 의미는 없지만 안전 습관)
            for (int i = _assetHandles.Count - 1; i >= 0; i--)
            {
                var h = _assetHandles[i];
                if (h.IsValid())
                {
                    Addressables.Release(h);
                    released++;
                }

                // 32개마다 한 프레임 양보(값은 프로젝트 상황에 맞게 조절 가능)
                if ((released & 31) == 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }

            _assetHandles.Clear();
            _assetMap.Clear();

            // 사용되지 않는 리소스 정리(비동기) -> 실제 메모리 해제 유도
            try
            {
                var op = Resources.UnloadUnusedAssets();
                await op.ToUniTask(); // UniTask 확장으로 대기
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // 관리 힙도 정리(옵션) - 상황에 따라 빼도 무방
            GC.Collect();
            GC.WaitForPendingFinalizers();

            QAUtil.Log($"[AddressablesManager] Released {released}/{total} handles, cleared dictionary, and unloaded unused assets.");
        }
    }
}
