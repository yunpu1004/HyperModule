
namespace HyperModule
{
    public static class QAUtil
    {
        public static void Log(string message)
        {
#if QA
        Debug.Log($"[QA Log] {message}");
#endif
        }

        public static void LogWarning(string message)
        {
#if QA
        Debug.LogWarning($"[QA Warning] {message}");
#endif
        }

        public static void LogError(string message)
        {
#if QA
        Debug.LogError($"[QA Error] {message}");
#endif
        }
    }
}