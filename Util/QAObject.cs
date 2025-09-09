using UnityEngine;

namespace HyperModule
{
    public class QAObject : MonoBehaviour
    {
        private void Awake()
        {
#if !QA
            gameObject.SetActive(false);
#endif
        }
    }
}