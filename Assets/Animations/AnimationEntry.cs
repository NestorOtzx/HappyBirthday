using UnityEngine;

namespace Nivelo.Animations {
  [System.Serializable]
  public class AnimationEntry {
    [Tooltip("Unique name used to identify this animation.")]
    [SerializeField]
    string name;

    [Tooltip("Sequence of indices that reference the shared sprite array.")]
    [SerializeField]
    int[] frameIndices;

    [Tooltip("Total duration of the animation in seconds.")]
    [SerializeField]
    float durationSeconds = 1f;

    [Tooltip("Determines if the animation loops back to the start after finishing.")]
    [SerializeField]
    bool loop = true;

    [Tooltip("If true, automatically plays the next animation when this one finishes. Ignored if loop is enabled.")]
    [SerializeField]
    bool continueOnFinish;

    [Tooltip("If true, the render target is flipped on X for this animation.")]
    [SerializeField]
    bool flipX;

    [Tooltip("If true, the render target is flipped on Y for this animation.")]
    [SerializeField]
    bool flipY;

    public string GetName() {
      return name;
    }

    public int[] GetFrameIndices() {
      return frameIndices;
    }

    public float GetDuration() {
      return durationSeconds;
    }

    public bool GetLoop() {
      return loop;
    }

    public bool GetContinueOnFinish() {
      return continueOnFinish;
    }

    public bool GetFlipX() {
      return flipX;
    }

    public bool GetFlipY() {
      return flipY;
    }
  }
}
