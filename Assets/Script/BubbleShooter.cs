using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace PlayHard
{
    public class BubbleShooter : SingletonMonobehaviour<BubbleShooter>
    {
        [Header("발사 좌/우 최대 각도")]
        [SerializeField]
        private float _maxAngle = 45;
        
        [Header("발사 앵글 이미지")]
        [SerializeField]
        private GameObject _obj_Angle;
        
        // 현재 장전중인 버블
        [HideInInspector] 
        public Bubble2D currentBubble;
        
        [Header("ETC")]
        public LineRenderer lineRenderer;
        public LayerMask WallLayer;
        public LayerMask BubbleLayer;
        public Bubble2D PreviewBubble;

        private Camera _cam;
        private bool _isDragging = false;
        private Vector2 _cachedClampedDirection;

        private void Start()
        {
            _cam = Camera.main;
            currentBubble = null;
            _obj_Angle.SetActive(false);
        }
        
        /// <summary>
        /// 발사 버블 장전
        /// </summary>
        public void PrepareNextBubble()
        {
            var bubbleSpawner = BubbleSpawner.Instance;
            var bubbleMgr = BubbleManager.Instance;
            
            int randomIndex = Random.Range(0, bubbleSpawner.SootbubbleTypes.Count);
            BubbleTypeData data = bubbleSpawner.SootbubbleTypes[randomIndex];
            Vector3 spawnPos = transform.position;
            Bubble2D bubble = bubbleSpawner.GenerateBubble(data, spawnPos);
            bubble.gameObject.name = "ShootBubble";
            bubble.gameObject.layer = LayerMask.NameToLayer("Default");
            var comp = bubble.gameObject.GetOrAddComponent<Rigidbody2D>();
            comp.bodyType = RigidbodyType2D.Kinematic;
            bubble.Initialize(data);
            
            _obj_Angle.SetActive(true);
            bubble.ActiveEnergyBubble(false);
            PreviewBubble.ActiveEnergyBubble(false);
            
            bubble.IsSkillBubble = false;
            
            if (bubbleMgr.CurrentEnergy.Value >= bubbleMgr.GoalEnergy)
            {
                bubble.ActiveEnergyBubble(true);
                PreviewBubble.ActiveEnergyBubble(true);
                bubbleMgr.CurrentEnergy.Value = 0;
                bubble.IsSkillBubble = true;
            }

            currentBubble = bubble;
        }

        private void Update()
        {
            if (currentBubble == null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
            }

            // Update 내부에서 마우스 꾹 누르고 있는 동안
            if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector3 target = _cam.ScreenToWorldPoint(Input.mousePosition);
                target.z = 0;

                Vector2 inputDir = (target - currentBubble.transform.position).normalized;
                float angle = Vector2.SignedAngle(Vector2.up, inputDir);
                angle = Mathf.Clamp(angle, -_maxAngle, _maxAngle);

                Vector2 clampedDir = Quaternion.Euler(0, 0, angle) * Vector2.up;

                _cachedClampedDirection = clampedDir; // ✅ 저장
                RenderTrajectory(currentBubble.transform.position, clampedDir);
            }

            // 마우스 떼면 발사
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;

                currentBubble.Launch(_cachedClampedDirection, WallLayer); // ✅ 제한된 방향으로 발사
                currentBubble.OnLandedCallback = bubble =>
                {
                    BubbleManager.Instance.CheckMatch(bubble).Forget();
                };
                currentBubble = null;
                lineRenderer.positionCount = 0;
                
                // ✅ 프리뷰 꺼주기!
                if (PreviewBubble != null)
                    PreviewBubble.gameObject.SetActive(false);
                
                _obj_Angle.SetActive(false);
            }
        }
       
        /// <summary>
        /// 발사 버블 궤적 가이드 그리기
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="dir"></param>
        private void RenderTrajectory(Vector2 startPos, Vector2 dir)
        {
            List<Vector3> points = new List<Vector3>();
            Vector2 currentPos = startPos;
            Vector2 currentDir = dir;

            points.Add(currentPos);

            int maxBounces = 1;
            int bounceCount = 0;
            
            while (bounceCount <= maxBounces)
            {
                float radius = 0.13f;

                // ✅ 반사 이전에는 wall + bubble, 이후에는 bubble만
                int layerMask = (bounceCount == 0) ? (WallLayer | BubbleLayer) : BubbleLayer;

                RaycastHit2D hit = Physics2D.CircleCast(currentPos, radius, currentDir, 100f, layerMask);

                if (hit.collider != null)
                {
                    points.Add(hit.point);

                    if (((1 << hit.collider.gameObject.layer) & BubbleLayer) != 0)
                    {
                        ShowPreviewAtHit(hit, currentPos);
                        break;
                    }
                    else if (((1 << hit.collider.gameObject.layer) & WallLayer) != 0)
                    {
                        currentPos = hit.point + currentDir.normalized * 0.1f;
                        currentDir = Vector2.Reflect(currentDir, hit.normal);
                        bounceCount++;
                    }
                }
                else
                {
                    points.Add(currentPos + currentDir * 20f);
                    break;
                }
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        /// <summary>
        /// 발사 버블 가이드
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="rayOrigin"></param>
        private void ShowPreviewAtHit(RaycastHit2D hit, Vector2 rayOrigin)
        {
            Bubble2D hitBubble = hit.collider.GetComponent<Bubble2D>();
            if (hitBubble == null || PreviewBubble == null) return;

            Vector3 baseWorldPos = hitBubble.transform.position;
            Vector2 rayDir = ((Vector2)rayOrigin - (Vector2)baseWorldPos).normalized;

            // 공통 유틸 함수 사용
            var (q, r) = Utillity.GetLandingQR(hitBubble, rayDir);
            Vector3 previewPos = Utillity.HexToWorld(q, r, 0.5f);

            if (!Physics2D.OverlapCircle(previewPos, 0.2f, BubbleLayer))
            {
                PreviewBubble.transform.position = previewPos;
                PreviewBubble.gameObject.SetActive(true);
            }
            else
            {
                PreviewBubble.gameObject.SetActive(false);
            }
        }
    }
}