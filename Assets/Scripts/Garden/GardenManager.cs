using System.Collections.Generic;
using HappyBirthday.Core;
using HappyBirthday.Data;
using HappyBirthday.SaveSystem;
using HappyBirthday.UI;
using UnityEngine;

namespace HappyBirthday.Garden
{
    public class GardenManager : MonoBehaviour
    {
        [SerializeField] private GardenConfig gardenConfig;
        [SerializeField] private List<FlowerData> flowerData = new List<FlowerData>();
        [SerializeField] private List<FlowerController> flowers = new List<FlowerController>();
        [SerializeField] private AudioClip flowerOpenSfx;
        [SerializeField] private AudioClip flowerReadSfx;
        [SerializeField] private bool autoFindFlowers = true;

        private void Start()
        {
            if (gardenConfig == null && GameManager.Instance != null)
            {
                gardenConfig = GameManager.Instance.GardenConfig;
            }

            TimeProgressionManager.Instance?.Configure(gardenConfig);
            RefreshFlowerStates();
        }

        public void RefreshFlowerStates()
        {
            if (autoFindFlowers)
            {
                flowers.Clear();
                flowers.AddRange(FindObjectsByType<FlowerController>(FindObjectsSortMode.None));
                flowers.Sort((a, b) => a.FlowerId.CompareTo(b.FlowerId));
            }

            for (int i = 0; i < flowers.Count; i++)
            {
                FlowerController flower = flowers[i];
                if (flower == null)
                {
                    continue;
                }

                FlowerData data = GetFlowerData(flower.FlowerId, i);
                flower.Initialize(this, data);

                bool opened = SaveManager.Instance != null && SaveManager.Instance.IsFlowerOpened(flower.FlowerId);
                bool available = TimeProgressionManager.Instance == null || TimeProgressionManager.Instance.IsFlowerAvailable(flower.FlowerId);

                flower.SetState(opened ? FlowerState.Opened : available ? FlowerState.Available : FlowerState.Locked);
            }
        }

        public void OpenFlower(FlowerController flower)
        {
            if (flower == null || flower.State == FlowerState.Locked)
            {
                return;
            }

            if (flower.State == FlowerState.Available)
            {
                SaveManager.Instance?.MarkFlowerOpened(flower.FlowerId);
                flower.SetState(FlowerState.Opened);
                AudioManager.Instance?.PlaySfx(flowerOpenSfx);
            }
            else
            {
                AudioManager.Instance?.PlaySfx(flowerReadSfx);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowFlowerMessage(flower.Data);
            }
            else if (flower.Data != null)
            {
                Debug.Log($"{flower.Data.title}: {flower.Data.message}");
            }
        }

        private FlowerData GetFlowerData(int flowerId, int fallbackIndex)
        {
            FlowerData byId = flowerData.Find(data => data != null && data.flowerId == flowerId);
            if (byId != null)
            {
                return byId;
            }

            if (fallbackIndex >= 0 && fallbackIndex < flowerData.Count)
            {
                return flowerData[fallbackIndex];
            }

            return null;
        }
    }
}
