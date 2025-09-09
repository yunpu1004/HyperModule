using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HyperModule
{
    [AddComponentMenu("UI/Extensions/ScrollRectEx")]
    public class ScrollRectEx : ScrollRect
    {
        private bool routeToParent = false;

        [Tooltip("스크롤 감도입니다. 1이 기본값이며, 낮을수록 둔감하고 높을수록 민감해집니다.")]
        public float dragSensitivity = 1f;

        public Action onEndDrag;
        public Action onBeginDrag;

        private Vector2 pointerStartLocalCursor = Vector2.zero; // 드래그 시작 시점의 실제 포인터 위치를 저장할 변수

        /// <summary>
        /// Do action for all parents
        /// </summary>
        private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                foreach (var component in parent.GetComponents<Component>())
                {
                    if (component is T)
                        action((T)(IEventSystemHandler)component);
                }
                parent = parent.parent;
            }
        } 

        /// <summary>
        /// Always route initialize potential drag event to parents
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            DoForParents<IInitializePotentialDragHandler>((parent) => { parent.OnInitializePotentialDrag(eventData); });
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// Begin drag event
        /// </summary>
        public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
                routeToParent = true;
            else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
                routeToParent = true;
            else
                routeToParent = false;

            if (routeToParent)
            {
                DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(eventData); });
            }
            else
            {
                // 부모의 OnBeginDrag를 먼저 호출하여 내부 변수(m_ContentStartPosition 등)를 초기화합니다.
                base.OnBeginDrag(eventData);
                // 드래그 시작 시점의 실제 포인터 위치를 저장합니다.
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out pointerStartLocalCursor);
            }

            if (onBeginDrag != null)
                onBeginDrag();
        }

        /// <summary>
        /// Drag event
        /// </summary>
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (routeToParent)
            {
                DoForParents<IDragHandler>((parent) => { parent.OnDrag(eventData); });
            }
            else
            {
                // 현재 포인터의 로컬 위치를 계산합니다.
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                    return;

                // 실제 드래그된 거리를 계산합니다.
                var pointerDelta = localCursor - pointerStartLocalCursor;
                // 실제 드래그된 거리에 감도를 적용합니다.
                var modifiedPointerDelta = pointerDelta * dragSensitivity;

                // 감도가 적용된 거리만큼 content의 위치를 계산합니다.
                // base.OnBeginDrag에서 저장된 m_ContentStartPosition 값을 사용합니다.
                Vector2 newPosition = m_ContentStartPosition + modifiedPointerDelta;

                // 계산된 새 위치를 content의 anchoredPosition에 직접 적용합니다.
                // 이렇게 하면 관성(inertia)과 탄성(elasticity) 효과가 사라지므로,
                // 이 효과들을 유지하기 위해 다른 방법을 사용합니다. (아래 설명 참조)

                // --- 관성과 탄성을 유지하는 더 나은 방법 ---
                // 1. 실제 드래그 시작 위치와 현재 위치의 차이를 구합니다.
                Vector2 originalPointerPosition = eventData.position;
                Vector2 dragVector = originalPointerPosition - (Vector2)eventData.pressPosition; // pressPosition을 사용하거나 OnBeginDrag에서 저장한 위치 사용

                // 2. 감도를 적용한 가상의 포인터 위치를 만듭니다.
                eventData.position = (Vector2)eventData.pressPosition + dragVector * dragSensitivity;

                // 3. 가상의 위치 정보로 부모의 OnDrag를 호출합니다.
                base.OnDrag(eventData);

                // 4. 다른 UI 요소에 영향을 주지 않도록 eventData의 위치를 원래대로 되돌립니다.
                eventData.position = originalPointerPosition;
            }
        }

        /// <summary>
        /// End drag event
        /// </summary>
        public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (routeToParent)
                DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(eventData); });
            else
                base.OnEndDrag(eventData);
            routeToParent = false;

            if (onEndDrag != null)
                onEndDrag();
        }

        /// <summary>
        /// 마우스 휠 스크롤 이벤트
        /// </summary>
        public override void OnScroll(PointerEventData eventData)
        {
            // OnScroll은 eventData.scrollDelta를 사용하므로 이 값을 변경해야 합니다.
            // 또한, 감도 조절이 필요하다면 dragSensitivity 대신 별도의 sensitivity 변수를 사용하는 것이 좋습니다.
            // eventData.scrollDelta *= scrollSensitivity; 

            if (!horizontal && Math.Abs(eventData.scrollDelta.x) > Math.Abs(eventData.scrollDelta.y))
            {
                routeToParent = true;
            }
            else if (!vertical && Math.Abs(eventData.scrollDelta.x) < Math.Abs(eventData.scrollDelta.y))
            {
                routeToParent = true;
            }
            else
            {
                routeToParent = false;
            }

            if (routeToParent)
                DoForParents<IScrollHandler>((parent) => {
                    parent.OnScroll(eventData);
                });
            else
                base.OnScroll(eventData);
        }
    }
}