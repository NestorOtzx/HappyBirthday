using System;
using UnityEngine;

namespace HappyBirthday.Data
{
    [CreateAssetMenu(fileName = "GardenConfig", menuName = "Happy Birthday/Garden Config")]
    public class GardenConfig : ScriptableObject
    {
        [Header("Daily Unlock")]
        [Min(1)] public int totalFlowers = 25;
        public bool dailyUnlockEnabled = true;

        [Tooltip("First calendar day. Flower 1 unlocks on this date.")]
        public int startYear = 2026;
        [Range(1, 12)] public int startMonth = 1;
        [Range(1, 31)] public int startDay = 1;

        public DateTime StartDate
        {
            get
            {
                int safeMonth = Mathf.Clamp(startMonth, 1, 12);
                int safeDay = Mathf.Clamp(startDay, 1, DateTime.DaysInMonth(startYear, safeMonth));
                return new DateTime(startYear, safeMonth, safeDay).Date;
            }
        }
    }
}
