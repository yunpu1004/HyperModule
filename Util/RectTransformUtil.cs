using UnityEngine;

namespace HyperModule
{
    public static class RectTransformUtil
    {
        /// <summary>
        /// RectTransform의 현재 위치를 기준으로 Canvas 상에서 화면 좌표를 계산한 후,
        /// 지정한 깊이(distanceFromCamera)를 적용하여 최종 월드 좌표를 출력합니다.
        /// </summary>
        /// <param name="safeAreaPoint">확장 메소드가 적용될 RectTransform</param>
        public static Vector3 GetWorldPosition(RectTransform safeAreaPoint)
        {
            Canvas canvas = safeAreaPoint.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.worldCamera;
            Vector3 uiWorldPosOnCanvasPlane = safeAreaPoint.position;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, uiWorldPosOnCanvasPlane);
            float distanceFromCamera = 10.0f; 
            Vector3 screenPointWithDepth = new Vector3(screenPoint.x, screenPoint.y, distanceFromCamera);
            Vector3 targetWorldPosition = uiCamera.ScreenToWorldPoint(screenPointWithDepth);
            return targetWorldPosition;
        }
    }
}