using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

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

public class RandomMapGenerater : MonoBehaviour
{
    [Header("타일맵 관련")]
    [SerializeField] private GameObject tileMapPrefab; // 타일맵 프리팹 (각 청크마다 타일맵을 생성)

    [Space]
    [Header("눈 지형")]
    [SerializeField] private TileBase snow, snow2;

    [Header("동굴 지형")]
    [SerializeField] private TileBase cave, cave2;

    [Header("바다 지형")]
    [SerializeField] private TileBase ocean, ocean2;

    [Header("사막 지형")]
    [SerializeField] private TileBase desert, desert2;

    [Header("숲 지형")]
    [SerializeField] private TileBase forest, forest2;

    [Header("습지 지형")]
    [SerializeField] private TileBase swamp, swamp2;

    [Header("용암 지형")]
    [SerializeField] private TileBase lava, lava2;

    [Header("초원 지형")]
    [SerializeField] private TileBase grassland, grassland2;

    [Space]
    [Header("값 관련")]
    [SerializeField] private float mapScale = 0.01f;
    [SerializeField] private int mapSize = 32; // 전체 맵 크기
    [SerializeField] private int chunkSize = 16; // 청크 크기 (16x16)

    // 노이즈 및 바이옴 배열 저장
    private float[,] noiseArr;
    private Biome[,] biomeArr;

    // 로드된 청크들을 저장하는 딕셔너리
    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();

    // 플레이어 주변 몇 개의 청크를 로드할지 설정
    [SerializeField] private int viewDistance = 3;
    private int octaves;
    private int pointNum;

    // Start 함수에서 노이즈 및 바이옴 배열 생성 후 초기 청크 로드
    private void Start()
    {
        noiseArr = GenerateNoise();
        var randomPoint = GenerateRandomPos(10);
        var biomePoint = GenerateRandomPos((int)Biome.MAX);
        biomeArr = GenerateBiome(randomPoint, biomePoint);

        UpdateChunks(Vector2Int.zero); // 초기 플레이어 위치에서 주변 청크 로드
    }

    // 매 프레임마다 플레이어 위치에 따라 필요한 청크 업데이트
    private void Update()
    {
        Vector2Int currentPlayerChunkPos = GetPlayerChunkPosition();
        UpdateChunks(currentPlayerChunkPos);
    }

