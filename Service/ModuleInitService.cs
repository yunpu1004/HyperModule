using UnityEngine;
using Cysharp.Threading.Tasks;

namespace HyperModule
{
    public class ModuleInitService : IService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            new ModuleInitService().Execute();
        }

        public void Execute()
        {
            QAUtil.Log("=== ModuleInitService ===");
            ExcelDictionaryManager.Init();
            AddressablesManager.Init().Forget();
            TouchInputManager.Init();
        }
    }
}
