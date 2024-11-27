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

        Dictionary<Vector3Int, TileBase> tileCache = mapGenerator.GetTileCache();

        MapData mapData = new MapData();

        foreach (var tileEntry in tileCache)
        {
            if (tileEntry.Value != null && !string.IsNullOrEmpty(tileEntry.Value.name))
            {
                TileData tileData = new TileData
                {
                    position = tileEntry.Key,
                    tileType = tileEntry.Value.name
                };
                mapData.tiles.Add(tileData);
            }
            else
            {
                Debug.LogWarning($"[���] Ÿ�� �̸� ����: ��ġ={tileEntry.Key}");
            }
        }

        string json = JsonUtility.ToJson(mapData, true);
        string path = Application.persistentDataPath + "/mapdata.json";

        File.WriteAllText(path, json);

        Debug.Log($"�� ������ ���� �Ϸ�: {path}\n{json}");
    }

    // JSON ���Ͽ��� �� �����͸� �ҷ����� �Լ�
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MapData mapData = JsonUtility.FromJson<MapData>(json);
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