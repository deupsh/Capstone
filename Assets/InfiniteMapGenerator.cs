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
    [Header("Ÿ�ϸ� ����")]
    [SerializeField] private GameObject tileMapPrefab; // Ÿ�ϸ� ������ (�� ûũ���� Ÿ�ϸ��� ����)

    [Space]
    [Header("�� ����")]
    [SerializeField] private TileBase snow, snow2;

    [Header("���� ����")]
    [SerializeField] private TileBase cave, cave2;

    [Header("�ٴ� ����")]
    [SerializeField] private TileBase ocean, ocean2;

    [Header("�縷 ����")]
    [SerializeField] private TileBase desert, desert2;

    [Header("�� ����")]
    [SerializeField] private TileBase forest, forest2;

    [Header("���� ����")]
    [SerializeField] private TileBase swamp, swamp2;

    [Header("��� ����")]
    [SerializeField] private TileBase lava, lava2;

    [Header("�ʿ� ����")]
    [SerializeField] private TileBase grassland, grassland2;

    [Space]
    [Header("�� ����")]
    [SerializeField] private float mapScale = 0.01f;
    [SerializeField] private int mapSize = 32; // ��ü �� ũ��
    [SerializeField] private int chunkSize = 16; // ûũ ũ�� (16x16)

    // ������ �� ���̿� �迭 ����
    private float[,] noiseArr;
    private Biome[,] biomeArr;

    // �ε�� ûũ���� �����ϴ� ��ųʸ�
    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();

    // �÷��̾� �ֺ� �� ���� ûũ�� �ε����� ����
    [SerializeField] private int viewDistance = 3;
    private int octaves;
    private int pointNum;

    // Start �Լ����� ������ �� ���̿� �迭 ���� �� �ʱ� ûũ �ε�
    private void Start()
    {
        noiseArr = GenerateNoise();
        var randomPoint = GenerateRandomPos(10);
        var biomePoint = GenerateRandomPos((int)Biome.MAX);
        biomeArr = GenerateBiome(randomPoint, biomePoint);

        UpdateChunks(Vector2Int.zero); // �ʱ� �÷��̾� ��ġ���� �ֺ� ûũ �ε�
    }

    // �� �����Ӹ��� �÷��̾� ��ġ�� ���� �ʿ��� ûũ ������Ʈ
    private void Update()
    {
        Vector2Int currentPlayerChunkPos = GetPlayerChunkPosition();
        UpdateChunks(currentPlayerChunkPos);
    }

    // �÷��̾��� ���� ��ġ�� �������� ûũ ��ǥ ���
    private Vector2Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        return new Vector2Int(Mathf.FloorToInt(playerPosition.x / chunkSize), Mathf.FloorToInt(playerPosition.y / chunkSize));
    }

    // �÷��̾� �ֺ��� �ʿ��� ûũ�� �ε��ϰ� �ָ� �ִ� ûũ�� ��ε�
    private void UpdateChunks(Vector2Int playerChunkPos)
    {
        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        // �÷��̾� �ֺ��� �ʿ��� ûũ ��� �� �ε�
        for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
        {
            for (int yOffset = -viewDistance; yOffset <= viewDistance; yOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkPos.x + xOffset, playerChunkPos.y + yOffset);
                activeChunks.Add(chunkCoord);

                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    // LoadChunk(chunkCoord); ��� CreateChunk ȣ��
                    CreateChunk(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
                }
            }
        }

        // �� �̻� �ʿ����� ���� ûũ ��ε�
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

    // ���ο� ûũ �ε� (�񵿱�� ó��)
    private void CreateChunk(int startX, int startY)
    {
        // Ÿ�ϸ� ������ �ν��Ͻ�ȭ
        GameObject tileMapObj = Instantiate(tileMapPrefab);
        tileMapObj.transform.position = new Vector3(startX, startY);

        Tilemap tileMapInstance = tileMapObj.GetComponent<Tilemap>();

        // �ش� ûũ�� Ÿ�ϵ��� ����
        for (int xOffset = 0; xOffset < chunkSize; xOffset++)
        {
            for (int yOffset = 0; yOffset < chunkSize; yOffset++)
            {
                int xPos = startX + xOffset;
                int yPos = startY + yOffset;

                // �迭 ���� �˻� �߰�
                if (xPos >= 0 && xPos < mapSize && yPos >= 0 && yPos < mapSize)
                {
                    Vector3Int tilePosition = new Vector3Int(xOffset, yOffset, 0);
                    tileMapInstance.SetTile(tilePosition, GetTileByHight(noiseArr[xPos, yPos], biomeArr[xPos, yPos]));
                }
            }
        }
    }

    // ���ʿ��� ûũ ��ε�
    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            Destroy(loadedChunks[chunkCoord]); // Ÿ�ϸ� ������Ʈ �ı�
            loadedChunks.Remove(chunkCoord); // ��ųʸ����� ����
        }
    }

    // ������ �迭 ���� (������ ����)
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

    // ���̿� �迭 ���� (������ ����)
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

                // idx�� ��ȿ���� Ȯ��
                if (idx >= 0 && idx < points.Length && idx < biomePoints.Length)
                {
                    biomeArr[x, y] = (Biome)GetMinIdx(points[idx], biomePoints);
                }
                else
                {
                    // ��ȿ���� ���� ��� �⺻�� ����
                    biomeArr[x, y] = Biome.Grassland;
                }
            }
        }

        return biomeArr;
    }

    // ���� ��ǥ ���� (������ ����)
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

    // ���̿� �ε��� ��� (������ ����)
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

    // ���̿� ���� Ÿ�� ���� �Լ� (������ ����)
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