using UnityEditor;
using UnityEngine;
using TMPro;

namespace HyperModule
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TranslatedText))]
    public class TranslatedTextEditor : UnityEditor.Editor
    {
        // 변수를 OnInspectorGUI 밖, 클래스 멤버로 선언합니다.
        private LanguageType selectedLanguage;

        // 인스펙터가 활성화될 때 한 번 호출됩니다.
        private void OnEnable()
        {
            // target은 현재 인스펙터가 보고 있는 컴포넌트입니다.
            TranslatedText translatedText = (TranslatedText)target;

            // 드롭다운의 초기값을 현재 컴포넌트의 언어 타입과 동기화합니다.
            // 만약 컴포넌트의 언어가 정의되지 않았다면(Undefined), enum의 첫 번째 유효한 값으로 설정할 수 있습니다.
            selectedLanguage = translatedText.languageType;
        }

        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 필드를 그립니다.
            DrawDefaultInspector();

            TranslatedText translatedText = (TranslatedText)target;
            TextMeshProUGUI tmpText = translatedText.GetComponent<TextMeshProUGUI>();

            EditorGUILayout.Space(); // 시각적인 구분을 위해 약간의 공간을 추가합니다.
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel); // 프리뷰 섹션 제목

            EditorGUILayout.BeginHorizontal();

            // EnumPopup은 selectedLanguage의 현재 값을 보여주고,
            // 사용자가 값을 변경하면 selectedLanguage에 그 값을 업데이트합니다.
            // 이 값은 이제 Editor 클래스 인스턴스가 살아있는 동안 유지됩니다.
            selectedLanguage = (LanguageType)EditorGUILayout.EnumPopup("Preview Language", selectedLanguage);

            if (GUILayout.Button("Preview"))
            {
                if (translatedText != null)
                {
                    // Undo.RecordObject를 사용하면 프리뷰 변경 후 Ctrl+Z로 되돌릴 수 있습니다.
                    var componentArr = new Object[] { translatedText, tmpText };
                    Undo.RecordObjects(componentArr, "Change Language for Preview");

                    // SetLanguage를 호출하여 컴포넌트의 실제 언어 설정을 변경합니다.
                    translatedText.SetLanguage(selectedLanguage);

                    // 씬에 있는 오브젝트의 데이터가 변경되었음을 Unity에 알립니다.
                    // 이렇게 해야 씬을 저장할 때 변경사항이 반영됩니다.
                    EditorUtility.SetDirty(translatedText);
                    EditorUtility.SetDirty(tmpText);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}