using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public enum Biome : int { Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX }

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 관련")]
    [SerializeField] private Tilemap tileMap;

    [Header("타일 리소스")]
    [SerializeField]
    private TileBase snow, snow2, cave, cave2, ocean, ocean2, desert, desert2, forest, forest2, swamp, swamp2, lava, lava2, grassland, grassland2;

    [Header("맵 설정")]
    [SerializeField] private float mapScale = 0.01f; // 노이즈 스케일
    [SerializeField] public int chunkSize = 16; // 청크 크기
    [SerializeField] private int octaves = 3; // 노이즈 복잡도
    [SerializeField] private int pointNum = 8; // 바이옴 크기 조절 (클수록 작은 바이옴)
    [SerializeField] private float unloadDistance = 3; // 플레이어로부터 몇 청크 이상 멀어진 경우 언로드

    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // 로드된 청크 관리
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // 타일 캐싱


    private Transform player; // 플레이어 Transform 참조

    public event Action<Vector3Int, Biome[,]> OnChunkGenerated;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;

        if (player == null)
        {
            Debug.LogError("플레이어가 설정되지 않았습니다. Player 태그가 있는 오브젝트를 확인하세요.");
            return;
        }

        StartCoroutine(UpdateChunks());
    }

    // 중앙화된 SetTile 메서드
    public void SetTile(Vector3Int position, TileBase tile)
    {
        // 타일맵 업데이트
        tileMap.SetTile(position, tile);

        // 캐시 업데이트
        if (tile != null)
        {
            tileCache[position] = tile;
        }
        else
        {
            tileCache.Remove(position);
        }

        Debug.Log($"[SetTile 호출] 위치: {position}, 타일 이름: {(tile != null ? tile.name : "null")}");
    }

    // 플레이어 이동에 따라 청크를 업데이트하는 코루틴
    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector3Int currentPlayerChunk = GetPlayerChunkPosition();
            LoadSurroundingChunks(currentPlayerChunk);
            UnloadDistantChunks(currentPlayerChunk);

            yield return new WaitForSeconds(0.5f); // 0.5초마다 업데이트
        }
    }

    // 플레이어의 현재 청크 좌표를 계산
    private Vector3Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = player.position;
        return new Vector3Int(
            Mathf.FloorToInt(playerPosition.x / chunkSize),
            Mathf.FloorToInt(playerPosition.y / chunkSize),
            0
        );
    }

    public void GenerateInitialChunks()
    {
        Vector3Int initialChunk = new Vector3Int(0, 0, 0);
        LoadChunk(initialChunk); // 청크 로드

        foreach (var kvp in tileCache)
        {
            Debug.Log($"생성된 타일: 위치={kvp.Key}, 이름={kvp.Value.name}");
        }
    }

    public Dictionary<Vector3Int, TileBase> GetTileCache()
    {
        return tileCache;
    }

    // 주변 청크를 로드
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

    // 멀리 떨어진 청크를 언로드
    private void UnloadDistantChunks(Vector3Int centerChunk)
    {
        List<Vector3Int> chunksToUnload = new List<Vector3Int>();

        foreach (var chunk in loadedChunks.Keys)
        {
            if (Vector3Int.Distance(chunk, centerChunk) > unloadDistance)
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

    // 특정 청크를 로드
    private void LoadChunk(Vector3Int chunkPos)
    {
        float[,] noiseArr = GenerateNoise(chunkPos);
        Vector2[] randomPoints = GenerateRandomPos(pointNum);
        Biome[,] biomeArr = GenerateBiome(randomPoints);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);

                if (!tileCache.ContainsKey(tilePos)) // 캐시에 없는 경우에만 추가
                {
                    TileBase tile = GetTileByHeight(noiseArr[x, y], biomeArr[x % chunkSize, y % chunkSize]);
                    if (tile != null)
                    {
                        SetTile(tilePos, tile); // 중앙화된 SetTile 호출
                    }
                }
            }
        }
        OnChunkGenerated?.Invoke(chunkPos, biomeArr);
    }

    // 특정 청크를 언로드
    private void UnloadChunk(Vector3Int chunkPos)
{
    for (int x = 0; x < chunkSize; x++)
    {
        for (int y = 0; y < chunkSize; y++)
        {
            Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);

            if (tileCache.ContainsKey(tilePos))
            {
                SetTile(tilePos, null); // 타일맵에서 제거
                tileCache.Remove(tilePos); // 캐시에서 제거
            }
        }
    }
    // MonsterSpawnManager 호출하여 청크 내 몬스터 언로드
    MonsterSpawnManager monsterSpawnManager = FindObjectOfType<MonsterSpawnManager>();
    if (monsterSpawnManager != null)
    {
        monsterSpawnManager.UnloadMonstersForChunk(chunkPos);
    }
}

    // 노이즈 배열 생성
    private float[,] GenerateNoise(Vector3Int chunkPos)
    {
        float[,] noiseArr = new float[chunkSize, chunkSize];
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;

                for (int i = 0; i < octaves; i++)
                {
                    noiseArr[x, y] += amplitude * Mathf.PerlinNoise(
                        (chunkPos.x * chunkSize + x) * mapScale * frequency,
                        (chunkPos.y * chunkSize + y) * mapScale * frequency
                    );
                    frequency *= 2f;
                    amplitude *= 0.5f;
                }
                // 최소/최대값 갱신
                if (noiseArr[x, y] < minValue) minValue = noiseArr[x, y];
                if (noiseArr[x, y] > maxValue) maxValue = noiseArr[x, y];
            }
        }
        // 정규화: 모든 값을 [0, 1] 범위로 변환
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                noiseArr[x, y] = Mathf.InverseLerp(minValue, maxValue, noiseArr[x, y]);
            }
        }
        return noiseArr;
    }

    // 랜덤 위치 배열 생성
    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] positions = new Vector2[num];

        // 먼저 모든 바이옴에 대해 최소 하나의 포인트를 생성
        for (int i = 0; i < (int)Biome.MAX; i++)
        {
            if (i < num)
            {
                positions[i] = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            }
        }

        // 나머지 포인트를 랜덤하게 생성
        for (int i = (int)Biome.MAX; i < num; i++)
        {
            positions[i] = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        }

        return positions;
    }

    // 바이옴 배열 생성
    private Biome[,] GenerateBiome(Vector2[] points)
    {
        Biome[,] biomeArr = new Biome[chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector2 currentPoint = new Vector2(x / (float)chunkSize, y / (float)chunkSize);
                float minDistance = float.MaxValue;
                int closestBiomeIndex = -1;

                // 현재 점과 가장 가까운 랜덤 포인트를 찾음
                for (int i = 0; i < points.Length; i++)
                {
                    float distanceToPoint = Vector2.Distance(currentPoint, points[i]);
                    if (distanceToPoint < minDistance)
                    {
                        minDistance = distanceToPoint;
                        closestBiomeIndex = i;
                    }
                }
                // 가까운 포인트를 기반으로 바이옴 결정
                biomeArr[x, y] = (Biome)(closestBiomeIndex % (int)Biome.MAX);
            }
        }

        return biomeArr;
    }

    // 높이에 따른 타일을 결정하는 함수
    private TileBase GetTileByHeight(float height, Biome biome)
    {
        switch (biome)
        {
        case Biome.Snow: return height <= 0.5f ? snow : snow2;
        case Biome.Cave: return height <= 0.5f ? cave : cave2;
        case Biome.Ocean: return height <= 0.5f ? ocean : ocean2;
        case Biome.Desert: return height <= 0.5f ? desert : desert2;
        case Biome.Forest: return height <= 0.5f ? forest : forest2;
        case Biome.Swamp: return height <= 0.5f ? swamp : swamp2;
        case Biome.Lava: return height <= 0.5f ? lava : lava2;
        case Biome.Grassland: return height <= 0.5f ? grassland : grassland2;
        default: return grassland; // 기본값으로 초원 타일 반환
        }
    }
    public bool ValidateIntegrity()
    {
        foreach (var kvp in tileCache)
        {
            TileBase mapTile = tileMap.GetTile(kvp.Key);
            if (mapTile == null || mapTile.name != kvp.Value.name)
            {
                Debug.LogError($"[무결성 깨짐] 위치: {kvp.Key}, 타일맵={mapTile?.name ?? "null"}, 캐시={kvp.Value.name}");
                return false;
            }
        }
        Debug.Log("[무결성 검증 완료] 모든 데이터가 일치합니다.");
        return true;
    }

    public Biome GetBiomeAt(Vector3Int position)
    {
    // 타일맵 또는 캐시에서 해당 위치의 바이옴 정보를 반환
        if (tileCache.TryGetValue(position, out TileBase tile))
        {
            // 타일 이름 또는 다른 속성을 기반으로 바이옴 결정 (예제)
            if (tile.name.Contains("Snow")) return Biome.Snow;
            if (tile.name.Contains("Forest")) return Biome.Forest;
            if (tile.name.Contains("Lava")) return Biome.Lava;
            if (tile.name.Contains("Ocean")) return Biome.Ocean;
            if (tile.name.Contains("Grassland")) return Biome.Grassland;
            if (tile.name.Contains("Cave")) return Biome.Cave;
            if (tile.name.Contains("Swamp")) return Biome.Swamp;
            if (tile.name.Contains("Desert")) return Biome.Desert;
        }

    return Biome.MAX; // 기본값 (알 수 없는 바이옴)
    }
}