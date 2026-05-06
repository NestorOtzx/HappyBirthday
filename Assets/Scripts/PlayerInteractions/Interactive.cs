using UnityEngine;

namespace Nivelo.PlayerInteractions
{
  /// <summary>
  /// Base class for any interactive object in the world.
  /// <para></para>
  /// Handles player proximity detection and interaction input,
  /// and exposes virtual methods to be overridden by specific interactables.
  /// </summary>
  public class Interactive : MonoBehaviour {
    [Tooltip("Key used to interact with this object.")]
    [SerializeField]
    KeyCode interactionKey = KeyCode.F;
    bool _isPlayerInRange;
    GameObject _player;

    // UNITY LIFECYCLE METHODS //

    void Update() {
      if (!_isPlayerInRange) {
        return;
      }

      if (Input.GetKeyDown(interactionKey)) {
        Interact();
      }
    }

    // UNITY PHYSICS CALLBACKS //

    protected virtual void OnTriggerEnter2D(Collider2D other) {
      if (!other.CompareTag("Player")) {
        return;
      }

      _isPlayerInRange = true;
      _player = other.gameObject;

      OnPlayerEnterRange(_player);
    }

    void OnTriggerExit2D(Collider2D other) {
      if (!other.CompareTag("Player")) {
        return;
      }

      _isPlayerInRange = false;
      _player = null;

      OnPlayerExitRange();
    }

    // PUBLIC / PROTECTED API //

    /// <summary>
    /// Called when the player presses the interaction key while in range.
    /// <para></para>
    /// Override this method to implement custom interaction logic.
    /// </summary>
    protected virtual void Interact() {
      Debug.Log(
        $"[Interactive] Player interacted with '{name}' ({GetType().Name})",
        this
      );
    }

    /// <summary>
    /// Called when the player enters the interaction range.
    /// </summary>
    protected virtual void OnPlayerEnterRange(GameObject player) {
      Debug.Log(
        $"[Interactive] Player entered range of '{name}'",
        this
      );
    }

    /// <summary>
    /// Called when the player exits the interaction range.
    /// </summary>
    protected virtual void OnPlayerExitRange() {
      Debug.Log(
        $"[Interactive] Player exited range of '{name}'",
        this
      );
    }
  }
}
