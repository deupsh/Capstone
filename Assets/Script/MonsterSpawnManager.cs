using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterSpawnManager : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    [SerializeField] private GameObject SnowMonster;
    [SerializeField] private GameObject CaveMonster;
    [SerializeField] private GameObject OceanMonster;
    [SerializeField] private GameObject DesertMonster;
    [SerializeField] private GameObject ForestMonster;
    [SerializeField] private GameObject SwampMonster;
    [SerializeField] private GameObject LavaMonster;
    [SerializeField] private GameObject GrasslandMonster;

    [Header("참조")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조
    [SerializeField] private Tilemap tileMap; // 타일맵 참조

    [Header("스폰 제한 설정")]
    [SerializeField] private int spawnLimitPerBiome = 5; // 바이옴당 최대 몬스터 스폰 수

    private Dictionary<Vector3Int, GameObject> spawnedMonsters = new Dictionary<Vector3Int, GameObject>(); // 타일 위치와 몬스터 매핑
    private Dictionary<Biome, int> biomeSpawnCount = new Dictionary<Biome, int>(); // 바이옴별 스폰 횟수 추적

    private void Start()
    {
        // MapGenerator가 설정되어 있는지 확인
        if (mapGenerator != null)
        {
            mapGenerator.OnChunkGenerated += HandleChunkGenerated; // 맵 생성 완료 이벤트 구독
        }
        else
        {
            Debug.LogError("[MonsterSpawnManager] MapGenerator가 설정되지 않았습니다.");
        }

        // 바이옴별 스폰 카운터 초기화
        foreach (Biome biome in System.Enum.GetValues(typeof(Biome)))
        {
            biomeSpawnCount[biome] = 0; // 초기화
        }
    }

    // 맵 생성 완료 시 호출되는 이벤트 핸들러
    private void HandleChunkGenerated(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        SpawnMonstersForChunk(chunkPos, biomeArr);
    }

    // 특정 타일 위치에 몬스터를 스폰하는 메서드
    public void SpawnMonsterAtTile(Vector3Int tilePos, Biome biome)
    {
        // 이미 해당 타일에 몬스터가 있거나, 바이옴의 스폰 제한을 초과한 경우 건너뜀
        if (spawnedMonsters.ContainsKey(tilePos) || biomeSpawnCount[biome] >= spawnLimitPerBiome)
        {
            return;
        }

        // 바이옴에 해당하는 몬스터 프리팹 가져오기
        GameObject monsterPrefab = GetMonsterPrefab(biome);

        if (monsterPrefab != null)
        {
            // 월드 좌표로 변환
            Vector3 worldPosition = tileMap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0); // 중앙 정렬

            // 몬스터 생성
            GameObject monster = Instantiate(monsterPrefab, worldPosition, Quaternion.identity);

            // Dictionary에 추가 및 카운터 증가
            spawnedMonsters[tilePos] = monster;
            biomeSpawnCount[biome]++;
        }
    }

    // 특정 청크 내 모든 타일에 대해 몬스터를 스폰하는 메서드
    public void SpawnMonstersForChunk(Vector3Int chunkPos, Biome[,] biomeArr)
    {
        for (int x = 0; x < mapGenerator.chunkSize; x++)
        {
            for (int y = 0; y < mapGenerator.chunkSize; y++)
            {
                Vector3Int tilePos = new Vector3Int(chunkPos.x * mapGenerator.chunkSize + x, chunkPos.y * mapGenerator.chunkSize + y, 0);

                // 바이옴 정보 가져오기
                Biome biome = biomeArr[x % mapGenerator.chunkSize, y % mapGenerator.chunkSize];

                SpawnMonsterAtTile(tilePos, biome);
            }
        }
    }

    // 특정 타일 위치의 몬스터를 언로드하는 메서드
    public void UnloadMonsterAtTile(Vector3Int tilePos)
    {
        if (spawnedMonsters.TryGetValue(tilePos, out GameObject monster))
        {
            // 해당 타일의 몬스터 제거 및 카운터 감소
            Destroy(monster);
            spawnedMonsters.Remove(tilePos);

            Biome biome = mapGenerator.GetBiomeAt(tilePos); // 해당 타일의 바이옴 정보 가져오기
            if (biomeSpawnCount.ContainsKey(biome))
            {
                biomeSpawnCount[biome]--;
            }
        }
    }

    // 특정 청크 내 모든 타일에서 몬스터를 언로드하는 메서드
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

    // 주어진 바이옴에 해당하는 몬스터 프리팹을 반환하는 메서드
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