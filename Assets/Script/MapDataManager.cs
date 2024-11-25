using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapDataManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����

    public void SaveMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator�� �������� �ʾҽ��ϴ�.");
            return;
        }

        // ����� �α�: tileCache ���� ���
        foreach (var kvp in mapGenerator.GetTileCache())
        {
            Debug.Log($"����Ǵ� Ÿ��: ��ġ={kvp.Key}, �̸�={kvp.Value.name}");
        }

        // MapData ���� �� JSON ���� ����
        Dictionary<Vector3Int, TileBase> tileCache = mapGenerator.GetTileCache();
        MapData mapData = new MapData();

        foreach (var tileEntry in tileCache)
        {
            if (tileEntry.Value != null)
            {
                TileData tileData = new TileData
                {
                    position = tileEntry.Key,
                    tileType = tileEntry.Value.name
                };
                mapData.tiles.Add(tileData);
            }
        }

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(Application.persistentDataPath + "/mapdata.json", json);

        Debug.Log("�� ������ ���� �Ϸ�: " + json);
    }

    // JSON ���Ͽ��� �� �����͸� �ҷ����� �Լ�
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Debug.Log($"[JSON ���� ����]:\n{json}");

            MapData mapData = JsonUtility.FromJson<MapData>(json);

            foreach (var tileData in mapData.tiles)
            {
                Debug.Log($"[�ε�� JSON ������] ��ġ: {tileData.position}, Ÿ�� Ÿ��: {tileData.tileType}");
            }
        }
    }

    // Ÿ�� �̸��� �´� ���� Ÿ�� ��ȯ �Լ�
    private TileBase GetTileByName(string name)
    {
        TileBase tile = Resources.Load<TileBase>($"Tiles/{name}");
        if (tile == null)
        {
            Debug.LogWarning($"[Ÿ�� �ε� ����] �̸�: {name}");
        }
        else
        {
            Debug.Log($"[Ÿ�� �ε� ����] �̸�: {name}, ���ҽ� ���: Tiles/{name}");
        }
        return tile;
    }
}