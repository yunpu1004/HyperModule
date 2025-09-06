using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyperModule
{

    public class TouchInputHandler : MonoBehaviour
    {
        [Header("Actions (2)")]
        [SerializeField] private InputActionReference press;     // Button: <Touchscreen>/primaryTouch/press
        [SerializeField] private InputActionReference position;  // PassThrough Vector2: <Touchscreen>/primaryTouch/position

        [Header("Config")]
        [Tooltip("드래그 시작으로 인정하는 이동 픽셀(탭 허용 이동량과 동일하게 두면 데드존이 사라집니다).")]
        [SerializeField] private float dragStartThreshold = 10f;

        [Tooltip("두 번 탭으로 인식하는 최대 간격(초). 0 이하면 더블탭 비활성화.")]
        [SerializeField] private float doubleTapMaxDelay = 0.35f;

        [Tooltip("두 번 탭 사이 허용 위치 오차(픽셀).")]
        [SerializeField] private float doubleTapMaxDistance = 40f;

        [SerializeField] private bool emitDragEveryFrame = true; // 드래그 중 매 프레임 OnDrag 호출
        [SerializeField] private bool logDebug = true;

        // 외부에서 구독할 수 있는 이벤트들
        public static Action<Vector2> OnPressed;
        public static Action<Vector2> OnTap;
        public static Action<Vector2> OnDoubleTap;
        public static Action<Vector2> OnDragStart;
        public static Action<Vector2, Vector2, Vector2> OnDrag;  // current, delta(from prev), total(from down)
        public static Action<Vector2, Vector2> OnDragEnd;        // endPos, total(from down)

        private bool isPressed;
        private bool dragging;
        private Vector2 startPos;
        private Vector2 lastPos;

        // 더블탭 판정용
        private bool lastReleaseWasTap;
        private float lastTapTime;
        private Vector2 lastTapPos;

        private void OnEnable()
        {
            press.action.performed += HandlePressed;   // finger down
            press.action.canceled += HandleReleased;  // finger up/cancel
            press.action.Enable();

            position.action.performed += HandlePosition;
            position.action.Enable();
        }

        private void OnDisable()
        {
            press.action.performed -= HandlePressed;
            press.action.canceled -= HandleReleased;
            press.action.Disable();

            position.action.performed -= HandlePosition;
            position.action.Disable();
        }

        private void Update()
        {
            // 드래그 중에는 매 프레임 OnDrag 호출 (움직이지 않아도 호출)
            if (emitDragEveryFrame && dragging)
            {
                var current = position.action.ReadValue<Vector2>();
                var delta = current - lastPos;
                var total = current - startPos;
                EmitDrag(current, delta, total);
                lastPos = current;
            }
        }

        private void HandlePressed(InputAction.CallbackContext ctx)
        {
            isPressed = true;
            dragging = false;
            startPos = ReadPointerPosition(ctx);
            lastPos = startPos;

            EmitPressed(startPos);
        }

        private Vector2 ReadPointerPosition(InputAction.CallbackContext ctx)
        {
            // 1) 이번 입력을 낸 '같은' 포인터 디바이스에서 바로 읽기
            if (ctx.control?.device is Pointer ptr)
                return ptr.position.ReadValue();

            // 2) 보조: 현재 활성 포인터
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();

            // 3) 최후: 장치별 직접/액션값
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return position.action.ReadValue<Vector2>();
        }

        private void HandleReleased(InputAction.CallbackContext _)
        {
            isPressed = false;

            var endPos = position.action.ReadValue<Vector2>();
            var moved = Vector2.Distance(endPos, startPos);

            // 드래그 중이면 종료만
            if (dragging)
            {
                var total = endPos - startPos;
                EmitDragEnd(endPos, total);
                dragging = false;
                lastReleaseWasTap = false;
                return;
            }

            // 드래그가 아니라면: Tap vs DoubleTap 중 하나만!
            if (moved <= dragStartThreshold)
            {
                var now = Time.unscaledTime;

                // "이번 릴리즈"가 더블탭 후보인지 먼저 검사
                bool canDouble =
                    doubleTapMaxDelay > 0f &&
                    lastReleaseWasTap &&
                    (now - lastTapTime) <= doubleTapMaxDelay &&
                    Vector2.Distance(endPos, lastTapPos) <= doubleTapMaxDistance;

                if (canDouble)
                {
                    // 두 번째 탭: DoubleTap만 발생
                    EmitDoubleTap(endPos);
                    lastReleaseWasTap = false;   // 더블탭 후에는 초기화
                    lastTapTime = 0f;
                }
                else
                {
                    // 첫 번째 탭 또는 간격/거리 초과: Tap만 발생
                    EmitTap(endPos);
                    lastReleaseWasTap = true;
                    lastTapTime = now;
                    lastTapPos = endPos;
                }
            }
            else
            {
                // 임계 이상 움직였는데 DragStart 전에 뗀 경우: 아무 이벤트도 아님
                lastReleaseWasTap = false;
            }
        }

        private void HandlePosition(InputAction.CallbackContext ctx)
        {
            if (!isPressed) return;

            var current = ctx.ReadValue<Vector2>();
            var movedFromStart = Vector2.Distance(current, startPos);

            // 최초 이동이 임계치 이상이면 드래그 시작
            if (!dragging && movedFromStart >= dragStartThreshold)
            {
                dragging = true;
                EmitDragStart(current);

                // 매 프레임 모드가 아니면 이 시점에도 한 번 OnDrag 내보내기
                if (!emitDragEveryFrame)
                {
                    var delta = current - lastPos;
                    var total = current - startPos;
                    EmitDrag(current, delta, total);
                }
            }

            if (!emitDragEveryFrame && dragging)
            {
                var delta = current - lastPos;
                var total = current - startPos;
                if (delta.sqrMagnitude > 0f)
                    EmitDrag(current, delta, total);
            }

            lastPos = current;
        }

        // ---- Emit & 로그 ----
        private void EmitPressed(Vector2 p) { if (logDebug) Debug.Log($"[Pressed] {p}"); OnPressed?.Invoke(p); }
        private void EmitTap(Vector2 p) { if (logDebug) Debug.Log($"[Tap] {p}"); OnTap?.Invoke(p); }
        private void EmitDoubleTap(Vector2 p) { if (logDebug) Debug.Log($"[DoubleTap] {p}"); OnDoubleTap?.Invoke(p); }
        private void EmitDragStart(Vector2 p) { if (logDebug) Debug.Log($"[DragStart] {p}"); OnDragStart?.Invoke(p); }
        private void EmitDrag(Vector2 c, Vector2 d, Vector2 t) { if (logDebug) Debug.Log($"[Drag] pos:{c} delta:{d} total:{t}"); OnDrag?.Invoke(c, d, t); }
        private void EmitDragEnd(Vector2 e, Vector2 t) { if (logDebug) Debug.Log($"[DragEnd] end:{e} total:{t}"); OnDragEnd?.Invoke(e, t); }
    }
}