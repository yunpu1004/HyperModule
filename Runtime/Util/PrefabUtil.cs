#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HyperModule
{
    public static class PrefabUtil
    {
        public static void RecordPrefabModifications(GameObject prefab)
        {
            // 프리팹과 모든 자식 오브젝트의 컴포넌트를 배열로 가져옵니다.
            var components = prefab.GetComponentsInChildren<Component>(true);

            // 프리팹과 모든 자식 오브젝트의 GameObject를 배열로 가져옵니다.
            var gameObjects = prefab.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();

            // 두 배열을 UnityEngin.Object 배열로 합칩니다.
            var objects = components.Cast<UnityEngine.Object>().Concat(gameObjects.Cast<UnityEngine.Object>()).ToArray();

            // 배열을 순회하며 프리팹에 대한 수정사항을 기록합니다.
            foreach (var obj in objects)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
#endif