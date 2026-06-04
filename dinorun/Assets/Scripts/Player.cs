using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AnimatedSprite))]
public class Player : MonoBehaviour
{
    private enum PlayerState
    {
        Waiting,
        Running,
        Jumping,
        Sliding,
        Dead
    }

    private CharacterController character;
    private Vector3 direction;
    private Vector3 spawnPosition;

    private PlayerState state = PlayerState.Waiting;

    [Header("Movement")]
    public float jumpForce = 8f;
    public float gravity = 9.81f * 2f;
    public float holdJumpForce = 8f;
    public float maxHoldJumpTime = 0.18f;
    public bool allowDoubleJump = true;
    public float crouchHeightMultiplier = 0.55f;
    public float slideDuration = 0.6f;

    [Header("Touch")]
    public float tapMaxTime = 0.2f;
    public float swipeThreshold = 50f;
    public float longPressJumpDelay = 0.08f;

    private float originalHeight;
    private Vector3 originalCenter;
    private int jumpsUsed;
    private float jumpHoldTimer;
    private float slideTimer;
    private bool jumpRequested;
    private bool jumpHeldInput;
    private bool crouchHeldInput;
    private bool touchActive;
    private bool touchMoved;
    private bool touchJumpConsumed;
    private Vector2 touchStartPosition;
    private float touchStartTime;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        originalHeight = character.height;
        originalCenter = character.center;
        spawnPosition = transform.position;
    }

    private void OnEnable()
    {
        ResetPlayer();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (state == PlayerState.Dead)
        {
            return;
        }

        if (GameManager.Instance.State != GameManager.GameState.Playing)
        {
            HandleTouchInput();
            HandleKeyboardInput();
            return;
        }

        HandleTouchInput();
        HandleKeyboardInput();

        if (character.isGrounded && direction.y <= 0f)
        {
            direction.y = -1f;
            jumpsUsed = 0;
            jumpHoldTimer = 0f;

            if (state == PlayerState.Jumping)
            {
                state = crouchHeldInput || slideTimer > 0f ? PlayerState.Sliding : PlayerState.Running;
            }

            if (state != PlayerState.Sliding && !crouchHeldInput)
            {
                SetCrouch(false);
                if (state != PlayerState.Waiting)
                {
                    state = PlayerState.Running;
                }
            }
        }

        if (crouchHeldInput || slideTimer > 0f)
        {
            if (character.isGrounded)
            {
                if (state != PlayerState.Sliding)
                {
                    state = PlayerState.Sliding;
                }

                SetCrouch(true);
            }

            if (slideTimer > 0f)
            {
                slideTimer -= Time.deltaTime;
            }

            if (slideTimer <= 0f && !crouchHeldInput)
            {
                SetCrouch(false);
                if (character.isGrounded)
                {
                    state = PlayerState.Running;
                }
            }
        }

        if (jumpRequested)
        {
            TryJump();
            jumpRequested = false;
        }

        if (state == PlayerState.Jumping && jumpHeldInput && jumpHoldTimer < maxHoldJumpTime && direction.y > 0f)
        {
            direction.y += holdJumpForce * Time.deltaTime;
            jumpHoldTimer += Time.deltaTime;
        }

        direction.y -= gravity * Time.deltaTime;

        character.Move(direction * Time.deltaTime);

        if (character.isGrounded && direction.y < 0f)
        {
            direction.y = -1f;
        }
    }

    public void ResetPlayer()
    {
        transform.position = spawnPosition;
        direction = Vector3.zero;
        jumpsUsed = 0;
        jumpHoldTimer = 0f;
        slideTimer = 0f;
        jumpRequested = false;
        jumpHeldInput = false;
        crouchHeldInput = false;
        touchActive = false;
        touchMoved = false;
        touchJumpConsumed = false;
        SetCrouch(false);
        state = PlayerState.Waiting;
    }

    public void BeginRun()
    {
        direction = Vector3.down;
        jumpsUsed = 0;
        jumpHoldTimer = 0f;
        slideTimer = 0f;
        jumpRequested = false;
        state = PlayerState.Running;
        SetCrouch(false);
    }

    public void SetDead()
    {
        state = PlayerState.Dead;
        direction = Vector3.zero;
        jumpRequested = false;
        jumpHeldInput = false;
        crouchHeldInput = false;
        slideTimer = 0f;
        SetCrouch(false);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
            jumpHeldInput = true;
        }

        if (Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space))
        {
            jumpHeldInput = false;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            crouchHeldInput = true;
            slideTimer = slideDuration;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.S))
        {
            crouchHeldInput = false;
        }
    }

    private void HandleTouchInput()
    {
        if (!Input.touchSupported || Input.touchCount == 0)
        {
            if (touchActive && !touchJumpConsumed && Time.time - touchStartTime <= tapMaxTime)
            {
                jumpRequested = true;
                jumpHeldInput = true;
            }

            touchActive = false;
            touchMoved = false;
            touchJumpConsumed = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchActive = true;
                touchMoved = false;
                touchJumpConsumed = false;
                touchStartPosition = touch.position;
                touchStartTime = Time.time;
                jumpHeldInput = true;
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
            {
                if (!touchActive)
                {
                    break;
                }

                Vector2 delta = touch.position - touchStartPosition;
                float absoluteX = Mathf.Abs(delta.x);
                float absoluteY = Mathf.Abs(delta.y);

                if (!touchMoved && absoluteX < swipeThreshold && absoluteY < swipeThreshold && Time.time - touchStartTime >= longPressJumpDelay)
                {
                    jumpRequested = true;
                    jumpHeldInput = true;
                    touchJumpConsumed = true;
                    touchMoved = true;
                }
                else if (!touchMoved && absoluteY >= swipeThreshold && absoluteY > absoluteX)
                {
                    touchMoved = true;

                    if (delta.y < 0f)
                    {
                        crouchHeldInput = true;
                        slideTimer = slideDuration;
                        jumpHeldInput = false;
                    }
                    else
                    {
                        jumpRequested = true;
                        jumpHeldInput = true;
                        touchJumpConsumed = true;
                    }
                }

                break;
            }

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!touchJumpConsumed)
                {
                    Vector2 delta = touch.position - touchStartPosition;

                    if (delta.y < -swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    {
                        crouchHeldInput = false;
                        slideTimer = slideDuration;
                    }
                    else if (Time.time - touchStartTime <= tapMaxTime)
                    {
                        jumpRequested = true;
                    }
                }

                touchActive = false;
                touchMoved = false;
                touchJumpConsumed = false;
                jumpHeldInput = false;
                break;
        }
    }

    private void TryJump()
    {
        int maxJumps = allowDoubleJump ? 2 : 1;

        if (character.isGrounded || jumpsUsed < maxJumps)
        {
            direction.y = jumpForce;
            jumpsUsed++;
            jumpHoldTimer = 0f;
            state = PlayerState.Jumping;
        }
    }

    private void SetCrouch(bool crouching)
    {
        if (character == null)
        {
            return;
        }

        float targetHeight = crouching ? originalHeight * crouchHeightMultiplier : originalHeight;
        character.height = targetHeight;
        character.center = crouching
            ? originalCenter + Vector3.down * ((originalHeight - targetHeight) * 0.5f)
            : originalCenter;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state == PlayerState.Dead)
        {
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            SetDead();
            GameManager.Instance.GameOver();
        }
    }

}
