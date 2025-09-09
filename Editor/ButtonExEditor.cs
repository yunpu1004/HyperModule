#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace HyperModule
{
    // ButtonEx 전용 커스텀 인스펙터
    [CustomEditor(typeof(ButtonEx), true)]
    [CanEditMultipleObjects]
    public class ButtonExEditor : ButtonEditor
    {
        private SerializedProperty _clickSoundName;
        private SerializedProperty _clickVolume;

        protected override void OnEnable()
        {
            base.OnEnable();
            _clickSoundName = serializedObject.FindProperty("clickSoundName");
            _clickVolume = serializedObject.FindProperty("clickVolume");
        }

        public override void OnInspectorGUI()
        {
            // 1) 기존 ButtonInspector GUI 출력
            base.OnInspectorGUI();

            // 2) 추가 필드 출력
            serializedObject.Update();
            EditorGUILayout.PropertyField(_clickSoundName, new GUIContent("Click Sound Name"));
            EditorGUILayout.PropertyField(_clickVolume, new GUIContent("Click Volume"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
