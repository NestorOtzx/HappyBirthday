using System;
using Nivelo.Enemies;
using UnityEditor;
using UnityEngine;

namespace Editor.Enemies
{
  [CustomEditor(typeof(EnemyWaypointRoute))]
  public class EnemyWaypointRouteEditor : UnityEditor.Editor
  {
    const float SegmentPickDistancePx = 10f;

    SerializedProperty waypointsProp;
    SerializedProperty linePlacementSpacingProp;
    SerializedProperty defaultSpeedMultiplierProp;
    SerializedProperty defaultWaitSecondsProp;

    int selectedWaypointIndex = -1;
    bool isSceneCallbackRegistered;

    void OnEnable()
    {
      waypointsProp = serializedObject.FindProperty("waypoints");
      linePlacementSpacingProp = serializedObject.FindProperty("linePlacementSpacing");
      defaultSpeedMultiplierProp = serializedObject.FindProperty("defaultSpeedMultiplier");
      defaultWaitSecondsProp = serializedObject.FindProperty("defaultWaitSeconds");

      if (!isSceneCallbackRegistered)
      {
        SceneView.duringSceneGui += OnSceneViewGUI;
        isSceneCallbackRegistered = true;
      }
    }

    void OnDisable()
    {
      if (!isSceneCallbackRegistered)
      {
        return;
      }

      SceneView.duringSceneGui -= OnSceneViewGUI;
      isSceneCallbackRegistered = false;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      EditorGUILayout.PropertyField(linePlacementSpacingProp);
      EditorGUILayout.PropertyField(defaultSpeedMultiplierProp);
      EditorGUILayout.PropertyField(defaultWaitSecondsProp);
      EditorGUILayout.Space();

      DrawWaypointArrayInspector();
      DrawInspectorControls();

      serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
      // Kept for compatibility with Unity editor lifecycle; actual drawing is handled in OnSceneViewGUI.
    }

    void OnSceneViewGUI(SceneView sceneView)
    {
      if (target == null ||
          Selection.activeObject != target)
      {
        return;
      }

      EnemyWaypointRoute route = (EnemyWaypointRoute)target;
      Event evt = Event.current;

      serializedObject.Update();

      DrawEdges();
      DrawWaypointHandles();
      HandleSceneShortcuts(route, evt);

      serializedObject.ApplyModifiedProperties();

      if (Event.current.type == EventType.Repaint)
      {
        sceneView.Repaint();
      }
    }

