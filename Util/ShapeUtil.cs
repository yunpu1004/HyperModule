using UnityEngine;

namespace HyperModule
{
    public static class ShapeUtil
    {
        /// <summary>
        /// 축에 평행한 직사각형과 원의 겹치는 영역 넓이를 근사적으로 계산합니다.
        /// 수치 적분(격자 샘플링)을 사용하여 근사값을 구합니다.
        /// </summary>
        /// <param name="rectMin">사각형의 최소 코너 (월드 좌표)</param>
        /// <param name="rectMax">사각형의 최대 코너 (월드 좌표)</param>
        /// <param name="circleCenter">원의 중심 (월드 좌표)</param>
        /// <param name="circleRadius">원의 반지름</param>
        /// <param name="resolution">사각형 내 축별 샘플링 수 (정밀도)</param>
        /// <returns>겹치는 영역의 근사 넓이. 겹치지 않거나 닿기만 하면 0을 반환.</returns>
        public static float CalculateRectangleCircleIntersectionArea(Vector2 rectMin, Vector2 rectMax, Vector2 circleCenter, float circleRadius, int resolution = 100)
        {
            // --- 기본 유효성 검사 ---
            if (circleRadius <= 0f || rectMin.x >= rectMax.x || rectMin.y >= rectMax.y || resolution <= 0) return 0f;

            // --- 1단계: 빠른 겹침 판정 (최적화) ---
            float closestX = Mathf.Clamp(circleCenter.x, rectMin.x, rectMax.x);
            float closestY = Mathf.Clamp(circleCenter.y, rectMin.y, rectMax.y);
            Vector2 closestPointToCircle = new Vector2(closestX, closestY);
            float distanceSquared = Vector2.SqrMagnitude(circleCenter - closestPointToCircle);
            float radiusSquared = circleRadius * circleRadius;

            if (distanceSquared >= radiusSquared)
            {
                if (Mathf.Approximately(distanceSquared, radiusSquared)) return 0f; // 닿기만 한 상태
                if (distanceSquared > radiusSquared) return 0f; // 겹치지 않는 상태
            }

            // --- 2단계: 완전 포함 케이스 (최적화) ---
            Vector2 corner1 = rectMin;
            Vector2 corner2 = new Vector2(rectMax.x, rectMin.y);
            Vector2 corner3 = rectMax;
            Vector2 corner4 = new Vector2(rectMin.x, rectMax.y);

            bool cornersInside =
                Vector2.SqrMagnitude(corner1 - circleCenter) <= radiusSquared &&
                Vector2.SqrMagnitude(corner2 - circleCenter) <= radiusSquared &&
                Vector2.SqrMagnitude(corner3 - circleCenter) <= radiusSquared &&
                Vector2.SqrMagnitude(corner4 - circleCenter) <= radiusSquared;

            if (cornersInside) return (rectMax.x - rectMin.x) * (rectMax.y - rectMin.y); // 사각형 넓이

            bool circleInsideRect =
                circleCenter.x - circleRadius >= rectMin.x &&
                circleCenter.x + circleRadius <= rectMax.x &&
                circleCenter.y - circleRadius >= rectMin.y &&
                circleCenter.y + circleRadius <= rectMax.y;

            if (circleInsideRect) return Mathf.PI * radiusSquared; // 원 넓이

            // --- 3단계: 수치 적분 (격자 샘플링) ---
            float intersectionArea = 0f;
            float rectWidth = rectMax.x - rectMin.x;
            float rectHeight = rectMax.y - rectMin.y;

            // 너비나 높이가 0인 경우 방지
            if (rectWidth <= 0 || rectHeight <= 0) return 0f;

            float stepX = rectWidth / resolution;
            float stepY = rectHeight / resolution;
            float cellArea = stepX * stepY;

            for (int i = 0; i < resolution; i++)
            {
                float cellCenterX = rectMin.x + (i + 0.5f) * stepX;
                for (int j = 0; j < resolution; j++)
                {
                    float cellCenterY = rectMin.y + (j + 0.5f) * stepY;
                    Vector2 cellCenter = new Vector2(cellCenterX, cellCenterY);

                    // 셀 중심이 원 내부에 있는지 확인 (경계 제외)
                    if (Vector2.SqrMagnitude(cellCenter - circleCenter) < radiusSquared)
                    {
                        intersectionArea += cellArea;
                    }
                }
            }

            return intersectionArea;
        }

        // CalculateRectangleCircleIntersectionArea의 사각형 좌표를 Rect로 받는 버전
        public static float CalculateRectangleCircleIntersectionArea(Rect rect, Vector2 circleCenter, float circleRadius, int resolution = 100)
        {
            return CalculateRectangleCircleIntersectionArea(rect.min, rect.max, circleCenter, circleRadius, resolution);
        }

        // CalculateRectangleCircleIntersectionArea의 사각형 좌표를 Bounds로 받는 버전
        public static float CalculateRectangleCircleIntersectionArea(Bounds bounds, Vector2 circleCenter, float circleRadius, int resolution = 100)
        {
            return CalculateRectangleCircleIntersectionArea(bounds.min, bounds.max, circleCenter, circleRadius, resolution);
        }
    }
}