using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;


namespace HyperModule
{
    public class Test : MonoBehaviour
    {
        public void MyTestMethod()
        {
            Debug.Log("Hello");
        }

        public void MyTestMethod2()
        {
        }

        public void MyTestMethod3()
        {
            UniTask.Void(async () =>
            {
                await AddressablesManager.Init();
                Debug.Log(AddressablesManager.Get<GameObject>("Cube"));
                await AddressablesManager.ReleaseAll();
                Debug.Log("Released all assets");
            });

        }
    }
}
