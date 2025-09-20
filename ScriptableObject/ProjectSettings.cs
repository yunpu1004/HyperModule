using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// 프로젝트 전역 설정: Addressables 라벨과 Excel 파일 경로들을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectSettings", menuName = "HyperModule/Project Settings")]
    public class ProjectSettings : ScriptableObject
    {
        [Tooltip("Addressables 라벨 문자열 목록")]
        public string[] addressablesLabels;

        [Tooltip("Resources 경로 기준의 Excel(.bytes) 파일 경로 목록")]
        public string[] excelFilePaths;
    }
}
