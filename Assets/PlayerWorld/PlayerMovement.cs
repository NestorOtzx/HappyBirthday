using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace Nivelo.Player
{
  public class PlayerMovement : MonoBehaviour
  {
    [Header("Movement")]
    [SerializeField, Tooltip("Maximum speed the player can move at.")]
    float maxMoveSpeed = 6f;

    [SerializeField, Tooltip("How quickly the player accelerates when starting to move.")]
    float acceleration = 20f;

    [SerializeField, Tooltip("How quickly the player decelerates when stopping movement.")]
    float deceleration = 25f;

    [SerializeField, Tooltip("If enabled, the player will gradually slow down when there is no input.")]
    bool useFrictionWhenIdle = true;

    [SerializeField, Tooltip("Amount of friction applied when there is no movement input.")]
    float frictionAmount = 8f;

    [Header("Input")]
    [SerializeField, Tooltip("Name of the horizontal axis configured in the Input Manager.")]
    string horizontalAxis = "Horizontal";

    [SerializeField, Tooltip("Name of the vertical axis configured in the Input Manager.")]
    string verticalAxis = "Vertical";

    Rigidbody2D rb;
    Vector2 moveInput;
    Vector2 inputAxisMultiplier = Vector2.one;
    Vector2 inputRemapX = new Vector2(1f, 0f);
    Vector2 inputRemapY = new Vector2(0f, 1f);

    bool canMove;

    public void SetCanMove(bool t)
    {
      canMove = t;
      if (!rb)
      {
        rb = GetComponent<Rigidbody2D>();  
      }
      rb.linearVelocity = Vector2.zero;
    }

    void Awake()
    {
      rb = GetComponent<Rigidbody2D>();

      // top-down: no gravity
      rb.gravityScale = 0f;

      // lock rotation to avoid strange spins due to collisions
      rb.freezeRotation = true;
      SetCanMove(true);
    }

    void Update()
    {
      HandleInput();
    }

    void FixedUpdate()
    {
      HandleMovement();
    }

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float x = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            return new Vector2(x, y);
        }
  #endif

  #if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
  #else
        return Vector2.zero;
  #endif
    }

    void HandleInput()
    {
      float x = ReadMoveInput().x;
      float y = ReadMoveInput().y;

      Vector2 rawInput = new Vector2(x, y);
      Vector2 remappedInput = new Vector2(
        Vector2.Dot(rawInput, inputRemapX),
        Vector2.Dot(rawInput, inputRemapY));

      moveInput = new Vector2(
        remappedInput.x * inputAxisMultiplier.x,
        remappedInput.y * inputAxisMultiplier.y).normalized;
    }

    void HandleMovement()
    {
      if (!canMove) { return; }

      Vector2 targetVelocity = moveInput * maxMoveSpeed;

      // current velocity
      Vector2 velocity = rb.linearVelocity;

      // when there is input → accelerate toward target
      if (moveInput.magnitude > 0.01f)
      {
        Vector2 velocityDiff = targetVelocity - velocity;
        Vector2 accelerationStep = velocityDiff * acceleration;

        rb.AddForce(accelerationStep);
      }
      else if (useFrictionWhenIdle)
      {
        // no input → apply smooth braking
        Vector2 friction = -velocity * frictionAmount;
        rb.AddForce(friction);
      }

      // clamp to max speed to avoid overshooting due to accumulated forces
      if (rb.linearVelocity.magnitude > maxMoveSpeed)
      {
        rb.linearVelocity = rb.linearVelocity.normalized * maxMoveSpeed;
      }
    }

    public void SetInputAxisMultiplier(Vector2 multiplier)
    {
      inputAxisMultiplier = multiplier;
    }

    public Vector2 GetInputAxisMultiplier()
    {
      return inputAxisMultiplier;
    }

    public void SetInputRemap(Vector2 remapX, Vector2 remapY)
    {
      inputRemapX = remapX;
      inputRemapY = remapY;
    }

    public Vector2 GetInputRemapX()
    {
      return inputRemapX;
    }

    public Vector2 GetInputRemapY()
    {
      return inputRemapY;
    }
  }
}
