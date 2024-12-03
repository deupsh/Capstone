using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public enum Biome : int { Snow = 0, PolForest = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX }

public class MapGenerator : MonoBehaviour
{
    [Header("Ÿ�ϸ� ����")]
    [SerializeField] private Tilemap tileMap;

    [Header("Ÿ�� ���ҽ�")]
    [SerializeField]
    private TileBase snow, snow2, polforest, polforest2, ocean, ocean2, desert, desert2, forest, forest2, swamp, swamp2, lava, lava2, grassland, grassland2;

    [Header("�� ����")]
    [SerializeField] private float mapScale = 0.01f; // ������ ������
    [SerializeField] public int chunkSize = 16; // ûũ ũ��
    [SerializeField] private int octaves = 3; // ������ ���⵵
    [SerializeField] private int pointNum = 8; // ���̿� ũ�� ���� (Ŭ���� ���� ���̿�)
    [SerializeField] public float unloadDistance = 3; // �÷��̾�κ��� �� ûũ �̻� �־��� ��� ��ε�

    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // �ε�� ûũ ����
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // Ÿ�� ĳ��


    private Transform player; // �÷��̾� Transform ����

    public event Action<Vector3Int, Biome[,]> OnChunkGenerated;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;

        if (player == null)
        {
            Debug.LogError("�÷��̾ �������� �ʾҽ��ϴ�. Player �±װ� �ִ� ������Ʈ�� Ȯ���ϼ���.");
            return;
        }

        // �ʱ� ûũ ���� ȣ��
        //GenerateInitialChunks();

        // �ʱ� ûũ ���� �� �ڷ�ƾ ����
        StartCoroutine(UpdateChunks());
    }

    // �߾�ȭ�� SetTile �޼���
    public void SetTile(Vector3Int position, TileBase tile)
    {
        tileMap.SetTile(position, tile);

        if (tile != null)
        {
            tileCache[position] = tile;
        }
        else
        {
            tileCache.Remove(position);
        }
    }

    // �÷��̾� �̵��� ���� ûũ�� ������Ʈ�ϴ� �ڷ�ƾ
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

    // �÷��̾��� ���� ûũ ��ǥ�� ���
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
        LoadChunk(initialChunk); // �ʱ� ûũ �ε�
        LoadSurroundingChunks(initialChunk);
    }

    public Dictionary<Vector3Int, TileBase> GetTileCache()
    {
        return tileCache;
    }

    // �ֺ� ûũ�� �ε�
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

    // �ָ� ������ ûũ�� ��ε�
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

    // Ư�� ûũ�� �ε�
    private void LoadChunk(Vector3Int chunkPos)
    {
        float[,] noiseArr = GenerateNoise(chunkPos);
        Biome[,] biomeArr = GenerateBiome(GenerateRandomPos(pointNum));

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);

                if (tileCache.ContainsKey(tilePos) || tileMap.HasTile(tilePos))
                {
                    continue;
                }

                TileBase tile = GetTileByHeight(noiseArr[x, y], biomeArr[x % chunkSize, y % chunkSize]);
                if (tile != null)
                {
                    SetTile(tilePos, tile);
                }
                else
                {
                    Debug.LogWarning($"Ÿ�� ���� ����: ��ġ={tilePos}");
                }
            }
        }

        OnChunkGenerated?.Invoke(chunkPos, biomeArr);
    }

    // Ư�� ûũ�� ��ε�
    private void UnloadChunk(Vector3Int chunkPos)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);

                if (tileCache.ContainsKey(tilePos))
                {
                    SetTile(tilePos, null); // Ÿ�ϸʿ��� ����
                    tileCache.Remove(tilePos); // ĳ�ÿ��� ����
                }
            }
        }
        // MonsterSpawnManager ȣ���Ͽ� ûũ �� ���� ��ε�
        MonsterSpawnManager monsterSpawnManager = FindFirstObjectByType<MonsterSpawnManager>();
        if (monsterSpawnManager != null)
        {
            monsterSpawnManager.UnloadMonstersForChunk(chunkPos);
        }
    }

        // ������ �迭 ����
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

        // ���� ��ġ �迭 ����
        private Vector2[] GenerateRandomPos(int num)
        {
            Vector2[] positions = new Vector2[num];

            // ���� ��� ���̿ȿ� ���� �ּ� �ϳ��� ����Ʈ�� ����
            for (int i = 0; i < (int)Biome.MAX; i++)
            {
                if (i < num)
                {
                    positions[i] = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                }
            }

            // ������ ����Ʈ�� �����ϰ� ����
            for (int i = (int)Biome.MAX; i < num; i++)
            {
                positions[i] = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            }

            return positions;
        }

        // ���̿� �迭 ����
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

    // ���̿� ���� Ÿ���� �����ϴ� �Լ�
    private TileBase GetTileByHeight(float height, Biome biome)
    {
        TileBase tile = biome switch
        {
            Biome.Snow => height <= 0.5f ? snow : snow2,
            Biome.PolForest => height <= 0.5f ? polforest : polforest2,
            Biome.Ocean => height <= 0.5f ? ocean : ocean2,
            Biome.Desert => height <= 0.5f ? desert : desert2,
            Biome.Forest => height <= 0.5f ? forest : forest2,
            Biome.Swamp => height <= 0.5f ? swamp : swamp2,
            Biome.Lava => height <= 0.5f ? lava : lava2,
            Biome.Grassland => height <= 0.5f ? grassland : grassland2,
            _ => grassland // �⺻������ �ʿ� Ÿ�� ��ȯ
        };
        return tile;
    }
    public bool ValidateIntegrity()
        {
            foreach (var kvp in tileCache)
            {
                TileBase mapTile = tileMap.GetTile(kvp.Key);
                if (mapTile == null || mapTile.name != kvp.Value.name)
                {
                    Debug.LogError($"[���Ἲ ����] ��ġ: {kvp.Key}, Ÿ�ϸ�={mapTile?.name ?? "null"}, ĳ��={kvp.Value.name}");
                    return false;
                }
            }
            Debug.Log("[���Ἲ ���� �Ϸ�] ��� �����Ͱ� ��ġ�մϴ�.");
            return true;
        }

        public Biome GetBiomeAt(Vector3Int position)
        {
        // Ÿ�ϸ� �Ǵ� ĳ�ÿ��� �ش� ��ġ�� ���̿� ������ ��ȯ
            if (tileCache.TryGetValue(position, out TileBase tile))
            {
                if (tile.name.Contains("Snow")) return Biome.Snow;
                if (tile.name.Contains("Forest")) return Biome.Forest;
                if (tile.name.Contains("Lava")) return Biome.Lava;
                if (tile.name.Contains("Ocean")) return Biome.Ocean;
                if (tile.name.Contains("Grassland")) return Biome.Grassland;
                if (tile.name.Contains("PolForest")) return Biome.PolForest;
                if (tile.name.Contains("Swamp")) return Biome.Swamp;
                if (tile.name.Contains("Desert")) return Biome.Desert;
            }
        return Biome.Lava; // �⺻�� (�� �� ���� ���̿�)
        }
    }