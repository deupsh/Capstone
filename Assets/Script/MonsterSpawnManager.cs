using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterSpawnManager : MonoBehaviour
{
    [Header("���� ������")]
    [SerializeField] private GameObject SnowMonster;
    [SerializeField] private GameObject PolForestMonster;
    [SerializeField] private GameObject OceanMonster;
    [SerializeField] private GameObject DesertMonster;
    [SerializeField] private GameObject ForestMonster;
    [SerializeField] private GameObject SwampMonster;
    [SerializeField] private GameObject LavaMonster;
    [SerializeField] private GameObject GrasslandMonster;

    [Header("����")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����
    [SerializeField] private Tilemap tileMap; // Ÿ�ϸ� ����
    [SerializeField] private Transform player; // �÷��̾� Transform ����

    [Header("���� ���� ����")]
    [SerializeField] private int spawnLimitPerBiome = 5; // ���̿ȴ� �ִ� ���� ���� ��

    private Dictionary<Vector3Int, GameObject> spawnedMonsters = new Dictionary<Vector3Int, GameObject>(); // Ÿ�� ��ġ�� ���� ����
    private Dictionary<Biome, int> biomeSpawnCount = new Dictionary<Biome, int>(); // ���̿Ⱥ� ���� Ƚ�� ����

    private void Start()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnChunkGenerated += HandleChunkGenerated; // �� ���� �Ϸ� �̺�Ʈ ����
        }
        else
        {
            Debug.LogError("[MonsterSpawnManager] MapGenerator�� �������� �ʾҽ��ϴ�.");
        }

        foreach (Biome biome in System.Enum.GetValues(typeof(Biome)))
        {
            biomeSpawnCount[biome] = 0; // �ʱ�ȭ
        }

        StartCoroutine(CheckUnloadMonsters());
    }

    private void HandleChunkGenerated(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        SpawnMonstersForChunk(chunkPos, biomeArr);
    }

    public void SpawnMonsterAtTile(Vector3Int tilePos, Biome biome)
    {
        if (spawnedMonsters.ContainsKey(tilePos) || biomeSpawnCount[biome] >= spawnLimitPerBiome)
        {
            return;
        }

        GameObject monsterPrefab = GetMonsterPrefab(biome);

        if (monsterPrefab != null)
        {
            Vector3 worldPosition = tileMap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0);
            GameObject monster = Instantiate(monsterPrefab, worldPosition, Quaternion.identity);
            spawnedMonsters[tilePos] = monster;
            biomeSpawnCount[biome]++;
        }
    }

    public void SpawnMonstersForChunk(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        for (int x = 0; x < mapGenerator.chunkSize; x++)
        {
            for (int y = 0; y < mapGenerator.chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * mapGenerator.chunkSize + x, chunkPos.y * mapGenerator.chunkSize + y, 0);
                Biome biome = biomeArr[x % mapGenerator.chunkSize, y % mapGenerator.chunkSize];
                SpawnMonsterAtTile(tilePos, biome);
            }
        }
    }

    public void UnloadMonsterAtTile(Vector3Int tilePos)
    {
        if (spawnedMonsters.TryGetValue(tilePos, out GameObject monster))
        {
            Destroy(monster);
            spawnedMonsters.Remove(tilePos);

            Biome biome = mapGenerator.GetBiomeAt(tilePos);
            if (biomeSpawnCount.ContainsKey(biome))
            {
                biomeSpawnCount[biome]--;
            }
        }
    }

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

    private IEnumerator CheckUnloadMonsters()
    {
        while (true)
        {
            List<Vector3Int> tilesToUnload = new List<Vector3Int>();

            foreach (var kvp in spawnedMonsters)
            {
                Vector3 worldPosition = tileMap.CellToWorld(kvp.Key);
                float distanceToPlayer = Vector3.Distance(player.position, worldPosition);

                if (distanceToPlayer > mapGenerator.unloadDistance * mapGenerator.chunkSize)
                {
                    tilesToUnload.Add(kvp.Key);
                }
            }

            foreach (var tile in tilesToUnload)
            {
                UnloadMonsterAtTile(tile);
            }

            yield return new WaitForSeconds(1f); // 1�ʸ��� üũ
        }
    }

    private GameObject GetMonsterPrefab(Biome biome)
    {
        switch (biome)
        {
            case Biome.Snow: return SnowMonster;
            case Biome.PolForest: return PolForestMonster;
            case Biome.Ocean: return OceanMonster;
            case Biome.Desert: return DesertMonster;
            case Biome.Forest: return ForestMonster;
            case Biome.Swamp: return SwampMonster;
            case Biome.Lava: return LavaMonster;
            case Biome.Grassland: return GrasslandMonster;
            default:
                Debug.LogError($"[GetMonsterPrefab] �������� �ʴ� ���̿�: {biome}");
                return null;
        }
    }
}