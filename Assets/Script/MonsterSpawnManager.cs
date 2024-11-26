using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterSpawnManager : MonoBehaviour
{
    [Header("���� ������")]
    [SerializeField] private GameObject SnowMonster;
    [SerializeField] private GameObject CaveMonster;
    [SerializeField] private GameObject OceanMonster;
    [SerializeField] private GameObject DesertMonster;
    [SerializeField] private GameObject ForestMonster;
    [SerializeField] private GameObject SwampMonster;
    [SerializeField] private GameObject LavaMonster;
    [SerializeField] private GameObject GrasslandMonster;

    [Header("����")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����

    [Header("���� ���� ����")]
    [SerializeField] private int spawnLimitPerBiome = 5; // ���̿ȴ� �ִ� ���� ���� ��

    private Dictionary<Vector3Int, GameObject> spawnedMonsters = new Dictionary<Vector3Int, GameObject>(); // Ÿ�� ��ġ�� ���� ����
    private Dictionary<Biome, int> biomeSpawnCount = new Dictionary<Biome, int>(); // ���̿Ⱥ� ���� Ƚ�� ����

    private void Start()
    {
        // MapGenerator�� �����Ǿ� �ִ��� Ȯ��
        if (mapGenerator != null)
        {
            mapGenerator.OnChunkGenerated += HandleChunkGenerated; // �� ���� �Ϸ� �̺�Ʈ ����
        }
        else
        {
            Debug.LogError("[MonsterSpawnManager] MapGenerator�� �������� �ʾҽ��ϴ�.");
        }

        // ���̿Ⱥ� ���� ī���� �ʱ�ȭ
        foreach (Biome biome in System.Enum.GetValues(typeof(Biome)))
        {
            biomeSpawnCount[biome] = 0; // �ʱ�ȭ
        }
    }

    // �� ���� �Ϸ� �� ȣ��Ǵ� �̺�Ʈ �ڵ鷯
    private void HandleChunkGenerated(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        Debug.Log($"[MonsterSpawnManager] ûũ ���� �Ϸ�: {chunkPos}");
        SpawnMonstersForChunk(chunkPos, biomeArr);
    }

    // Ư�� Ÿ�� ��ġ�� ���͸� �����ϴ� �޼���
    public void SpawnMonsterAtTile(Vector3Int tilePos, Biome biome)
    {
        // �̹� �ش� Ÿ�Ͽ� ���Ͱ� �ְų�, ���̿��� ���� ������ �ʰ��� ��� �ǳʶ�
        if (spawnedMonsters.ContainsKey(tilePos) || biomeSpawnCount[biome] >= spawnLimitPerBiome)
        {
            return;
        }

        // ���̿ȿ� �ش��ϴ� ���� ������ ��������
        GameObject monsterPrefab = GetMonsterPrefab(biome);

        if (monsterPrefab != null)
        {
            // ���� ��ǥ�� ��ȯ
            Vector3 worldPosition = tileMap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0); // �߾� ����

            // ���� ����
            GameObject monster = Instantiate(monsterPrefab, worldPosition, Quaternion.identity);

            // Dictionary�� �߰� �� ī���� ����
            spawnedMonsters[tilePos] = monster;
            biomeSpawnCount[biome]++;
            
            Debug.Log($"���� ����: ��ġ={tilePos}, ���̿�={biome}, ���� ���̿� ī��Ʈ={biomeSpawnCount[biome]}");
        }
    }

    // Ư�� ûũ �� ��� Ÿ�Ͽ� ���� ���͸� �����ϴ� �޼���
    public void SpawnMonstersForChunk(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        for (int x = 0; x < mapGenerator.chunkSize; x++)
        {
            for (int y = 0; y < mapGenerator.chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * mapGenerator.chunkSize + x, chunkPos.y * mapGenerator.chunkSize + y, 0);

                // ���̿� ���� ��������
                Biome biome = biomeArr[x % mapGenerator.chunkSize, y % mapGenerator.chunkSize];

                SpawnMonsterAtTile(tilePos, biome);
            }
        }
    }

    // Ư�� Ÿ�� ��ġ�� ���͸� ��ε��ϴ� �޼���
    public void UnloadMonsterAtTile(Vector3Int tilePos)
    {
        if (spawnedMonsters.TryGetValue(tilePos, out GameObject monster))
        {
            // �ش� Ÿ���� ���� ���� �� ī���� ����
            Destroy(monster);
            spawnedMonsters.Remove(tilePos);

            Biome biome = mapGenerator.GetBiomeAt(tilePos); // �ش� Ÿ���� ���̿� ���� ��������
            if (biomeSpawnCount.ContainsKey(biome))
            {
                biomeSpawnCount[biome]--;
                Debug.Log($"���� ��ε�: ��ġ={tilePos}, ���̿�={biome}, ���� ���̿� ī��Ʈ={biomeSpawnCount[biome]}");
            }
        }
    }

    // Ư�� ûũ �� ��� Ÿ�Ͽ��� ���͸� ��ε��ϴ� �޼���
    public void UnloadMonstersForChunk(Vector3Int chunkPos)
    {
        for (int x = 0; x < mapGenerator.chunkSize; x++)
        {
            for (int y = 0; y < mapGenerator.chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * mapGenerator.chunkSize + x, chunkPos.y * mapGenerator.chunkSize + y, 0);

                UnloadMonsterAtTile(tilePos);
            }
        }
    }

    // �־��� ���̿ȿ� �ش��ϴ� ���� �������� ��ȯ�ϴ� �޼���
    private GameObject GetMonsterPrefab(Biome biome)
    {
        switch (biome)
        {
            case Biome.Snow: return SnowMonster;
            case Biome.Cave: return CaveMonster;
            case Biome.Ocean: return OceanMonster;
            case Biome.Desert: return DesertMonster;
            case Biome.Forest: return ForestMonster;
            case Biome.Swamp: return SwampMonster;
            case Biome.Lava: return LavaMonster;
            case Biome.Grassland: return GrasslandMonster;
            default: return null;
        }
    }
}