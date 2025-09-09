#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HyperModule
{
    [CustomPropertyDrawer(typeof(TagMask))]
    public class TagMaskPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty selectedTagsProperty = property.FindPropertyRelative("selectedTags");

            // 태그 목록 가져오기
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

            // 현재 선택된 태그 인덱스 가져오기
            int mask = 0;
            for (int i = 0; i < tags.Length; i++)
            {
                if (IsTagSelected(selectedTagsProperty, tags[i]))
                {
                    mask |= 1 << i;
                }
            }

            EditorGUI.BeginChangeCheck();

            // 마스크 필드 표시 (한 줄)
            mask = EditorGUI.MaskField(position, label, mask, tags);

            if (EditorGUI.EndChangeCheck())
            {
                // 선택된 태그 업데이트
                UpdateSelectedTags(selectedTagsProperty, tags, mask);
            }

            EditorGUI.EndProperty();
        }

        private bool IsTagSelected(SerializedProperty selectedTagsProperty, string tag)
        {
            for (int i = 0; i < selectedTagsProperty.arraySize; i++)
            {
                if (selectedTagsProperty.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateSelectedTags(SerializedProperty selectedTagsProperty, string[] tags, int mask)
        {
            List<string> selectedTags = new List<string>();
            for (int i = 0; i < tags.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    selectedTags.Add(tags[i]);
                }
            }

            selectedTagsProperty.arraySize = selectedTags.Count;
            for (int i = 0; i < selectedTags.Count; i++)
            {
                selectedTagsProperty.GetArrayElementAtIndex(i).stringValue = selectedTags[i];
            }
        }
    }
    #endif
}