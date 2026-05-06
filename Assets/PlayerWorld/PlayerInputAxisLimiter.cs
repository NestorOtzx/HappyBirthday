using Nivelo.Player;
using UnityEngine;

namespace Nivelo.PlayerWorld
{
  [RequireComponent(typeof(Collider2D))]
  public class PlayerInputAxisLimiter : MonoBehaviour
  {
    public enum InputAxisLimitMode
    {
      None,
      HorizontalOnly,
      VerticalOnly,
      BlockAll,
      VerticalToHorizontalInverted
    }

    [SerializeField, Tooltip("Tag used to detect the player.")]
    string playerTag = "Player";

    [SerializeField, Tooltip("How movement input should be limited while player is inside trigger.")]
    InputAxisLimitMode limitMode;

    Collider2D triggerCollider;
    PlayerMovement currentPlayerMovement;
    Vector2 originalInputAxisMultiplier = Vector2.one;
    Vector2 originalInputRemapX = new Vector2(1f, 0f);
    Vector2 originalInputRemapY = new Vector2(0f, 1f);
    bool hasSavedMultiplier;

    void Reset()
    {
      Collider2D collider2d = GetComponent<Collider2D>();
      if (collider2d != null)
      {
        collider2d.isTrigger = true;
      }
    }

    void Awake()
    {
      triggerCollider = GetComponent<Collider2D>();
      if (triggerCollider != null)
      {
        triggerCollider.isTrigger = true;
      }
    }

    void Start()
    {
      TryApplyForPlayerAlreadyInside();
    }

    void OnValidate()
    {
      if (currentPlayerMovement != null)
      {
        ApplyLimit(currentPlayerMovement);
      }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      TryApplyLimit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
      TryApplyLimit(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
      if (!other.CompareTag(playerTag) || currentPlayerMovement == null)
      {
        return;
      }

      if (hasSavedMultiplier)
      {
        currentPlayerMovement.SetInputAxisMultiplier(originalInputAxisMultiplier);
        currentPlayerMovement.SetInputRemap(originalInputRemapX, originalInputRemapY);
      }

      currentPlayerMovement = null;
      hasSavedMultiplier = false;
    }

    public void SetLimitMode(InputAxisLimitMode newMode)
    {
      limitMode = newMode;
      if (currentPlayerMovement != null)
      {
        ApplyLimit(currentPlayerMovement);
      }
    }

    void TryApplyForPlayerAlreadyInside()
    {
      if (triggerCollider == null)
      {
        return;
      }

      GameObject player = GameObject.FindGameObjectWithTag(playerTag);
      if (player == null)
      {
        return;
      }

      Collider2D playerCollider = player.GetComponent<Collider2D>();
      if (playerCollider == null)
      {
        return;
      }

      if (!triggerCollider.bounds.Intersects(playerCollider.bounds))
      {
        return;
      }

      TryApplyLimit(playerCollider);
    }

    void TryApplyLimit(Collider2D other)
    {
      if (!other.CompareTag(playerTag))
      {
        return;
      }

      PlayerMovement playerMovement =
        other.GetComponent<PlayerMovement>() ??
        other.GetComponentInParent<PlayerMovement>();

      if (playerMovement == null)
      {
        return;
      }

      if (currentPlayerMovement != playerMovement)
      {
        currentPlayerMovement = playerMovement;
        originalInputAxisMultiplier = currentPlayerMovement.GetInputAxisMultiplier();
        originalInputRemapX = currentPlayerMovement.GetInputRemapX();
        originalInputRemapY = currentPlayerMovement.GetInputRemapY();
        hasSavedMultiplier = true;
      }

      ApplyLimit(currentPlayerMovement);
    }

    void ApplyLimit(PlayerMovement playerMovement)
    {
      if (playerMovement == null)
      {
        return;
      }

      playerMovement.SetInputRemap(
        new Vector2(1f, 0f),
        new Vector2(0f, 1f));

      switch (limitMode)
      {
        case InputAxisLimitMode.HorizontalOnly:
          // right/left are preserved via +x,
          // up contributes to left (-x),
          // down contributes to right (+x),
          // vertical output is cancelled.
          playerMovement.SetInputAxisMultiplier(Vector2.one);
          playerMovement.SetInputRemap(
            new Vector2(1f, -1f),
            Vector2.zero);
          break;

        case InputAxisLimitMode.VerticalOnly:
          playerMovement.SetInputAxisMultiplier(new Vector2(0f, 1f));
          break;

        case InputAxisLimitMode.BlockAll:
          playerMovement.SetInputAxisMultiplier(Vector2.zero);
          break;

        case InputAxisLimitMode.VerticalToHorizontalInverted:
          // right/left are preserved via +x,
          // up contributes to left (-x),
          // down contributes to right (+x),
          // vertical output is cancelled.
          playerMovement.SetInputAxisMultiplier(Vector2.one);
          playerMovement.SetInputRemap(
            new Vector2(1f, -1f),
            Vector2.zero);
          break;

        case InputAxisLimitMode.None:
        default:
          playerMovement.SetInputAxisMultiplier(Vector2.one);
          break;
      }
    }
  }
}
