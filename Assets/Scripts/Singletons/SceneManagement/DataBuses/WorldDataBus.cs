using UnityEngine;
using System.Collections.Generic;
using Nivelo.Singletons;
using Unity.VisualScripting;

namespace Nivelo.SceneManagement.DataBuses
{
  /// <summary>
  /// One-time data bus used to pass combat data between scenes.
  /// <para></para>
  /// Data is written explicitly before loading the combat scene and
  /// consumed atomically by the combat scene. Once consumed, the data
  /// is optionally invalidated to prevent accidental reuse.
  /// </summary>
  public static class WorldDataBus
  {
    // INTERNAL STATE //
    static Vector3 spawnPositionValue;

    static bool hasPendingValues;
    
    public static bool HasPendingValues
    {
      get
      {
        return hasPendingValues;
      }
    }

    // PUBLIC WRITE API //
    public static void Set(WorldData data)
    {
      spawnPositionValue = data.spawnPosition;
      hasPendingValues=true;
    }

    // PUBLIC READ API //
    public static WorldData Consume(bool clearAfterRead = true)
    {
      WorldData data = new()
      {
        spawnPosition = spawnPositionValue
      };

      if (!clearAfterRead)
      {
        return data;
      }

      spawnPositionValue = Vector3.zero;
      hasPendingValues = false;

      return data;
    }
  }
}
