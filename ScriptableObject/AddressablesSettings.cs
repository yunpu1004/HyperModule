using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// Addressables 관련 라벨 문자열만 보관하는 설정 오브젝트
    /// </summary>
    [CreateAssetMenu(fileName = "AddressablesSettings", menuName = "HyperModule/Addressables Settings")]
    public class AddressablesSettings : ScriptableObject
    {
        [Tooltip("Addressables 라벨 문자열 목록")]
        public string[] labels;
    }
}
