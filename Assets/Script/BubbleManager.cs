using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using Observable = UniRx.Observable;

namespace PlayHard
{
    public class BubbleManager : SingletonMonobehaviour<BubbleManager>
    {
        [Header("버블 반지름")] public float bubbleRadius = 0.5f;

        [Header("오른쪽 프리셋")] public BubbleSpawnPreset rightPreset;
        private List<Bubble2D> rightBubbles = new List<Bubble2D>();
        private Queue<int> recentColorsRight = new Queue<int>();

        [Header("왼쪽 프리셋")] public BubbleSpawnPreset leftPreset;
        private List<Bubble2D> leftBubbles = new List<Bubble2D>();
        private Queue<int> recentColorsLeft = new Queue<int>();
        
        /// <summary>
        /// 유저가 발사한 버블들 ( 프리셋 포지션이 아닌 버블 )
        /// </summary>
        private List<Bubble2D> activeBubbles = new List<Bubble2D>();

        /// <summary>
        /// 동시에 생성 가능한 색상 갯수
        /// </summary>
        private const int MaxColorHistory = 2;

        /// <summary>
        /// 좌/우 각각 버블 최대 갯수
        /// </summary>
        private const int MAX_BUBBLE_COUNT = 17;

        /// <summary>
        /// 도달해야하는 점수
        /// </summary>
        public int GoalScore { get; } = 100;
        
        /// <summary>
        /// 현재 점수
        /// </summary>
        public ReactiveProperty<float> CurrentScore = new ReactiveProperty<float>();

        [Header("스킬을 사용하는데 필요한 에너지")]
        public int GoalEnergy = 20;
        
        /// <summary>
        /// 현재 모인 스킬 에너지
        /// </summary>
        public ReactiveProperty<float> CurrentEnergy = new ReactiveProperty<float>();

        private async void Start()
        {
            // 씬에 생성되어있는 버블들을 찾아서 풀에 등록
            await ResourceManager.Instance.InitializePoolAsync();
            
            // 버블 스폰 시작
            await BubbleSpawner.Instance.StartAutoSpawnAsync();
            
            // 다음 발사 버블 장전
            BubbleShooter.Instance.PrepareNextBubble();
        }

        /// <summary>
        /// 버블 생성시 필요한 최대 갯수
        /// </summary>
        /// <returns></returns>
        public int GetCreateBubbleMaxCount()
        {
            if (leftBubbles.Count <= rightBubbles.Count)
                return MAX_BUBBLE_COUNT - leftBubbles.Count;
            
            return MAX_BUBBLE_COUNT - rightBubbles.Count;
        }
        
        /// <summary>
        /// 오른쪽 버블 생성 시작
        /// </summary>
        public void CreateRightBubble()
        {
            if (rightBubbles.Count >= MAX_BUBBLE_COUNT)
                return;

            var qr = rightPreset.FirstSpawnPosition;
            Vector3 spawnPos = Utillity.HexToWorld(qr.q, qr.r, bubbleRadius);

            //int colorKey = BubbleSpawner.Instance.GetLimitedRandomKey(recentColorsRight);
            int colorKey = UnityEngine.Random.Range(0, BubbleSpawner.Instance.bubbleTypes.Count);
            BubbleTypeData data = BubbleSpawner.Instance.bubbleTypes[colorKey];
            Bubble2D bubble = BubbleSpawner.Instance.GenerateBubble(data, spawnPos);
            bubble.gameObject.layer = LayerMask.NameToLayer("Bubble");

            if (bubble != null)
            {
                // 같은색상이 2개이상 나오지 않도록 설정했으나, 게임이 재미없어서 주석처리
                //UpdateHistory(recentColorsRight, colorKey);
                
                rightBubbles.Insert(0, bubble);
                UpdateRightBubblePositions();
            }
        }

        public void UpdateRightBubblePositions()
        {
            for (int i = 0; i < rightBubbles.Count; i++)
            {
                if (i >= rightPreset.spawnPositions.Count) break;
                var qr = rightPreset.spawnPositions[i];
                
                Vector3 pos = Utillity.HexToWorld(qr.q, qr.r, bubbleRadius);
                rightBubbles[i].MoveTo(pos);
                rightBubbles[i].q = qr.q;
                rightBubbles[i].r = qr.r;
            }
        }

