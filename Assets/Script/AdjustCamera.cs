using UnityEngine;

namespace PlayHard
{
    public class AdjustCamera : MonoBehaviour
    {
        void Start()
        {
            SetAdjustCamera();
        }

        /// <summary>
        /// 3D 카메라 해상도 대응
        /// </summary>
        private void SetAdjustCamera()
        {
            float targetAspect = 9f / 16f; // 기준 비율: 9:16
            float windowAspect = (float)Screen.width / (float)Screen.height;

            float scaleHeight = windowAspect / targetAspect;
            Camera cam = Camera.main;

            if (scaleHeight < 1.0f)
            {
                cam.orthographicSize = 5f / scaleHeight; // 기준 높이: 10 (조정 가능)
            }
            else
            {
                cam.orthographicSize = 5f; // 기준값 유지
            }
        }
    }
}
