using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;  // ī�޶� ���� ��� (�÷��̾�)
    public Vector3 offset = new Vector3(0, 5, -10);  // ī�޶�� �÷��̾� ������ �Ÿ�
    public float smoothSpeed = 0.125f;  // ī�޶� �̵��� �ε巯�� ����

    void LateUpdate()
    {
        if (target != null)
        {
            // ��ǥ ��ġ ��� (�÷��̾� ��ġ + ������)
            Vector3 desiredPosition = target.position + offset;

            // �ε巴�� ī�޶� �̵� (Lerp ���)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // ī�޶� ��ġ ������Ʈ
            transform.position = smoothedPosition;
        }
    }
}