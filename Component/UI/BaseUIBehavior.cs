using UnityEngine;
using UnityEngine.EventSystems;

namespace HyperModule
{
    public abstract class BaseUIBehavior : UIBehaviour
    {
        public Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>(true);
                }
                return _canvas;
            }
        }
        private Canvas _canvas;

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

        /// <summary>
        /// Canvas가 활성화되는 경우에 호출됩니다. 
        /// <br/> 만약 Awake가 아직 호출되지 않았다면, 이 메서드는 호출되지 않습니다.
        /// </summary>
        protected abstract void OnCanvasActiveAndEnabled();

        /// <summary>
        /// Canvas가 비활성화되는 경우에 호출됩니다. 
        /// <br/> 만약 Awake가 아직 호출되지 않았다면, 이 메서드는 호출되지 않습니다.
        /// </summary>
        protected abstract void OnCanvasInactiveOrDisabled();

        protected override void OnCanvasHierarchyChanged()
        {
            if(canvas == null) return;
            if(!didAwake) return;

            if (canvas.isActiveAndEnabled)
            {
                OnCanvasActiveAndEnabled();
            }
            else
            {
                OnCanvasInactiveOrDisabled();
            }
        }
    }
}