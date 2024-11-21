using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

// �� ���ڰ� ������ �ǹ�
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

    [Header("Ÿ�ϸ� ����")]
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����

    [Space]
    [Header("�� ����")]
    [SerializeField] private TileBase snow, snow2; // �� Ÿ��

    [Header("���� ����")]
    [SerializeField] private TileBase cave, cave2; // ���� Ÿ��

    [Header("�ٴ� ����")]
    [SerializeField] private TileBase ocean, ocean2; // �ٴ� Ÿ��

    [Header("�縷 ����")]
    [SerializeField] private TileBase desert, desert2; // �縷 Ÿ��

    [Header("�� ����")]
    [SerializeField] private TileBase forest, forest2; // �� Ÿ��

    [Header("���� ����")]
    [SerializeField] private TileBase swamp, swamp2; // ���� Ÿ��

    [Header("��� ����")]
    [SerializeField] private TileBase lava, lava2; // ��� Ÿ��

    [Header("�ʿ� ����")]
    [SerializeField] private TileBase grassland, grassland2; // �ʿ� ���� Ÿ�ϵ�

    [Header("�ð� ���� ����")]
    [SerializeField] private float dayDuration = 600f; // ���� ���� �� ���� (�� ����, 600�� = 10��)
    private float currentTime = 0f; // ���� �ð� (0 ~ dayDuration)
    private bool isDay = true; // ���� �ð��� ������ ������

    [Header("�� ��� ����")]
    [SerializeField] private Color dayColor = Color.white; // ���� Ÿ�ϸ� ����
    [SerializeField] private Color nightColor = Color.black; // ���� Ÿ�ϸ� ����
    [SerializeField] private float transitionSpeed = 0.5f; // ���� �� ��ȯ �ӵ�

    private TilemapRenderer tilemapRenderer;

    [Space]
    [Header("�� ����")]
    [SerializeField] private float mapScale = 0.01f; // ������ : �ڿ������� ������ �� ����
    [SerializeField] private int chunkSize = 16; // ûũ ũ��
    [SerializeField] private int worldSizeInChunks = 10; // ���� ���� ���� ũ��
    [SerializeField] private int octaves = 3; // ��Ÿ�� : ������ ������ ���⵵
    [SerializeField] private int pointNum = 2; // ���̿� ũ��� �ݺ�� (Ŭ���� ���̿� ũ�Ⱑ �۾���)

    private float seed = 123; // �õ� ��
    private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>(); // �ε�� ûũ ����
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>(); // Ÿ�� ĳ�� (����� ����)


    ///���� ���� �� ȣ��Ǵ� �Լ�
    void Start()
    {
        //seed = Random.Range(0f, 10000f); // �õ� �� �ʱ�ȭ

        if (File.Exists(Application.persistentDataPath + "/mapdata.json"))
        {
            LoadMap();  // ���� ���� ������ �ҷ�����
        }
        else
        {
            GenerateFullMap();  // ������ ���� ����
        }
        StartCoroutine(TimeCycle()); // �ð� �ֱ⸦ ����
        StartCoroutine(UpdateChunks()); // �÷��̾� �̵��� ���� ûũ ������Ʈ

    }


    /// <summary>
    /// �� �����͸� JSON ���Ϸ� �����ϴ� �Լ�
    /// </summary>
    public void SaveMap()
    {
        MapData mapData = new MapData();

        foreach (var tileEntry in tileCache)
        {
            TileData tileData = new TileData();
            tileData.position = tileEntry.Key;
            tileData.tileType = tileEntry.Value.name;  // Ÿ���� �̸��� ����
            mapData.tiles.Add(tileData);
        }

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(Application.persistentDataPath + "/mapdata.json", json);
    }


    // JSON ���Ͽ��� �� �����͸� �ҷ����� �Լ�
    public void LoadMap()
    {
        string path = Application.persistentDataPath + "/mapdata.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MapData mapData = JsonUtility.FromJson<MapData>(json);

            // ���� ������ �ʱ�ȭ
            tileMap.ClearAllTiles();
            tileCache.Clear();

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileByName(tileData.tileType);
                if (tile != null)
                {
                    tileMap.SetTile(tileData.position, tile);
                    tileCache[tileData.position] = tile;
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

    // Ư�� ûũ�� �ε��ϴ� �Լ� (Ÿ�� ĳ�� ����)
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

                    // �̹� ������ Ÿ���� �ǳʶ�
                    if (!tileCache.ContainsKey(tilePos))
                    {
                        TileBase tile = GetTileByHight(noiseArr[x, y], biomeArr[x, y]);
                        tileMap.SetTile(tilePos, tile);
                        tileCache[tilePos] = tile;
                    }
                }
            }
            loadedChunks[chunkPos] = true; // ûũ �ε� ���� ������Ʈ
        }
    }

    // Ÿ�� �̸��� �´� ���� Ÿ�� ��ȯ �Լ�
    private TileBase GetTileByName(string name)
    {
        switch (name.ToLower()) // �ҹ��ڷ� ���Ͽ� �ϰ��� ����
        {
            case "snowtile": return snow;
            case "snowtile2": return snow2;
            case "cavetile": return cave;
            case "cavetile2": return cave2;
            case "oceantile": return ocean;
            case "oceantile2": return ocean2;
            case "deserttile": return desert;
            case "deserttile2": return desert2;
            case "foresttile": return forest;
            case "foresttile2": return forest2;
            case "swamptile": return swamp;
            case "swamptile2": return swamp2;
            case "lavatile": return lava;
            case "lavatile2": return lava2;
            case "grasslandtile": return grassland;
            case "grasslandtile2": return grassland2;
            default:
                Debug.LogWarning($"�� �� ���� Ÿ�� �̸�: {name}");
                return null;
        }
    }

    // ���� ���� �� �� ���� ȣ��
    void OnApplicationQuit()
    {
        SaveMap();
    }
    // ���� �������� �����ϴ� �Լ�
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
    // �÷��̾��� ���� ûũ ��ǥ�� �������� �Լ�
    private Vector3Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        return new Vector3Int(
            Mathf.FloorToInt(playerPosition.x / chunkSize),
            Mathf.FloorToInt(playerPosition.y / chunkSize),
            0);
    }

    /// �÷��̾� �̵��� ���� ûũ�� ������Ʈ�ϴ� �ڷ�ƾ �Լ�
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

    // �ֺ� ûũ���� �ε��ϴ� �Լ�
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

    // �־��� ûũ���� ��ε��ϴ� �Լ�
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

    // Ư�� ûũ�� ��ε��ϴ� �Լ� (Ÿ�ϸʿ��� ����)
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
    // ������ �迭�� �����ϴ� �Լ� (ûũ����)
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

        // ������ ���� [0,1] ������ ����ȭ
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                noiseArr[x, y] = Mathf.InverseLerp(min, max, noiseArr[x, y]);
            }
        }

        return noiseArr;
    }

    // ���̿� �迭�� �����ϴ� �Լ�
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

    // ���� ��ġ �迭�� �����ϴ� �Լ�
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

    // ���� ����� ���̿� �ε����� ����ϴ� �Լ�
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

    // Ÿ�ϸʿ� Ÿ���� �����ϴ� �Լ�
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

    // ���̿� ���� Ÿ���� �����ϴ� �Լ�
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
                return grassland; // �⺻������ �ʿ� Ÿ�� ��ȯ
        }
    }
    // �÷��̾ �� �ִ� Ÿ���� ��ǥ�� ���̿��� �ֿܼ� ����ϴ� �Լ�
    private void PrintPlayerTileInfo()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        Vector3Int tilePosition = tileMap.WorldToCell(playerPosition);

        TileBase currentTile = tileMap.GetTile(tilePosition);
        if (currentTile != null)
        {
            Debug.Log($"�÷��̾� ��ġ: {tilePosition}, ���� Ÿ��: {currentTile.name}");
        }
        else
        {
            Debug.Log($"�÷��̾� ��ġ: {tilePosition}, ���� Ÿ�� ����");
        }
    }
    void Update()
    {
        PrintPlayerTileInfo();  // �� �����Ӹ��� �÷��̾ �� �ִ� Ÿ�� ���� ���
        UpdateLighting();
    }

    private IEnumerator TimeCycle()
    {
        while (true)
        {
            currentTime += Time.deltaTime;

            // ��/�� ��ȯ �ð� ���
            float transitionStart = dayDuration * 2 / 3; // ��/���� ������ 1/3 ����
            float transitionEnd = dayDuration; // ��/���� ������ ����

            if (currentTime >= transitionStart && currentTime < transitionEnd)
            {
                // ��ȯ �������� ��� ������ ��ȭ
                float t = (currentTime - transitionStart) / (transitionEnd - transitionStart);
                tileMap.color = Color.Lerp(isDay ? dayColor : nightColor, isDay ? nightColor : dayColor, t);
            }

            if (currentTime >= dayDuration)
            {
                // ��/�� ���� ��ȯ
                isDay = !isDay;
                currentTime = 0f;
            }

            yield return null;
        }
    }

    // ���� ��⸦ ������ ��ȭ��Ű�� �Լ�
    private void UpdateLighting()
    {
        // ��ǥ ������ ���� (���̸� dayColor, ���̸� nightColor)
        Color targetColor = isDay ? dayColor : nightColor;

        // Ÿ�ϸ��� ������ ������ ��ȭ��Ŵ (Lerp �Լ� ���)
        tileMap.color = Color.Lerp(tileMap.color, targetColor, transitionSpeed * Time.deltaTime);
    }
}
