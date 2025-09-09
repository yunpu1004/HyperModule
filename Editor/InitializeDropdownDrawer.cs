using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HyperModule
{
    /// <summary>
    /// InitializeDropdown<T> 필드를 위한 커스텀 프로퍼티 드로어.
    /// </summary>
    [CustomPropertyDrawer(typeof(InitializeDropdownBase<>), true)]
    public class InitializeDropdownDrawer : PropertyDrawer
    {
        // 캐싱을 통해 성능 최적화
        private static Dictionary<Type, List<Type>> initializeTypeCache = new Dictionary<Type, List<Type>>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // InitializeDropdown<T>의 T 타입을 추출
            Type genericType = fieldInfo.FieldType.GetGenericArguments().FirstOrDefault();
            if (genericType == null)
            {
                EditorGUI.LabelField(position, label.text, "Generic type not found.");
                EditorGUI.EndProperty();
                return;
            }

            // IInitialize<U>의 U는 genericType 또는 그 기반 클래스
            Type initializeInterfaceType = typeof(IInitialize<>).MakeGenericType(genericType);

            // 캐시에서 구현 타입을 가져오거나 새로 검색
            List<Type> initializeTypes;
            if (!initializeTypeCache.TryGetValue(genericType, out initializeTypes))
            {
                initializeTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException e)
                        {
                            return e.Types.Where(t => t != null);
                        }
                    })
                    .Where(t => !t.IsAbstract &&
                                t.GetInterfaces().Any(i =>
                                    i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == typeof(IInitialize<>) &&
                                    i.GetGenericArguments()[0].IsAssignableFrom(genericType))) // 수정된 부분
                    .ToList();

                // InitializeTypes를 ABC 순으로 정렬
                initializeTypes.Sort((t1, t2) => t1.FullName.CompareTo(t2.FullName));

                initializeTypeCache[genericType] = initializeTypes;
            }

            // 드롭다운 옵션 목록 생성 (<None> 추가)
            List<string> options = new List<string> { "<None>" };
            options.AddRange(initializeTypes.Select(t => t.FullName));

            // SerializedProperty에서 현재 선택된 타입 이름 가져오기
            SerializedProperty typeNameProp = property.FindPropertyRelative("selectedInitializeTypeName");
            string currentTypeName = typeNameProp.stringValue;

            // 현재 선택된 인덱스 찾기 (<None>을 0으로 설정)
            int currentIndex = 0; // 기본은 <None>
            if (!string.IsNullOrEmpty(currentTypeName))
            {
                int typeIndex = options.IndexOf(currentTypeName);
                if (typeIndex != -1)
                    currentIndex = typeIndex;
            }

            // 드롭다운 표시
            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());

            // 선택된 타입 이름 저장 (<None> 선택 시 빈 문자열로 설정)
            if (selectedIndex >= 0 && selectedIndex < options.Count)
            {
                if (selectedIndex == 0)
                    typeNameProp.stringValue = ""; // <None> 선택
                else
                    typeNameProp.stringValue = options[selectedIndex];
            }

            EditorGUI.EndProperty();
        }
    }
}