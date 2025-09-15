using UnityEngine;

namespace HyperModule
{
    public static class Collider2dUtil
    {
        /// <summary>
        /// BoxCollider2D의 네 꼭짓점의 월드 좌표를 반환합니다.
        /// 이 메소드는 BoxCollider2D가 연결된 Transform 및 모든 부모 Transform의
        /// 위치, 회전, 스케일을 고려합니다.
        /// </summary>
        /// <param name="collider">월드 좌표를 계산할 BoxCollider2D</param>
        /// <returns>
        /// 네 꼭짓점의 월드 좌표 배열 (Vector2[4]).
        /// 순서는 로컬 좌표 정의에 따라 Top-Right, Top-Left, Bottom-Left, Bottom-Right 입니다.
        /// collider가 null이면 null을 반환합니다.
        /// </returns>
        public static Vector2[] GetBoxColliderWorldCorners(BoxCollider2D collider)
        {
            if (collider == null) return null;

            Transform tx = collider.transform;
            Vector2 size = collider.size;
            Vector2 offset = collider.offset;
            Vector2 halfSize = size * 0.5f;

            Vector2[] localCorners = new Vector2[4];
            localCorners[0] = offset + new Vector2(halfSize.x, halfSize.y);
            localCorners[1] = offset + new Vector2(-halfSize.x, halfSize.y);
            localCorners[2] = offset + new Vector2(-halfSize.x, -halfSize.y);
            localCorners[3] = offset + new Vector2(halfSize.x, -halfSize.y);

            Vector2[] worldCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = tx.TransformPoint(localCorners[i]);
            }

            return worldCorners;
        }
    }
}
