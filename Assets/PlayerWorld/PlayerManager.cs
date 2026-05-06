using System.Collections;
using System.Collections.Generic;
using Nivelo.Animations;
using Nivelo.SceneManagement.DataBuses;
using UnityEngine;


namespace Nivelo.PlayerWorld
{
  public class PlayerManager : MonoBehaviour
  {
  
    private void Awake()
    {
      SpriteAnimator spriteAnimator = GetComponentInChildren<SpriteAnimator>();
      if (spriteAnimator == null)
      {
        return;
      }

    
    }

    private void Start()
    {
      if (WorldDataBus.HasPendingValues)
      {
        WorldData worldData = WorldDataBus.Consume(true);
        transform.position = worldData.spawnPosition;
      }
    }

  }
}