using UnityEngine;

namespace HyperModule
{
    public class LeftDragManager : MonoBehaviour
    {
        private static LeftDragManager _instance;
        public static LeftDragManager Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = FindObjectOfType<LeftDragManager>(true);
                }
                return _instance;
            }
        }

        private Vector2 startDragPosition;
        private Vector2 currentDragPosition;
        private ILeftDrag currentDragTarget;
        private Camera mainCamera;

        public bool debugMode;


        void Update()
        {
            HandleMouseInput();
        } 

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if(mainCamera == null) mainCamera = Camera.main;
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 rayStartPos = new Vector3(mousePos.x, mousePos.y, -10000);
                Ray ray = new Ray(rayStartPos, Vector3.forward);

                if(debugMode) QAUtil.Log($"Left Click Down at world position: {mousePos}");
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
                if(hit.collider == null)
                {
                    if(debugMode) QAUtil.Log("Left Click Down: No Collider");
                    currentDragTarget = null;
                }
                else
                {
                    if(debugMode) QAUtil.Log($"Left Click Down: {hit.collider.name}");
                    currentDragTarget = hit.collider.GetComponent<ILeftDrag>();
                    if (currentDragTarget != null)
                    {
                        startDragPosition = mousePos;
                        currentDragPosition = mousePos;
                        currentDragTarget.OnLeftDragStart(startDragPosition, currentDragPosition);
                    }
                }
            }

            else if (currentDragTarget != null && Input.GetMouseButton(0))
            {
                if(debugMode) QAUtil.Log("Left Click");
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                currentDragPosition = mousePos;
                currentDragTarget.OnLeftDrag(startDragPosition, currentDragPosition);
            }

            else if (currentDragTarget != null && Input.GetMouseButtonUp(0))
            {
                if(debugMode) QAUtil.Log("Left Click Up");
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                currentDragPosition = mousePos;
                currentDragTarget.OnLeftDragEnd(startDragPosition, currentDragPosition);
                currentDragTarget = null;
            }
            else
            {
                currentDragTarget = null;
            }
        }
    }
}