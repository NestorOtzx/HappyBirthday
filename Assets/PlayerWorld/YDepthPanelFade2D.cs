using UnityEngine;
using UnityEngine.UI;

namespace Nivelo.UI
{
  /// <summary>
  /// Controls a UI panel opacity based on the Y position of a 2D player
  /// inside a trigger collider.
  /// <para></para>
  /// The panel becomes darker as the player moves towards the positive Y
  /// direction inside the trigger bounds.
  /// </summary>
  public class YDepthPanelFade2D : MonoBehaviour
  {
    public enum FadeDirection
    {
      BottomToTop,
      TopToBottom,
      LeftToRight,
      RightToLeft
    }

    // SERIALIZED CONFIGURATION FIELDS //

    [SerializeField]
    [Tooltip("Panel image whose opacity will be controlled.")]
    Image panelImage;

    [SerializeField]
    [Tooltip("Tag used to detect the player.")]
    string playerTag = "Player";

    [SerializeField]
    [Tooltip("Direction used to map player position to panel alpha.")]
    FadeDirection fadeDirection = FadeDirection.BottomToTop;

    [SerializeField]
    [Tooltip("Curve evaluated with normalized displacement (0=start, 1=end) to compute panel opacity.")]
    AnimationCurve opacityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // INTERNAL COMPONENT REFERENCE FIELDS //

    Collider2D triggerCollider;
    Transform playerTransform;

    // INTERNAL CONFIGURATION FIELDS //

    bool playerInside;
    float minX;
    float maxX;
    float minY;
    float maxY;

    // UNITY LIFECYCLE METHODS //

    void Awake()
    {
      triggerCollider = GetComponent<Collider2D>();
      panelImage.gameObject.SetActive(false);
    }

    void Update()
    {
      if (!playerInside || playerTransform == null)
        return;

      float normalizedDisplacement = GetNormalizedValueByDirection();
      float curveAlpha = opacityCurve.Evaluate(normalizedDisplacement);
      float alpha = Mathf.Clamp01(curveAlpha);

      Color color = panelImage.color;
      color.a = alpha;
      panelImage.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      if (!other.CompareTag(playerTag))
        return;

      playerTransform = other.transform;
      playerInside = true;

      Bounds bounds = triggerCollider.bounds;
      minX = bounds.min.x;
      maxX = bounds.max.x;
      minY = bounds.min.y;
      maxY = bounds.max.y;

      panelImage.gameObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
      if (!other.CompareTag(playerTag))
        return;

      playerInside = false;
      playerTransform = null;

      Color color = panelImage.color;
      color.a = 0f;
      panelImage.color = color;

      panelImage.gameObject.SetActive(false);
    }

    float GetNormalizedValueByDirection()
    {
      Vector3 playerPosition = playerTransform.position;

      switch (fadeDirection)
      {
        case FadeDirection.TopToBottom:
          return Mathf.InverseLerp(maxY, minY, playerPosition.y);

        case FadeDirection.LeftToRight:
          return Mathf.InverseLerp(minX, maxX, playerPosition.x);

        case FadeDirection.RightToLeft:
          return Mathf.InverseLerp(maxX, minX, playerPosition.x);

        case FadeDirection.BottomToTop:
        default:
          return Mathf.InverseLerp(minY, maxY, playerPosition.y);
      }
    }
  }
}
