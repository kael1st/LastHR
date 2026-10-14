using UnityEngine;

/// <summary>
/// Top-down 2D Player Controller inspired by games like Stardew Valley.
/// Features 8-directional movement, normalized diagonal speed, sprint support,
/// facing direction tracking, smooth physics collisions via Rigidbody2D,
/// and ready-to-use Animator & Direction indicator hooks.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerController : MonoBehaviour
{
    public enum Direction { Down, Up, Left, Right }

    [Header("Movement Settings")]
    [Tooltip("Base walking speed in units per second.")]
    [SerializeField] private float walkSpeed = 5f;

    [Tooltip("Running speed when sprinting.")]
    [SerializeField] private float sprintSpeed = 8f;

    [Tooltip("Key used to sprint/run.")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Tooltip("If true, movement stops instantly when releasing keys (crisp Stardew feel). If false, uses smooth acceleration/deceleration.")]
    [SerializeField] private bool snappyMovement = true;

    [Tooltip("Acceleration factor when snappyMovement is disabled.")]
    [SerializeField] private float acceleration = 60f;

    [Tooltip("Deceleration factor when snappyMovement is disabled.")]
    [SerializeField] private float deceleration = 60f;

    [Header("Visual & Animation Settings")]
    [Tooltip("Optional reference to the player's SpriteRenderer. If null, auto-finds on this object or children.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Optional reference to a direction pointer / face indicator object.")]
    [SerializeField] private Transform directionIndicator;

    [Tooltip("Optional reference to Animator for sprite walk/idle blend trees.")]
    [SerializeField] private Animator animator;

    [Tooltip("If true, flips the sprite horizontally when moving left.")]
    [SerializeField] private bool flipSpriteOnLeft = true;

    [Header("Debug & Status (Read Only)")]
    [SerializeField] private Vector2 currentInput;
    [SerializeField] private Vector2 moveVelocity;
    [SerializeField] private Vector2 lastFacingDirection = Vector2.down;
    [SerializeField] private Direction currentDirection = Direction.Down;
    [SerializeField] private bool isSprinting;

    private Rigidbody2D rb;

    // Animator parameter hashes for high performance
    private static readonly int AnimParamHorizontal = Animator.StringToHash("Horizontal");
    private static readonly int AnimParamVertical = Animator.StringToHash("Vertical");
    private static readonly int AnimParamSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimParamIsMoving = Animator.StringToHash("IsMoving");

    // Public properties for external gameplay scripts, UI, and interactions
    public Vector2 MoveInput => currentInput;
    public Vector2 LastFacingDirection => lastFacingDirection;
    public Direction CurrentDirection => currentDirection;
    public bool IsMoving => currentInput.sqrMagnitude > 0.001f;
    public bool IsSprinting => isSprinting;
    public float CurrentSpeed => isSprinting ? sprintSpeed : walkSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ConfigureRigidbody();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (directionIndicator == null)
        {
            Transform ind = transform.Find("FaceIndicator");
            if (ind != null) directionIndicator = ind;
        }
    }

    private void Start()
    {
        // Default facing down on start (classic Stardew Valley behavior)
        UpdateFacingDirection(Vector2.down);
    }

    private void Update()
    {
        GatherInput();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ConfigureRigidbody()
    {
        // Enforce top-down physics settings
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void GatherInput()
    {
        // Read raw input axes (-1, 0, 1)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 rawInput = new Vector2(x, y);

        // Normalize diagonal movement so diagonal isn't 41% faster
        currentInput = rawInput.sqrMagnitude > 1f ? rawInput.normalized : rawInput;

        // Check sprint key
        isSprinting = Input.GetKey(sprintKey);

        // Update facing direction when there is input
        if (rawInput.sqrMagnitude > 0.01f)
        {
            UpdateFacingDirection(rawInput);
        }
    }

    private void UpdateFacingDirection(Vector2 input)
    {
        // 4-way direction determination
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            currentDirection = input.x > 0 ? Direction.Right : Direction.Left;
            lastFacingDirection = input.x > 0 ? Vector2.right : Vector2.left;
        }
        else if (Mathf.Abs(input.y) > 0.01f)
        {
            currentDirection = input.y > 0 ? Direction.Up : Direction.Down;
            lastFacingDirection = input.y > 0 ? Vector2.up : Vector2.down;
        }

        // Reposition visual direction indicator
        if (directionIndicator != null)
        {
            Vector3 offset = new Vector3(lastFacingDirection.x * 0.22f, lastFacingDirection.y * 0.22f + 0.1f, 0f);
            directionIndicator.localPosition = offset;
        }
    }

    private void UpdateVisuals()
    {
        // Sprite flip handling
        if (spriteRenderer != null && flipSpriteOnLeft)
        {
            if (currentInput.x > 0.05f)
            {
                spriteRenderer.flipX = false;
            }
            else if (currentInput.x < -0.05f)
            {
                spriteRenderer.flipX = true;
            }
        }

        // Animator integration if present
        if (animator != null)
        {
            animator.SetFloat(AnimParamHorizontal, lastFacingDirection.x);
            animator.SetFloat(AnimParamVertical, lastFacingDirection.y);
            animator.SetFloat(AnimParamSpeed, currentInput.sqrMagnitude > 0.001f ? CurrentSpeed : 0f);
            animator.SetBool(AnimParamIsMoving, IsMoving);
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = CurrentSpeed;
        Vector2 targetVelocity = currentInput * targetSpeed;

        if (snappyMovement)
        {
            moveVelocity = targetVelocity;
        }
        else
        {
            float rate = currentInput.sqrMagnitude > 0.001f ? acceleration : deceleration;
            moveVelocity = Vector2.MoveTowards(moveVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        }

        rb.velocity = moveVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw facing direction ray in Scene view for visual debugging
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 dir = new Vector3(lastFacingDirection.x, lastFacingDirection.y, 0f) * 1.2f;
        Gizmos.DrawRay(origin, dir);
        Gizmos.DrawWireSphere(origin + dir, 0.12f);
    }
}
