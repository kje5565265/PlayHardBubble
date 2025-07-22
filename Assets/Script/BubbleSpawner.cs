using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlayHard
{
    public class BubbleSpawner : SingletonMonobehaviour<BubbleSpawner>
    {
        [Header("버블 타입 데이터 목록")]
        public List<BubbleTypeData> bubbleTypes;
        
        [Header("발사되는 버블 타입 데이터 목록")]
        public List<BubbleTypeData> SootbubbleTypes;

        [Header("버블 생성 주기")]
        [SerializeField] 
        private float spawnInterval = 0.2f;
        
        [Header("버블 최대 갯수 ( 왼쪽, 오른쪽 개별 )")]
        [SerializeField] private int maxBubbleCount = 17;

        private int _createMaxCount;
        
        /// <summary>
        /// 버블 자동 생성
        /// </summary>
        public async UniTask StartAutoSpawnAsync()
        {
            int createdCount = 0; 
            _createMaxCount = BubbleManager.Instance.GetCreateBubbleMaxCount();
            
            while (createdCount < _createMaxCount)
            {
                BubbleManager.Instance.CreateRightBubble();
                BubbleManager.Instance.CreateLeftBubble();

                createdCount++;
                await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval));
            }

            Debug.Log("모든 버블 생성 완료");
        }
        
        /// <summary>
        /// 외부에서 색상 히스토리를 전달받아 제한된 랜덤 키를 선택
        /// </summary>
        public int GetLimitedRandomKey(Queue<int> recentHistory)
        {
            List<int> availableKeys = new List<int>();

            foreach (var bubbleType in bubbleTypes)
            {
                int key = bubbleType.key;

                // 최근 2개 색상과 동일한 색상은 제외
                if (recentHistory.Count >= 2)
                {
                    var recent = recentHistory.ToArray();
                    if (recent[0] == key && recent[1] == key)
                        continue;
                }

                availableKeys.Add(key);
            }

            // 모든 색상이 제한되었을 경우 예외 처리
            if (availableKeys.Count == 0)
            {
                availableKeys.AddRange(bubbleTypes.Select(bt => bt.key));
            }

            return availableKeys[Random.Range(0, availableKeys.Count)];
        }

        public Bubble2D GenerateBubble(BubbleTypeData typeData, Vector3 worldPos)
        {
            if (typeData == null || typeData.prefab == null) return null;

            var bubble = ResourceManager.Instance.GetBubble();
            bubble.transform.position = worldPos;
            Bubble2D bubble2D = bubble.GetComponent<Bubble2D>();
            bubble2D.Initialize(typeData);

            return bubble2D;
        }

        public BubbleTypeData GetTypeDataByKey(int key)
        {
            return bubbleTypes.Find(bt => bt.key == key);
        }
    }
}