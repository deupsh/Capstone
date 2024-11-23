using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;  // ī�޶� ���� ��� (�÷��̾�)
    public Vector3 offset = new Vector3(0, 2, -5);  // ī�޶�� �÷��̾� ������ �Ÿ� (�÷��̾� ������ �°� ����)

    public float zoomLevel = 5f;  // �� ���� (�÷��̾� �߽� �������� ������ ũ��� ���� ���� ��)

    void Start()
    {
        // �ʱ� �� ����
        Camera.main.orthographicSize = zoomLevel;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Lerp ���� ��� �÷��̾ ����
            Vector3 desiredPosition = target.position + offset;

            // ī�޶� ��ġ�� ��� ������Ʈ
            transform.position = desiredPosition;
        }
    }
}