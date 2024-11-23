using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Biome : int { Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX }

public class MapGenerator : MonoBehaviour
{
    [Header("Ÿ�ϸ� ����")]
    [SerializeField] private Tilemap tileMap;

    [Header("Ÿ�� ���ҽ�")]
    [SerializeField]
    private TileBase snow, snow2, cave, cave2, ocean, ocean2,
                                      desert, desert2, forest, forest2,
                                      swamp, swamp2, lava, lava2,
                                      grassland, grassland2;

    [Header("�� ����")]
    [SerializeField] private float mapScale = 0.01f; // ������ ������
    [SerializeField] private int chunkSize = 16; // ûũ ũ��
    [SerializeField] private int octaves = 3; // ������ ���⵵
    [SerializeField] private int pointNum = 8; // ���̿� ũ�� ���� (Ŭ���� ���� ���̿�)
    [SerializeField] private float unloadDistance = 3; // �÷��̾�κ��� �� ûũ �̻� �־��� ��� ��ε�

    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // �ε�� ûũ ����
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // Ÿ�� ĳ��

    private Transform player; // �÷��̾� Transform ����

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;

        if (player == null)
        {
            Debug.LogError("�÷��̾ �������� �ʾҽ��ϴ�. Player �±װ� �ִ� ������Ʈ�� Ȯ���ϼ���.");
            return;
        }

        StartCoroutine(UpdateChunks());
    }

    /// <summary>
    /// �÷��̾� �̵��� ���� ûũ�� ������Ʈ�ϴ� �ڷ�ƾ
    /// </summary>
    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector3Int currentPlayerChunk = GetPlayerChunkPosition();
            LoadSurroundingChunks(currentPlayerChunk);
            UnloadDistantChunks(currentPlayerChunk);

            yield return new WaitForSeconds(0.5f); // 0.5�ʸ��� ������Ʈ
        }
    }

    /// <summary>
    /// �÷��̾��� ���� ûũ ��ǥ�� ���
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
        Vector3Int initialChunk = new Vector3Int(0, 0, 0); // �ʱ� ûũ ��ġ
        LoadSurroundingChunks(initialChunk); // �ֺ� ûũ�� �ε�
    }

    public Dictionary<Vector3Int, TileBase> GetTileCache()
    {
        return tileCache;
    }
    /// <summary>
    /// �ֺ� ûũ�� �ε�
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
    /// �ָ� ������ ûũ�� ��ε�
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
    /// Ư�� ûũ�� �ε�
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

                if (!tileCache.ContainsKey(tilePos)) // ĳ�ÿ� ���� ��쿡�� �߰�
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
    /// Ư�� ûũ�� ��ε�
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
                    tileMap.SetTile(tilePos, null); // Ÿ�ϸʿ��� ����
                    tileCache.Remove(tilePos); // ĳ�ÿ��� ����
                }
            }
        }
    }

    /// <summary>
    /// ������ �迭 ����
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

                // �ּ�/�ִ밪 ����
                if (noiseArr[x, y] < minValue) minValue = noiseArr[x, y];
                if (noiseArr[x, y] > maxValue) maxValue = noiseArr[x, y];
            }
        }

        // ����ȭ: ��� ���� [0, 1] ������ ��ȯ
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
    /// ���� ��ġ �迭 ����
    /// </summary>
    private Vector2[] GenerateRandomPos(int num)
    {
        Vector2[] positions = new Vector2[num];

        // ���� ��� ���̿ȿ� ���� �ּ� �ϳ��� ����Ʈ�� ����
        for (int i = 0; i < (int)Biome.MAX; i++)
        {
            if (i < num)
            {
                positions[i] = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            }
        }

        // ������ ����Ʈ�� �����ϰ� ����
        for (int i = (int)Biome.MAX; i < num; i++)
        {
            positions[i] = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        return positions;
    }

    /// <summary>
    /// ���̿� �迭 ����
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

                // ���� ���� ���� ����� ���� ����Ʈ�� ã��
                for (int i = 0; i < points.Length; i++)
                {
                    float distanceToPoint = Vector2.Distance(currentPoint, points[i]);
                    if (distanceToPoint < minDistance)
                    {
                        minDistance = distanceToPoint;
                        closestBiomeIndex = i;
                    }
                }

                // ����� ����Ʈ�� ������� ���̿� ����
                biomeArr[x, y] = (Biome)(closestBiomeIndex % (int)Biome.MAX);
            }
        }

        return biomeArr;
    }

    /// <summary>
    /// ���̿� ���� Ÿ���� �����ϴ� �Լ�
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
        default: return grassland; // �⺻������ �ʿ� Ÿ�� ��ȯ
        }
    }

    

 }