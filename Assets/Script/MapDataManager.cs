using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapDataManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����

    // �ʿ��� Ÿ�ϵ��� �߰��մϴ�.
    [SerializeField] private TileBase snow, snow2, polforest, polforest2, ocean, ocean2, desert, desert2, forest, forest2, swamp, swamp2, lava, lava2, grassland, grassland2;

    public void SaveMap()
    {
        Dictionary<Vector3Int, TileBase> tileCache = mapGenerator.GetTileCache();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator�� �������� �ʾҽ��ϴ�.");
            return;
        }

        if (tileCache == null || tileCache.Count == 0)
        {
            Debug.LogWarning("������ Ÿ�� �����Ͱ� �����ϴ�.");
            return;
        }

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

    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MapData mapData = JsonUtility.FromJson<MapData>(json);

            foreach (var tileData in mapData.tiles)
            {
                TileBase tile = GetTileByName(tileData.tileType);
                if (tile != null)
                {
                    mapGenerator.SetTile(tileData.position, tile);
                }
                else
                {
                    Debug.LogWarning($"Ÿ���� ã�� �� �����ϴ�: {tileData.tileType}");
                }
            }

            Debug.Log("�� ������ �ε� �Ϸ�");
        }
        else
        {
            Debug.LogWarning("�� ������ ������ �������� �ʽ��ϴ�.");
        }
    }

    private TileBase GetTileByName(string tileName)
    {
        switch (tileName)
        {
            case "SnowTile":
                return snow;
            case "SnowTile2":
                return snow2;
            case "PolForestTile":
                return polforest;
            case "PolForestTile2":
                return polforest2;
            case "OceanTile":
                return ocean;
            case "OceanTile2":
                return ocean2;
            case "DesertTile":
                return desert;
            case "DesertTile2":
                return desert2;
            case "ForestTile":
                return forest;
            case "ForestTile2":
                return forest2;
            case "SwampTile":
                return swamp;
            case "SwampTile2":
                return swamp2;
            case "LavaTile":
                return lava;
            case "LavaTile2":
                return lava2;
            case "GrasslandTile":
                return grassland;
            case "GrasslandTile2":
                return grassland2;
            default:
                Debug.LogWarning($"�� �� ���� Ÿ�� �̸�: {tileName}");
                return null;
        }
    }
}