    void DrawWaypointArrayInspector()
    {
      for (int i = 0; i < waypointsProp.arraySize; i++)
      {
        SerializedProperty waypointProp = waypointsProp.GetArrayElementAtIndex(i);
        SerializedProperty positionProp = waypointProp.FindPropertyRelative("position");
        SerializedProperty speedProp = waypointProp.FindPropertyRelative("speedMultiplier");
        SerializedProperty waitProp = waypointProp.FindPropertyRelative("waitSeconds");

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Waypoint {i}", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(positionProp);
        EditorGUILayout.PropertyField(speedProp);
        EditorGUILayout.PropertyField(waitProp);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Seleccionar"))
        {
          selectedWaypointIndex = i;
          SceneView.RepaintAll();
        }

        if (GUILayout.Button("Eliminar"))
        {
          RemoveWaypoint(i);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
      }
    }

    void DrawInspectorControls()
    {
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Agregar Waypoint"))
      {
        AddWaypointAtEnd(Vector2.zero);
      }

      if (GUILayout.Button("Limpiar"))
      {
        waypointsProp.ClearArray();
        selectedWaypointIndex = -1;
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.HelpBox(
        "Atajos escena: Ctrl+X elimina waypoint seleccionado | Alt+Click en arista inserta waypoint | Shift+Click dibuja linea desde el ultimo waypoint hasta el click.",
        MessageType.Info
      );
    }

    void DrawEdges()
    {
      if (waypointsProp.arraySize < 2)
      {
        return;
      }

      Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
      for (int i = 0; i < waypointsProp.arraySize - 1; i++)
      {
        Vector3 a = GetWaypointPosition(i);
        Vector3 b = GetWaypointPosition(i + 1);
        Handles.DrawAAPolyLine(3f, a, b);
      }
    }

    void DrawWaypointHandles()
    {
      for (int i = 0; i < waypointsProp.arraySize; i++)
      {
        SerializedProperty waypointProp = waypointsProp.GetArrayElementAtIndex(i);
        SerializedProperty positionProp = waypointProp.FindPropertyRelative("position");

        Vector3 position = positionProp.vector2Value;
        float handleSize = HandleUtility.GetHandleSize(position) * 0.08f;

        Handles.color = i == selectedWaypointIndex
          ? new Color(1f, 0.84f, 0.2f, 1f)
          : new Color(0.95f, 0.33f, 0.2f, 1f);

        if (Handles.Button(position, Quaternion.identity, handleSize, handleSize * 1.2f, Handles.CircleHandleCap))
        {
          selectedWaypointIndex = i;
          Repaint();
        }

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(position, handleSize * 0.85f, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
          Undo.RecordObject(target, "Move waypoint");
          positionProp.vector2Value = new Vector2(moved.x, moved.y);
          selectedWaypointIndex = i;
          EditorUtility.SetDirty(target);
        }

        Handles.Label(position + Vector3.up * (handleSize * 4f), i.ToString());
      }
    }

    void HandleSceneShortcuts(EnemyWaypointRoute route, Event evt)
    {
      if (evt.type != EventType.MouseDown && evt.type != EventType.KeyDown)
      {
        return;
      }

      if (evt.type == EventType.KeyDown &&
          evt.control &&
          evt.keyCode == KeyCode.X)
      {
        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < waypointsProp.arraySize)
        {
          RemoveWaypoint(selectedWaypointIndex);
          evt.Use();
        }
        return;
      }

      if (evt.type != EventType.MouseDown || evt.button != 0)
      {
        return;
      }

      Vector2 worldClick = GetMouseWorldPosition(evt.mousePosition);

      if (evt.shift)
      {
        InsertStraightLineToClick(route, worldClick);
        evt.Use();
        return;
      }

      if (evt.alt)
      {
        int segmentIndex = GetClosestSegmentIndex(evt.mousePosition);
        if (segmentIndex >= 0)
        {
          InsertWaypoint(segmentIndex + 1, worldClick, route.DefaultSpeedMultiplier);
          selectedWaypointIndex = segmentIndex + 1;
          evt.Use();
          return;
        }
      }
    }

    void InsertStraightLineToClick(EnemyWaypointRoute route, Vector2 worldClick)
    {
      if (waypointsProp.arraySize == 0)
      {
        AddWaypointAtEnd(worldClick);
        selectedWaypointIndex = 0;
        return;
      }

      Vector2 start = GetWaypointPosition(waypointsProp.arraySize - 1);
      Vector2 delta = worldClick - start;
      float distance = delta.magnitude;

      if (distance <= 0.0001f)
      {
        return;
      }

      float spacing = Mathf.Max(0.05f, route.LinePlacementSpacing);
      Vector2 direction = delta / distance;
      float currentDistance = spacing;

      Undo.RecordObject(target, "Paint waypoint line");

      while (currentDistance < distance)
      {
        Vector2 point = start + direction * currentDistance;
        AddWaypointAtEnd(point, route.DefaultSpeedMultiplier, route.DefaultWaitSeconds);
        currentDistance += spacing;
      }

      AddWaypointAtEnd(worldClick, route.DefaultSpeedMultiplier, route.DefaultWaitSeconds);
      selectedWaypointIndex = waypointsProp.arraySize - 1;
      EditorUtility.SetDirty(target);
    }

    int GetClosestSegmentIndex(Vector2 mousePosition)
    {
      if (waypointsProp.arraySize < 2)
      {
        return -1;
      }

      int bestIndex = -1;
      float bestDistance = float.MaxValue;

      for (int i = 0; i < waypointsProp.arraySize - 1; i++)
      {
        Vector2 aGui = HandleUtility.WorldToGUIPoint(GetWaypointPosition(i));
        Vector2 bGui = HandleUtility.WorldToGUIPoint(GetWaypointPosition(i + 1));

        float distance = DistancePointToSegment(mousePosition, aGui, bGui);
        if (distance < bestDistance)
        {
          bestDistance = distance;
          bestIndex = i;
        }
      }

      return bestDistance <= SegmentPickDistancePx ? bestIndex : -1;
    }

    static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
      Vector2 ab = b - a;
      float lengthSq = ab.sqrMagnitude;
      if (lengthSq <= Mathf.Epsilon)
      {
        return Vector2.Distance(p, a);
      }

      float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
      Vector2 projection = a + ab * t;
      return Vector2.Distance(p, projection);
    }

