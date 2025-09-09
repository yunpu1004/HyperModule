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
        /// ���ΰ� �ڽ� ������Ʈ�� ��� ������Ʈ�� ���ؼ� �ν��Ͻ��� �����Ǿ����� ����մϴ�
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