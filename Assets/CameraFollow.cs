using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;  // 카메라가 따라갈 대상 (플레이어)
    public Vector3 offset = new Vector3(0, 5, -10);  // 카메라와 플레이어 사이의 거리
    public float smoothSpeed = 0.125f;  // 카메라 이동의 부드러움 정도

    void LateUpdate()
    {
        if (target != null)
        {
            // 목표 위치 계산 (플레이어 위치 + 오프셋)
            Vector3 desiredPosition = target.position + offset;

            // 부드럽게 카메라 이동 (Lerp 사용)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // 카메라 위치 업데이트
            transform.position = smoothedPosition;
        }
    }
}