    static Vector2 GetMouseWorldPosition(Vector2 guiPosition)
    {
      Ray worldRay = HandleUtility.GUIPointToWorldRay(guiPosition);
      Plane plane = new Plane(Vector3.forward, Vector3.zero);

      if (!plane.Raycast(worldRay, out float enter))
      {
        return Vector2.zero;
      }

      Vector3 point = worldRay.GetPoint(enter);
      return new Vector2(point.x, point.y);
    }

    void AddWaypointAtEnd(Vector2 position)
    {
      EnemyWaypointRoute route = (EnemyWaypointRoute)target;
      AddWaypointAtEnd(position, route.DefaultSpeedMultiplier, route.DefaultWaitSeconds);
    }

    void AddWaypointAtEnd(Vector2 position, float speedMultiplier)
    {
      EnemyWaypointRoute route = (EnemyWaypointRoute)target;
      AddWaypointAtEnd(position, speedMultiplier, route.DefaultWaitSeconds);
    }

    void AddWaypointAtEnd(Vector2 position, float speedMultiplier, float waitSeconds)
    {
      Undo.RecordObject(target, "Add waypoint");

      int index = waypointsProp.arraySize;
      waypointsProp.InsertArrayElementAtIndex(index);

      SerializedProperty waypointProp = waypointsProp.GetArrayElementAtIndex(index);
      waypointProp.FindPropertyRelative("position").vector2Value = position;
      waypointProp.FindPropertyRelative("speedMultiplier").floatValue = Mathf.Max(0f, speedMultiplier);
      waypointProp.FindPropertyRelative("waitSeconds").floatValue = Mathf.Max(0f, waitSeconds);

      EditorUtility.SetDirty(target);
    }

    void InsertWaypoint(int index, Vector2 position, float speedMultiplier)
    {
      EnemyWaypointRoute route = (EnemyWaypointRoute)target;
      InsertWaypoint(index, position, speedMultiplier, route.DefaultWaitSeconds);
    }

    void InsertWaypoint(int index, Vector2 position, float speedMultiplier, float waitSeconds)
    {
      Undo.RecordObject(target, "Insert waypoint");

      waypointsProp.InsertArrayElementAtIndex(index);
      SerializedProperty waypointProp = waypointsProp.GetArrayElementAtIndex(index);
      waypointProp.FindPropertyRelative("position").vector2Value = position;
      waypointProp.FindPropertyRelative("speedMultiplier").floatValue = Mathf.Max(0f, speedMultiplier);
      waypointProp.FindPropertyRelative("waitSeconds").floatValue = Mathf.Max(0f, waitSeconds);

      EditorUtility.SetDirty(target);
    }

    void RemoveWaypoint(int index)
    {
      Undo.RecordObject(target, "Remove waypoint");

      waypointsProp.DeleteArrayElementAtIndex(index);

      if (selectedWaypointIndex >= waypointsProp.arraySize)
      {
        selectedWaypointIndex = waypointsProp.arraySize - 1;
      }

      EditorUtility.SetDirty(target);
      Repaint();
      SceneView.RepaintAll();
    }

    Vector2 GetWaypointPosition(int index)
    {
      SerializedProperty waypointProp = waypointsProp.GetArrayElementAtIndex(index);
      return waypointProp.FindPropertyRelative("position").vector2Value;
    }
  }
}
