using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfoManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap;
    [SerializeField] private Transform player;

    void Update()
    {
        DisplayPlayerTileInfo();
    }

    private void DisplayPlayerTileInfo()
    {
        if (tileMap == null || player == null)
        {
            Debug.LogWarning("Ÿ�ϸ� �Ǵ� �÷��̾ �������� �ʾҽ��ϴ�.");
            return;
        }

        Vector3 playerPosition = player.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            Debug.Log($"�÷��̾� ��ġ: {tilePosition}, ���� Ÿ��: {currentTile.name}");
        }
        else
        {
            Debug.Log($"�÷��̾� ��ġ: {tilePosition}, ���� Ÿ�� ����");
        }
    }
}