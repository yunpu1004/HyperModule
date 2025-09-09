using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HyperModule
{
    public class Carousel : MonoBehaviour
    {
        [SerializeField] private ScrollRectEx scroll; // 커스텀 ScrollRect
        [SerializeField] private Button leftButton; // 왼쪽 이동 버튼
        [SerializeField] private Button rightButton; // 오른쪽 이동 버튼
        [SerializeField] private ToggleGroup toggleGroup; // 토글 그룹

        public float autoScrollTime = 7f; // 자동 스크롤 시간 간격
        public bool isVertical = false; // 수직 스크롤 여부
        public float transitionSpeed = 0.005f; // 전환 속도

        private float elapsedTime = 0f; // 경과 시간
        private int currentFocus = 0; // 현재 포커스 인덱스
        private bool onTouching = false; // 터치 중 여부
        private List<Toggle> toggles = new List<Toggle>(); // 토글 목록 (ToggleGroup에서 동적으로 가져옴)

        private bool isUpdatingToggle = false; // 재귀 호출 방지를 위한 플래그

        private void Awake()
        {
            if (toggleGroup != null) toggleGroup.allowSwitchOff = false;
            if (leftButton != null) leftButton.onClick.AddListener(() => MoveFocusIndex(-1));
            if (rightButton != null) rightButton.onClick.AddListener(() => MoveFocusIndex(1));

            scroll.onBeginDrag = OnScrollBegin;
            scroll.onEndDrag = OnScrollEnd;


            toggles = toggleGroup.GetComponentsInChildren<Toggle>().ToList();

            // 캐러셀에 포함된 오브젝트 개수에 따라 토글을 활성화
            int liveChildCount = GetLiveChildCount();
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].gameObject.SetActive(i < liveChildCount);
            }

            // 사용자 상호작용을 위한 토글 리스너 추가
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isUpdatingToggle) return; // 재귀 호출 방지

                    if (isOn)
                    {
                        int index = toggles.IndexOf(toggle);
                        MoveFocusIndex(index - currentFocus);
                    }
                    else
                    {
                        // 토글이 비활성화되지 않도록 다시 활성화
                        isUpdatingToggle = true;
                        toggle.isOn = true;
                        isUpdatingToggle = false;
                    }
                });
            }

            // 첫 번째 토글 초기화
            UpdateToggle();
        }

        /// <summary>
        /// 스크롤 드래그 시작 시 호출됩니다.
        /// </summary>
        void OnScrollBegin()
        {
            onTouching = true;
        }

        /// <summary>
        /// 스크롤 드래그 종료 시 호출됩니다.
        /// 현재 포커스를 계산하고 토글을 업데이트합니다.
        /// </summary>
        void OnScrollEnd()
        {
            int liveChildCount = GetLiveChildCount();
            float scrollPosition = scroll.normalizedPosition.x * (liveChildCount - 1);

            if (scrollPosition > currentFocus)
            {
                scrollPosition += 0.15f; // 오른쪽으로 스크롤 시 포커스 증가
            }
            else if (scrollPosition < currentFocus)
            {
                scrollPosition -= 0.15f; // 왼쪽으로 스크롤 시 포커스 감소
            }

            currentFocus = Mathf.RoundToInt(scrollPosition);

            // 포커스 인덱스 클램핑
            currentFocus = Mathf.Clamp(currentFocus, 0, liveChildCount - 1);

            UpdateToggle();
            onTouching = false;
        }

        /// <summary>
        /// 활성화된 자식(캐러셀 아이템)의 수를 반환합니다.
        /// </summary>
        /// <returns>활성화된 자식의 수</returns>
        int GetLiveChildCount()
        {
            int lives = 0;
            for (int i = 0; i < scroll.content.childCount; i++)
            {
                Transform child = scroll.content.GetChild(i);
                if (child == null) continue;
                if (!child.gameObject.activeSelf) continue;
                lives++;
            }
            return lives;
        }

        /// <summary>
        /// 포커스 인덱스를 증가 또는 감소시킵니다.
        /// </summary>
        /// <param name="increase">증가시킬 값 (음수는 감소)</param>
        public void MoveFocusIndex(int increase)
        {
            int liveChildCount = GetLiveChildCount();
            currentFocus += increase;

            if (currentFocus < 0) currentFocus = liveChildCount - 1;
            if (currentFocus >= liveChildCount) currentFocus = 0;

            elapsedTime = 0f;
            UpdateToggle();
        }

        private void Update()
        {
            int liveChildCount = GetLiveChildCount();
            elapsedTime = onTouching ? 0f : elapsedTime + Time.deltaTime;
            currentFocus = Mathf.Clamp(currentFocus, 0, liveChildCount - 1);

            if (elapsedTime > autoScrollTime)
            {
                MoveFocusIndex(1);
                elapsedTime = 0f;
            }

            if (!onTouching)
            {
                float expectedNormalizedPos = currentFocus / Mathf.Max((float)liveChildCount - 1, 1f);

                Vector2 targetPosition = isVertical
                    ? new Vector2(scroll.normalizedPosition.x, expectedNormalizedPos)
                    : new Vector2(expectedNormalizedPos, scroll.normalizedPosition.y);

                scroll.normalizedPosition = Vector2.Lerp(scroll.normalizedPosition, targetPosition, transitionSpeed);
            }
        }

        /// <summary>
        /// 현재 포커스에 맞춰 토글을 업데이트합니다.
        /// 해당 포커스의 토글을 활성화하고 나머지는 비활성화합니다.
        /// </summary>
        private void UpdateToggle()
        {
            if (toggles == null || toggles.Count == 0) return;

            // 현재 포커스가 토글 목록 내에 있는지 확인
            currentFocus = Mathf.Clamp(currentFocus, 0, toggles.Count - 1);

            // 토글 업데이트 시 재귀 호출 방지
            isUpdatingToggle = true;
            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i] != null)
                {
                    toggles[i].isOn = (i == currentFocus);
                }
            }
            isUpdatingToggle = false;
        }
    }
}