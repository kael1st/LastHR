using UnityEngine;

/// <summary>
/// Adjusts the SpriteRenderer sortingOrder dynamically based on the Y coordinate.
/// Gives the classic 2D top-down RPG depth effect (e.g. Stardew Valley),
/// where characters can walk behind and in front of trees, rocks, and buildings.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TopDownYOrder : MonoBehaviour
{
    [Tooltip("Sorting order precision multiplier. 100 means 1 unit in Y changes sorting order by 100.")]
    [SerializeField] private int precision = 100;

    [Tooltip("Offset added to the calculated sorting order.")]
    [SerializeField] private int sortingOffset = 0;

    [Tooltip("If true, updates every frame (for moving objects like the Player). Set to false for static obstacles.")]
    [SerializeField] private bool isDynamic = true;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        if (isDynamic)
        {
            UpdateSorting();
        }
    }

    [ContextMenu("Update Sorting")]
    public void UpdateSorting()
    {
        if (spriteRenderer != null)
        {
            // Lower Y (closer to bottom of screen) renders in front (higher sorting order)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * precision) + sortingOffset;
        }
    }
}
