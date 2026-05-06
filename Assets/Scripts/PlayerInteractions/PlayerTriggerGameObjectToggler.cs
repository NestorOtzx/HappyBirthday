using System.Collections.Generic;
using UnityEngine;

/// <summary>
///   Toggles GameObjects based on whether the player is inside this trigger.
///   <para></para>
///   If the player starts already inside the trigger when the scene loads,
///   the state will be correctly evaluated on Awake.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerTriggerGameObjectToggler : MonoBehaviour {

  // SERIALIZED CONFIGURATION FIELDS //

  [SerializeField]
  [Tooltip("Tag used to identify the player.")]
  string playerTag = "Player";

  [SerializeField]
  [Tooltip("GameObjects that will be enabled when the player is inside the trigger.")]
  GameObject[] objectsToEnable;

  [SerializeField]
  [Tooltip("GameObjects that will be disabled when the player is inside the trigger.")]
  GameObject[] objectsToDisable;

  // INTERNAL COMPONENT REFERENCE FIELDS //

  Collider triggerCollider;

  // INTERNAL CONFIGURATION FIELDS //

  bool isPlayerInside;

  // UNITY LIFECYCLE METHODS //

  void Awake() {
    triggerCollider = GetComponent<Collider>();
    triggerCollider.isTrigger = true;
  }

  void Start() {
    EvaluateInitialState();
  }

  void OnTriggerEnter(Collider other) {
    if (!other.CompareTag(playerTag)) return;
    isPlayerInside = true;
    ApplyState();
  }

  void OnTriggerExit(Collider other) {
    if (!other.CompareTag(playerTag)) return;
    isPlayerInside = false;
    ApplyState();
  }

  // INNER BUSINESS LOGIC METHODS //

  /// <summary>
  ///   Checks if the player is already inside the trigger when the scene starts.
  ///   <para></para>
  ///   Uses Physics.OverlapBox to evaluate current overlaps.
  /// </summary>
  void EvaluateInitialState() {

    BoxCollider boxCollider = triggerCollider as BoxCollider;
    if (boxCollider == null) {
      Debug.LogError("PlayerTriggerGameObjectToggler requires a BoxCollider.");
      return;
    }

    Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
    Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, transform.lossyScale);

    Collider[] overlaps = Physics.OverlapBox(
      worldCenter,
      halfExtents,
      transform.rotation
    );

    for (int i = 0; i < overlaps.Length; i++) {
      if (overlaps[i].CompareTag(playerTag)) {
        isPlayerInside = true;
        ApplyState();
        return;
      }
    }

    isPlayerInside = false;
    ApplyState();
  }

  /// <summary>
  ///   Applies the enable/disable state to the configured GameObjects.
  /// </summary>
  void ApplyState() {

    for (int i = 0; i < objectsToEnable.Length; i++) {
      if (objectsToEnable[i] != null)
        objectsToEnable[i].SetActive(isPlayerInside);
    }

    for (int i = 0; i < objectsToDisable.Length; i++) {
      if (objectsToDisable[i] != null)
        objectsToDisable[i].SetActive(!isPlayerInside);
    }
  }
}