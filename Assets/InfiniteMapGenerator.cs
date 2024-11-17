using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Biome : int
{
    Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX
}

public class InfiniteMapGenerator : MonoBehaviour
{
    [Header("타일맵 관련")]
    [SerializeField] private Tilemap tileMap;

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
    [SerializeField] private float mapScale = 0.01f; // 노이즈 스케일
    [SerializeField] private int chunkSize = 32; // 하나의 청크 크기
    [SerializeField] private int octaves = 3; // 노이즈 옥타브 수
    [SerializeField] private int pointNum = 3; // 랜덤 포인트 개수

    private float seed; // 시드 값
    private Vector3Int previousPlayerChunk; // 이전 플레이어 위치의 청크 좌표
    private Dictionary<Vector3Int, bool> generatedChunks = new Dictionary<Vector3Int, bool>(); // 이미 생성된 청크 캐싱
    // 타일 캐싱을 위한 딕셔너리 추가 (타일 재생성 방지)
    private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>();

   private void Start()
    {
        seed = Random.Range(0f, 10000f); // 시드 값 초기화
        previousPlayerChunk = GetPlayerChunkPosition();
        GenerateChunk(previousPlayerChunk); // 시작할 때 플레이어 주변 청크를 생성
        StartCoroutine(UpdateChunks()); // 플레이어 이동에 따라 청크 업데이트

        // 카메라 확대/축소 조정 (타일맵 해상도에 맞춤)
        Camera.main.orthographicSize = (Screen.height / 2f) / tileMap.cellSize.y;
    }

    // 플레이어의 현재 청크 좌표를 가져오는 함수
    private Vector3Int GetPlayerChunkPosition()
    {
        Vector3 playerPosition = Camera.main.transform.position; // 카메라를 기준으로 플레이어 위치 추적
        return new Vector3Int(Mathf.FloorToInt(playerPosition.x / chunkSize), Mathf.FloorToInt(playerPosition.y / chunkSize), 0);
    }

    // 플레이어 이동에 따라 청크를 업데이트하는 코루틴 함수
    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector3Int currentPlayerChunk = GetPlayerChunkPosition();

