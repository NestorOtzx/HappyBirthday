using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nivelo.Animations {
  public class SpriteAnimator : MonoBehaviour {
    [Tooltip("Determines which rendering targets are driven by this animator.")]
    [SerializeField]
    SpriteAnimatorRenderMode renderMode = SpriteAnimatorRenderMode.SpriteRenderer;

    [Tooltip("SpriteRenderer target (used when Render Mode includes SpriteRenderer).")]
    [SerializeField]
    SpriteRenderer spriteRenderer;

    [Tooltip("UI Image target (used when Render Mode includes Image).")]
    [SerializeField]
    Image image;

    [Tooltip("Shared sprite list used by all animations. Animation frames reference this array by index.")]
    [SerializeField]
    Sprite[] sprites;

    [Tooltip("List of animations available for this animator.")]
    [SerializeField]
    AnimationEntry[] animations;

    [Tooltip("Footstep clips picked at random when a configured sprite frame is reached.")]
    [SerializeField]
    AudioClip[] footstepClips;

    [Tooltip("Sprite frame indices where footsteps are allowed to play.")]
    [SerializeField]
    int[] footstepSpriteIndices;

    [SerializeField]
    float startAnimationDelay;

    [Tooltip("Name of the animation that should start playing automatically when the scene begins.")]
    [SerializeField]
    string startAnimation;

    AnimationEntry _currentAnimation;
    int _currentFrame;
    float _timer;
    bool _isPlayLocked;

    float _currentStartAnimationDelay;
    Image _cachedFlipImageTarget;
    Vector3 _cachedImageBaseScale = Vector3.one;
    AudioSource _footstepAudioSource;
    readonly Queue<AudioClip> _pendingFootstepClips = new Queue<AudioClip>();
    Coroutine _footstepRoutine;

    // UNITY LIFECYCLE METHODS //

    void Awake() {
      ResolveRenderTargets();
      ResolveFootstepAudioSource();
    }

    void Start() {
      if (!Application.isPlaying) {
        return;
      }

      ResolveRenderTargets();

      if (animations == null || animations.Length == 0) {
        return;
      }

      Play(startAnimation);
      _currentStartAnimationDelay = startAnimationDelay;
    }

    void Update() {
      if (!Application.isPlaying) {
        return;
      }

      _currentStartAnimationDelay -= Time.deltaTime;
      if (_currentStartAnimationDelay > 0)
      {
        return;
      }

      if (_currentAnimation == null ||
          _currentAnimation.GetFrameIndices() == null ||
          _currentAnimation.GetFrameIndices().Length == 0 ||
          sprites == null) {
        return;
      }

      _timer += Time.deltaTime;

      float frameDuration =
        _currentAnimation.GetDuration() /
        _currentAnimation.GetFrameIndices().Length;

      if (_timer < frameDuration) {
        return;
      }

      _timer -= frameDuration;
      _currentFrame++;
      

      if (_currentFrame >= _currentAnimation.GetFrameIndices().Length) {
        if (_currentAnimation.GetLoop()) {
          _currentFrame = 0;
        }
        else {
          if (_currentAnimation.GetContinueOnFinish()) {
            PlayNextAnimation();
            return;
          }

          _currentFrame =
            _currentAnimation.GetFrameIndices().Length - 1;
        }
      }

      ApplyCurrentFrame();
      TryPlayFootstepForCurrentFrame();
    }

    // PUBLIC API METHODS //

    public void Play(string animationName) {
      if (_isPlayLocked) {
        return;
      }

      PlayInternal(animationName);
    }

    public void PlayForced(string animationName) {
      _currentStartAnimationDelay = 0f;
      PlayInternal(animationName);
    }

    public void SetPlayLocked(bool locked) {
      _isPlayLocked = locked;
    }

    public bool IsPlayLocked() {
      return _isPlayLocked;
    }

    public void SetStartAnimation(string animationName) {
      startAnimation = animationName;
    }

    public string GetStartAnimation() {
      return startAnimation;
    }

    public void SetStartAnimationDelay(float delaySeconds) {
      startAnimationDelay = Mathf.Max(0f, delaySeconds);
      _currentStartAnimationDelay = startAnimationDelay;
    }

    public float GetStartAnimationDelay() {
      return startAnimationDelay;
    }

    public AnimationEntry[] GetAnimations() {
      return CloneAnimationEntries(animations);
    }

    public void SetAnimations(AnimationEntry[] animationsIn) {
      animations = CloneAnimationEntries(animationsIn);
    }

    void PlayInternal(string animationName) {
      if (animations == null) {
        return;
      }

      for (int i = 0; i < animations.Length; i++) {
        if (animations[i].GetName() == animationName) {
          ClearFootstepQueue();
          _currentAnimation = animations[i];
          _currentFrame = 0;
          _timer = 0f;
          ApplyCurrentFrame();
          TryPlayFootstepForCurrentFrame();
          return;
        }
      }
    }

    public SpriteAnimatorRenderMode GetRenderMode() {
      return renderMode;
    }

    public bool TryGetAnimationDuration(string animationName, out float durationSeconds) {
      durationSeconds = 0f;

      AnimationEntry entry = FindAnimationEntry(animationName);
      if (entry == null) {
        return false;
      }

      durationSeconds = Mathf.Max(0f, entry.GetDuration());
      return true;
    }

    // INNER BUSINESS LOGIC METHODS //

    void PlayNextAnimation() {
      for (int i = 0; i < animations.Length; i++) {
        if (animations[i] == _currentAnimation) {
          int nextIndex = i + 1;

          if (nextIndex < animations.Length) {
            Play(animations[nextIndex].GetName());
          }

          return;
        }
      }
    }

    AnimationEntry FindAnimationEntry(string animationName) {
      if (animations == null || string.IsNullOrEmpty(animationName)) {
        return null;
      }

      for (int i = 0; i < animations.Length; i++) {
        if (animations[i] != null && animations[i].GetName() == animationName) {
          return animations[i];
        }
      }

      return null;
    }

    void ApplyCurrentFrame() {
      ResolveRenderTargets();

      if (_currentAnimation == null ||
          sprites == null) {
        return;
      }

      int[] indices = _currentAnimation.GetFrameIndices();

      if (indices == null ||
          indices.Length == 0 ||
          _currentFrame < 0 ||
          _currentFrame >= indices.Length) {
        return;
      }

      int spriteIndex = indices[_currentFrame];

      if (spriteIndex < 0 || spriteIndex >= sprites.Length) {
        return;
      }

      Sprite sprite = sprites[spriteIndex];

      if (UsesSpriteRenderer() && spriteRenderer != null) {
        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = _currentAnimation.GetFlipX();
        spriteRenderer.flipY = _currentAnimation.GetFlipY();
      }

      if (UsesImage() && image != null) {
        image.sprite = sprite;
        ApplyImageFlip(
          _currentAnimation.GetFlipX(),
          _currentAnimation.GetFlipY()
        );
      }
    }

    void ResolveFootstepAudioSource()
    {
      if (_footstepAudioSource != null)
      {
        return;
      }

      _footstepAudioSource = GetComponent<AudioSource>();
      if (_footstepAudioSource == null)
      {
        _footstepAudioSource = gameObject.AddComponent<AudioSource>();
      }

      _footstepAudioSource.playOnAwake = false;
      _footstepAudioSource.loop = false;
      _footstepAudioSource.spatialBlend = 1f;
      _footstepAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
      _footstepAudioSource.minDistance = 1f;
      _footstepAudioSource.maxDistance = 20f;
    }

    void TryPlayFootstepForCurrentFrame()
    {
      if (_currentAnimation == null || footstepClips == null || footstepClips.Length == 0)
      {
        return;
      }

      int[] frameIndices = _currentAnimation.GetFrameIndices();
      if (frameIndices == null || _currentFrame < 0 || _currentFrame >= frameIndices.Length)
      {
        return;
      }

      int spriteIndex = frameIndices[_currentFrame];
      if (!IsFootstepFrame(spriteIndex))
      {
        return;
      }

      AudioClip clip = GetRandomFootstepClip();
      if (clip == null)
      {
        return;
      }

      _pendingFootstepClips.Enqueue(clip);

      if (_footstepRoutine == null)
      {
        _footstepRoutine = StartCoroutine(PlayQueuedFootsteps());
      }
    }

    bool IsFootstepFrame(int spriteIndex)
    {
      if (footstepSpriteIndices == null || footstepSpriteIndices.Length == 0)
      {
        return false;
      }

      for (int i = 0; i < footstepSpriteIndices.Length; i++)
      {
        if (footstepSpriteIndices[i] == spriteIndex)
        {
          return true;
        }
      }

      return false;
    }

    AudioClip GetRandomFootstepClip()
    {
      if (footstepClips == null || footstepClips.Length == 0)
      {
        return null;
      }

      int attempts = footstepClips.Length;
      while (attempts-- > 0)
      {
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null)
        {
          return clip;
        }
      }

      return null;
    }

    void ClearFootstepQueue()
    {
      if (_footstepRoutine != null)
      {
        StopCoroutine(_footstepRoutine);
        _footstepRoutine = null;
      }

      _pendingFootstepClips.Clear();

      if (_footstepAudioSource != null)
      {
        _footstepAudioSource.Stop();
      }
    }

    IEnumerator PlayQueuedFootsteps()
    {
      ResolveFootstepAudioSource();

      while (_pendingFootstepClips.Count > 0)
      {
        AudioClip clip = _pendingFootstepClips.Dequeue();
        if (clip == null)
        {
          continue;
        }

        _footstepAudioSource.PlayOneShot(clip);
        yield return new WaitWhile(() => _footstepAudioSource != null && _footstepAudioSource.isPlaying);
      }

      _footstepRoutine = null;
    }

    void ApplyImageFlip(bool flipX, bool flipY)
    {
      if (image == null)
      {
        return;
      }

      Transform imageTransform = image.rectTransform != null
        ? image.rectTransform
        : image.transform;

      CacheImageBaseScale(imageTransform);

      Vector3 currentScale = imageTransform.localScale;
      imageTransform.localScale = new Vector3(
        GetFlippedScale(currentScale.x, _cachedImageBaseScale.x, flipX),
        GetFlippedScale(currentScale.y, _cachedImageBaseScale.y, flipY),
        currentScale.z
      );
    }

    bool UsesSpriteRenderer() {
      return renderMode == SpriteAnimatorRenderMode.SpriteRenderer ||
             renderMode == SpriteAnimatorRenderMode.Both;
    }

    bool UsesImage() {
      return renderMode == SpriteAnimatorRenderMode.Image ||
             renderMode == SpriteAnimatorRenderMode.Both;
    }

    public Sprite [] GetSprites()
    {
      return sprites;
    }

    public void SetSprites(Sprite [] spritesIn)
    {
      sprites = spritesIn;
    }

    public void SetRenderScale(Vector3 scaleMultiplier)
    {
      ResolveRenderTargets();

      Transform renderTransform = null;

      if (spriteRenderer != null)
      {
        renderTransform = spriteRenderer.transform;
      }

      if (renderTransform == null)
      {
        renderTransform =
          image != null && image.rectTransform != null
            ? image.rectTransform
            : transform;
      }

      renderTransform.localScale = Vector3.Scale(renderTransform.localScale, scaleMultiplier);

      if (image != null &&
          (renderTransform == image.transform ||
           (image.rectTransform != null && renderTransform == image.rectTransform)))
      {
        CacheImageBaseScale(renderTransform);
      }
    }

    void ResolveRenderTargets() {
      if (UsesSpriteRenderer() && spriteRenderer == null) {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null) {
          spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer == null) {
          spriteRenderer = GetComponentInParent<SpriteRenderer>();
        }
      }

      if (UsesImage() && image == null) {
        image = GetComponent<Image>();

        if (image == null) {
          image = GetComponentInChildren<Image>(true);
        }

        if (image == null) {
          image = GetComponentInParent<Image>();
        }
      }

      if (image != null)
      {
        Transform imageTransform = image.rectTransform != null
          ? image.rectTransform
          : image.transform;

        CacheImageBaseScale(imageTransform);
      }
    }

    void CacheImageBaseScale(Transform imageTransform)
    {
      if (image == null || imageTransform == null)
      {
        _cachedFlipImageTarget = null;
        _cachedImageBaseScale = Vector3.one;
        return;
      }

      if (_cachedFlipImageTarget == image)
      {
        return;
      }

      _cachedFlipImageTarget = image;
      _cachedImageBaseScale = imageTransform.localScale;
    }

    static float GetFlippedScale(float currentAxisScale, float baseAxisScale, bool flipped)
    {
      float magnitude = Mathf.Abs(currentAxisScale);
      if (Mathf.Approximately(magnitude, 0f))
      {
        magnitude = Mathf.Abs(baseAxisScale);
      }

      float baseSign = baseAxisScale < 0f ? -1f : 1f;
      return magnitude * baseSign * (flipped ? -1f : 1f);
    }

    static AnimationEntry[] CloneAnimationEntries(AnimationEntry[] source)
    {
      if (source == null)
      {
        return null;
      }

      AnimationEntry[] clone = new AnimationEntry[source.Length];
      for (int i = 0; i < source.Length; i++)
      {
        if (source[i] == null)
        {
          clone[i] = null;
          continue;
        }

        string json = JsonUtility.ToJson(source[i]);
        clone[i] = JsonUtility.FromJson<AnimationEntry>(json);
      }

      return clone;
    }
  }
}
