using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // �̵� �ӵ�

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        // Rigidbody2D ������Ʈ�� ������
        rb = GetComponent<Rigidbody2D>();

        // ���� Rigidbody2D�� ���� ��� ��� �޽����� ���
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D�� Player ������Ʈ�� �����ϴ�. Rigidbody2D�� �߰��ϼ���.");
        }
    }

    void Update()
    {
        // WASD �Է� �ޱ�
        float moveX = Input.GetAxis("Horizontal");  // A, D Ű�� �¿� �̵�
        float moveY = Input.GetAxis("Vertical");    // W, S Ű�� �յ� �̵�

        moveDirection = new Vector2(moveX, moveY).normalized;  // ���� ����
    }

    void FixedUpdate()
    {
        // Rigidbody�� �̿��� �̵� ó�� (������ �������� �ӵ��� �̵�)
        if (rb != null)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }
}