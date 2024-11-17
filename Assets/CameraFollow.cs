using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;  // 카메라가 따라갈 대상 (플레이어)
    public Vector3 offset = new Vector3(0, 2, -5);  // 카메라와 플레이어 사이의 거리 (플레이어 시점에 맞게 조정)
    
    public float zoomLevel = 5f;  // 줌 레벨 (플레이어 중심 시점에서 적절한 크기로 보기 위한 값)

    void Start()
    {
        // 초기 줌 설정
        Camera.main.orthographicSize = zoomLevel; 
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Lerp 없이 즉시 플레이어를 따라감
            Vector3 desiredPosition = target.position + offset;

            // 카메라 위치를 즉시 업데이트
            transform.position = desiredPosition;
        }
    }
}