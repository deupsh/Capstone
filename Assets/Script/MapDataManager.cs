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
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator가 설정되지 않았습니다.");
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
                Debug.LogWarning($"[경고] 타일 이름 없음: 위치={tileEntry.Key}");
            }
        }

        string json = JsonUtility.ToJson(mapData, true);
        string path = Application.persistentDataPath + "/mapdata.json";

        File.WriteAllText(path, json);

        Debug.Log($"맵 데이터 저장 완료: {path}\n{json}");
    }

    // JSON 파일에서 맵 데이터를 불러오는 함수
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MapData mapData = JsonUtility.FromJson<MapData>(json);
        }
    }

    // 타일 이름에 맞는 실제 타일 반환 함수
    private TileBase GetTileByName(string name)
    {
        TileBase tile = Resources.Load<TileBase>($"Tiles/{name}");
        if (tile == null)
        {
            Debug.LogWarning($"[타일 로드 실패] 이름: {name}");
        }
        else
        {
            Debug.Log($"[타일 로드 성공] 이름: {name}, 리소스 경로: Tiles/{name}");
        }
        return tile;
    }
}