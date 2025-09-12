using UnityEngine.EventSystems;

namespace HyperModule
{
    public abstract class BaseUIBehavior : UIBehaviour
    {
        public virtual void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public abstract void Refresh();
    }
}