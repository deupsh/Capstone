using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapDataManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap; // 타일맵 참조
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조

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

        string json = JsonUtility.ToJson(mapData, true); // JSON 형식으로 변환
        File.WriteAllText(Application.persistentDataPath + "/mapdata.json", json); // 파일로 저장

        Debug.Log("맵 데이터 저장 완료");
    }

    /// <summary>
    /// JSON 파일에서 맵 데이터를 불러오는 함수
    /// </summary>
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path); // JSON 파일 읽기
            MapData mapData = JsonUtility.FromJson<MapData>(json); // JSON 데이터 파싱

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileByName(tileData.tileType);
                if (tile != null)
                {
                    tileMap.SetTile(tileData.position, tile); // 타일맵에 타일 배치

                    // 캐시에 추가 또는 업데이트
                    if (!mapGenerator.GetTileCache().ContainsKey(tileData.position))
                    {
                        mapGenerator.GetTileCache().Add(tileData.position, tile);
                    }
                }
                else
                {
                    Debug.LogWarning($"알 수 없는 타일 이름: {tileData.tileType}");
                }
            }

            Debug.Log("맵 데이터 로드 완료");
        }
        else
        {
            Debug.LogError("맵 데이터 파일을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 타일 이름에 맞는 실제 타일 반환 함수
    /// </summary>
    private TileBase GetTileByName(string name)
    {
        switch (name.ToLower()) // 대소문자 무시
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
                Debug.LogWarning($"알 수 없는 타일 이름: {name}");
                return null;
        }
    }
}