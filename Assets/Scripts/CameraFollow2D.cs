using UnityEngine;

/// <summary>
/// Smooth 2D Camera Follow for top-down games like Stardew Valley.
/// Follows a target (usually the Player) smoothly using Vector3.SmoothDamp in LateUpdate.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The target Transform to follow (usually the Player).")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("Rough time it takes to catch up to the target. Lower = snappier, higher = smoother.")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("Offset relative to the target. Z should typically remain -10 for 2D cameras.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Camera Bounds (Optional)")]
    [Tooltip("If true, camera will be restricted within the min and max world coordinates.")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

    private Vector3 currentVelocity = Vector3.zero;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        if (target == null)
        {
            // Auto-find player by tag or component if not assigned
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                TopDownPlayerController controller = FindObjectOfType<TopDownPlayerController>();
                if (controller != null)
                {
                    target = controller.transform;
                }
            }
        }

        // Snap immediately to target on start
        if (target != null)
        {
            Vector3 targetPos = target.position + offset;
            if (useBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
                targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
            }
            transform.position = targetPos;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Keep camera Z offset intact
        targetPosition.z = offset.z;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
