using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

// 각 숫자가 지형을 의미
public enum Biome : int
{
    Snow = 0,
    Cave = 1,
    Ocean = 2,
    Desert = 3,
    Forest = 4,
    Swamp = 5,
    Lava = 6,
    Grassland = 7,
    MAX
}

[System.Serializable]
public class TileData
{
    public Vector3Int position;
    public string tileType;
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>();
}

public class InfiniteMapGenerator : MonoBehaviour
{

    [Header("타일맵 관련")]
    [SerializeField] private Tilemap tileMap; // 타일맵 참조

    [Space]
    [Header("눈 지형")]
    [SerializeField] private TileBase snow, snow2; // 눈 타일

    [Header("동굴 지형")]
    [SerializeField] private TileBase cave, cave2; // 동굴 타일

    [Header("바다 지형")]
    [SerializeField] private TileBase ocean, ocean2; // 바다 타일

    [Header("사막 지형")]
    [SerializeField] private TileBase desert, desert2; // 사막 타일

    [Header("숲 지형")]
    [SerializeField] private TileBase forest, forest2; // 숲 타일

    [Header("습지 지형")]
    [SerializeField] private TileBase swamp, swamp2; // 습지 타일

    [Header("용암 지형")]
    [SerializeField] private TileBase lava, lava2; // 용암 타일

    [Header("초원 지형")]
    [SerializeField] private TileBase grassland, grassland2; // 초원 지역 타일들

    [Header("시간 관련 설정")]
    [SerializeField] private float dayDuration = 600f; // 낮과 밤의 총 길이 (초 단위, 600초 = 10분)
    private float currentTime = 0f; // 현재 시간 (0 ~ dayDuration)
    private bool isDay = true; // 현재 시간이 낮인지 밤인지

    [Header("맵 밝기 설정")]
    [SerializeField] private Color dayColor = Color.white; // 낮의 타일맵 색상
    [SerializeField] private Color nightColor = Color.black; // 밤의 타일맵 색상
    [SerializeField] private float transitionSpeed = 0.5f; // 낮과 밤 전환 속도

    private TilemapRenderer tilemapRenderer;

    [Space]
    [Header("값 관련")]
    [SerializeField] private float mapScale = 0.01f; // 노이즈 : 자연스럽게 랜덤한 맵 생성
    [SerializeField] private int chunkSize = 16; // 청크 크기
    [SerializeField] private int worldSizeInChunks = 10; // 최초 생성 월드 크기
    [SerializeField] private int octaves = 3; // 옥타브 : 지형의 세부적 복잡도
    [SerializeField] private int pointNum = 2; // 바이옴 크기랑 반비례 (클수록 바이옴 크기가 작아짐)

    private float seed = 123; // 시드 값
    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // 로드된 청크 관리
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // 타일 캐싱 (재생성 방지)


    /// <summary>
    /// 게임 시작 시 호출되는 함수
    /// </summary>
    void Start()
    {
        //seed = Random.Range(0f, 10000f); // 시드 값 초기화

        if (File.Exists(Application.persistentDataPath + "/mapdata.json"))
        {
            LoadMap();  // 기존 맵이 있으면 불러오기
        }
        else
        {
            GenerateFullMap();  // 없으면 새로 생성
        }
        StartCoroutine(TimeCycle()); // 시간 주기를 시작
        StartCoroutine(UpdateChunks()); // 플레이어 이동에 따라 청크 업데이트

    }


    /// <summary>
    /// 맵 데이터를 JSON 파일로 저장하는 함수
    /// </summary>
    public void SaveMap()
    {
        MapData mapData = new MapData();

        foreach (var tileEntry in tileCache)
        {
            TileData tileData = new TileData();
            tileData.position = tileEntry.Key;
            tileData.tileType = tileEntry.Value.name;  // 타일의 이름을 저장
            mapData.tiles.Add(tileData);
        }

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(Application.persistentDataPath + "/mapdata.json", json);
    }


    /// <summary>
    /// JSON 파일에서 맵 데이터를 불러오는 함수
    /// </summary>
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MapData mapData = JsonUtility.FromJson<MapData>(json);

