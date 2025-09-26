using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace HyperModule
{
    [AddComponentMenu("HyperModule/Touch Input Manager")]
    [DefaultExecutionOrder(-90)]
    public class TouchInputManager : MonoBehaviour
    {
        [Header("Actions (2)")]
        [SerializeField] private InputActionReference press;     // Pointer/press (Button)
        [SerializeField] private InputActionReference position;  // Pointer/position (Pass Through, Vector2 권장)

        [Header("Config")]
        [Tooltip("드래그 시작으로 인정하는 이동 픽셀(탭 허용 이동량과 동일하게 두면 데드존이 사라집니다).")]
        [SerializeField] private float dragStartThreshold = 10f;

        [Tooltip("두 번 탭으로 인식하는 최대 간격(초). 0 이하면 더블탭 비활성화.")]
        [SerializeField] private float doubleTapMaxDelay = 0.35f;

        [Tooltip("두 번 탭 사이 허용 위치 오차(픽셀).")]
        [SerializeField] private float doubleTapMaxDistance = 40f;

        [Tooltip("핀치 프레임별 거리 변화 노이즈 억제(px). 너무 작으면 흔들림이 로그를 가득 채울 수 있습니다.")]
        [SerializeField] private float pinchDeltaDistanceDeadzone = 0.5f;

        [Tooltip("드래그 중 매 프레임 OnDrag 호출")]
        [SerializeField] private bool emitDragEveryFrame = true;

        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool logDebug = true;

        [Header("Stability")]
        [Tooltip("EnhancedTouch 사용 가능 시 자동 활성화")]
        [SerializeField] private bool autoEnableEnhancedTouch = true;

        [Tooltip("press.canceled가 누락될 때 디바이스 상태를 폴링해서 릴리즈를 합성")]
        [SerializeField] private bool synthesizeReleaseIfMissing = true;

        [Header("HitTest / Raycast")]
        [Tooltip("월드(2D/3D) 레이캐스트에 사용할 카메라. 비우면 Camera.main 사용.")]
        [SerializeField] private Camera worldCamera;

        [SerializeField] private bool raycastUI = true;
        [SerializeField] private bool raycast3D = true;
        [SerializeField] private bool raycast2D = true;

        [Tooltip("UI가 있다면 UI를 우선 선택할지 여부.")]
        [SerializeField] private bool preferUIOverWorld = true;

        [Tooltip("UI 위에서는 2D/3D 오브젝트를 클릭 대상으로 잡지 않음.")]
        [SerializeField] private bool blockWorldIfUI = true;

        [SerializeField] private LayerMask layerMask3D = ~0;
        [SerializeField] private LayerMask layerMask2D = ~0;
        [SerializeField] private float raycastMaxDistance = 1000f;

        // ========= 이벤트 (모든 시그니처에 PointerTarget 포함) =========
        public static Action<PointerTarget> OnPressed;
        public static Action<PointerTarget> OnTap;
        public static Action<PointerTarget> OnDoubleTap;
        public static Action<PointerTarget> OnDragStart;
        // delta(from prev), total(from down), target(press 기준)
        public static Action<Vector2, Vector2, PointerTarget> OnDrag;
        public static Action<Vector2, PointerTarget> OnDragEnd;  // total(from down), target(press 기준)

        // Pinch/Spread (center, fingerA target, fingerB target) / (center, scale, deltaScale, A, B)
        public static Action<Vector2, PointerTarget, PointerTarget> OnPinchStart;
        public static Action<Vector2, float, float, PointerTarget, PointerTarget> OnPinch;
        public static Action<Vector2, PointerTarget, PointerTarget> OnPinchEnd;

        // ========= 간단한 PointerTarget =========
        public struct PointerTarget
        {
            public GameObject target;
            public GameObjectType type; // target 이 null 이면 None
            public Vector2 screenPosition; // 이벤트 발생 시점의 스크린 좌표
            public Vector3 rayHitPosition; // 월드 히트 포인트 또는 레이 시작점
            public Vector3 rayDirection;   // 레이 발사 방향(카메라 기반)
            public bool hasWorldHit;       // 2D/3D 레이캐스트 성공 여부
        }

        // ---- 내부 상태 ----
        private bool isPressed;
        private bool dragging;
        private Vector2 startPos;
        private Vector2 lastPos;
        private Vector2 currentPos;

        // 더블탭 판정용
        private bool lastReleaseWasTap;
        private float lastTapTime;
        private Vector2 lastTapPos;

        // 핀치 상태
        private bool pinching;
        private float pinchInitialDist;
        private float pinchPrevDist;
        private Vector2 pinchCenter;

        // 대상 스냅샷
        private PointerTarget pressTarget; // 드래그 동안 유지
        private PointerTarget pinchTargetA;
        private PointerTarget pinchTargetB;

        // UI 레이캐스트 캐시
        private PointerEventData pointerEventData;
        private static readonly List<RaycastResult> s_UIResults = new List<RaycastResult>(16);

        // 관리
        private bool enhancedTouchEnabledByMe;

        // ★ 추가: 현재 입력을 발생시킨 '활성 포인터 디바이스'를 기억 (첫 입력 좌표 보장용)
        private InputDevice activePointerDevice;

        public static void Init()
        {
            if (FindAnyObjectByType<TouchInputManager>(FindObjectsInactive.Include) != null)
                return;

            var prefab = Resources.Load<GameObject>("Prefab/[TouchInputManager]");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = prefab.name;
            }
            else
            {
                QAUtil.LogWarning("Resources/Prefab/[TouchInputManager] prefab not found.");
            }
        }

        private void Awake()
        {
            if (autoEnableEnhancedTouch && !EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
                enhancedTouchEnabledByMe = true;
            }

            // ★ 첫 프레임 전에 포인터 좌표 프라임(Pre-warm)
            var primed = TryReadBestPointerPosition(out var primedPos);
            lastPos = currentPos = primed ? primedPos : Vector2.zero;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (press != null && press.action != null)
            {
                press.action.started   += HandlePressStarted;   // 디바이스에 따라 performed 보다 먼저 올 수 있음
                press.action.performed += HandlePressPerformed; // 일부 설정에서 only performed 가 올 수 있음
                press.action.canceled  += HandleReleased;       // 릴리즈
                press.action.Enable();
            }

            if (position != null && position.action != null)
            {
                // 위치는 콜백에서 '값 업데이트'만 하고, 판정은 Update에서 일괄 처리
                position.action.performed += HandlePositionPerformed;
                position.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (press != null && press.action != null)
            {
                press.action.started   -= HandlePressStarted;
                press.action.performed -= HandlePressPerformed;
                press.action.canceled  -= HandleReleased;
                press.action.Disable();
            }

            if (position != null && position.action != null)
            {
                position.action.performed -= HandlePositionPerformed;
                position.action.Disable();
            }
        }

        private void OnDestroy()
        {
            if (enhancedTouchEnabledByMe && EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // ★ 에디터로 포커스 복귀 시 좌표 재프라임(드물게 발생하는 (0,0) 방지)
            if (hasFocus && TryReadBestPointerPosition(out var p))
                currentPos = lastPos = p;
        }

        private void Update()
        {
            // 0) 최신 포인터 위치를 매 프레임 확보 (디바이스 우선)
            currentPos = ReadCurrentPointerPosition();

            // 1) 두 손가락(핀치) 우선 처리
            HandlePinch();

            // 2) 릴리즈 누락 시 합성(press.canceled 미수신 보정)
            if (synthesizeReleaseIfMissing)
            {
                bool anyPressedNow = IsAnyPointerPressed();
                if (anyPressedNow && !isPressed && !pinching)
                {
                    // ★ 합성 프레스 시에도 활성 디바이스 추정 + 해당 좌표로 시작
                    var guessedDev = GuessActivePointerDevice();
                    if (TryGetDevicePosition(guessedDev, out var at))
                    {
                        activePointerDevice = guessedDev;
                        BeginPress(at);
                    }
                    else
                    {
                        BeginPress(currentPos);
                    }
                }
                else if (!anyPressedNow && isPressed && !pinching)
                {
                    EndPress(currentPos);
                }
            }

            // 3) 드래그 처리 (핀치 중에는 드래그 중단)
            if (isPressed && !pinching)
            {
                float movedFromStart = Vector2.Distance(currentPos, startPos);

                // 최초 이동이 임계치 이상이면 드래그 시작
                if (!dragging && movedFromStart >= dragStartThreshold)
                {
                    dragging = true;
                    var dragTarget = RefreshPressTarget(currentPos);
                    dragTarget.screenPosition = currentPos;
                    EmitDragStart(dragTarget);

                    if (!emitDragEveryFrame)
                    {
                        var delta = currentPos - lastPos;
                        var total = currentPos - startPos;
                        EmitDrag(delta, total, dragTarget);
                    }
                }

                // 드래그 중 매 프레임 혹은 변화가 있을 때 OnDrag
                if (dragging)
                {
                    var delta = currentPos - lastPos;
                    var total = currentPos - startPos;
                    if (delta.sqrMagnitude > 0f)
                    {
                        var dragTarget = RefreshPressTarget(currentPos);
                        dragTarget.screenPosition = currentPos;
                        EmitDrag(delta, total, dragTarget);
                    }
                }
            }

            lastPos = currentPos; // 마지막에 동기화
        }

        // -------------- Input Callbacks --------------

        private void HandlePressStarted(InputAction.CallbackContext ctx)
        {
            // 일부 설정에서는 started만 오거나, started + performed가 둘 다 올 수 있음
            if (!isPressed && !pinching)
            {
                if (TryGetPositionFromContext(ctx, out var at, out var dev))
                {
                    activePointerDevice = dev;   // ★ 이번 입력을 낸 디바이스 기억
                    BeginPress(at);
                }
                else
                {
                    // 컨텍스트에서 못 뽑으면 최선의 좌표로 폴백
                    BeginPress(ReadCurrentPointerPosition());
                }
            }
        }

        private void HandlePressPerformed(InputAction.CallbackContext ctx)
        {
            // 일부 설정에서는 performed만 옴
            if (!isPressed && !pinching)
            {
                if (TryGetPositionFromContext(ctx, out var at, out var dev))
                {
                    activePointerDevice = dev;
                    BeginPress(at);
                }
                else
                {
                    BeginPress(ReadCurrentPointerPosition());
                }
            }
        }

        private void HandleReleased(InputAction.CallbackContext _)
        {
            if (pinching) // 핀치 중 릴리즈는 핀치 로직이 책임
            {
                lastReleaseWasTap = false;
                isPressed = false; // 드문 케이스 대비 상태만 정리
                activePointerDevice = null;
                return;
            }
            if (isPressed)
            {
                EndPress(ReadCurrentPointerPosition());
                activePointerDevice = null; // ★ 활성 디바이스 클리어
            }
        }

        private void HandlePositionPerformed(InputAction.CallbackContext ctx)
        {
            // 위치 갱신만 수행, 판정은 Update에서
            currentPos = ctx.ReadValue<Vector2>();
        }

        // -------------- Press / Release Core --------------

        private void BeginPress(Vector2 at)
        {
            isPressed = true;
            dragging  = false;

            startPos = at;
            lastPos  = at;

            // 프레스 순간의 대상 스냅샷(드래그 타깃으로 유지)
            var pressSnapshot = RefreshPressTarget(at);

            EmitPressed(pressSnapshot);
        }

        private void EndPress(Vector2 at)
        {
            isPressed = false;

            // 드래그 중이면 드래그 종료
            if (dragging)
            {
                var total = at - startPos;
                var dragTarget = RefreshPressTarget(at);
                dragTarget.screenPosition = at;
                EmitDragEnd(total, dragTarget);
                dragging = false;
                lastReleaseWasTap = false;
                return;
            }

            // 드래그가 아니었다면 Tap/DoubleTap 판정
            var moved = Vector2.Distance(at, startPos);
            if (moved <= dragStartThreshold)
            {
                var now = Time.unscaledTime;

                bool canDouble =
                    doubleTapMaxDelay > 0f &&
                    lastReleaseWasTap &&
                    (now - lastTapTime) <= doubleTapMaxDelay &&
                    Vector2.Distance(at, lastTapPos) <= doubleTapMaxDistance;

                var releaseTarget = PickTargetAt(at);

                if (canDouble)
                {
                    EmitDoubleTap(releaseTarget);
                    lastReleaseWasTap = false;
                    lastTapTime = 0f;
                }
                else
                {
                    EmitTap(releaseTarget);
                    lastReleaseWasTap = true;
                    lastTapTime = now;
                    lastTapPos  = at;
                }
            }
            else
            {
                lastReleaseWasTap = false;
            }
        }

        // ---------------- Pinch / Spread ----------------

        private void HandlePinch()
        {
            if (TryGetTwoActiveTouchPositions(out var p0, out var p1))
            {
                var center = (p0 + p1) * 0.5f;
                var dist   = Vector2.Distance(p0, p1);

                if (!pinching)
                {
                    // 드래그 중이었다면 먼저 종료
                    if (dragging)
                    {
                        var dragTarget = RefreshPressTarget(lastPos);
                        dragTarget.screenPosition = lastPos;
                        EmitDragEnd(lastPos - startPos, dragTarget);
                        dragging = false;
                    }

                    pinching = true;
                    pinchInitialDist = Mathf.Max(dist, 0.0001f);
                    pinchPrevDist    = dist;
                    pinchCenter      = center;

                    // 핀치 시작 시 각 손가락의 대상 스냅샷
                    RefreshPinchTargets(p0, p1);

                    EmitPinchStart(center, pinchTargetA, pinchTargetB);
                }
                else
                {
                    pinchCenter = center;
                    RefreshPinchTargets(p0, p1);
                    var deltaDist = dist - pinchPrevDist;

                    // 노이즈 억제
                    if (Mathf.Abs(deltaDist) >= pinchDeltaDistanceDeadzone)
                    {
                        var scale      = dist / pinchInitialDist;
                        var deltaScale = (pinchPrevDist > 0f) ? deltaDist / pinchPrevDist : 0f;

                        EmitPinch(center, scale, deltaScale, pinchTargetA, pinchTargetB);
                        pinchPrevDist = dist;
                    }
                }
            }
            else
            {
                if (pinching)
                {
                    EmitPinchEnd(pinchCenter, pinchTargetA, pinchTargetB);
                    pinching = false;

                    // 선택: 핀치 직후 한 손가락이 남아있다면 기준점 재설정
                    if (isPressed)
                    {
                        startPos    = ReadCurrentPointerPosition();
                        lastPos     = startPos;
                        RefreshPressTarget(startPos);
                    }
                }
            }
        }

        private bool TryGetTwoActiveTouchPositions(out Vector2 p0, out Vector2 p1)
        {
            p0 = default; p1 = default;

            // 1) EnhancedTouch가 활성화되어 있으면 그것을 우선 사용
            if (EnhancedTouchSupport.enabled)
            {
                int found = 0;
                var active = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                for (int i = 0; i < active.Count; i++)
                {
                    var t = active[i];
                    // Began / Moved / Stationary 인 것들만 유효 터치로 취급
                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                        t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    {
                        var pos = t.screenPosition;
                        if (found == 0) p0 = pos;
                        else if (found == 1) p1 = pos;
                        found++;
                        if (found >= 2) return true;
                    }
                }
                return false;
            }

            // 2) Touchscreen 폴백
            var ts = Touchscreen.current;
            if (ts == null) return false;

            int found2 = 0;
            var touches = ts.touches; // ReadOnlyArray<TouchControl>
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.press.isPressed)
                {
                    var pos = t.position.ReadValue();
                    if (found2 == 0) p0 = pos;
                    else if (found2 == 1) p1 = pos;
                    found2++;
                    if (found2 >= 2) return true;
                }
            }
            return false;
        }

        // ---------- 레이캐스트(UI/2D/3D) & 타겟 선택 ----------

        private PointerTarget RefreshPressTarget(Vector2 screenPos)
        {
            pressTarget = PickTargetAt(screenPos);
            return pressTarget;
        }

        private void RefreshPinchTargets(Vector2 fingerA, Vector2 fingerB)
        {
            pinchTargetA = PickTargetAt(fingerA);
            pinchTargetB = PickTargetAt(fingerB);
        }

        private PointerTarget PickTargetAt(Vector2 screenPos)
        {
            var result = new PointerTarget
            {
                screenPosition = screenPos,
                target = null,
                type = GameObjectType.None,
                rayHitPosition = Vector3.zero,
                rayDirection = Vector3.zero,
                hasWorldHit = false
            };

            GameObject uiTop = null;

            // 1) UI 레이캐스트
            if (raycastUI && EventSystem.current != null)
            {
                if (pointerEventData == null)
                    pointerEventData = new PointerEventData(EventSystem.current);
                else
                    pointerEventData.Reset(); // ★ 첫 프레임/첫 클릭 안정화 (잔여 상태 제거)

                pointerEventData.position = screenPos;

                s_UIResults.Clear();
                EventSystem.current.RaycastAll(pointerEventData, s_UIResults);

                if (s_UIResults.Count > 0)
                    uiTop = s_UIResults[0].gameObject;
            }

            bool blockedByUI = blockWorldIfUI && uiTop != null;

            // 2) 월드(3D/2D) 레이캐스트
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Ray ray = default;
            bool hasRay = cam != null;

            if (hasRay)
            {
                ray = cam.ScreenPointToRay(screenPos);
                result.rayDirection = ray.direction;
                result.rayHitPosition = ray.origin; // 기본값: 히트하지 않은 경우 레이 시작점
            }

            GameObject hit3DGo = null;
            float hit3DDist = float.MaxValue;
            RaycastHit hit3D = default;

            GameObject hit2DGo = null;
            float hit2DDist = float.MaxValue;
            RaycastHit2D hit2D = default;

            if (!blockedByUI && hasRay && (raycast3D || raycast2D))
            {
                if (raycast3D)
                {
                    if (Physics.Raycast(ray, out hit3D, raycastMaxDistance, layerMask3D, QueryTriggerInteraction.Collide))
                    {
                        hit3DGo = hit3D.collider.gameObject;
                        hit3DDist = hit3D.distance;
                    }
                }

                if (raycast2D)
                {
                    hit2D = Physics2D.GetRayIntersection(ray, raycastMaxDistance, layerMask2D);
                    if (hit2D.collider != null)
                    {
                        hit2DGo = hit2D.collider.gameObject;
                        hit2DDist = hit2D.distance;
                    }
                }
            }

            GameObject worldChosen = null;
            GameObjectType worldType = GameObjectType.None;

            if (hit2DGo != null && hit3DGo != null)
            {
                bool pick2D = hit2DDist < hit3DDist;
                worldChosen = pick2D ? hit2DGo : hit3DGo;
                worldType = pick2D ? GameObjectType.Sprite : GameObjectType.Mesh;
                result.hasWorldHit = true;
                if (pick2D)
                    result.rayHitPosition = hasRay ? ray.GetPoint(hit2DDist) : Vector3.zero;
                else
                    result.rayHitPosition = hit3D.point;
            }
            else if (hit2DGo != null)
            {
                worldChosen = hit2DGo;
                worldType = GameObjectType.Sprite;
                result.hasWorldHit = true;
                result.rayHitPosition = hasRay ? ray.GetPoint(hit2DDist) : Vector3.zero;
            }
            else if (hit3DGo != null)
            {
                worldChosen = hit3DGo;
                worldType = GameObjectType.Mesh;
                result.hasWorldHit = true;
                result.rayHitPosition = hit3D.point;
            }

            // 3) 최종 타겟 선택
            GameObject chosen = null;
            GameObjectType chosenType = GameObjectType.None;

            if (blockedByUI)
            {
                chosen = uiTop;
                chosenType = GameObjectType.UI;
            }
            else if (preferUIOverWorld)
            {
                if (uiTop != null)
                {
                    chosen = uiTop;
                    chosenType = GameObjectType.UI;
                }
                else if (worldChosen != null)
                {
                    chosen = worldChosen;
                    chosenType = worldType;
                }
            }
            else
            {
                if (worldChosen != null)
                {
                    chosen = worldChosen;
                    chosenType = worldType;
                }

                if (chosen == null && uiTop != null)
                {
                    chosen = uiTop;
                    chosenType = GameObjectType.UI;
                }
            }

            result.target = chosen;
            result.type = chosenType;

            if (!hasRay)
            {
                result.rayDirection = Vector3.zero;
                result.rayHitPosition = Vector3.zero;
            }

            return result;
        }

        // ---------- 유틸 (입력 좌표/디바이스) ----------

        // ★ press 콜백의 컨텍스트에서 좌표와 디바이스를 최대한 직접 획득
        private bool TryGetPositionFromContext(InputAction.CallbackContext ctx, out Vector2 pos, out InputDevice device)
        {
            device = null;
            pos = default;

            var control = ctx.control;
            if (control == null)
                return false;

            device = control.device;

            // TouchControl 이면 해당 터치의 position 우선
            if (control is UnityEngine.InputSystem.Controls.TouchControl touchCtrl)
            {
                pos = touchCtrl.position.ReadValue();
                return true;
            }

            // Pointer(상위) 혹은 Mouse/Pen 디바이스
            if (device is Pointer ptr)
            {
                pos = ptr.position.ReadValue();
                return true;
            }
            if (device is Mouse m)
            {
                pos = m.position.ReadValue();
                return true;
            }
            if (device is Pen pen)
            {
                pos = pen.position.ReadValue();
                return true;
            }
            if (device is Touchscreen ts)
            {
                // 구체 터치를 못 얻었을 때는 primaryTouch 폴백
                pos = ts.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        // ★ 활성 디바이스를 우선으로 현재 좌표를 읽는다 (position.action 보다 항상 디바이스 우선)
        private Vector2 ReadCurrentPointerPosition()
        {
            // 1) 현재 활성 포인터 디바이스(프레스가 시작된 주체)가 있으면 그것을 우선
            if (TryGetDevicePosition(activePointerDevice, out var byActive))
                return byActive;

            // 2) Pointer.current → 가장 최근 사용 포인터(대개 Mouse)
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();

            // 3) 디바이스 개별 폴백
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Pen.current != null)
                return Pen.current.position.ReadValue();
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            // 4) 마지막으로 액션 값 (액션은 첫 프레임에 0,0 가능 → 최후순위로 내림)
            if (position != null && position.action != null && position.action.enabled)
            {
                var v = position.action.ReadValue<Vector2>();
                if (v != Vector2.zero) // 0,0일 때는 신뢰도 낮음
                    return v;
            }

            // 5) 최후의 폴백
            return lastPos;
        }

        private bool TryReadBestPointerPosition(out Vector2 pos)
        {
            // 초기 프라임/포커스 복귀용
            if (Pointer.current != null)
            {
                pos = Pointer.current.position.ReadValue();
                return true;
            }
            if (Mouse.current != null)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Pen.current != null)
            {
                pos = Pen.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            pos = Vector2.zero;
            return false;
        }

        private bool TryGetDevicePosition(InputDevice dev, out Vector2 pos)
        {
            pos = default;
            if (dev == null) return false;

            if (dev is Pointer ptr)
            {
                pos = ptr.position.ReadValue();
                return true;
            }
            if (dev is Mouse m)
            {
                pos = m.position.ReadValue();
                return true;
            }
            if (dev is Pen pen)
            {
                pos = pen.position.ReadValue();
                return true;
            }
            if (dev is Touchscreen ts)
            {
                // 여러 터치 중 어느 것인지 식별이 필요하지만, 여기서는 primary로 폴백
                pos = ts.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        private InputDevice GuessActivePointerDevice()
        {
            // 합성 프레스 시작 시 현재 눌린 디바이스 추정
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return Mouse.current;
            if (Pen.current != null && Pen.current.tip.isPressed)
                return Pen.current;
            if (Touchscreen.current != null)
            {
                var ts = Touchscreen.current;
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                    if (touches[i].press.isPressed)
                        return ts; // 어떤 터치인지 모르면 기기 단위로 반환
            }
            return Pointer.current != null ? Pointer.current : null;
        }

        private bool IsAnyPointerPressed()
        {
            // 마우스 왼쪽/펜 팁/터치 중 하나라도 눌림 상태인지 검사
            bool pressed = false;

            if (Mouse.current != null)
                pressed |= Mouse.current.leftButton.isPressed;

            if (Pen.current != null)
                pressed |= Pen.current.tip.isPressed;

            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed)
                    {
                        pressed = true;
                        break;
                    }
                }
            }

            return pressed;
        }

        // ---- Emit & 로그 (모두 PointerTarget 포함) ----
        private static string FormatPointerTarget(PointerTarget t)
        {
            string targetName = t.target ? t.target.name : "null";
            return $"target:{targetName} type:{t.type} screen:{t.screenPosition} hasWorldHit:{t.hasWorldHit} rayPos:{t.rayHitPosition} rayDir:{t.rayDirection}";
        }

        private void EmitPressed(PointerTarget t)
        {
            if (logDebug) Debug.Log($"[Pressed] {FormatPointerTarget(t)}");
            OnPressed?.Invoke(t);
        }

        private void EmitTap(PointerTarget t)
        {
            if (logDebug) Debug.Log($"[Tap] {FormatPointerTarget(t)}");
            OnTap?.Invoke(t);
        }

        private void EmitDoubleTap(PointerTarget t)
        {
            if (logDebug) Debug.Log($"[DoubleTap] {FormatPointerTarget(t)}");
            OnDoubleTap?.Invoke(t);
        }

        private void EmitDragStart(PointerTarget t)
        {
            if (logDebug) Debug.Log($"[DragStart] {FormatPointerTarget(t)}");
            OnDragStart?.Invoke(t);
        }

        private void EmitDrag(Vector2 delta, Vector2 total, PointerTarget t)
        {
            if (logDebug) Debug.Log($"[Drag] delta:{delta} total:{total} {FormatPointerTarget(t)}");
            OnDrag?.Invoke(delta, total, t);
        }

        private void EmitDragEnd(Vector2 total, PointerTarget t)
        {
            if (logDebug) Debug.Log($"[DragEnd] total:{total} {FormatPointerTarget(t)}");
            OnDragEnd?.Invoke(total, t);
        }

        private void EmitPinchStart(Vector2 center, PointerTarget a, PointerTarget b)
        {
            if (logDebug) Debug.Log($"[PinchStart] center:{center} A:({FormatPointerTarget(a)}) B:({FormatPointerTarget(b)})");
            OnPinchStart?.Invoke(center, a, b);
        }

        private void EmitPinch(Vector2 center, float scale, float deltaScale, PointerTarget a, PointerTarget b)
        {
            if (logDebug)
            {
                string mode = deltaScale > 0f ? "Spread(+)" : (deltaScale < 0f ? "Pinch(-)" : "Neutral");
                Debug.Log($"[Pinch] center:{center} scale:{scale:F3} dScale:{deltaScale:F3} {mode} A:({FormatPointerTarget(a)}) B:({FormatPointerTarget(b)})");
            }
            OnPinch?.Invoke(center, scale, deltaScale, a, b);
        }

        private void EmitPinchEnd(Vector2 center, PointerTarget a, PointerTarget b)
        {
            if (logDebug) Debug.Log($"[PinchEnd] center:{center} A:({FormatPointerTarget(a)}) B:({FormatPointerTarget(b)})");
            OnPinchEnd?.Invoke(center, a, b);
        }


    }
}
