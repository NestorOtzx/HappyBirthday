using UnityEngine;
using Nivelo.Animations;

namespace Nivelo.Player
{
  /// <summary>
  /// Controls player sprite animations based solely on world-space movement.
  /// <para></para>
  /// Determines idle and walk animations using Rigidbody2D velocity,
  /// without relying on input or gameplay logic.
  /// </summary>
  public class CharacterMovementAnimatorController : MonoBehaviour
  {
    // SERIALIZED CONFIGURATION FIELDS //
    [Header("Animator Reference")]
    [SerializeField, Tooltip("SpriteAnimator component responsible for playing sprite animations.")]
    SpriteAnimator spriteAnimator;

    [Header("Movement Detection")]
    [SerializeField, Tooltip("Minimum velocity required to be considered as moving.")]
    float velocityThreshold = 0.01f;

    [SerializeField, Tooltip("If enabled, diagonal movement will be resolved to the dominant axis.")]
    bool prioritizeDominantAxis = true;

    [Header("Idle Animation Names")]
    [SerializeField, Tooltip("Idle animation played when facing up.")]
    string idleUpAnimation = "Idle_Up";

    [SerializeField, Tooltip("Idle animation played when facing down.")]
    string idleDownAnimation = "Idle_Down";

    [SerializeField, Tooltip("Idle animation played when facing left.")]
    string idleLeftAnimation = "Idle_Left";

    [SerializeField, Tooltip("Idle animation played when facing right.")]
    string idleRightAnimation = "Idle_Right";

    [Header("Walk Animation Names")]
    [SerializeField, Tooltip("Walk animation played when moving up.")]
    string walkUpAnimation = "Walk_Up";

    [SerializeField, Tooltip("Walk animation played when moving down.")]
    string walkDownAnimation = "Walk_Down";

    [SerializeField, Tooltip("Walk animation played when moving left.")]
    string walkLeftAnimation = "Walk_Left";

    [SerializeField, Tooltip("Walk animation played when moving right.")]
    string walkRightAnimation = "Walk_Right";

    // INTERNAL COMPONENT REFERENCE FIELDS //
    Rigidbody2D _rb;

    // INTERNAL CONFIGURATION FIELDS //
    Vector2 _lastFacingDirection = Vector2.down;
    string _currentAnimation;
    bool _isAnimationControlBlocked;

    // UNITY LIFECYCLE METHODS //
    void Awake()
    {
      _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
      UpdateAnimation();
    }

    // INNER BUSINESS LOGIC METHODS //
    void UpdateAnimation()
    {
      if (spriteAnimator == null || _rb == null)
      {
        return;
      }

      if (_isAnimationControlBlocked)
      {
        return;
      }

      Vector2 velocity = _rb.linearVelocity;
      bool isMoving = velocity.sqrMagnitude > velocityThreshold * velocityThreshold;

      if (isMoving)
      {
        Vector2 direction = ResolveDirection(velocity);
        _lastFacingDirection = direction;
        PlayWalkAnimation(direction);
      }
      else
      {
        PlayIdleAnimation(_lastFacingDirection);
      }
    }

    Vector2 ResolveDirection(Vector2 vector)
    {
      if (!prioritizeDominantAxis)
      {
        return vector.normalized;
      }

      if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
      {
        return new Vector2(Mathf.Sign(vector.x), 0f);
      }

      return new Vector2(0f, Mathf.Sign(vector.y));
    }

    // INNER HELPER METHODS //
    void PlayWalkAnimation(Vector2 direction)
    {
      if (direction.y > 0f)
      {
        PlayIfDifferent(walkUpAnimation);
      }
      else if (direction.y < 0f)
      {
        PlayIfDifferent(walkDownAnimation);
      }
      else if (direction.x < 0f)
      {
        PlayIfDifferent(walkLeftAnimation);
      }
      else if (direction.x > 0f)
      {
        PlayIfDifferent(walkRightAnimation);
      }
    }

    void PlayIdleAnimation(Vector2 direction)
    {
      if (direction.y > 0f)
      {
        PlayIfDifferent(idleUpAnimation);
      }
      else if (direction.y < 0f)
      {
        PlayIfDifferent(idleDownAnimation);
      }
      else if (direction.x < 0f)
      {
        PlayIfDifferent(idleLeftAnimation);
      }
      else if (direction.x > 0f)
      {
        PlayIfDifferent(idleRightAnimation);
      }
    }

    void PlayIfDifferent(string animationName)
    {
      if (string.IsNullOrEmpty(animationName) ||
          _currentAnimation == animationName)
      {
        return;
      }

      _currentAnimation = animationName;
      spriteAnimator.Play(animationName);
    }

    // PUBLIC API METHODS //
    public void SetAnimationControlBlocked(bool blocked)
    {
      _isAnimationControlBlocked = blocked;
    }

    public void ClearCurrentAnimationCache()
    {
      _currentAnimation = null;
    }

    public void ForceIdleHorizontalFacing(bool faceRight)
    {
      _lastFacingDirection = faceRight ? Vector2.right : Vector2.left;

      if (spriteAnimator == null)
      {
        return;
      }

      string targetAnimation = faceRight ? idleRightAnimation : idleLeftAnimation;
      if (string.IsNullOrEmpty(targetAnimation) || _currentAnimation == targetAnimation)
      {
        return;
      }

      _currentAnimation = targetAnimation;
      spriteAnimator.SetPlayLocked(false);
      spriteAnimator.PlayForced(targetAnimation);
    }
  }
}
