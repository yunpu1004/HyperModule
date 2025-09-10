using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyperModule
{
    public class TouchInputManager : MonoBehaviour
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

        [Tooltip("핀치 프레임별 거리 변화 노이즈 억제(px). 너무 작으면 흔들림이 로그를 가득 채울 수 있습니다.")]
        [SerializeField] private float pinchDeltaDistanceDeadzone = 0.5f;

        [SerializeField] private bool emitDragEveryFrame = true; // 드래그 중 매 프레임 OnDrag 호출
        [SerializeField] private bool logDebug = true;

        // ---- 외부에서 구독할 수 있는 이벤트들 ----
        public static Action<Vector2> OnPressed;
        public static Action<Vector2> OnTap;
        public static Action<Vector2> OnDoubleTap;
        public static Action<Vector2> OnDragStart;
        public static Action<Vector2, Vector2, Vector2> OnDrag;  // current, delta(from prev), total(from down)
        public static Action<Vector2, Vector2> OnDragEnd;        // endPos, total(from down)

        // Pinch/Spread 이벤트 (center, scale, deltaScale)
        // scale = currentDistance / initialDistance
        // deltaScale > 0 => Spread(확대), deltaScale < 0 => Pinch-in(축소)
        public static Action<Vector2> OnPinchStart;
        public static Action<Vector2, float, float> OnPinch;
        public static Action<Vector2> OnPinchEnd;

        // ---- 내부 상태 ----
        private bool isPressed;
        private bool dragging;
        private Vector2 startPos;
        private Vector2 lastPos;

        // 더블탭 판정용
        private bool lastReleaseWasTap;
        private float lastTapTime;
        private Vector2 lastTapPos;

        // 핀치 상태
        private bool pinching;
        private float pinchInitialDist;
        private float pinchPrevDist;
        private Vector2 pinchCenter;

        private void OnEnable()
        {
            press.action.performed += HandlePressed;   // finger down
            press.action.canceled  += HandleReleased;  // finger up/cancel
            press.action.Enable();

            position.action.performed += HandlePosition;
            position.action.Enable();
        }

        private void OnDisable()
        {
            press.action.performed -= HandlePressed;
            press.action.canceled  -= HandleReleased;
            press.action.Disable();

            position.action.performed -= HandlePosition;
            position.action.Disable();
        }

        private void Update()
        {
            // 1) 두 손가락 처리(핀치)가 최우선
            HandlePinch();

            // 2) 드래그 중이면 매 프레임 콜백 (핀치 중에는 드래그 중단)
            if (emitDragEveryFrame && dragging && !pinching)
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
            lastPos  = startPos;

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

            // 핀치 중이면 탭/드래그 종료 로직을 실행하지 않음(핀치 종료는 HandlePinch가 책임)
            if (pinching)
            {
                lastReleaseWasTap = false;
                return;
            }

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
                    lastTapPos  = endPos;
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

            // 핀치 중이면 1손가락 드래그 갱신 금지
            if (pinching)
            {
                lastPos = current;
                return;
            }

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

        // ---------------- Pinch / Spread ----------------

        private void HandlePinch()
        {
            // 활성화된 두 터치의 좌표를 가져옴(순서는 중요하지 않음)
            if (TryGetTwoActiveTouchPositions(out var p0, out var p1))
            {
                var center = (p0 + p1) * 0.5f;
                var dist   = Vector2.Distance(p0, p1);

                if (!pinching)
                {
                    // 드래그 중이었다면 우선 종료
                    if (dragging)
                    {
                        var endPos = lastPos;
                        EmitDragEnd(endPos, endPos - startPos);
                        dragging = false;
                    }

                    pinching = true;
                    pinchInitialDist = Mathf.Max(dist, 0.0001f);
                    pinchPrevDist    = dist;
                    pinchCenter      = center;

                    EmitPinchStart(center);
                }
                else
                {
                    pinchCenter = center;
                    var deltaDist = dist - pinchPrevDist;

                    // 노이즈 억제
                    if (Mathf.Abs(deltaDist) >= pinchDeltaDistanceDeadzone)
                    {
                        var scale      = dist / pinchInitialDist;
                        var deltaScale = (pinchPrevDist > 0f) ? deltaDist / pinchPrevDist : 0f;

                        EmitPinch(center, scale, deltaScale);

                        pinchPrevDist = dist;
                    }
                }
            }
            else
            {
                if (pinching)
                {
                    EmitPinchEnd(pinchCenter);
                    pinching = false;

                    // 핀치가 끝났고 첫 손가락이 여전히 눌려있다면,
                    // 이후 드래그/탭 판정을 위해 기준점을 현재 위치로 재설정(선택적)
                    if (isPressed)
                    {
                        startPos = position.action.ReadValue<Vector2>();
                        lastPos  = startPos;
                    }
                }
            }
        }

        private bool TryGetTwoActiveTouchPositions(out Vector2 p0, out Vector2 p1)
        {
            p0 = default; p1 = default;

            var ts = Touchscreen.current;
            if (ts == null) return false;

            int found = 0;
            var touches = ts.touches; // ReadOnlyArray<TouchControl>
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.press.isPressed)
                {
                    var pos = t.position.ReadValue();
                    if (found == 0) p0 = pos;
                    else if (found == 1) p1 = pos;
                    found++;
                    if (found >= 2) return true;
                }
            }
            return false;
        }

        // ---- Emit & 로그 ----
        private void EmitPressed   (Vector2 p) { if (logDebug) Debug.Log($"[Pressed] {p}"); OnPressed?.Invoke(p); }
        private void EmitTap       (Vector2 p) { if (logDebug) Debug.Log($"[Tap] {p}");     OnTap?.Invoke(p); }
        private void EmitDoubleTap (Vector2 p) { if (logDebug) Debug.Log($"[DoubleTap] {p}"); OnDoubleTap?.Invoke(p); }
        private void EmitDragStart (Vector2 p) { if (logDebug) Debug.Log($"[DragStart] {p}"); OnDragStart?.Invoke(p); }
        private void EmitDrag      (Vector2 c, Vector2 d, Vector2 t)
        { if (logDebug) Debug.Log($"[Drag] pos:{c} delta:{d} total:{t}"); OnDrag?.Invoke(c, d, t); }
        private void EmitDragEnd   (Vector2 e, Vector2 t)
        { if (logDebug) Debug.Log($"[DragEnd] end:{e} total:{t}"); OnDragEnd?.Invoke(e, t); }

        private void EmitPinchStart(Vector2 c)
        {
            if (logDebug) Debug.Log($"[PinchStart] center:{c}");
            OnPinchStart?.Invoke(c);
        }

        private void EmitPinch(Vector2 c, float scale, float deltaScale)
        {
            if (logDebug)
            {
                string mode = deltaScale > 0f ? "Spread(+)" : (deltaScale < 0f ? "Pinch(-)" : "Neutral");
                Debug.Log($"[Pinch] center:{c} scale:{scale:F3} dScale:{deltaScale:F3} {mode}");
            }
            OnPinch?.Invoke(c, scale, deltaScale);
        }

        private void EmitPinchEnd(Vector2 c)
        {
            if (logDebug) Debug.Log($"[PinchEnd] center:{c}");
            OnPinchEnd?.Invoke(c);
        }
    }
}
