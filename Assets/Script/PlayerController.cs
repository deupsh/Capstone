using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class PlayerStats
{
    public string name;
    public int HP;
    public int ATK;
    public float SPEED;
}

[System.Serializable]
public class PlayerData
{
    public List<PlayerStats> Player;
}

public class PlayerController : MonoBehaviour
{
    private float moveSpeed; // 기본 이동 속도

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    [Header("Tilemap Settings")]
    public Tilemap tileMap; // 타일맵 참조

    private PlayerStats playerStats;

    void Start()
    {
        // Rigidbody2D 컴포넌트를 가져옴
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D가 Player 오브젝트에 없습니다.");
        }

        // JSON에서 플레이어 데이터 로드
        LoadPlayerData();

        // 초기 이동 속도 설정
        if (playerStats != null)
        {
            moveSpeed = playerStats.SPEED;
        }
    }

    void Update()
    {
        // WASD 입력 받기
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        moveDirection = new Vector2(moveX, moveY).normalized; // 방향 설정
    }

    void FixedUpdate()
    {
        AdjustSpeedBasedOnTile();

        if (rb != null)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }

    private void AdjustSpeedBasedOnTile()
    {
        if (tileMap == null) return;

        // 플레이어 위치를 타일맵 좌표로 변환
        Vector3 playerPosition = transform.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        // 현재 타일 가져오기
        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            if (currentTile.name == "SwampTile2")
            {
                moveSpeed = playerStats.SPEED - 9f; // 이동 속도 감소
            }
            else
            {
                moveSpeed = playerStats.SPEED; // 기본 속도로 복원
            }
        }
    }

    private void LoadPlayerData()
    {
        string path = Application.dataPath + "/Resources/Data/PlayerData.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);

            if (data.Player.Count > 0)
            {
                playerStats = data.Player[0]; // 첫 번째 플레이어 데이터 로드
                Debug.Log($"플레이어 이름: {playerStats.name}, HP: {playerStats.HP}, ATK: {playerStats.ATK}, SPEED: {playerStats.SPEED}");
            }
            else
            {
                Debug.LogError("플레이어 데이터가 비어있습니다.");
            }
        }
        else
        {
            Debug.LogError("PlayerData.json 파일을 찾을 수 없습니다.");
        }
    }
}