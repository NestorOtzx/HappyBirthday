using UnityEngine;

namespace HappyBirthday.Data
{
    [CreateAssetMenu(fileName = "FlowerData_01", menuName = "Happy Birthday/Flower Data")]
    public class FlowerData : ScriptableObject
    {
        [Min(1)] public int flowerId = 1;
        public string title = "Flower";

        [TextArea(4, 12)]
        public string message = "Write a cozy message here.";

        public bool hasSpecialGift;

        [TextArea(2, 6)]
        public string giftDescription;

        [Header("Sprites")]
        public Sprite lockedSprite;
        public Sprite availableSprite;
        public Sprite openedSprite;
    }
}
