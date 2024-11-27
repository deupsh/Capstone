using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfoManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����
    [SerializeField] private Transform player; // �÷��̾� ����
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����

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
            Debug.Log($"[�÷��̾� ��ġ] ��ǥ: {tilePosition}, Ÿ�ϸ� Ÿ��: {currentTile.name}");
        }
        else
        {
            Debug.Log($"[�÷��̾� ��ġ] ��ǥ: {tilePosition}, ���� Ÿ�� ����");
        }

        if (mapGenerator != null)
        {
            var tileCache = mapGenerator.GetTileCache();
            if (tileCache.ContainsKey(tilePosition))
            {
                Debug.Log($"[ĳ�� ������] ��ǥ: {tilePosition}, ĳ�� Ÿ��: {tileCache[tilePosition].name}");

                if (currentTile != null && currentTile.name != tileCache[tilePosition].name)
                {
                    Debug.LogError($"[����ġ �߰�] ��ǥ: {tilePosition}, Ÿ�ϸ�={currentTile.name}, ĳ��={tileCache[tilePosition].name}");
                }
            }
           
        }
    }
}