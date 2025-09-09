using UnityEngine;
using UnityEditor;

namespace HyperModule
{
    [CustomEditor(typeof(Test))]
    public class TestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Test testScript = (Test)target;

            if (GUILayout.Button("Execute Test Method"))
            {
                Undo.RecordObject(testScript, "MyTestMethod");
                testScript.MyTestMethod();
                ApplyModification(testScript.gameObject);
            }

            if (GUILayout.Button("Execute Test Method2"))
            {
                Undo.RecordObject(testScript, "MyTestMethod2");
                testScript.MyTestMethod2();
                ApplyModification(testScript.gameObject);
            }

            if (GUILayout.Button("Execute Test Method3"))
            {
                Undo.RecordObject(testScript, "MyTestMethod3");
                testScript.MyTestMethod3();
                ApplyModification(testScript.gameObject);
            }
        }

        /// <summary>
        /// 본인과 자식 오브젝트의 모든 컴포넌트에 대해서 인스턴스가 수정되었음을 기록합니다
        /// </summary>
        /// <param name="gameObject"></param>
        private void ApplyModification(GameObject gameObject)
        {
            foreach (Transform t in gameObject.GetComponentsInChildren<Transform>(true))
            {
                Component[] components = t.GetComponents<Component>();

                foreach (Component comp in components)
                {
                    if (comp != null)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
                    }
                }
            }
        }
    }
}