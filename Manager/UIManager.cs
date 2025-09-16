using UnityEngine;

namespace HyperModule
{
    public static class UIManager
    {
        public static void ShowUI(string uiName)
        {
            Debug.Log($"Showing UI: {uiName}");
        }

        public static void HideUI(string uiName)
        {
            Debug.Log($"Hiding UI: {uiName}");
        }
    }
}