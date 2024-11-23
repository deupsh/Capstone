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
        MapData mapData = new MapData();

        foreach (var tileEntry in mapGenerator.GetTileCache())
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

        string json = JsonUtility.ToJson(mapData, true); // JSON �������� ��ȯ
        File.WriteAllText(Application.persistentDataPath + "/mapdata.json", json); // ���Ϸ� ����

        Debug.Log("�� ������ ���� �Ϸ�");
    }

    /// <summary>
    /// JSON ���Ͽ��� �� �����͸� �ҷ����� �Լ�
    /// </summary>
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path); // JSON ���� �б�
            MapData mapData = JsonUtility.FromJson<MapData>(json); // JSON ������ �Ľ�

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileByName(tileData.tileType);
                if (tile != null)
                {
                    tileMap.SetTile(tileData.position, tile); // Ÿ�ϸʿ� Ÿ�� ��ġ

                    // ĳ�ÿ� �߰� �Ǵ� ������Ʈ
                    if (!mapGenerator.GetTileCache().ContainsKey(tileData.position))
                    {
                        mapGenerator.GetTileCache().Add(tileData.position, tile);
                    }
                }
                else
                {
                    Debug.LogWarning($"�� �� ���� Ÿ�� �̸�: {tileData.tileType}");
                }
            }

            Debug.Log("�� ������ �ε� �Ϸ�");
        }
        else
        {
            Debug.LogError("�� ������ ������ ã�� �� �����ϴ�.");
        }
    }

    /// <summary>
    /// Ÿ�� �̸��� �´� ���� Ÿ�� ��ȯ �Լ�
    /// </summary>
    private TileBase GetTileByName(string name)
    {
        switch (name.ToLower()) // ��ҹ��� ����
        {
            case "snowtile": return Resources.Load<TileBase>("Tiles/SnowTile");
            case "snowtile2": return Resources.Load<TileBase>("Tiles/SnowTile2");
            case "cavetile": return Resources.Load<TileBase>("Tiles/CaveTile");
            case "cavetile2": return Resources.Load<TileBase>("Tiles/CaveTile2");
            case "oceantile": return Resources.Load<TileBase>("Tiles/OceanTile");
            case "oceantile2": return Resources.Load<TileBase>("Tiles/OceanTile2");
            case "deserttile": return Resources.Load<TileBase>("Tiles/DesertTile");
            case "deserttile2": return Resources.Load<TileBase>("Tiles/DesertTile2");
            case "foresttile": return Resources.Load<TileBase>("Tiles/ForestTile");
            case "foresttile2": return Resources.Load<TileBase>("Tiles/ForestTile2");
            case "swamptile": return Resources.Load<TileBase>("Tiles/SwampTile");
            case "swamptile2": return Resources.Load<TileBase>("Tiles/SwampTile2");
            case "lavatile": return Resources.Load<TileBase>("Tiles/LavaTile");
            case "lavatile2": return Resources.Load<TileBase>("Tiles/LavaTile2");
            case "grasslandtile": return Resources.Load<TileBase>("Tiles/GrasslandTile");
            case "grasslandtile2": return Resources.Load<TileBase>("Tiles/GrasslandTile2");
            default:
                Debug.LogWarning($"�� �� ���� Ÿ�� �̸�: {name}");
                return null;
        }
    }
}