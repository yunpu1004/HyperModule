using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;


namespace HyperModule
{
    public class Test : MonoBehaviour
    {
        protected void Start()
        {
            QAUtil.Log("QA Symbol Active");
        }

        public void MyTestMethod()
        {


        }

        public void MyTestMethod2()
        {
        }

        public void MyTestMethod3()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}