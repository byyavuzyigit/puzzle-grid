using UnityEngine;

public class UIFollowWorldAnchor : MonoBehaviour
{
    public Transform worldAnchor;
    public Vector2 pixelOffset; // pixels to offset from anchor

    RectTransform rt;
    Canvas canvas;
    Camera cam;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (worldAnchor == null || canvas == null || cam == null)
            return;

        // Convert world position to screen position
        Vector3 screenPos = cam.WorldToScreenPoint(worldAnchor.position);
        
        // Handle different render modes
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For Overlay, screen position maps directly with pixel offset
            rt.position = new Vector3(screenPos.x + pixelOffset.x, screenPos.y + pixelOffset.y, 0);
        }
        else
        {
            // For Camera/World render mode, convert screen point to canvas local point
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceCamera ? cam : null,
                out localPoint
            );
            rt.anchoredPosition = localPoint + pixelOffset;
        }
    }
}