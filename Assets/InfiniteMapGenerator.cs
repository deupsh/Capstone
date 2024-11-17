using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Biome : int
{
    Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX
}

public class RandomMapGenerater : MonoBehaviour
{
    [Header("타일맵 관련")]
    [SerializeField] private GameObject tileMapPrefab;
    [SerializeField] private TileBase snow, snow2, cave, cave2, ocean, ocean2, desert, desert2, forest, forest2, swamp, swamp2, lava, lava2, grassland, grassland2;

    [Header("값 관련")]
    [SerializeField] private float mapScale = 0.01f;
    [SerializeField] private int chunkSize = 4;
    [SerializeField] private int viewDistance = 3;

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private float seed;
    private int octaves = 3;
    private int pointNum = 10;

    private void Start()
    {
        seed = Random.Range(0f, 10000f);
        UpdateChunks(Vector2Int.zero);
    }

    private void Update()
    {
        Vector2Int currentPlayerChunkPos = GetPlayerChunkPosition();
        UpdateChunks(currentPlayerChunkPos);
    }

    private Vector2Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        return new Vector2Int(Mathf.FloorToInt(playerPosition.x / chunkSize), Mathf.FloorToInt(playerPosition.y / chunkSize));
    }

    private void UpdateChunks(Vector2Int playerChunkPos)
    {
        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
        {
            for (int yOffset = -viewDistance; yOffset <= viewDistance; yOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkPos.x + xOffset, playerChunkPos.y + yOffset);
                activeChunks.Add(chunkCoord);

                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    // 비동기 함수 호출 시 StartCoroutine 사용
                    StartCoroutine(CreateChunkAsync(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize));
                }
            }
        }

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

    private IEnumerator CreateChunkAsync(int startX, int startY)
    {
        GameObject tileMapObj = Instantiate(tileMapPrefab);
        tileMapObj.transform.position = new Vector3(startX, startY);
        Tilemap tileMapInstance = tileMapObj.GetComponent<Tilemap>();

        for (int xOffset = 0; xOffset < chunkSize; xOffset++)
        {
            for (int yOffset = 0; yOffset < chunkSize; yOffset++)
            {
                int xPos = startX + xOffset;
                int yPos = startY + yOffset;
                Vector3Int tilePosition = new Vector3Int(xOffset, yOffset, 0);
                tileMapInstance.SetTile(tilePosition, GetTileByHight(GenerateNoiseAt(xPos, yPos), GenerateBiomeAt(xPos, yPos)));
            }
            yield return null; // 한 프레임 대기
        }

        loadedChunks[new Vector2Int(startX / chunkSize, startY / chunkSize)] = tileMapObj;
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            Destroy(loadedChunks[chunkCoord]);
            loadedChunks.Remove(chunkCoord);
        }
    }

    private float GenerateNoiseAt(int x, int y)
    {
        float lacunarity = 2.0f;
        float gain = 0.5f;

        float amplitude = 0.5f;
        float frequency = 1f;

        float noiseValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            noiseValue += amplitude * (Mathf.PerlinNoise(seed + (x * mapScale * frequency), seed + (y * mapScale * frequency)) * 2 - 1);
            frequency *= lacunarity;
            amplitude *= gain;
        }
        return Mathf.InverseLerp(-1f, 1f, noiseValue);
    }

    private Biome GenerateBiomeAt(int x, int y)
    {
        Vector2[] randomPoints = GenerateRandomPos(pointNum);
        Vector2[] biomePoints = GenerateRandomPos((int)Biome.MAX);

        float minDist = float.MaxValue;
        int closestPointIndex = -1;

        for (int i = 0; i < randomPoints.Length; i++)
        {
            float dist = Vector2.Distance(randomPoints[i], new Vector2(x, y));
            if (dist < minDist)
            {
                minDist = dist;
                closestPointIndex = i;
            }
        }
        return (Biome)GetMinIdx(randomPoints[closestPointIndex], biomePoints);
    }

    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] arr = new Vector2[num];
        for (int i = 0; i < num; i++) arr[i] = new Vector2(Random.Range(0, chunkSize - 1), Random.Range(0, chunkSize - 1));
        return arr;
    }

    private int GetMinIdx(Vector2 point, Vector2[] biomeArr)
    {
        int curIdx = 0; float minDist = float.MaxValue;
        for (int i = 0; i < biomeArr.Length; i++)
        {
            float dist = Vector2.Distance(point, biomeArr[i]);
            if (dist < minDist) { minDist = dist; curIdx = i; }
        }
        return curIdx;
    }

    private TileBase GetTileByHight(float hight, Biome biome)
    {
        if (biome == Biome.Snow)
        {
            if (hight <= 0.35f) return snow;
            else if (hight <= 0.45f) return snow2;
            else return snow;
        }
        else if (biome == Biome.Cave)
        {
            if (hight <= 0.35f) return cave;
            else if (hight <= 0.45f) return cave2;
            else return cave;
        }
        else if (biome == Biome.Ocean)
        {
            if (hight <= 0.35f) return ocean;
            else return ocean;
        }
        else if (biome == Biome.Desert)
        {
            if (hight <= 0.35f) return desert;
            else return desert;
        }
        else if (biome == Biome.Forest)
        {
            if (hight <= 0.35f) return forest;
            else if (hight <= 0.45f) return forest2;
            else return forest;
        }
        else if (biome == Biome.Swamp)
        {
            if (hight <= 0.35f) return swamp;
            else if (hight <= 0.45f) return swamp2;
            else return swamp;
        }
        else if (biome == Biome.Lava)
        {
            if (hight <= 0.35f) return lava;
            else if (hight <= 0.45f) return lava2;
            else return lava;
        }
        else
        {
            if (hight <= 0.35f) return grassland;
            else if (hight <= 0.45f) return grassland2;
            else return grassland;
        }
    }
}