using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmingSystem
{
    /// <summary>
    /// Attach to any UI element (such as a header title bar or panel background)
    /// to make the target UI panel draggable across the screen with the mouse.
    /// Also brings the panel to front on click.
    /// </summary>
    public class UIDraggablePanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        [Tooltip("The RectTransform to move when dragging. If null, targets this object or parent.")]
        [SerializeField] private RectTransform targetPanel;

        private Canvas canvas;
        private RectTransform canvasRect;

        private void Awake()
        {
            if (targetPanel == null)
            {
                targetPanel = transform as RectTransform;
            }
            InitCanvas();
        }

        private void InitCanvas()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvasRect = canvas.transform as RectTransform;
                }
            }
        }

        public void SetTarget(RectTransform panel)
        {
            targetPanel = panel;
            InitCanvas();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetPanel != null)
            {
                // Bring panel to front of other UI elements on click
                targetPanel.SetAsLastSibling();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            InitCanvas();
            if (targetPanel != null)
            {
                targetPanel.SetAsLastSibling();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;
            InitCanvas();
            if (canvas == null) return;

            // Move by mouse movement delta adjusted by canvas scale factor
            targetPanel.anchoredPosition += eventData.delta / canvas.scaleFactor;

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            if (canvasRect == null || targetPanel == null) return;

            Vector3[] panelCorners = new Vector3[4];
            targetPanel.GetWorldCorners(panelCorners);

            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);

            // canvasCorners: 0 = BottomLeft, 1 = TopLeft, 2 = TopRight, 3 = BottomRight
            float minX = canvasCorners[0].x;
            float maxX = canvasCorners[2].x;
            float minY = canvasCorners[0].y;
            float maxY = canvasCorners[2].y;

            Vector3 pos = targetPanel.position;

            // Keep at least 60px horizontally and 30px vertically visible inside the canvas
            float clampedX = Mathf.Clamp(pos.x, minX + 60f, maxX - 60f);
            float clampedY = Mathf.Clamp(pos.y, minY + 30f, maxY - 30f);

            if (pos.x != clampedX || pos.y != clampedY)
            {
                targetPanel.position = new Vector3(clampedX, clampedY, pos.z);
            }
        }
    }
}
