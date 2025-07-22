using System.Collections.Generic;
using UnityEngine;

namespace PlayHard
{
    [CreateAssetMenu(fileName = "BubbleSpawnPreset", menuName = "PlayHard/Bubble Spawn Preset")]
    public class BubbleSpawnPreset : ScriptableObject
    {
        [System.Serializable]
        public struct QR
        {
            public float q;
            public float r;
        }

        [Header("최초 생성 포지션")] 
        public QR FirstSpawnPosition;
        
        [Header("Bubble Spawn QR List (순서대로 0~16)")]
        public List<QR> spawnPositions = new List<QR>(17);
    }
}
