using UnityEngine;
using UnityEngine.UI;

namespace HyperModule
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridLayoutFitter : MonoBehaviour
    {
        // 기준 해상도 비율과 해당 셀 사이즈를 인스펙터에서 설정
        [SerializeField] private float referenceRatio1 = 0.45f;  // 기준 낮은 해상도 비율
        [SerializeField] private Vector2 cellSize1 = new Vector2(200, 280);

        [SerializeField] private float referenceRatio2 = 0.75f;  // 기준 높은 해상도 비율
        [SerializeField] private Vector2 cellSize2 = new Vector2(200, 215);

        // 현재 화면 해상도와 비율을 저장할 필드
        private float screenWidth;
        private float screenHeight;
        private float screenRatio;

        private GridLayoutGroup gridLayout;

        private void Awake()
        {
            // 현재 화면 해상도 및 가로/세로 비율 계산
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            screenRatio = screenWidth / screenHeight;

            gridLayout = GetComponent<GridLayoutGroup>();

            // 기준 비율 범위에 상관없이 보간을 위한 t 값 계산
            // 기준 범위 밖이라면 t가 0 미만 또는 1 초과의 값을 가질 수 있음
            float t = (screenRatio - referenceRatio1) / (referenceRatio2 - referenceRatio1);

            // LerpUnclamped를 사용하여 t 값에 따라 보간 (t가 0~1 범위를 벗어나면 보간 결과도 외삽됨)
            Vector2 interpolatedCellSize = Vector2.LerpUnclamped(cellSize1, cellSize2, t);

            gridLayout.cellSize = interpolatedCellSize;
        }
    }
}