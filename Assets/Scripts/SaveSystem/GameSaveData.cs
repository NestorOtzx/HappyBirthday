using System;
using System.Collections.Generic;

namespace HappyBirthday.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public bool introSeen;
        public List<int> openedFlowerIds = new List<int>();
        public string lastPlayedDateIso;
    }
}