            // 기존 맵 초기화
            tileMap.ClearAllTiles();
            tileCache.Clear();

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileByName(tileData.tileType);  // 이름에 맞는 타일 찾기
                if (tile != null)
                {
                    tileMap.SetTile(tileData.position, tile);
                    tileCache[tileData.position] = tile;  // 캐시에 다시 저장
                }
            }
        }
        else
        {
            Debug.LogError("맵 데이터 파일을 찾을 수 없습니다.");
        }
    }


    /// <summary>
    /// 특정 청크를 로드하는 함수 (타일 캐싱 적용)
    /// </summary>
    private void LoadChunk(Vector3Int chunkPos)
    {
        if (!loadedChunks.ContainsKey(chunkPos))
        {
            float[,] noiseArr = GenerateNoise(chunkPos);
            Vector2[] randomPoints = GenerateRandomPos(pointNum);
            Vector2[] biomePoints = GenerateRandomPos((int)Biome.MAX);
            Biome[,] biomeArr = GenerateBiome(randomPoints, biomePoints);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                    if (!tileCache.ContainsKey(tilePos))
                    {
                        TileBase tile = GetTileByHight(noiseArr[x, y], biomeArr[x, y]);
                        tileMap.SetTile(tilePos, tile);
                        tileCache[tilePos] = tile;
                    }
                }
            }
        }
    }


    /// <summary>
    /// 타일 이름에 맞는 실제 타일 반환 함수
    /// </summary>
    private TileBase GetTileByName(string name)
    {
        switch (name)
        {
            case "snow": return snow;
            case "snow2": return snow2;
            case "cave": return cave;
            case "cave2": return cave2;
            case "ocean": return ocean;
            case "ocean2": return ocean2;
            case "desert": return desert;
            case "desert2": return desert2;
            case "forest": return forest;
            case "forest2": return forest2;
            case "swamp": return swamp;
            case "swamp2": return swamp2;
            case "lava": return lava;
            case "lava2": return lava2;
            case "grassland": return grassland;
            case "grassland2": return grassland2;
            default: return null;
        }
    }


    /// <summary>
    /// 게임 종료 시 맵 저장 호출
    /// </summary>
    void OnApplicationQuit()
    {
        SaveMap();
    }


    /// <summary>
    /// 최초 생성맵을 생성하는 함수
    /// </summary>
    private void GenerateFullMap()
    {
        for (int chunkX = 0; chunkX < worldSizeInChunks; chunkX++)
        {
            for (int chunkY = 0; chunkY < worldSizeInChunks; chunkY++)
            {
                Vector3Int chunkPos = new Vector3Int(chunkX, chunkY, 0);
                LoadChunk(chunkPos);
            }
        }
    }


    /// <summary>
    /// 플레이어의 현재 청크 좌표를 가져오는 함수
    /// </summary>
    private Vector3Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        return new Vector3Int(
            Mathf.FloorToInt(playerPosition.x / chunkSize),
            Mathf.FloorToInt(playerPosition.y / chunkSize),
            0);
    }


    /// <summary>
    /// 플레이어 이동에 따라 청크를 업데이트하는 코루틴 함수
    /// </summary>
    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector3Int currentPlayerChunk = GetPlayerChunkPosition();
            LoadSurroundingChunks(currentPlayerChunk);
            UnloadDistantChunks(currentPlayerChunk);
            yield return new WaitForSeconds(0.5f);
        }
    }


    /// <summary>
    /// 주변 청크들을 로드하는 함수
    /// </summary>
    private void LoadSurroundingChunks(Vector3Int centerChunk)
    {
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                Vector3Int chunkPos = new Vector3Int(centerChunk.x + xOffset, centerChunk.y + yOffset, 0);
                if (!loadedChunks.ContainsKey(chunkPos))
                {
                    LoadChunk(chunkPos);
                    loadedChunks[chunkPos] = true;
                }
            }
        }
    }


    /// <summary>
    /// 멀어진 청크들을 언로드하는 함수
    /// </summary>
    private void UnloadDistantChunks(Vector3Int centerChunk)
    {
        List<Vector3Int> chunksToUnload = new List<Vector3Int>();
        foreach (var chunk in loadedChunks.Keys)
        {
            if (Vector3Int.Distance(chunk, centerChunk) > 2)
            {
                chunksToUnload.Add(chunk);
            }
        }
        foreach (var chunk in chunksToUnload)
        {
            UnloadChunk(chunk);
            loadedChunks.Remove(chunk);
        }
    }


    /// <summary>
    /// 특정 청크를 언로드하는 함수 (타일맵에서 제거)
    /// </summary>
    private void UnloadChunk(Vector3Int chunkPos)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                if (tileCache.ContainsKey(tilePos))
                {
                    tileMap.SetTile(tilePos, null);
                    tileCache.Remove(tilePos);
                }
            }
        }
    }
    // 노이즈 배열을 생성하는 함수 (청크별로)
    private float[,] GenerateNoise(Vector3Int chunkPos)
    {
        float[,] noiseArr = new float[chunkSize, chunkSize];
        float min = float.MaxValue, max = float.MinValue;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float lacunarity = 2.0f;
                float gain = 0.5f;
                float amplitude = 0.5f;
                float frequency = 1f;
                int worldX = chunkPos.x * chunkSize + x;
                int worldY = chunkPos.y * chunkSize + y;

                for (int i = 0; i < octaves; i++)
                {
                    noiseArr[x, y] += amplitude * (Mathf.PerlinNoise(
                        seed + (worldX * mapScale * frequency),
                        seed + (worldY * mapScale * frequency)
                    ) * 2 - 1);
                    frequency *= lacunarity;
                    amplitude *= gain;
                }

                if (noiseArr[x, y] < min) min = noiseArr[x, y];
                if (noiseArr[x, y] > max) max = noiseArr[x, y];
            }
        }

        // 노이즈 값을 [0,1] 범위로 정규화
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                noiseArr[x, y] = Mathf.InverseLerp(min, max, noiseArr[x, y]);
            }
        }

        return noiseArr;
    }

    // 바이옴 배열을 생성하는 함수
    private Biome[,] GenerateBiome(Vector2[] points, Vector2[] biomePoints)
    {
        Biome[,] biomeArr = new Biome[chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float minDist = float.MaxValue;
                int idx = -1;

                for (int i = 0; i < pointNum; i++)
                {
                    float dist = Vector2.Distance(points[i], new Vector2(x, y));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        idx = i;
                    }
                }

                biomeArr[x, y] = (Biome)GetMinIdx(points[idx], biomePoints);
            }
        }

        return biomeArr;
    }

    // 랜덤 위치 배열을 생성하는 함수
    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] arr = new Vector2[num];

        for (int i = 0; i < num; i++)
        {
            int x = Random.Range(0, chunkSize - 1);
            int y = Random.Range(0, chunkSize - 1);
            arr[i] = new Vector2(x, y);
        }

        return arr;
    }

    // 가장 가까운 바이옴 인덱스를 계산하는 함수
    private int GetMinIdx(Vector2 point, Vector2[] biomeArr)
    {
        int curIdx = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < biomeArr.Length; i++)
        {
            float dist = Vector2.Distance(point, biomeArr[i]);
            if (dist < minDist)
            {
                minDist = dist;
                curIdx = i;
            }
        }

        return curIdx;
    }

    // 타일맵에 타일을 설정하는 함수
    private void SettingTileMap(float[,] noiseArr, Biome[,] biomeArr)
    {
        Vector3Int point = Vector3Int.zero;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                point.Set(x, y, 0);
                tileMap.SetTile(point, GetTileByHight(noiseArr[x, y], biomeArr[x, y]));
            }
        }
    }

    // 높이에 따른 타일을 결정하는 함수
    private TileBase GetTileByHight(float height, Biome biome)
    {
        switch (biome)
        {
            case Biome.Snow:
                if (height <= 0.35f) return snow;
                else if (height <= 0.45f) return snow2;
                else return snow;

            case Biome.Cave:
                if (height <= 0.35f) return cave;
                else if (height <= 0.45f) return cave2;
                else return cave;

            case Biome.Ocean:
                if (height <= 0.35f) return ocean;
                else if (height <= 0.45f) return ocean2;
                else return ocean;

            case Biome.Desert:
                if (height <= 0.35f) return desert;
                else if (height <= 0.45f) return desert2;
                else return desert;

            case Biome.Forest:
                if (height <= 0.35f) return forest;
                else if (height <= 0.45f) return forest2;
                else return forest;

            case Biome.Swamp:
                if (height <= 0.35f) return swamp;
                else if (height <= 0.45f) return swamp2;
                else return swamp;

            case Biome.Lava:
                if (height <= 0.35f) return lava;
                else if (height <= 0.45f) return lava2;
                else return lava;

            case Biome.Grassland:
                if (height <= 0.35f) return grassland;
                else if (height <= 0.45f) return grassland2;
                else return grassland;

            default:
                return grassland; // 기본값으로 초원 타일 반환
        }
    }
    // 플레이어가 서 있는 타일의 좌표와 바이옴을 콘솔에 출력하는 함수
    private void PrintPlayerTileInfo()
    {
        // 플레이어의 현재 위치를 타일맵 좌표로 변환
        Vector3 playerPosition = Camera.main.transform.position;
        Vector3Int tilePosition = new Vector3Int(
            Mathf.FloorToInt(playerPosition.x),
            Mathf.FloorToInt(playerPosition.y),
            0  // z 값은 0으로 고정
        );

        // 해당 좌표에서 타일맵에서 타일 정보 가져오기
        TileBase tileAtPlayer = tileMap.GetTile(tilePosition);

        // 타일이 존재하는지 확인하고, 콘솔에 출력
        if (tileAtPlayer != null)
        {
            Debug.Log($"타일 종류: {tilePosition} with tile type: {tileAtPlayer.name}");
        }
        else
        {
            Debug.Log($"버그?: {tilePosition}");
        }
    }
    void Update()
    {
        PrintPlayerTileInfo();  // 매 프레임마다 플레이어가 서 있는 타일 정보 출력
        UpdateLighting();
    }

    private IEnumerator TimeCycle()
    {
        while (true)
        {
            currentTime += Time.deltaTime;

            if (currentTime >= dayDuration) // 시간이 다 되면 낮/밤 전환
            {
                isDay = !isDay;
                currentTime = 0f;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 맵의 밝기를 서서히 변화시키는 함수
    /// </summary>
    private void UpdateLighting()
    {
        // 목표 색상을 설정 (낮이면 dayColor, 밤이면 nightColor)
        Color targetColor = isDay ? dayColor : nightColor;

        // 타일맵의 색상을 서서히 변화시킴 (Lerp 함수 사용)
        tileMap.color = Color.Lerp(tileMap.color, targetColor, transitionSpeed * Time.deltaTime);
    }
}