            if (currentPlayerChunk != previousPlayerChunk)
            {
                GenerateSurroundingChunks(currentPlayerChunk);
                previousPlayerChunk = currentPlayerChunk;
            }

            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
        }
    }

    // 현재 플레이어 주변 청크들을 생성하는 함수
    private void GenerateSurroundingChunks(Vector3Int centerChunk)
    {
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                Vector3Int chunkPos = new Vector3Int(centerChunk.x + xOffset, centerChunk.y + yOffset, 0);

                if (!generatedChunks.ContainsKey(chunkPos)) // 아직 생성되지 않은 청크만 생성
                {
                    GenerateChunk(chunkPos);
                    generatedChunks[chunkPos] = true;
                }
            }
        }
    }

    // 특정 청크를 생성하는 함수
   private void GenerateChunk(Vector3Int chunkPos)
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

               if (!tileCache.ContainsKey(tilePos)) // 이미 생성된 타일은 다시 생성하지 않음
               {
                   TileBase tile = GetTileByHight(noiseArr[x, y], biomeArr[x, y]);
                   tileMap.SetTile(tilePos, tile);
                   tileCache[tilePos] = tile; // 캐싱하여 재생성 방지
               }
           }
       }
   }

   // 노이즈 배열을 생성하는 함수 (청크별로)
   private float[,] GenerateNoise(Vector3Int chunkPos)
   {
       float[,] noiseArr=new float[chunkSize,chunkSize];
       float min=float.MaxValue,max=float.MinValue;

       for(int x=0;x<chunkSize;x++)
       {
           for(int y=0;y<chunkSize;y++)
           {
               float lacunarity=2.0f;
               float gain=0.5f;

               float amplitude=0.5f;
               float frequency=1f;

               int worldX=chunkPos.x*chunkSize+x;
               int worldY=chunkPos.y*chunkSize+y;

               for(int i=0;i<octaves;i++)
               {
                   noiseArr[x,y]+=amplitude*(Mathf.PerlinNoise(
                       seed+(worldX*mapScale*frequency),
                       seed+(worldY*mapScale*frequency))*2-1);

                   frequency*=lacunarity;
                   amplitude*=gain;
               }

               if(noiseArr[x,y]<min)min=noiseArr[x,y];
               if(noiseArr[x,y]>max)max=noiseArr[x,y];
           }
       }

       for(int x=0;x<chunkSize;x++)
       {
           for(int y=0;y<chunkSize;y++)
           {
               noiseArr[x,y]=Mathf.InverseLerp(min,max,noiseArr[x,y]);
           }
       }

       return noiseArr;
   }

   // 바이옴 배열을 생성하는 함수
   private Biome[,] GenerateBiome(Vector2[] points, Vector2[] biomePoints)
   {
       Biome[,] biomeArr=new Biome[chunkSize,chunkSize];

       for(int x=0;x<chunkSize;x++)
       {
           for(int y=0;y<chunkSize;y++)
           {
               float minDist=float.MaxValue;
               int idx=-1;

               for(int i=0;i<pointNum;i++)
               {
                   float dist=Vector2.Distance(points[i],new Vector2(x,y));

                   if(dist<minDist)
                   {
                       minDist=dist;
                       idx=i;
                   }
               }

               biomeArr[x,y]=(Biome)GetMinIdx(points[idx],biomePoints);
           }
       }

       return biomeArr;
   }

   // 랜덤 위치 배열을 생성하는 함수
   private Vector2[] GenerateRandomPos(int num)
   {
       Vector2[] arr=new Vector2[num];

       for(int i=0;i<num;i++)
       {
           int x=Random.Range(0,chunkSize-1);
           int y=Random.Range(0,chunkSize-1);

           arr[i]=new Vector2(x,y);
       }

       return arr;
   }

   // 가장 가까운 바이옴 인덱스를 계산하는 함수
   private int GetMinIdx(Vector2 point,Vector2[] biomeArr)
   {
       int curIdx=0;
       float min=float.MaxValue;

       for(int i=0;i<biomeArr.Length;i++)
       {
           float value=Vector2.Distance(point,biomeArr[i]);

           if(min>value)
           {
               min=value;
               curIdx=i;
           }
       }

       return curIdx;
   }

   // 타일맵에 타일을 설정하는 함수
   private void SettingTileMap(float[,] noiseArr,Biome[,] biomeArr)
   {
       Vector3Int point=Vector3Int.zero;

       for(int x=0;x<chunkSize;x++)
       {
           for(int y=0;y<chunkSize;y++)
           {
               point.Set(x,y,0);
               tileMap.SetTile(point,GetTileByHight(noiseArr[x,y],biomeArr[x,y]));
           }
       }
   }

   // 높이에 따른 타일을 결정하는 함수
   private TileBase GetTileByHight(float hight,Biome biome)
   {
       if(biome==Biome.Snow)
       {
           switch(hight)
           {
               case<=0.35f:return snow;
               case<=0.45f:return snow2;
               case<=0.6f:return snow;
               default:return snow;
           }
       }
       else if(biome==Biome.Cave)
       {
           switch(hight)
           {
               case<=0.35f:return cave;
               case<=0.45f:return cave2;
               case<=0.6f:return cave;
               default:return cave2;

           }
       }
       else if(biome==Biome.Ocean)
       {
           switch(hight)
           {
               case<=0.35f:return ocean;
               case<=0.45f:return ocean2;
               case<=0.6f:return ocean;
               default:return ocean2;
           }
       }
       else if(biome==Biome.Desert)
       {
           switch(hight)
           {
               case<=0.35f:return desert;
               case<=0.45f:return desert2;
               case<=0.6f:return desert;
               default:return desert2;
           }
       }
       else if(biome==Biome.Forest)
       {
           switch(hight)
           {
               case<=0.35f:return forest;
               case<=0.45f:return forest2;
               case<=0.6f:return forest;
               default:return forest2;
           }
       }
       else if(biome==Biome.Swamp)
       {
           switch(hight)
           {
               case<=0.35f:return swamp;
               case<=0.45f:return swamp2;
               case<=0.6f:return swamp;
               default:return swamp2;
           }
       }
       else if(biome==Biome.Lava)
       {
           switch(hight)
           {
               case<=0.35f:return lava;
               case<=0.45f:return lava2;
               case<=0.6f:return lava;
               default:return lava2;
           }
       }
       else if(biome==Biome.Grassland)
       {
           switch(hight)
           {
               case<=0.35f:return grassland;
               case<=0.45f:return grassland2;
               case<=0.6f:return grassland;
               default:return grassland2;
           }
       }
       
      return grassland; 
   } 
}