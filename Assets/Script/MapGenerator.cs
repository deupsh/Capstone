using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Biome : int { Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX }

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 관련")]
    [SerializeField] private Tilemap tileMap;

    [Header("타일 리소스")]
    [SerializeField]
    private TileBase snow, snow2, cave, cave2, ocean, ocean2,
                                      desert, desert2, forest, forest2,
                                      swamp, swamp2, lava, lava2,
                                      grassland, grassland2;

    [Header("맵 설정")]
    [SerializeField] private float mapScale = 0.01f; // 노이즈 스케일
    [SerializeField] private int chunkSize = 16; // 청크 크기
    [SerializeField] private int octaves = 3; // 노이즈 복잡도
    [SerializeField] private int pointNum = 8; // 바이옴 크기 조절 (클수록 작은 바이옴)
    [SerializeField] private float unloadDistance = 3; // 플레이어로부터 몇 청크 이상 멀어진 경우 언로드

    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // 로드된 청크 관리
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // 타일 캐싱

    private Transform player; // 플레이어 Transform 참조

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

    /// <summary>
    /// 플레이어 이동에 따라 청크를 업데이트하는 코루틴
    /// </summary>
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

    /// <summary>
    /// 플레이어의 현재 청크 좌표를 계산
    /// </summary>
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
        Vector3Int initialChunk = new Vector3Int(0, 0, 0); // 초기 청크 위치
        LoadSurroundingChunks(initialChunk); // 주변 청크를 로드
    }

    public Dictionary<Vector3Int, TileBase> GetTileCache()
    {
        return tileCache;
    }
    /// <summary>
    /// 주변 청크를 로드
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
    /// 멀리 떨어진 청크를 언로드
    /// </summary>
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


    /// <summary>
    /// 특정 청크를 로드
    /// </summary>
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
                        tileMap.SetTile(tilePos, tile);
                        tileCache[tilePos] = tile;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 특정 청크를 언로드
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
                    tileMap.SetTile(tilePos, null); // 타일맵에서 제거
                    tileCache.Remove(tilePos); // 캐시에서 제거
                }
            }
        }
    }

    /// <summary>
    /// 노이즈 배열 생성
    /// </summary>
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

    /// <summary>
    /// 랜덤 위치 배열 생성
    /// </summary>
    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] positions = new Vector2[num];

        // 먼저 모든 바이옴에 대해 최소 하나의 포인트를 생성
        for (int i = 0; i < (int)Biome.MAX; i++)
        {
            if (i < num)
            {
                positions[i] = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            }
        }

        // 나머지 포인트를 랜덤하게 생성
        for (int i = (int)Biome.MAX; i < num; i++)
        {
            positions[i] = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        return positions;
    }

    /// <summary>
    /// 바이옴 배열 생성
    /// </summary>
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

    /// <summary>
    /// 높이에 따른 타일을 결정하는 함수
    /// </summary>
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

    

 }