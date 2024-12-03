using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterSpawnManager : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    [SerializeField] private GameObject SnowMonster;
    [SerializeField] private GameObject PolForestMonster;
    [SerializeField] private GameObject OceanMonster;
    [SerializeField] private GameObject DesertMonster;
    [SerializeField] private GameObject ForestMonster;
    [SerializeField] private GameObject SwampMonster;
    [SerializeField] private GameObject LavaMonster;
    [SerializeField] private GameObject GrasslandMonster;

    [Header("참조")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조
    [SerializeField] private Tilemap tileMap; // 타일맵 참조
    [SerializeField] private Transform player; // 플레이어 Transform 참조

    [Header("스폰 제한 설정")]
    [SerializeField] private int spawnLimitPerBiome = 5; // 바이옴당 최대 몬스터 스폰 수

    private Dictionary<Vector3Int, GameObject> spawnedMonsters = new Dictionary<Vector3Int, GameObject>(); // 타일 위치와 몬스터 매핑
    private Dictionary<Biome, int> biomeSpawnCount = new Dictionary<Biome, int>(); // 바이옴별 스폰 횟수 추적

    private void Start()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnChunkGenerated += HandleChunkGenerated; // 맵 생성 완료 이벤트 구독
        }
        else
        {
            Debug.LogError("[MonsterSpawnManager] MapGenerator가 설정되지 않았습니다.");
        }

        foreach (Biome biome in System.Enum.GetValues(typeof(Biome)))
        {
            biomeSpawnCount[biome] = 0; // 초기화
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

            yield return new WaitForSeconds(1f); // 1초마다 체크
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
                Debug.LogError($"[GetMonsterPrefab] 지원되지 않는 바이옴: {biome}");
                return null;
        }
    }
}