        /// <summary>
        /// 왼쪽 버블 생성 시작
        /// </summary>
        public void CreateLeftBubble()
        {
            if (leftBubbles.Count >= MAX_BUBBLE_COUNT)
                return;

            var qr = leftPreset.FirstSpawnPosition;
            Vector3 spawnPos = Utillity.HexToWorld(qr.q, qr.r, bubbleRadius);

            //int colorKey = BubbleSpawner.Instance.GetLimitedRandomKey(recentColorsLeft);
            int colorKey = UnityEngine.Random.Range(0, BubbleSpawner.Instance.bubbleTypes.Count);
            BubbleTypeData data = BubbleSpawner.Instance.bubbleTypes[colorKey];
            Bubble2D bubble = BubbleSpawner.Instance.GenerateBubble(data, spawnPos);
            bubble.gameObject.layer = LayerMask.NameToLayer("Bubble");
            
            if (bubble != null)
            {
                // 같은색상이 2개이상 나오지 않도록 설정했으나, 게임이 재미없어서 주석처리
                //UpdateHistory(recentColorsRight, colorKey);
                
                leftBubbles.Insert(0, bubble);
                UpdateLeftBubblePositions();
            }
        }

        public void UpdateLeftBubblePositions()
        {
            for (int i = 0; i < leftBubbles.Count; i++)
            {
                if (i >= leftPreset.spawnPositions.Count) break;
                var qr = leftPreset.spawnPositions[i];
                
                Vector3 pos = Utillity.HexToWorld(qr.q, qr.r, bubbleRadius);
                leftBubbles[i].MoveTo(pos);
                leftBubbles[i].q = qr.q;
                leftBubbles[i].r = qr.r;
            }
        }
        
        /// <summary>
        /// 주어진 좌표가 프리셋에 정의된 스폰 위치인지 확인
        /// </summary>
        private bool IsInPresetPosition(float q, float r, BubbleSpawnPreset preset)
        {
            if (q == 0 && r == 0)
                return true;
            
            foreach (var pos in preset.spawnPositions)
            {
                if (Mathf.Approximately(pos.q, q) && Mathf.Approximately(pos.r, r))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 같은색상이 2개 이상 연속으로 나오지 않도록 큐에 저장
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="key"></param>
        private void UpdateHistory(Queue<int> queue, int key)
        {
            queue.Enqueue(key);
            if (queue.Count > MaxColorHistory)
                queue.Dequeue();
        }

        /// <summary>
        /// 발사한 버블이 착지 완료후 리스트 포지션 결정
        /// </summary>
        /// <param name="bubble"></param>
        public void BubbleLandComplete(Bubble2D bubble)
        {
            var (q, r) = bubble.GetQR();

            if (IsInPresetPosition(q, r, leftPreset))
            {
                leftBubbles.Add(bubble);
            }
            else if (IsInPresetPosition(q, r, rightPreset))
            {
                rightBubbles.Add(bubble);
            }
            else
            {
                activeBubbles.Add(bubble);
            }
        }
        
        /// <summary>
        /// 버블 매치 체크시작
        /// </summary>
        /// <param name="startBubble"></param>
        public async UniTaskVoid CheckMatch(Bubble2D startBubble)
        {
            // 다음 버블 생성
            // - 원래 가장 하위에서 모든 동작이 종료후 호출 이었으나, 연속발사를 위해 이전 버블 착지후 바로 생성
            BubbleShooter.Instance.PrepareNextBubble();
            
            List<Bubble2D> matched = new List<Bubble2D>();
            HashSet<Bubble2D> visited = new HashSet<Bubble2D>();

            if (startBubble.IsSkillBubble)
            {
                startBubble.IsSkillBubble = false;
                
                // 스킬 활성화 시 자기 + 주변 6방향 모두 파괴
                matched = GetEnergySkillTargets(startBubble);
            }
            else
            {
                DFS(startBubble, startBubble.GetKey(), matched, visited);
            }

            if (matched.Count >= 3)
            {
                List<UniTask> destroyTasks = new();

                foreach (var bubble in matched)
                {
                    destroyTasks.Add(DestroyBubbleAsync(bubble));
                }

                // 모든 버블 제거 애니메이션이 끝날 때까지 대기
                await UniTask.WhenAll(destroyTasks);
                
                // 매치되지 않았지만 떨어져야 할 버블 제거
                await CheckFloatingBubblesAsync();  
                
                // ✅ 최소 버블 유지
                await BubbleSpawner.Instance.StartAutoSpawnAsync();
            }
            else
            {
                // 매칭 안 됐어도 잠깐 기다리자
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            }
        }
        
        /// <summary>
        /// 허공에 떠있는 버블 제거
        /// </summary>
        public async UniTask CheckFloatingBubblesAsync()
        {
            HashSet<Bubble2D> visited = new HashSet<Bubble2D>();
            Queue<Bubble2D> queue = new Queue<Bubble2D>();

            // 1. 천장에 붙어있는 버블들만 큐에 넣음 (r == 0 또는 y 위치가 일정 이상)
            foreach (var bubble in GetAllBubbles())
            {
                if (bubble.r == 0) // 또는 bubble.transform.position.y > 특정값
                {
                    visited.Add(bubble);
                    queue.Enqueue(bubble);
                }
            }

            // 2. BFS 시작
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(current.q, current.r))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // 3. 연결되지 않은 버블 = visited에 없는 버블
            List<UniTask> destroyTasks = new List<UniTask>();
            foreach (var bubble in GetAllBubbles())
            {
                if (!visited.Contains(bubble))
                {
                    destroyTasks.Add(DestroyBubbleAsync(bubble));
                }
            }

            await UniTask.WhenAll(destroyTasks);
        }

        
        /// <summary>
        /// 버블 파괴
        /// </summary>
        /// <param name="bubble"></param>
        private async UniTask DestroyBubbleAsync(Bubble2D bubble)
        {
            var sr = bubble.GetComponent<SpriteRenderer>();

            // 동시에 애니메이션
            var scaleUp = bubble.transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutBack);
            await scaleUp.ToUniTask();

            var scaleDown = bubble.transform.DOScale(0f, 0.15f).SetEase(Ease.InBack);
            var fadeOut = sr.DOFade(0f, 0.15f);
            await UniTask.WhenAll(scaleDown.ToUniTask(), fadeOut.ToUniTask());
            
            if(bubble.GetIsScoreBubble())
                CurrentScore.Value += UnityEngine.Random.Range(0.1f, 2f);
            
            if(bubble.GetIsEnergyBubble())
                CurrentEnergy.Value += UnityEngine.Random.Range(0.1f, 2f);
            
            leftBubbles.Remove(bubble);
            rightBubbles.Remove(bubble);
            activeBubbles.Remove(bubble);
            
            ResourceManager.Instance.ReturnBubble(bubble);
        }
        
