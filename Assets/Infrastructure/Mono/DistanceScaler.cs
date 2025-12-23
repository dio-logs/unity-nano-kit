using UnityEngine;

namespace Infrastructure.Mono
{
public class DistanceScaler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("비워둘 경우 MainCamera를 자동으로 캐싱합니다.")]
        [SerializeField] private Camera _targetCamera;

        [Header("Distance Settings")]
        [SerializeField, Min(0f)] private float _minDistance = 5f;
        [SerializeField, Min(0f)] private float _maxDistance = 50f;

        [Header("Scale Settings")]
        [Tooltip("최소 거리일 때의 스케일 (가까울 때)")]
        [SerializeField] private Vector3 _minScale = Vector3.one * 0.5f;
        
        [Tooltip("최대 거리일 때의 스케일 (멀 때)")]
        [SerializeField] private Vector3 _maxScale = Vector3.one * 3.0f;

        // 캐싱된 트랜스폼 (메서드 호출 오버헤드 방지)
        private Transform _objTransform;
        private Transform _camTransform;

        private void Awake()
        {
            _objTransform = transform;
            
            // 카메라 캐싱 전략: FindObjectOfType은 비용이 크므로 초기화 시점에만 수행
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
            
            _camTransform = _targetCamera.transform;
        }

        // 카메라 이동 로직이 보통 Update에서 발생하므로, 
        // 떨림 방지를 위해 LateUpdate에서 스케일을 계산합니다.
        private void LateUpdate()
        {
            UpdateScale();
        }

        private void UpdateScale()
        {
            // 1. 거리 계산 (SqrMagnitude를 쓰지 않는 이유: 선형 보간을 위해 정확한 Distance가 필요함)
            // 성능 최적화: Vector3.Distance는 내부적으로 sqrt를 사용하지만, 프레임당 1회 호출은 현대 CPU에서 무시할 수준입니다.
            float distance = Vector3.Distance(_camTransform.position, _objTransform.position);

            // 2. 거리 정규화 (0.0 ~ 1.0)
            // Mathf.InverseLerp는 값을 자동으로 min/max 사이로 Clamp 해줍니다.
            float t = Mathf.InverseLerp(_minDistance, _maxDistance, distance);

            // 3. 선형 보간 (Lerp)을 통한 스케일 적용
            // Vector3.Lerp를 사용하여 각 축별로 부드럽게 크기 변화
            _objTransform.localScale = Vector3.Lerp(_minScale, _maxScale, t);
        }
    }
}