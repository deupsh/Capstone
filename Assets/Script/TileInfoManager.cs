using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfoManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����
    [SerializeField] private Transform player; // �÷��̾� ����
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����

    private float lastLogTime = 0f; // ������ �α� ��� �ð�

    void Update()
    {
        if (Time.time - lastLogTime >= 1f) // 1�ʸ��� ����
        {
            DisplayPlayerTileInfo();
            lastLogTime = Time.time; // ������ �α� �ð� ����
        }
    }

    private void DisplayPlayerTileInfo()
    {
        Vector3 playerPosition = player.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            Debug.Log($"[�÷��̾� ��ġ] ��ǥ: {tilePosition}, Ÿ�ϸ� Ÿ��: {currentTile.name}");
        }
        else
        {
            Debug.Log($"[�÷��̾� ��ġ] ��ǥ: {tilePosition}, ���� Ÿ�� ����");
        }

        if (mapGenerator != null)
        {
            var tileCache = mapGenerator.GetTileCache();
        }
    }
}