using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Interactive Street Vendor NPC / Stall in the game world.
    /// Allows the player to approach and interact via Space, F, or mouse click to open the selling interface.
    /// </summary>
    public class StreetVendor : MonoBehaviour
    {
        public static StreetVendor Instance { get; private set; }

        [Header("Vendor Identity")]
        [SerializeField] private string vendorName = "The Merchant";

        [Header("Interaction Settings")]
        [Tooltip("Maximum distance from player to allow interaction.")]
        [SerializeField] private float interactDistance = 2.2f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer stallRenderer;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private SpriteRenderer highlightRenderer;

        private Transform playerTransform;
        private bool isPlayerInRange = false;

        public bool IsPlayerInRange => isPlayerInRange;
        public string VendorName => vendorName;
        public string GetInteractionPrompt() => $"[E] {vendorName}";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (stallRenderer == null)
                stallRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;

            SetPromptActive(false);
            SetHighlight(false);
        }

        private void Update()
        {
            CheckPlayerProximity();
            HandleInteractionInput();
        }

        private void CheckPlayerProximity()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactDistance;

            if (inRange != isPlayerInRange)
            {
                isPlayerInRange = inRange;
                SetPromptActive(isPlayerInRange);
                SetHighlight(isPlayerInRange);
            }
        }

        private void HandleInteractionInput()
        {
            if (!isPlayerInRange) return;

            // Don't trigger if any UI modal (like Main Inventory or Vendor UI itself) is open or was just closed this frame
            if (VendorUI.Instance != null && (VendorUI.Instance.IsOpen || VendorUI.Instance.ClosedFrame == Time.frameCount)) return;
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsInventoryOpen) return;

            // Keyboard input (mainly E, with Space as secondary)
            bool keyPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);

            // Mouse click directly on the vendor stall
            bool mouseClicked = false;
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                        mouseWorld.z = 0f;
                        var col = GetComponent<Collider2D>();
                        if (col != null && col.OverlapPoint(mouseWorld))
                        {
                            mouseClicked = true;
                        }
                    }
                }
            }

            if (keyPressed || mouseClicked)
            {
                Interact();
            }
        }

        public void Interact()
        {
            if (VendorUI.Instance != null)
            {
                VendorUI.Instance.OpenVendor();
            }
            else
            {
                Debug.LogWarning("[StreetVendor] VendorUI instance not found in scene!");
            }
        }

        public void SetPromptActive(bool active)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(active);
            }
        }

        public void SetHighlight(bool active)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = active;
            }
            else if (stallRenderer != null)
            {
                stallRenderer.color = active ? new Color(1f, 1f, 0.85f) : Color.white;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }
    }
}
