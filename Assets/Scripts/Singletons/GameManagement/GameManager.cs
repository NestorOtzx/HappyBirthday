using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nivelo.PlayerWorld;

namespace Nivelo.Singletons.GameManagement
{
  public class GameManager : Singleton<GameManager>
  {
    [HideInInspector]
    PlayerManager playerWorld;

    GameScenes currentScene;

    protected override void Awake()
    {
      base.Awake();
      GameManager.Instance.playerWorld = FindObjectOfType<PlayerManager>();
    }

    public PlayerManager GetPlayerManager()
    {
      return playerWorld;
    }
  }
}