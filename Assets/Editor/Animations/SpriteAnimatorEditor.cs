using System.Collections.Generic;
using Nivelo.Animations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor.Animations {
  [CustomEditor(typeof(SpriteAnimator))]
  [CanEditMultipleObjects]
  public class SpriteAnimatorEditor : UnityEditor.Editor {
    class PreviewTarget {
      public SpriteAnimator animator;
      public int animationIndex;
      public int previewFrame;
      public float frameDuration;
      public float frameTimer;
      public double lastEditorTime;
      public Image imageTarget;
      public Vector3 imageBaseScale;
    }

    SerializedProperty _renderModeProp;
    SerializedProperty _spriteRendererProp;
    SerializedProperty _imageProp;
    SerializedProperty _spritesProp;
    SerializedProperty _animationsProp;
    SerializedProperty _footstepClipsProp;
    SerializedProperty _footstepSpriteIndicesProp;
    SerializedProperty _startAnimationProp;
    SerializedProperty _startAnimationDelayProp;

    bool _isPreviewPlaying;
    readonly List<PreviewTarget> _previewTargets = new();

    void OnEnable() {
      _renderModeProp = serializedObject.FindProperty("renderMode");
      _spriteRendererProp = serializedObject.FindProperty("spriteRenderer");
      _imageProp = serializedObject.FindProperty("image");
      _spritesProp = serializedObject.FindProperty("sprites");
      _animationsProp = serializedObject.FindProperty("animations");
      _footstepClipsProp = serializedObject.FindProperty("footstepClips");
      _footstepSpriteIndicesProp =
        serializedObject.FindProperty("footstepSpriteIndices");
      _startAnimationProp = serializedObject.FindProperty("startAnimation");
      _startAnimationDelayProp =
        serializedObject.FindProperty("startAnimationDelay");
    }

    void OnDisable() {
      StopPreview();
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUILayout.LabelField("Sprite Index Animator", EditorStyles.boldLabel);
      EditorGUILayout.Space();

      EditorGUILayout.PropertyField(_renderModeProp);
      EditorGUILayout.Space();

      SpriteAnimatorRenderMode mode =
        (SpriteAnimatorRenderMode)_renderModeProp.enumValueIndex;

      if (UsesSpriteRenderer(mode)) {
        EditorGUILayout.PropertyField(_spriteRendererProp);
      }

      if (UsesImage(mode)) {
        EditorGUILayout.PropertyField(_imageProp);
      }

      EditorGUILayout.PropertyField(_spritesProp);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Footsteps", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(_footstepClipsProp, true);
      EditorGUILayout.PropertyField(_footstepSpriteIndicesProp, true);

      EditorGUILayout.PropertyField(_startAnimationProp);
      EditorGUILayout.PropertyField(_startAnimationDelayProp);

      DrawAnimations();

      serializedObject.ApplyModifiedProperties();
    }

    void DrawAnimations() {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

      EditorGUILayout.PropertyField(_animationsProp, true);

      if (serializedObject.isEditingMultipleObjects) {
        EditorGUILayout.HelpBox(
          "Preview runs on every selected SpriteAnimator object.",
          MessageType.Info
        );
      }

      if (!_animationsProp.isExpanded) {
        return;
      }

      EditorGUI.indentLevel++;

      for (int i = 0; i < _animationsProp.arraySize; i++) {
        SerializedProperty entry =
          _animationsProp.GetArrayElementAtIndex(i);

        SerializedProperty nameProp =
          entry.FindPropertyRelative("name");

        SerializedProperty frameIndicesProp =
          entry.FindPropertyRelative("frameIndices");

        SerializedProperty durationProp =
          entry.FindPropertyRelative("durationSeconds");

        bool isPlaying =
          _isPreviewPlaying && IsPreviewingAnimation(i);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        string animName = string.IsNullOrEmpty(nameProp.stringValue)
          ? $"Animation {i}"
          : nameProp.stringValue;

        GUIStyle labelStyle = new(EditorStyles.label);

        if (isPlaying) {
          labelStyle.fontStyle = FontStyle.Bold;
          labelStyle.normal.textColor = Color.green;
        }

        EditorGUILayout.LabelField(
          isPlaying ? $"▶ {animName}" : animName,
          labelStyle,
          GUILayout.Width(150)
        );

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("▶ Play", GUILayout.Width(70))) {
          StartPreview(i);
        }

        if (GUILayout.Button("⏹ Stop", GUILayout.Width(70))) {
          StopPreview();
        }

        EditorGUILayout.EndHorizontal();
      }

      EditorGUI.indentLevel--;
    }

    void StartPreview(int animationIndex) {
      StopPreview();

      _isPreviewPlaying = true;

      double currentTime = EditorApplication.timeSinceStartup;

      foreach (Object targetObject in serializedObject.targetObjects) {
        SpriteAnimator animator = targetObject as SpriteAnimator;

        if (animator == null) {
          continue;
        }

        SerializedObject animatorObject = new(animator);
        animatorObject.Update();

        SerializedProperty spritesProp = animatorObject.FindProperty("sprites");
        SerializedProperty animationsProp = animatorObject.FindProperty("animations");

        if (spritesProp == null ||
            animationsProp == null ||
            animationIndex < 0 ||
            animationIndex >= animationsProp.arraySize) {
          continue;
        }

        SerializedProperty entry =
          animationsProp.GetArrayElementAtIndex(animationIndex);

        SerializedProperty frameIndicesProp =
          entry.FindPropertyRelative("frameIndices");

        SerializedProperty durationProp =
          entry.FindPropertyRelative("durationSeconds");

        if (frameIndicesProp == null ||
            frameIndicesProp.arraySize == 0) {
          continue;
        }

        PreviewTarget previewTarget = new() {
          animator = animator,
          animationIndex = animationIndex,
          previewFrame = 0,
          frameTimer = 0f,
          frameDuration = Mathf.Max(0.01f, durationProp.floatValue) /
            frameIndicesProp.arraySize,
          lastEditorTime = currentTime
        };

        _previewTargets.Add(previewTarget);
      }

      if (_previewTargets.Count == 0) {
        StopPreview();
        return;
      }

      EditorApplication.update += PreviewTick;
    }

    void StopPreview() {
      if (!_isPreviewPlaying) {
        return;
      }

      EditorApplication.update -= PreviewTick;

      _isPreviewPlaying = false;
      _previewTargets.Clear();
    }

    void PreviewTick() {
      if (!_isPreviewPlaying || _previewTargets.Count == 0) {
        StopPreview();
        return;
      }

      double currentTime = EditorApplication.timeSinceStartup;
      for (int i = _previewTargets.Count - 1; i >= 0; i--) {
        if (!AdvancePreviewTarget(_previewTargets[i], currentTime)) {
          _previewTargets.RemoveAt(i);
        }
      }

      if (_previewTargets.Count == 0) {
        StopPreview();
        return;
      }

      SceneView.RepaintAll();
    }

    bool AdvancePreviewTarget(PreviewTarget previewTarget, double currentTime) {
      if (previewTarget == null || previewTarget.animator == null) {
        return false;
      }

      SerializedObject animatorObject = new(previewTarget.animator);
      animatorObject.Update();

      SerializedProperty renderModeProp = animatorObject.FindProperty("renderMode");
      SerializedProperty spriteRendererProp = animatorObject.FindProperty("spriteRenderer");
      SerializedProperty imageProp = animatorObject.FindProperty("image");
      SerializedProperty spritesProp = animatorObject.FindProperty("sprites");
      SerializedProperty animationsProp = animatorObject.FindProperty("animations");

      if (renderModeProp == null ||
          spriteRendererProp == null ||
          imageProp == null ||
          spritesProp == null ||
          animationsProp == null ||
          previewTarget.animationIndex < 0 ||
          previewTarget.animationIndex >= animationsProp.arraySize) {
        return false;
      }

      float deltaTime = (float)(currentTime - previewTarget.lastEditorTime);
      previewTarget.lastEditorTime = currentTime;
      previewTarget.frameTimer += deltaTime;

      if (previewTarget.frameTimer < previewTarget.frameDuration) {
        return true;
      }

      previewTarget.frameTimer -= previewTarget.frameDuration;

      SerializedProperty entry =
        animationsProp.GetArrayElementAtIndex(previewTarget.animationIndex);

      SerializedProperty frameIndicesProp =
        entry.FindPropertyRelative("frameIndices");

      SerializedProperty flipXProp =
        entry.FindPropertyRelative("flipX");

      SerializedProperty flipYProp =
        entry.FindPropertyRelative("flipY");

      if (frameIndicesProp == null ||
          frameIndicesProp.arraySize == 0) {
        return false;
      }

      if (previewTarget.previewFrame >= frameIndicesProp.arraySize) {
        previewTarget.previewFrame = 0;
      }

      int spriteIndex =
        frameIndicesProp.GetArrayElementAtIndex(previewTarget.previewFrame).intValue;

      if (spriteIndex < 0 || spriteIndex >= spritesProp.arraySize) {
        return true;
      }

      Sprite sprite =
        spritesProp.GetArrayElementAtIndex(spriteIndex).objectReferenceValue as Sprite;

      SpriteAnimatorRenderMode mode =
        (SpriteAnimatorRenderMode)renderModeProp.enumValueIndex;

      if (UsesSpriteRenderer(mode)) {
        SpriteRenderer renderer =
          spriteRendererProp.objectReferenceValue as SpriteRenderer;

        if (renderer != null) {
          renderer.sprite = sprite;
          renderer.flipX = flipXProp != null && flipXProp.boolValue;
          renderer.flipY = flipYProp != null && flipYProp.boolValue;
        }
      }

      if (UsesImage(mode)) {
        Image image = imageProp.objectReferenceValue as Image;

        if (image != null) {
          image.sprite = sprite;
          ApplyImagePreviewFlip(
            previewTarget,
            image,
            flipXProp != null && flipXProp.boolValue,
            flipYProp != null && flipYProp.boolValue
          );
        }
      }

      previewTarget.previewFrame++;

      if (previewTarget.previewFrame >= frameIndicesProp.arraySize) {
        previewTarget.previewFrame = 0;
      }

      return true;
    }

    bool IsPreviewingAnimation(int animationIndex) {
      for (int i = 0; i < _previewTargets.Count; i++) {
        PreviewTarget previewTarget = _previewTargets[i];

        if (previewTarget != null &&
            previewTarget.animationIndex == animationIndex) {
          return true;
        }
      }

      return false;
    }

    bool UsesSpriteRenderer(SpriteAnimatorRenderMode mode) {
      return mode == SpriteAnimatorRenderMode.SpriteRenderer ||
             mode == SpriteAnimatorRenderMode.Both;
    }

    bool UsesImage(SpriteAnimatorRenderMode mode) {
      return mode == SpriteAnimatorRenderMode.Image ||
             mode == SpriteAnimatorRenderMode.Both;
    }

    void ApplyImagePreviewFlip(
      PreviewTarget previewTarget,
      Image image,
      bool flipX,
      bool flipY)
    {
      if (previewTarget == null || image == null) {
        return;
      }

      Transform imageTransform = image.rectTransform != null
        ? image.rectTransform
        : image.transform;

      if (previewTarget.imageTarget != image) {
        previewTarget.imageTarget = image;
        previewTarget.imageBaseScale = imageTransform.localScale;
      }

      Vector3 currentScale = imageTransform.localScale;
      imageTransform.localScale = new Vector3(
        GetFlippedScale(currentScale.x, previewTarget.imageBaseScale.x, flipX),
        GetFlippedScale(currentScale.y, previewTarget.imageBaseScale.y, flipY),
        currentScale.z
      );
    }

    static float GetFlippedScale(float currentAxisScale, float baseAxisScale, bool flipped) {
      float magnitude = Mathf.Abs(currentAxisScale);
      if (Mathf.Approximately(magnitude, 0f)) {
        magnitude = Mathf.Abs(baseAxisScale);
      }

      float baseSign = baseAxisScale < 0f ? -1f : 1f;
      return magnitude * baseSign * (flipped ? -1f : 1f);
    }
  }
}