    // 플레이어의 현재 위치를 기준으로 청크 좌표 계산
    private Vector2Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        return new Vector2Int(Mathf.FloorToInt(playerPosition.x / chunkSize), Mathf.FloorToInt(playerPosition.y / chunkSize));
    }

    // 플레이어 주변의 필요한 청크를 로드하고 멀리 있는 청크는 언로드
    private void UpdateChunks(Vector2Int playerChunkPos)
    {
        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        // 플레이어 주변의 필요한 청크 계산 및 로드
        for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
        {
            for (int yOffset = -viewDistance; yOffset <= viewDistance; yOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkPos.x + xOffset, playerChunkPos.y + yOffset);
                activeChunks.Add(chunkCoord);

                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    // LoadChunk(chunkCoord); 대신 CreateChunk 호출
                    CreateChunk(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
                }
            }
        }

        // 더 이상 필요하지 않은 청크 언로드
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (var chunk in loadedChunks)
        {
            if (!activeChunks.Contains(chunk.Key))
            {
                chunksToUnload.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
    }

    // 새로운 청크 로드 (비동기로 처리)
    private void CreateChunk(int startX, int startY)
    {
        // 타일맵 프리팹 인스턴스화
        GameObject tileMapObj = Instantiate(tileMapPrefab);
        tileMapObj.transform.position = new Vector3(startX, startY);

        Tilemap tileMapInstance = tileMapObj.GetComponent<Tilemap>();

        // 해당 청크의 타일들을 설정
        for (int xOffset = 0; xOffset < chunkSize; xOffset++)
        {
            for (int yOffset = 0; yOffset < chunkSize; yOffset++)
            {
                int xPos = startX + xOffset;
                int yPos = startY + yOffset;

                // 배열 범위 검사 추가
                if (xPos >= 0 && xPos < mapSize && yPos >= 0 && yPos < mapSize)
                {
                    Vector3Int tilePosition = new Vector3Int(xOffset, yOffset, 0);
                    tileMapInstance.SetTile(tilePosition, GetTileByHight(noiseArr[xPos, yPos], biomeArr[xPos, yPos]));
                }
            }
        }
    }

    // 불필요한 청크 언로드
    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            Destroy(loadedChunks[chunkCoord]); // 타일맵 오브젝트 파괴
            loadedChunks.Remove(chunkCoord); // 딕셔너리에서 제거
        }
    }

    // 노이즈 배열 생성 (기존과 동일)
    private float[,] GenerateNoise()
    {
        float[,] noiseArr = new float[mapSize, mapSize];
        float min = float.MaxValue, max = float.MinValue;

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float lacunarity = 2.0f;
                float gain = 0.5f;

                float amplitude = 0.5f;
                float frequency = 1f;

                for (int i = 0; i < octaves; i++)
                {
                    noiseArr[x, y] += amplitude * (Mathf.PerlinNoise(
                         (x * mapScale * frequency),
                         (y * mapScale * frequency)) * 2 - 1);

                    frequency *= lacunarity;
                    amplitude *= gain;
                }

                if (noiseArr[x, y] < min) min = noiseArr[x, y];
                else if (noiseArr[x, y] > max) max = noiseArr[x, y];
            }
        }

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                noiseArr[x, y] = Mathf.InverseLerp(min, max, noiseArr[x, y]);
            }
        }

        return noiseArr;
    }

    // 바이옴 배열 생성 (기존과 동일)
    private Biome[,] GenerateBiome(Vector2[] points, Vector2[] biomePoints)
    {
        Biome[,] biomeArr = new Biome[mapSize, mapSize];

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
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

                // idx가 유효한지 확인
                if (idx >= 0 && idx < points.Length && idx < biomePoints.Length)
                {
                    biomeArr[x, y] = (Biome)GetMinIdx(points[idx], biomePoints);
                }
                else
                {
                    // 유효하지 않은 경우 기본값 설정
                    biomeArr[x, y] = Biome.Grassland;
                }
            }
        }

        return biomeArr;
    }

    // 랜덤 좌표 생성 (기존과 동일)
    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] arr = new Vector2[num];

        for (int i = 0; i < num; i++)
        {
            int x = Random.Range(0, mapSize - 1);
            int y = Random.Range(0, mapSize - 1);

            arr[i] = new Vector2(x, y);
        }

        return arr;
    }

    // 바이옴 인덱스 계산 (기존과 동일)
    private int GetMinIdx(Vector2 point, Vector2[] biomeArr)
    {
        int curIdx = 0;
        float min = float.MaxValue;

        for (int i = 0; i < biomeArr.Length; i++)
        {
            float value = Vector2.Distance(point, biomeArr[i]);

            if (min > value)
            {
                min = value;
                curIdx = i;
            }
        }

        return curIdx;
    }

    // 높이에 따른 타일 결정 함수 (기존과 동일)
    private TileBase GetTileByHight(float hight, Biome biome)
    {

        if (biome == Biome.Snow)
        {

            switch (hight)
            {

                case <= 0.35f: return snow;
                case <= 0.45f: return snow2;
                case <= 0.6f: return snow;
                default: return snow;

            }

        }
        else if (biome == Biome.Cave)
        {
            switch (hight)
            {

                case <= 0.35f: return cave;
                case <= 0.45f: return cave2;
                case <= 0.6f: return cave;
                default: return cave2;

            }
        }

        else if (biome == Biome.Ocean)
        {
            switch (hight)
            {

                case <= 0.35f: return ocean;
                case <= 0.45f: return ocean2;
                case <= 0.6f: return ocean;
                default: return ocean2;

            }
        }

        else if (biome == Biome.Desert)
        {
            switch (hight)
            {

                case <= 0.35f: return desert;
                case <= 0.45f: return desert2;
                case <= 0.6f: return desert;
                default: return desert2;

            }
        }

        else if (biome == Biome.Forest)
        {
            switch (hight)
            {

                case <= 0.35f: return forest;
                case <= 0.45f: return forest2;
                case <= 0.6f: return forest;
                default: return forest2;

            }
        }

        else if (biome == Biome.Swamp)
        {
            switch (hight)
            {

                case <= 0.35f: return swamp;
                case <= 0.45f: return swamp2;
                case <= 0.6f: return swamp;
                default: return swamp2;

            }
        }

        else if (biome == Biome.Lava)
        {
            switch (hight)
            {

                case <= 0.35f: return lava;
                case <= 0.45f: return lava2;
                case <= 0.6f: return lava;
                default: return lava2;

            }
        }

        else if (biome == Biome.Grassland)
        {
            switch (hight)
            {

                case <= 0.35f: return grassland;
                case <= 0.45f: return grassland2;
                case <= 0.6f: return grassland;
                default: return grassland2;

            }
        }
        else
            return grassland;

    }

}