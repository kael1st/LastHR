using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Player controller component for targeting and interacting with GardenPlots.
    /// Supports facing-direction targeting, mouse-hover targeting, and action execution.
    /// </summary>
    public class FarmingController : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [Tooltip("Distance in front of player to check for garden plots.")]
        [SerializeField] private float facingTargetDistance = 1.1f;

        [Tooltip("Radius around target point to detect plots.")]
        [SerializeField] private float checkRadius = 0.5f;

        [Tooltip("Max distance from player allowed for mouse click interactions.")]
        [SerializeField] private float maxMouseInteractDistance = 2.5f;

        [Header("Controls")]
        [SerializeField] private KeyCode interactKey = KeyCode.Space;
        [SerializeField] private KeyCode alternateInteractKey = KeyCode.E;
        [SerializeField] private KeyCode advanceDayKey = KeyCode.T;

        [Header("Feedback")]
        [Tooltip("Visual bob effect on player when using tools.")]
        [SerializeField] private bool punchOnInteract = true;

        private TopDownPlayerController playerMovement;
        private PlayerInventory playerInventory;
        private GardenPlot currentTargetPlot;
        private Camera mainCamera;

        private void Awake()
        {
            playerMovement = GetComponent<TopDownPlayerController>();
            playerInventory = GetComponent<PlayerInventory>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdateTargetPlot();
            HandleInteractionInput();
            HandleDayAdvanceInput();
        }

        private void UpdateTargetPlot()
        {
            GardenPlot foundPlot = null;

            // 1. First priority: Check if mouse is hovering over a plot within range
            if (mainCamera != null)
            {
                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0f;

                float distToMouse = Vector2.Distance(transform.position, mouseWorld);
                if (distToMouse <= maxMouseInteractDistance)
                {
                    Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
                    if (hit != null)
                    {
                        foundPlot = hit.GetComponentInParent<GardenPlot>();
                    }
                }
            }

            // 2. Second priority: Facing direction target point
            if (foundPlot == null)
            {
                Vector2 facingDir = playerMovement != null ? playerMovement.LastFacingDirection : Vector2.down;
                Vector2 targetPos = (Vector2)transform.position + facingDir * facingTargetDistance;

                Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, checkRadius);
                float closestDist = float.MaxValue;

                foreach (var hit in hits)
                {
                    GardenPlot plot = hit.GetComponentInParent<GardenPlot>();
                    if (plot != null)
                    {
                        float dist = Vector2.Distance(targetPos, plot.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            foundPlot = plot;
                        }
                    }
                }
            }

            // Update highlighting
            if (currentTargetPlot != foundPlot)
            {
                if (currentTargetPlot != null)
                {
                    currentTargetPlot.SetHighlight(false);
                }

                currentTargetPlot = foundPlot;

                if (currentTargetPlot != null)
                {
                    currentTargetPlot.SetHighlight(true);
                }
            }
        }

        private void HandleInteractionInput()
        {
            bool keyPressed = Input.GetKeyDown(interactKey) || Input.GetKeyDown(alternateInteractKey);
            bool mouseClicked = Input.GetMouseButtonDown(0);

            // Avoid triggering when clicking on UI
            if (mouseClicked && UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if ((keyPressed || mouseClicked) && currentTargetPlot != null)
            {
                ExecuteInteraction(currentTargetPlot);
            }
        }

        private void ExecuteInteraction(GardenPlot plot)
        {
            if (plot == null) return;

            PlayerInventory inv = playerInventory != null ? playerInventory : PlayerInventory.Instance;
            bool success = plot.Interact(inv, out string feedback);

            if (!string.IsNullOrEmpty(feedback))
            {
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.ShowNotification(feedback, success);
                }
                else
                {
                    Debug.Log($"[Farming] {feedback}");
                }
            }

            if (punchOnInteract && success)
            {
                TriggerToolPunch();
            }
        }

        private void HandleDayAdvanceInput()
        {
            if (Input.GetKeyDown(advanceDayKey))
            {
                if (GardenManager.Instance != null)
                {
                    GardenManager.Instance.AdvanceDay();
                }
                else
                {
                    // Fallback: Find all plots in scene
                    GardenPlot[] allPlots = FindObjectsOfType<GardenPlot>();
                    foreach (var plot in allPlots)
                    {
                        plot.AdvanceDay();
                    }
                    if (InventoryUI.Instance != null)
                    {
                        InventoryUI.Instance.ShowNotification("Advanced to Next Day!", true);
                    }
                }
            }
        }

        private void TriggerToolPunch()
        {
            // Subtle player tool swing animation (nudge in facing direction)
            Vector2 dir = playerMovement != null ? playerMovement.LastFacingDirection : Vector2.down;
            transform.position += (Vector3)(dir * 0.08f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 facingDir = playerMovement != null ? playerMovement.LastFacingDirection : Vector2.down;
            Vector2 targetPos = (Vector2)transform.position + facingDir * facingTargetDistance;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, checkRadius);
        }
    }
}
