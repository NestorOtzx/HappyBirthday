using System;
using HappyBirthday.Data;
using UnityEngine;

namespace HappyBirthday.Core
{
    public class TimeProgressionManager : MonoBehaviour
    {
        public static TimeProgressionManager Instance { get; private set; }

        [SerializeField] private GardenConfig gardenConfig;

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
            if (config != null)
            {
                gardenConfig = config;
            }
        }

        public int GetUnlockedFlowerCount(DateTime? dateOverride = null)
        {
            int totalFlowers = gardenConfig != null ? gardenConfig.totalFlowers : 25;

            if (gardenConfig == null || !gardenConfig.dailyUnlockEnabled)
            {
                return totalFlowers;
            }

            DateTime today = (dateOverride ?? DateTime.Today).Date;
            int daysElapsed = (today - gardenConfig.StartDate).Days;
            return Mathf.Clamp(daysElapsed + 1, 0, totalFlowers);
        }

        public bool IsFlowerAvailable(int flowerId, DateTime? dateOverride = null)
        {
            return flowerId > 0 && flowerId <= GetUnlockedFlowerCount(dateOverride);
        }
    }
}
