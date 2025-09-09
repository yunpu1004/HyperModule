using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperModule
{
    /// <summary>
    /// 씬 전환을 관리하는 static 클래스입니다.
    /// </summary>
    public static class SceneLoadManager
    {
        /// <summary>
        /// 현재 활성화된 씬의 이름입니다.
        /// </summary>
        public static string currentSceneName => SceneManager.GetActiveScene().name;

        /// <summary>
        /// 다음에 로드될 씬의 이름입니다. (로딩 중일 때만 유효하고, 로딩이 완료된 후 또는 로딩 중이 아닐 때는 null을 반환합니다.)
        /// </summary>
        public static string nextSceneName => isSceneLoading ? _nextSceneName : null;
        private static string _nextSceneName;

        /// <summary>
        /// 씬 로딩 상태를 나타냅니다. 로딩 중일 때 true입니다.
        /// </summary>
        public static bool isSceneLoading => currentSceneName != _nextSceneName && !string.IsNullOrEmpty(_nextSceneName);

        /// <summary>
        /// 지정된 이름의 씬을 동기적으로 로드합니다.
        /// </summary>
        /// <param name="name">로드할 씬의 이름 또는 경로</param>
        public static void LoadScene(string name)
        {
            if (isSceneLoading)
            {
                QAUtil.LogWarning($"씬 로딩이 이미 진행 중입니다 ('{nextSceneName}'). '{name}' 로드 요청을 무시합니다.");
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                QAUtil.LogError("로드할 씬 이름이 비어있습니다.");
                return;
            }

            if (name == currentSceneName)
            {
                QAUtil.LogWarning($"현재 활성화된 씬 '{name}'을(를) 다시 로드하려고 합니다.");
                return;
            }

            QAUtil.Log($"SceneController: 씬 '{name}' 로드를 시작합니다");

            _nextSceneName = name;

            try
            {
                SceneManager.LoadScene(name);
            }
            catch (System.Exception ex)
            {
                QAUtil.LogError($"씬 '{name}' 로드 실패. 오류: {ex.Message}");
                // 실패 시 상태 초기화
                _nextSceneName = null;
            }
        }
    }
}
