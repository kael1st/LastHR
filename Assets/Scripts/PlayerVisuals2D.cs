using UnityEngine;

/// <summary>
/// Drives 4-directional 2D sprite animation for the top-down player.
/// Uses explicit sprites for each direction (Down, Up, Left, Right) with
/// idle and 4-frame walk cycles. No runtime sprite flipping — all directions
/// have their own dedicated sprites.
/// Attach to the same GameObject as TopDownPlayerController and SpriteRenderer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TopDownPlayerController))]
public class PlayerVisuals2D : MonoBehaviour
{
    // =========================================================
    // Inspector: Idle Sprites (one per direction)
    // =========================================================
    [Header("Idle Sprites")]
    [Tooltip("Player facing toward the camera (down).")]
    [SerializeField] private Sprite idleDown;
    [Tooltip("Player facing away from the camera (up). Shows the braid.")]
    [SerializeField] private Sprite idleUp;
    [Tooltip("Player facing right (side profile).")]
    [SerializeField] private Sprite idleRight;
    [Tooltip("Player facing left (mirrored side profile).")]
    [SerializeField] private Sprite idleLeft;

    // =========================================================
    // Inspector: Walk Cycle Sprites (4 frames per direction)
    // =========================================================
    [Header("Walk Cycle Sprites (4 frames each)")]
    [SerializeField] private Sprite[] walkDown  = new Sprite[4];
    [SerializeField] private Sprite[] walkUp    = new Sprite[4];
    [SerializeField] private Sprite[] walkRight = new Sprite[4];
    [SerializeField] private Sprite[] walkLeft  = new Sprite[4];

    // =========================================================
    // Inspector: Animation Tuning
    // =========================================================
    [Header("Animation Settings")]
    [Tooltip("Walk animation playback speed in frames per second.")]
    [SerializeField] private float walkFPS = 6f;
    [Tooltip("Multiplier applied to walkFPS when sprinting.")]
    [SerializeField] private float sprintFPSMultiplier = 1.5f;

    // =========================================================
    // Runtime
    // =========================================================
    private SpriteRenderer sr;
    private TopDownPlayerController controller;
    private float animTimer;
    private TopDownPlayerController.Direction lastDir;

    // Public property accessors for editor scripts
    public Sprite IdleDown  { get => idleDown;  set => idleDown  = value; }
    public Sprite IdleUp    { get => idleUp;    set => idleUp    = value; }
    public Sprite IdleRight { get => idleRight; set => idleRight = value; }
    public Sprite IdleLeft  { get => idleLeft;  set => idleLeft  = value; }

    public Sprite[] WalkDown  { get => walkDown;  set => walkDown  = value; }
    public Sprite[] WalkUp    { get => walkUp;    set => walkUp    = value; }
    public Sprite[] WalkRight { get => walkRight; set => walkRight = value; }
    public Sprite[] WalkLeft  { get => walkLeft;  set => walkLeft  = value; }

    // =========================================================
    // Unity Lifecycle
    // =========================================================
    private void Awake()
    {
        sr         = GetComponent<SpriteRenderer>();
        controller = GetComponent<TopDownPlayerController>();

        // Hide the old FaceIndicator child — directional sprites replace it
        Transform faceIndicator = transform.Find("FaceIndicator");
        if (faceIndicator != null) faceIndicator.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Default to idle-down on spawn
        if (idleDown != null) sr.sprite = idleDown;
    }

    private void Update()
    {
        if (sr == null || controller == null) return;

        var dir     = controller.CurrentDirection;
        bool moving = controller.IsMoving;

        // Reset timer when direction changes so animation doesn't skip mid-stride
        if (dir != lastDir)
        {
            animTimer = 0f;
            lastDir   = dir;
        }

        if (moving)
        {
            float fps = controller.IsSprinting
                ? walkFPS * sprintFPSMultiplier
                : walkFPS;

            animTimer += Time.deltaTime * fps;

            Sprite[] cycle = GetWalkCycle(dir);
            if (cycle != null && cycle.Length > 0)
            {
                int frame = Mathf.FloorToInt(animTimer) % cycle.Length;
                sr.sprite = cycle[frame];
            }
            else
            {
                // Fallback — show idle sprite for this direction
                sr.sprite = GetIdleSprite(dir);
            }
        }
        else
        {
            // Idle: freeze on frame 0 and show idle sprite
            animTimer = 0f;
            sr.sprite = GetIdleSprite(dir);
        }
    }

    // =========================================================
    // Helpers
    // =========================================================
    private Sprite GetIdleSprite(TopDownPlayerController.Direction dir)
    {
        return dir switch
        {
            TopDownPlayerController.Direction.Down  => idleDown,
            TopDownPlayerController.Direction.Up    => idleUp,
            TopDownPlayerController.Direction.Right => idleRight,
            TopDownPlayerController.Direction.Left  => idleLeft,
            _                                       => idleDown,
        };
    }

    private Sprite[] GetWalkCycle(TopDownPlayerController.Direction dir)
    {
        return dir switch
        {
            TopDownPlayerController.Direction.Down  => walkDown,
            TopDownPlayerController.Direction.Up    => walkUp,
            TopDownPlayerController.Direction.Right => walkRight,
            TopDownPlayerController.Direction.Left  => walkLeft,
            _                                       => walkDown,
        };
    }
}
