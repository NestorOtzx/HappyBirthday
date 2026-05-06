using HappyBirthday.Data;
using UnityEngine;

namespace HappyBirthday.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GardenConfig gardenConfig;

        public GardenConfig GardenConfig => gardenConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Configure(GardenConfig config)
        {
            if (config == null)
            {
                return;
            }

            gardenConfig = config;
        }
    }
}
