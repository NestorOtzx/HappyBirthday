using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HappyBirthday.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "happy_birthday_save.json";

        public static SaveManager Instance { get; private set; }

        public GameSaveData Data { get; private set; } = new GameSaveData();
        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            if (!File.Exists(SavePath))
            {
                Data = new GameSaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
                Data.openedFlowerIds ??= new List<int>();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save file could not be loaded. Starting with empty progress. {exception.Message}");
                Data = new GameSaveData();
            }
        }

        public void Save()
        {
            try
            {
                Data.openedFlowerIds ??= new List<int>();
                Data.lastPlayedDateIso = DateTime.Now.Date.ToString("yyyy-MM-dd");

                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save file could not be written. {exception.Message}");
            }
        }

        public bool IsFlowerOpened(int flowerId)
        {
            return Data.openedFlowerIds != null && Data.openedFlowerIds.Contains(flowerId);
        }

        public void MarkFlowerOpened(int flowerId)
        {
            if (flowerId <= 0)
            {
                return;
            }

            Data.openedFlowerIds ??= new List<int>();
            if (Data.openedFlowerIds.Contains(flowerId))
            {
                return;
            }

            Data.openedFlowerIds.Add(flowerId);
            Data.openedFlowerIds.Sort();
            Save();
        }

        public void SetIntroSeen(bool seen)
        {
            if (Data.introSeen == seen)
            {
                return;
            }

            Data.introSeen = seen;
            Save();
        }

        public void DeleteSave()
        {
            Data = new GameSaveData();

            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
    }
}
