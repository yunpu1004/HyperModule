using UnityEditor;
using UnityEngine;

namespace HyperModule
{
    [InitializeOnLoad]
    public static class SelectParentObject
    {
        static SelectParentObject()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }


        static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            // Ctrl + 윗 방향 화살표 키 조합 감지
            if (e != null && e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.UpArrow)
            {
                SelectParent();
                e.Use(); // 이벤트가 다른 곳에서 처리되지 않도록 합니다.
            }
        }

        static void SelectParent()
        {
            if (Selection.activeGameObject != null)
            {
                Transform parent = Selection.activeGameObject.transform.parent;
                if (parent != null)
                {
                    Selection.activeGameObject = parent.gameObject;
                }
                else
                {
                    Debug.Log("선택한 오브젝트는 부모가 없습니다.");
                }
            }
            else
            {
                Debug.Log("선택된 오브젝트가 없습니다.");
            }
        }
    }
}