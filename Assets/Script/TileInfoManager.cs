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
            Debug.LogWarning("타일맵 또는 플레이어가 설정되지 않았습니다.");
            return;
        }

        Vector3 playerPosition = player.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            Debug.Log($"플레이어 위치: {tilePosition}, 현재 타일: {currentTile.name}");
        }
        else
        {
            Debug.Log($"플레이어 위치: {tilePosition}, 현재 타일 없음");
        }
    }
}