using UnityEditor;
using UnityEditor.UI;

namespace HyperModule
{
    // ScrollRectEx 컴포넌트를 위한 커스텀 에디터임을 명시합니다.
    [CustomEditor(typeof(ScrollRectEx), true)]
    public class ScrollRectExEditor : ScrollRectEditor // ScrollRectEditor를 상속받습니다.
    {
        private SerializedProperty dragSensitivity;

        protected override void OnEnable()
        {
            // 기존 ScrollRectEditor의 OnEnable 로직을 먼저 실행합니다.
            base.OnEnable();

            // "dragSensitivity"라는 이름의 프로퍼티를 찾아서 연결합니다.
            dragSensitivity = serializedObject.FindProperty("dragSensitivity");
        }

        public override void OnInspectorGUI()
        {
            // 수정 가능한 상태로 만듭니다.
            serializedObject.Update();

            // 새로 추가한 dragSensitivity 필드를 인스펙터에 그립니다.
            EditorGUILayout.PropertyField(dragSensitivity);

            // 변경 사항을 적용합니다.
            serializedObject.ApplyModifiedProperties();

            // 기존 ScrollRect의 인스펙터를 아래에 그립니다.
            // 순서를 바꾸고 싶다면 이 줄을 위로 올리면 됩니다.
            base.OnInspectorGUI();
        }
    }
}