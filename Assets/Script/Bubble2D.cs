using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace PlayHard
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bubble2D : MonoBehaviour
    {
        [Header("HEX 좌표")]
        public float q;
        public float r;

        [Header("스코어 버블 표식")]
        [SerializeField] 
        private GameObject _obj_isScoreBubble;
        
        [Header("에너지 버블 표식")]
        [SerializeField] 
        private GameObject _obj_isEnergyBubble;
        
        [Header("에너지 버블 활성화시 사용할 오브젝트")]
        [SerializeField] 
        private GameObject _obj_EnergyBubble;
        
        [Header("에너지 버블 프리뷰이미지")]
        [SerializeField] 
        private GameObject _obj_EnergyBubblePreview;

        /// <summary>
        /// 스킬버블인지 여부
        /// </summary>
        public bool IsSkillBubble;

        private SpriteRenderer _spriteRenderer;
        private BubbleTypeData _myData;
        private Vector2 _velocity;
        private bool _isFlying = false;
        private LayerMask _wallMask;

        public Action<Bubble2D> OnLandedCallback;
        private Bubble2D _lastCollidedBubble;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if(_obj_EnergyBubble)
                _obj_EnergyBubble.SetActive(false);
        }

        public void Initialize(BubbleTypeData data)
        {
            _myData = data;
            
            if (_spriteRenderer != null && data != null)
                _spriteRenderer.color = data.color;
            
            _obj_isScoreBubble.SetActive(_myData.BubbleSkill == BubbleSkill.Score);
            _obj_isEnergyBubble.SetActive(_myData.BubbleSkill == BubbleSkill.Energy);
            
            if(_obj_EnergyBubble)
                _obj_EnergyBubble.SetActive(false);
        }
        
        public (float, float) GetQR()
        {
            return (this.q, this.r);
        }
        
        public int GetKey()
        {
            return _myData.key;
        }

        public bool GetIsScoreBubble()
        {
            return _myData.BubbleSkill == BubbleSkill.Score;
        }
        
        public bool GetIsEnergyBubble()
        {
            return _myData.BubbleSkill == BubbleSkill.Energy;
        }

        public void MoveTo(Vector3 pos)
        {
            transform.DOMove(pos, 0.2f).SetEase(Ease.Linear);
        }

        public void Launch(Vector2 dir, LayerMask _wallMask)
        {
            this._wallMask = _wallMask;
            _velocity = dir.normalized * 10f;
            _isFlying = true;
        }

        private void Update()
        {
            if (!_isFlying) return;

            float moveDist = _velocity.magnitude * Time.deltaTime;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _velocity.normalized, moveDist, _wallMask);

            if (hit.collider != null && hit.collider.CompareTag("Wall"))
            {
                // 반사 처리
                _velocity = Vector2.Reflect(_velocity, hit.normal);
                transform.position = hit.point + hit.normal * 0.01f;
            }
            else
            {
                transform.position += (Vector3)(_velocity * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 버블 착지
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isFlying) return;

            if (other.TryGetComponent(out Bubble2D target))
            {
                _lastCollidedBubble = target;
                _isFlying = false; // << 착지 전에 상태를 바꿔서 중복 방지
                Land();
            }
        }
        
        /// <summary>
        /// 착지 이후 처리
        /// </summary>
        private void Land()
        {
            _isFlying = false;

            if (_lastCollidedBubble == null)
            {
                Debug.LogWarning("충돌한 버블 정보 없음");
                return;
            }
            
            gameObject.layer = LayerMask.NameToLayer("Bubble");

            Vector3 baseWorldPos = _lastCollidedBubble.transform.position;
            Vector2 dir = (transform.position - baseWorldPos).normalized;

            (q, r) = Utillity.GetLandingQR(_lastCollidedBubble, dir);

            Vector3 snapPos = Utillity.HexToWorld(q, r, 0.5f);
            transform.DOMove(snapPos, 0.05f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                BubbleManager.Instance.BubbleLandComplete(this);
                OnLandedCallback?.Invoke(this);    
            });
        }
        
        public void ActiveEnergyBubble(bool active)
        {
            if(_obj_EnergyBubble)
                _obj_EnergyBubble?.SetActive(active);
            
            if(_obj_EnergyBubblePreview)
                _obj_EnergyBubblePreview?.SetActive(active);
        }
    }
}
