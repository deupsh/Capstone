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
    private float moveSpeed; // �⺻ �̵� �ӵ�

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    [Header("Tilemap Settings")]
    public Tilemap tileMap; // Ÿ�ϸ� ����

    private PlayerStats playerStats;

    void Start()
    {
        // Rigidbody2D ������Ʈ�� ������
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D�� Player ������Ʈ�� �����ϴ�.");
        }

        // JSON���� �÷��̾� ������ �ε�
        LoadPlayerData();

        // �ʱ� �̵� �ӵ� ����
        if (playerStats != null)
        {
            moveSpeed = playerStats.SPEED;
        }
    }

    void Update()
    {
        // WASD �Է� �ޱ�
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        moveDirection = new Vector2(moveX, moveY).normalized; // ���� ����
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

        // �÷��̾� ��ġ�� Ÿ�ϸ� ��ǥ�� ��ȯ
        Vector3 playerPosition = transform.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        // ���� Ÿ�� ��������
        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            if (currentTile.name == "SwampTile2")
            {
                moveSpeed = playerStats.SPEED - 9f; // �̵� �ӵ� ����
            }
            else
            {
                moveSpeed = playerStats.SPEED; // �⺻ �ӵ��� ����
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
                playerStats = data.Player[0]; // ù ��° �÷��̾� ������ �ε�
                Debug.Log($"�÷��̾� �̸�: {playerStats.name}, HP: {playerStats.HP}, ATK: {playerStats.ATK}, SPEED: {playerStats.SPEED}");
            }
            else
            {
                Debug.LogError("�÷��̾� �����Ͱ� ����ֽ��ϴ�.");
            }
        }
        else
        {
            Debug.LogError("PlayerData.json ������ ã�� �� �����ϴ�.");
        }
    }
}