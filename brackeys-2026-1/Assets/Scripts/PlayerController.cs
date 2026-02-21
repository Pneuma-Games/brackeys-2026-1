using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float coyoteTime = 0.15f;
    private float coyoteCounter;

    [Header("Physics Tweaks")]
    public float fallMultiplier = 4f;
    public float lowJumpMultiplier = 3f;

    [Header("Detection & Buffer")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public float jumpBufferTime = 0.1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isJumpPressed;
    private float jumpBufferCounter;

    private bool controlsReversed = false;
    private bool jumpInverted = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            Debug.LogError("[PlayerController] No Animator found on this GameObject or its children.");
        else
            Debug.Log($"[PlayerController] Animator found: {animator.gameObject.name}, Controller: {animator.runtimeAnimatorController}");

        if (spriteRenderer == null)
            Debug.LogWarning("[PlayerController] No SpriteRenderer found on this GameObject or its children.");
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        if (controlsReversed) moveInput = -moveInput;
    }

    public void OnJump(InputValue value)
    {
        isJumpPressed = value.isPressed;
        if (isJumpPressed)
            jumpBufferCounter = jumpBufferTime;
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            ExecuteJump();
            coyoteCounter = 0;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
            animator.SetBool("IsJumping", !isGrounded);
        }

        if (spriteRenderer != null && moveInput.x != 0)
            spriteRenderer.flipX = moveInput.x < 0;
    }

    private void ExecuteJump()
    {
        float force = jumpInverted ? -jumpForce : jumpForce;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        jumpBufferCounter = 0;
    }

    public void SetControlsReversed(bool reversed) => controlsReversed = reversed;
    public void SetJumpInverted(bool inverted) => jumpInverted = inverted;

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        ApplyJumpGravity();
    }

    private void ApplyJumpGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !isJumpPressed)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
    }
}