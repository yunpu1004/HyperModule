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
            ReactiveProperty<float> reactiveProperty = new ReactiveProperty<float>(0f);
            Observable<float> floatObservable = reactiveProperty.AsObservable();
            Observable<object> objectObservable = floatObservable.Select(value => (object)value);

            floatObservable.Subscribe(value => Debug.Log($"Observable<float> value: {value}"));
            objectObservable.Subscribe(value => Debug.Log($"Observable<object> value: {value}"));

            reactiveProperty.Value = 1.23f;
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
