using System;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PlayHard
{
    public class UIManager : SingletonMonobehaviour<UIManager>
    {
        [Header("해상도 대응 캔버스 스케일러")]
        [SerializeField]
        private CanvasScaler _canvasScaler;

        [Header("스코어에 따른 fill 이미지 매태리얼")]
        [SerializeField]
        private Image _fill_Image;
        private Material _fillImage_material;
        
        [Header("에너지 량에 따른 fill Image")]
        [SerializeField]
        private SlicedFilledImage _fill_SliceImage;

        [Header("클리어 오브젝트")]
        [SerializeField]
        private GameObject _obj_Clear;

        private float _goalScore;
        private float _currentFill = 0;
        private float _goalEnergy;
        private float _currentFill_enrgy = 0;
        private float _nextFill = 0;
        private Tween _fillTween;
        private Tween _clearTween;
        
        void Start()
        {
            _goalScore = BubbleManager.Instance.GoalScore;
            _goalEnergy = BubbleManager.Instance.GoalEnergy;
            _fillImage_material = _fill_Image.material;
            
            AdjustCanvasMatch();
            
            // 점수 변경에 따른 옵저빙
            BubbleManager.Instance.CurrentScore.ObserveEveryValueChanged(d => d.Value).Subscribe(value =>
            {
                _nextFill = Mathf.Clamp01(value / _goalScore);
                
                // 이미 트윈 중인 게 있다면 멈춤
                DOTween.Kill(_fillImage_material);

                // fillAmount를 부드럽게 증가
                DOTween.To(() => _currentFill, x =>
                {
                    _currentFill = x;
                    _fillImage_material.SetFloat("_FillAmount", _currentFill);
                }, _nextFill, 0.5f).SetEase(Ease.OutCubic).SetTarget(_fillImage_material);

                if (BubbleManager.Instance.CurrentScore.Value >= BubbleManager.Instance.GoalScore && null == _clearTween)
                {
                    _obj_Clear.SetActive(true);
                    _clearTween = _obj_Clear.transform.DOScale(1f, 0.5f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo); // 무한 반복
                }
                
            }).AddTo(this);
            
            // 에너지 변경에 따른 옵저빙
            BubbleManager.Instance.CurrentEnergy.ObserveEveryValueChanged(d => d.Value).Subscribe(value =>
            {
                float targetFill = Mathf.Clamp01(value / _goalEnergy);

                // 기존 Tween이 있으면 중지
                _fillTween?.Kill();

                // 새로운 Tween 시작
                _fillTween = DOTween.To(() => _currentFill_enrgy, x =>
                    {
                        _currentFill_enrgy = x;
                        _fill_SliceImage.fillAmount = x;
                    }, targetFill, 0.4f) // 0.4초 동안 부드럽게 변화
                    .SetEase(Ease.OutQuad);
            }).AddTo(this);
        }

        /// <summary>
        /// 해상도 대응
        /// </summary>
        void AdjustCanvasMatch()
        {
            float screenRatio = (float)Screen.width / Screen.height;
            float referenceRatio = _canvasScaler.referenceResolution.x / _canvasScaler.referenceResolution.y;

            // 화면 비율에 따라 Match 비율을 조정 (너비 중심인지, 높이 중심인지)
            _canvasScaler.matchWidthOrHeight = (screenRatio >= referenceRatio) ? 1f : 0f;
        }
    }
}
