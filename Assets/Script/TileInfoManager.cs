using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfoManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // 타일맵 참조
    [SerializeField] private Transform player; // 플레이어 참조
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조

    private float lastLogTime = 0f; // 마지막 로그 출력 시간

    void Update()
    {
        if (Time.time - lastLogTime >= 1f) // 1초마다 실행
        {
            DisplayPlayerTileInfo();
            lastLogTime = Time.time; // 마지막 로그 시간 갱신
        }
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
        }
    }
}