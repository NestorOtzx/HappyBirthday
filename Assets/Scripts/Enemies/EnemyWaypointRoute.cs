using System;
using UnityEngine;

namespace Nivelo.Enemies
{
  [Serializable]
  public struct EnemyWaypoint
  {
    [SerializeField, Tooltip("World-space position for this waypoint.")]
    Vector2 position;

    [SerializeField, Tooltip("Speed multiplier applied while moving toward this waypoint.")]
    float speedMultiplier;

    [SerializeField, Tooltip("Seconds to wait on this waypoint before continuing to the next one.")]
    float waitSeconds;

    public Vector2 Position => position;
    public float SpeedMultiplier => speedMultiplier;
    public float WaitSeconds => waitSeconds;

    public EnemyWaypoint(Vector2 position, float speedMultiplier)
      : this(position, speedMultiplier, 0f)
    {
    }

    public EnemyWaypoint(Vector2 position, float speedMultiplier, float waitSeconds)
    {
      this.position = position;
      this.speedMultiplier = Mathf.Max(0f, speedMultiplier);
      this.waitSeconds = Mathf.Max(0f, waitSeconds);
    }

    public EnemyWaypoint WithPosition(Vector2 newPosition)
    {
      return new EnemyWaypoint(newPosition, speedMultiplier, waitSeconds);
    }

    public EnemyWaypoint WithSpeedMultiplier(float newMultiplier)
    {
      return new EnemyWaypoint(position, Mathf.Max(0f, newMultiplier), waitSeconds);
    }

    public EnemyWaypoint WithWaitSeconds(float newWaitSeconds)
    {
      return new EnemyWaypoint(position, speedMultiplier, Mathf.Max(0f, newWaitSeconds));
    }
  }

  [CreateAssetMenu(
    fileName = "EnemyWaypointRoute",
    menuName = "Nivelo/Enemies/Waypoint Route"
  )]
  public class EnemyWaypointRoute : ScriptableObject
  {
    [Header("Path")]
    [SerializeField, Tooltip("Ordered waypoint list used by patrol enemies.")]
    EnemyWaypoint[] waypoints = Array.Empty<EnemyWaypoint>();

    [Header("Editor")]
    [SerializeField, Tooltip("Spacing used by Shift+Click when painting a straight waypoint line.")]
    float linePlacementSpacing = 1f;

    [SerializeField, Tooltip("Default speed multiplier for newly created waypoints.")]
    float defaultSpeedMultiplier = 1f;

    [SerializeField, Tooltip("Default wait in seconds for newly created waypoints.")]
    float defaultWaitSeconds;

    public EnemyWaypoint[] Waypoints => waypoints;
    public int WaypointCount => waypoints?.Length ?? 0;
    public float LinePlacementSpacing => linePlacementSpacing;
    public float DefaultSpeedMultiplier => defaultSpeedMultiplier;
    public float DefaultWaitSeconds => defaultWaitSeconds;

    public EnemyWaypoint GetWaypoint(int index)
    {
      if (waypoints == null ||
          index < 0 ||
          index >= waypoints.Length)
      {
        return default;
      }

      return waypoints[index];
    }

    public Vector2 GetWaypointPosition(int index)
    {
      return GetWaypoint(index).Position;
    }

    public float GetWaypointSpeedMultiplier(int index)
    {
      EnemyWaypoint waypoint = GetWaypoint(index);
      return waypoint.SpeedMultiplier <= 0f ? 1f : waypoint.SpeedMultiplier;
    }

    public float GetWaypointWaitSeconds(int index)
    {
      EnemyWaypoint waypoint = GetWaypoint(index);
      return Mathf.Max(0f, waypoint.WaitSeconds);
    }

    public void SetWaypoints(EnemyWaypoint[] points)
    {
      waypoints = points ?? Array.Empty<EnemyWaypoint>();
    }

    void OnValidate()
    {
      linePlacementSpacing = Mathf.Max(0.05f, linePlacementSpacing);
      defaultSpeedMultiplier = Mathf.Max(0f, defaultSpeedMultiplier);
      defaultWaitSeconds = Mathf.Max(0f, defaultWaitSeconds);

      if (waypoints == null)
      {
        waypoints = Array.Empty<EnemyWaypoint>();
        return;
      }

      for (int i = 0; i < waypoints.Length; i++)
      {
        if (waypoints[i].SpeedMultiplier < 0f)
        {
          waypoints[i] = waypoints[i].WithSpeedMultiplier(0f);
        }

        if (waypoints[i].WaitSeconds < 0f)
        {
          waypoints[i] = waypoints[i].WithWaitSeconds(0f);
        }
      }
    }
  }
}
