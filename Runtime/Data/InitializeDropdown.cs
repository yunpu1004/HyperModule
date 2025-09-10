using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// InitializeDropdown의 추상 기반 클래스.
    /// </summary>
    /// <typeparam name="T">초기화할 MonoBehaviour 타입.</typeparam>
    [System.Serializable]
    public abstract class InitializeDropdownBase<T> where T : MonoBehaviour
    {
        /// <summary>
        /// 선택된 클래스의 전체 타입 이름을 저장.
        /// </summary>
        public string selectedInitializeTypeName;

        /// <summary>
        /// 선택된 클래스를 인스턴스화하여 초기화 메서드 호출.
        /// </summary>
        /// <param name="component">초기화할 컴포넌트.</param>
        public abstract void Initialize(T component);
    }

    /// <summary>
    /// 특정 타입 T에 대한 초기화 드롭다운 클래스.
    /// </summary>
    /// <typeparam name="T">초기화할 MonoBehaviour 타입.</typeparam>
    [System.Serializable]
    public class InitializeDropdown<T> : InitializeDropdownBase<T> where T : MonoBehaviour
    {
        // ---------------------
        // 추가된 정적 멤버들
        // ---------------------

        /// <summary>
        /// (타입 이름 → Type) 매핑 정보를 저장하기 위한 캐시.
        /// </summary>
        private static Dictionary<string, Type> s_typeCache;

        /// <summary>
        /// 캐시를 초기화했는지 여부를 판단하는 플래그.
        /// </summary>
        private static bool s_isInitialized;

        // ---------------------
        // 메인 로직
        // ---------------------

        /// <summary>
        /// 선택된 IInitializeBase 구현 클래스를 인스턴스화하여 초기화 메서드 호출.
        /// </summary>
        /// <param name="component">초기화할 컴포넌트.</param>
        public override void Initialize(T component)
        {
            if (string.IsNullOrEmpty(selectedInitializeTypeName))
                return;

            // (1) 캐시 초기화
            EnsureTypeCache();

            // (2) 딕셔너리에서 바로 조회
            if (s_typeCache.TryGetValue(selectedInitializeTypeName, out Type type))
            {
                if (typeof(IInitializeBase).IsAssignableFrom(type))
                {
                    var initializer = Activator.CreateInstance(type) as IInitializeBase;
                    if (initializer != null)
                    {
                        initializer.Initialize(component);
                    }
                }
                else
                {
                    QAUtil.LogError($"Type {selectedInitializeTypeName} does not implement IInitializeBase.");
                }
            }
            else
            {
                QAUtil.LogError($"Cannot find type in cache: {selectedInitializeTypeName}");
            }
        }

        /// <summary>
        /// 타입 캐시가 초기화되었는지 확인하고, 아직 초기화되지 않았다면 한 번만 초기화.
        /// </summary>
        private static void EnsureTypeCache()
        {
            if (s_isInitialized)
                return;

            s_isInitialized = true;  // 캐시 초기화가 이미 진행됨을 표시

            // (타입 이름 → Type) 매핑을 저장할 딕셔너리 생성
            s_typeCache = new Dictionary<string, Type>();

            // ----------------------------------------------------
            // 1) 모든 어셈블리를 검색하는 기본 로직
            // ----------------------------------------------------
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var t in types)
                {
                    // t.FullName이 null인 경우가 드물게 있을 수 있으므로 체크
                    if (!string.IsNullOrEmpty(t.FullName))
                    {
                        // 키 충돌 시 덮어쓸지 여부는 상황에 따라 결정
                        // 일반적으로 동일 FullName이 존재하지 않으므로 그냥 대입
                        s_typeCache[t.FullName] = t;
                    }
                }
            }
        }
    }
}