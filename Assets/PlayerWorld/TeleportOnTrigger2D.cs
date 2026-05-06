using System.Collections;
using UnityEngine;

namespace Nivelo.PlayerWorld
{
  public class TeleportOnTrigger2D : MonoBehaviour
  {
    [Header("Detection")]
    [SerializeField, Tooltip("Tag used to detect the player collider.")]
    string playerTag = "Player";

    [SerializeField, Tooltip("If enabled, the trigger is disabled after first use.")]
    bool useOnlyOnce = true;

    [Header("Teleport")]
    [SerializeField, Tooltip("Destination transform where the player will be teleported.")]
    Transform teleportTarget;

    [Header("Fade")]
    [SerializeField, Tooltip("GameObject that plays the fade-in animation (transparent -> visible) when activated.")]
    GameObject visibleTransitionObject;

    [SerializeField, Tooltip("GameObject that plays the fade-out animation (visible -> transparent) when activated.")]
    GameObject notVisibleTransitionObject;

    [SerializeField, Min(0f), Tooltip("Seconds to wait after showing the panel before teleporting.")]
    float preTeleportDelay = 0.35f;

    [SerializeField, Min(0f), Tooltip("Duration of the fade-out animation after teleport (Visible -> Transparent).")]
    float postTeleportDelay = 0.1f;

    [SerializeField]
    GameObject [] deactivateOnWarp;
    

    bool isProcessing;

    

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
      if (visibleTransitionObject != null)
      {
        visibleTransitionObject.SetActive(false);
      }
    }

    void OnTriggerEnter2D(Collider2D otherCollider)
    {
      if (isProcessing || !otherCollider.CompareTag(playerTag))
      {
        return;
      }

      if (teleportTarget == null)
      {
        Debug.LogWarning("Teleport target is missing.", this);
        return;
      }

      StartCoroutine(TeleportRoutine(otherCollider.transform));
    }

    IEnumerator TeleportRoutine(Transform player)
    {
      isProcessing = true;

      PlayVisibleTransition();

      if (preTeleportDelay > 0f)
      {
        yield return new WaitForSeconds(preTeleportDelay);
      }

      player.position = teleportTarget.position;

      PlayNotVisibleTransition();

      if (postTeleportDelay > 0f)
      {
        yield return new WaitForSeconds(postTeleportDelay);
      }

      foreach (GameObject obj in deactivateOnWarp)
      {
        if (obj != null)
        {
          obj.SetActive(false);
        }
      }

      if (notVisibleTransitionObject != null)
      {
        notVisibleTransitionObject.SetActive(false);
      }

      if (useOnlyOnce)
      {
        gameObject.SetActive(false);
      }

      isProcessing = false;
    }

    void PlayVisibleTransition()
    {
      if (notVisibleTransitionObject != null)
      {
        notVisibleTransitionObject.SetActive(false);
      }

      if (visibleTransitionObject == null)
      {
        return;
      }

      visibleTransitionObject.SetActive(false);
      visibleTransitionObject.SetActive(true);
    }

    void PlayNotVisibleTransition()
    {
      if (visibleTransitionObject != null)
      {
        visibleTransitionObject.SetActive(false);
      }

      if (notVisibleTransitionObject == null)
      {
        return;
      }

      notVisibleTransitionObject.SetActive(false);
      notVisibleTransitionObject.SetActive(true);
    }
  }
}
