using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // 이동 속도

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        // Rigidbody2D 컴포넌트를 가져옴
        rb = GetComponent<Rigidbody2D>();

        // 만약 Rigidbody2D가 없을 경우 경고 메시지를 출력
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D가 Player 오브젝트에 없습니다. Rigidbody2D를 추가하세요.");
        }
    }

    void Update()
    {
        // WASD 입력 받기
        float moveX = Input.GetAxis("Horizontal");  // A, D 키로 좌우 이동
        float moveY = Input.GetAxis("Vertical");    // W, S 키로 앞뒤 이동

        moveDirection = new Vector2(moveX, moveY).normalized;  // 방향 설정
    }

    void FixedUpdate()
    {
        // Rigidbody를 이용한 이동 처리 (프레임 독립적인 속도로 이동)
        if (rb != null)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }
}