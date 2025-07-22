using UnityEngine;

namespace PlayHard
{
    [CreateAssetMenu(fileName = "BubbleTypeData", menuName = "Scriptable Objects/BubbleTypeData")]
    public class BubbleTypeData : ScriptableObject
    {
        public int key;
        public Color color; // or Sprite sprite;
        public GameObject prefab; // 실제 사용할 프리팹 (옵션)
        public BubbleSkill BubbleSkill;
    }

    public enum BubbleSkill
    {
        Normal = 0,
        Score = 1,
        Energy = 2,
    }
}