using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfoManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // 타일맵 참조
    [SerializeField] private Transform player; // 플레이어 참조
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조

    void Update()
    {
        DisplayPlayerTileInfo();
    }

    private void DisplayPlayerTileInfo()
    {
        Vector3 playerPosition = player.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        TileBase currentTile = tileMap.GetTile(tilePosition);

        if (currentTile != null)
        {
            Debug.Log($"[플레이어 위치] 좌표: {tilePosition}, 타일맵 타일: {currentTile.name}");
        }
        else
        {
            Debug.Log($"[플레이어 위치] 좌표: {tilePosition}, 현재 타일 없음");
        }

        if (mapGenerator != null)
        {
            var tileCache = mapGenerator.GetTileCache();
            if (tileCache.ContainsKey(tilePosition))
            {
                Debug.Log($"[캐시 데이터] 좌표: {tilePosition}, 캐시 타일: {tileCache[tilePosition].name}");

                if (currentTile != null && currentTile.name != tileCache[tilePosition].name)
                {
                    Debug.LogError($"[불일치 발견] 좌표: {tilePosition}, 타일맵={currentTile.name}, 캐시={tileCache[tilePosition].name}");
                }
            }
           
        }
    }
}