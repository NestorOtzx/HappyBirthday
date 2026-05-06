using UnityEngine;

namespace Nivelo.Animations
{
  public class SpriteAnimatorVisibleDelayTrigger : MonoBehaviour
  {
    [Header("References")]
    [SerializeField, Tooltip("SpriteAnimator that will receive the animation trigger.")]
    SpriteAnimator spriteAnimator;

    [SerializeField, Tooltip("SpriteRenderer used to detect visibility. If empty, this GameObject renderer is used.")]
    SpriteRenderer targetRenderer;

    [Header("Trigger")]
    [SerializeField, Tooltip("Animation name to play after the renderer remains visible for the configured delay.")]
    string animationName;

    [SerializeField, Tooltip("Seconds that the renderer must remain visible before triggering the animation.")]
    float visibleDelaySeconds = 0.2f;

    [SerializeField, Tooltip("If true, uses PlayForced on SpriteAnimator.")]
    bool forcePlay = true;

    [SerializeField, Tooltip("If true, the trigger can only happen once in this object lifetime.")]
    bool triggerOnlyOnce;

    [SerializeField, Tooltip("If true, timer resets when the renderer is no longer visible.")]
    bool resetWhenInvisible = true;

    float _visibleTimer;
    bool _triggeredForCurrentVisibility;
    bool _triggeredEver;

    void Awake()
    {
      if (targetRenderer == null)
      {
        targetRenderer = GetComponent<SpriteRenderer>();
      }

      if (spriteAnimator == null)
      {
        spriteAnimator = GetComponent<SpriteAnimator>();
      }
    }

    void OnEnable()
    {
      _visibleTimer = 0f;
      _triggeredForCurrentVisibility = false;
    }

    void Update()
    {
      if (!Application.isPlaying ||
          spriteAnimator == null ||
          targetRenderer == null ||
          string.IsNullOrEmpty(animationName))
      {
        return;
      }

      if (triggerOnlyOnce && _triggeredEver)
      {
        return;
      }

      if (!targetRenderer.isVisible)
      {
        if (resetWhenInvisible)
        {
          _visibleTimer = 0f;
          _triggeredForCurrentVisibility = false;
        }

        return;
      }

      if (_triggeredForCurrentVisibility)
      {
        return;
      }

      _visibleTimer += Time.deltaTime;
      if (_visibleTimer < Mathf.Max(0f, visibleDelaySeconds))
      {
        return;
      }

      if (forcePlay)
      {
        spriteAnimator.PlayForced(animationName);
      }
      else
      {
        spriteAnimator.Play(animationName);
      }

      _triggeredForCurrentVisibility = true;
      _triggeredEver = true;
    }
  }
}