        /// <summary>
        /// 중심 버블 기준으로 자기 자신 + 주변 6방향 및 추가 +1칸씩 버블을 찾아 리스트로 반환
        /// 거리 계산으로 하니까 오차가 생겨서 위치를 잡아놓고 검사
        /// </summary>
        private static readonly Vector2[] RelativePositions = new Vector2[]
        {
            new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
            new Vector2(-1.5f, -0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(1.5f, -0.5f),
            new Vector2(-2f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 0f),
            new Vector2(-1.5f, 0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.5f, 0.5f),
            new Vector2(-1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        private List<Bubble2D> GetEnergySkillTargets(Bubble2D center)
        {
            List<Bubble2D> result = new List<Bubble2D>();
            if (center == null) return result;

            foreach (var offset in RelativePositions)
            {
                float q = center.q + offset.x;
                float r = center.r + offset.y;

                Bubble2D found = FindBubbleAt(q, r);
                if (found != null)
                    result.Add(found);
            }

            return result;
        }
        
        /// <summary>
        /// DFS 탐색
        /// </summary>
        /// <param name="current"></param>
        /// <param name="targetKey"></param>
        /// <param name="result"></param>
        /// <param name="visited"></param>
        private void DFS(Bubble2D current, int targetKey, List<Bubble2D> result, HashSet<Bubble2D> visited)
        {
            if (visited.Contains(current)) return;

            visited.Add(current);
            result.Add(current);

            var neighbors = GetNeighbors(current.q, current.r);

            foreach (var neighbor in neighbors)
            {
                if (neighbor.GetKey() == targetKey)
                {
                    DFS(neighbor, targetKey, result, visited);
                }
            }
        }
        
        /// <summary>
        /// 주변 인접한 버블 검사
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        private List<Bubble2D> GetNeighbors(float q, float r)
        {
            var directions = new (float dq, float dr)[]
            {
                (1f, 0f), (-1f, 0f),
                (0.5f, 0.5f), (-0.5f, 0.5f),
                (0.5f, -0.5f), (-0.5f, -0.5f),
            };

            List<Bubble2D> neighbors = new List<Bubble2D>();

            foreach (var (dq, dr) in directions)
            {
                float nq = q + dq;
                float nr = r + dr;

                Bubble2D found = FindBubbleAt(nq, nr);
                if (found != null)
                    neighbors.Add(found);
            }

            return neighbors;
        }
        
        /// <summary>
        /// 버블 검색
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public Bubble2D FindBubbleAt(float q, float r)
        {
            return GetAllBubbles().FirstOrDefault(b => Mathf.Approximately(b.q, q) && Mathf.Approximately(b.r, r));
        }
        
        /// <summary>
        /// 모든 버블 가져오기
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Bubble2D> GetAllBubbles()
        {
            return leftBubbles.Concat(rightBubbles).Concat(activeBubbles);
        }
    }
}
