using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nivelo.Animations
{
  /// <summary>
  /// Animates ellipsis (dots) after a base text.
  /// <para></para>
  /// Cycles from 0 to maxDots and restarts.
  /// Works with both Text and TextMeshProUGUI.
  /// </summary>
  public class AnimatedEllipsis : MonoBehaviour
  {
    // SERIALIZED CONFIGURATION FIELDS //

    [SerializeField]
    [Tooltip("Base text shown before the animated dots.")]
    string baseText = "Loading";

    [SerializeField]
    [Tooltip("Maximum number of dots to animate.")]
    int maxDots = 3;

    [SerializeField]
    [Tooltip("Time in seconds between each dot change.")]
    float dotInterval = 0.5f;

    [SerializeField]
    [Tooltip("Start animation automatically on enable.")]
    bool playOnEnable = true;

    // INTERNAL COMPONENT REFERENCE FIELDS //

    Text uiText;
    TextMeshProUGUI tmpText;

    // INTERNAL CONFIGURATION FIELDS //

    int currentDotCount;
    Coroutine animationCoroutine;

    // UNITY LIFECYCLE METHODS //

    void Awake()
    {
      uiText = GetComponent<Text>();
      tmpText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
      if (playOnEnable)
      {
        Play();
      }
    }

    void OnDisable()
    {
      Stop();
    }

    // PUBLIC API METHODS //

    /// <summary>
    /// Starts the ellipsis animation.
    /// <para></para>
    /// If already running, it will restart.
    /// </summary>
    public void Play()
    {
      Stop();

      currentDotCount = 0;
      animationCoroutine = StartCoroutine(Animate());
    }

    /// <summary>
    /// Stops the ellipsis animation and resets text to baseText.
    /// </summary>
    public void Stop()
    {
      if (animationCoroutine != null)
      {
        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
      }

      SetText(baseText);
    }

    /// <summary>
    /// Changes the base text at runtime.
    /// <para></para>
    /// Useful when switching states like "Saving", "Connecting", etc.
    /// </summary>
    public void SetBaseText(string newBaseText)
    {
      baseText = newBaseText;
      SetText(baseText);
    }

    // INNER BUSINESS LOGIC METHODS //

    IEnumerator Animate()
    {
      while (true)
      {
        string dots = new string('.', currentDotCount);
        SetText(baseText + dots);

        currentDotCount++;

        if (currentDotCount > maxDots)
        {
          currentDotCount = 0;
        }

        yield return new WaitForSeconds(dotInterval);
      }
    }

    // INNER HELPER METHODS //

    void SetText(string value)
    {
      if (uiText != null)
      {
        uiText.text = value;
      }

      if (tmpText != null)
      {
        tmpText.text = value;
      }
    }
  }
}
