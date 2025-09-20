using UnityEngine;
using Cysharp.Threading.Tasks;

namespace HyperModule
{
    public static class BeforeSplashScreenInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            ExcelDictionaryManager.Init();
            AddressablesManager.Init().Forget();
        }
    }
}
