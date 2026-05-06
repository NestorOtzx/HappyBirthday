using HappyBirthday.Data;
using HappyBirthday.SaveSystem;
using UnityEngine;

namespace HappyBirthday.Core
{
    public class CoreBootstrap : MonoBehaviour
    {
        [SerializeField] private GardenConfig gardenConfig;

        private void Awake()
        {
            GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            }

            SaveManager saveManager = SaveManager.Instance ?? FindFirstObjectByType<SaveManager>();
            if (saveManager == null)
            {
                new GameObject("SaveManager").AddComponent<SaveManager>();
            }

            TimeProgressionManager timeManager = TimeProgressionManager.Instance ?? FindFirstObjectByType<TimeProgressionManager>();
            if (timeManager == null)
            {
                timeManager = new GameObject("TimeProgressionManager").AddComponent<TimeProgressionManager>();
            }

            if (FindFirstObjectByType<SceneLoader>() == null)
            {
                new GameObject("SceneLoader").AddComponent<SceneLoader>();
            }

            if (AudioManager.Instance == null && FindFirstObjectByType<AudioManager>() == null)
            {
                new GameObject("AudioManager").AddComponent<AudioManager>();
            }

            gameManager.Configure(gardenConfig);
            timeManager.Configure(gardenConfig);
        }
    }
}
