using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PlayHard
{
    public class ResourceManager : SingletonMonobehaviour<ResourceManager>
    {
        [Header("버블 프리팹")]
        public Bubble2D bubblePrefab;

        // 비활성화된 버블 풀
        private readonly Queue<Bubble2D> bubblePool = new Queue<Bubble2D>();

        /// <summary>
        /// 게임 실행시 최초 1번 Scene 에 등록되어있는 Bubble2D 모두 검색해서 Queue 에 등록
        /// </summary>
        public async Task InitializePoolAsync()
        {
            // 자식 중 비활성 포함한 Bubble2D 전부 검색
            var bubbles = GetComponentsInChildren<Bubble2D>(true);

            foreach (var bubble in bubbles)
            {
                bubble.gameObject.SetActive(false); // 초기엔 비활성화
                bubblePool.Enqueue(bubble);
            }

            // 약간의 비동기 처리 흉내 (없어도 되지만 디버깅 시 확인용)
            await Task.Yield();
        }
        
        /// <summary>
        /// 버블 가져오기 (새로 생성 or 풀에서 꺼내기)
        /// </summary>
        public Bubble2D GetBubble()
        {
            Bubble2D bubble;

            if (bubblePool.Count > 0)
            {
                bubble = bubblePool.Dequeue();
                bubble.gameObject.SetActive(true);
            }
            else
            {
                bubble = Instantiate(bubblePrefab);
            }

            return bubble;
        }

        /// <summary>
        /// 버블 반환 (풀에 다시 넣기)
        /// </summary>
        public void ReturnBubble(Bubble2D bubble)
        {
            bubble.gameObject.SetActive(false);
            bubble.transform.localScale = new Vector3(0.5f, 0.5f, 1);
            bubble.transform.SetParent(transform); // 정리 목적
            bubblePool.Enqueue(bubble);
        }

        /// <summary>
        /// 모든 풀 초기화 (필요 시)
        /// </summary>
        public void Clear()
        {
            bubblePool.Clear();
        }
    }
}