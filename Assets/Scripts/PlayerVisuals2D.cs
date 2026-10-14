using UnityEngine;

/// <summary>
/// Controls 2D character sprite visuals, directional facing, and walk cycle animations
/// based on TopDownPlayerController state.
/// Supports 4-directional visuals (Down, Up, Side/Flipped) and customizable frame rates.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TopDownPlayerController))]
public class PlayerVisuals2D : MonoBehaviour
{
    [Header("Idle Sprites")]
    [Tooltip("Facing down / front view idle sprite")]
    [SerializeField] private Sprite idleDown;

    [Tooltip("Facing up / back view idle sprite (long braid)")]
    [SerializeField] private Sprite idleUp;

    [Tooltip("Facing right / side profile idle sprite")]
    [SerializeField] private Sprite idleSide;

    [Header("Walk Animation Cycles")]
    [Tooltip("Walk cycle frames facing down")]
    [SerializeField] private Sprite[] walkDown;

    [Tooltip("Walk cycle frames facing up")]
    [SerializeField] private Sprite[] walkUp;

    [Tooltip("Walk cycle frames facing right / side")]
    [SerializeField] private Sprite[] walkSide;

    [Header("Animation Settings")]
    [Tooltip("Walking animation speed in frames per second")]
    [SerializeField] private float walkFPS = 6f;

    [Tooltip("Sprinting animation speed multiplier")]
    [SerializeField] private float sprintFPSMultiplier = 1.4f;

    [Tooltip("Flip side sprites horizontally when moving/facing Left")]
    [SerializeField] private bool flipOnLeft = true;

    private SpriteRenderer sr;
    private TopDownPlayerController controller;

    private float animationTimer;
    private int currentFrameIndex;

    public Sprite IdleDown { get => idleDown; set => idleDown = value; }
    public Sprite IdleUp { get => idleUp; set => idleUp = value; }
    public Sprite IdleSide { get => idleSide; set => idleSide = value; }
    public Sprite[] WalkDown { get => walkDown; set => walkDown = value; }
    public Sprite[] WalkUp { get => walkUp; set => walkUp = value; }
    public Sprite[] WalkSide { get => walkSide; set => walkSide = value; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponent<TopDownPlayerController>();
    }

    private void Update()
    {
        if (sr == null || controller == null) return;

        bool isMoving = controller.IsMoving;
        TopDownPlayerController.Direction dir = controller.CurrentDirection;

        // 1. Horizontal Flip handling
        if (flipOnLeft)
        {
            if (dir == TopDownPlayerController.Direction.Left)
            {
                sr.flipX = true;
            }
            else if (dir == TopDownPlayerController.Direction.Right)
            {
                sr.flipX = false;
            }
            // Retain current flip when facing Up or Down
        }

        // 2. Sprite selection
        if (!isMoving)
        {
            // Idle state: reset timer & frame
            animationTimer = 0f;
            currentFrameIndex = 0;

            switch (dir)
            {
                case TopDownPlayerController.Direction.Down:
                    if (idleDown != null) sr.sprite = idleDown;
                    break;
                case TopDownPlayerController.Direction.Up:
                    if (idleUp != null) sr.sprite = idleUp;
                    break;
                case TopDownPlayerController.Direction.Left:
                case TopDownPlayerController.Direction.Right:
                    if (idleSide != null) sr.sprite = idleSide;
                    break;
            }
        }
        else
        {
            // Walking state: advance animation frame
            float fps = controller.IsSprinting ? walkFPS * sprintFPSMultiplier : walkFPS;
            animationTimer += Time.deltaTime * fps;

            Sprite[] currentCycle = null;
            switch (dir)
            {
                case TopDownPlayerController.Direction.Down:
                    currentCycle = (walkDown != null && walkDown.Length > 0) ? walkDown : null;
                    break;
                case TopDownPlayerController.Direction.Up:
                    currentCycle = (walkUp != null && walkUp.Length > 0) ? walkUp : null;
                    break;
                case TopDownPlayerController.Direction.Left:
                case TopDownPlayerController.Direction.Right:
                    currentCycle = (walkSide != null && walkSide.Length > 0) ? walkSide : null;
                    break;
            }

            if (currentCycle != null && currentCycle.Length > 0)
            {
                int frame = Mathf.FloorToInt(animationTimer) % currentCycle.Length;
                sr.sprite = currentCycle[frame];
            }
            else
            {
                // Fallback to idle sprite if walk cycle is missing
                switch (dir)
                {
                    case TopDownPlayerController.Direction.Down:
                        if (idleDown != null) sr.sprite = idleDown;
                        break;
                    case TopDownPlayerController.Direction.Up:
                        if (idleUp != null) sr.sprite = idleUp;
                        break;
                    case TopDownPlayerController.Direction.Left:
                    case TopDownPlayerController.Direction.Right:
                        if (idleSide != null) sr.sprite = idleSide;
                        break;
                }
            }
        }
    }
}
