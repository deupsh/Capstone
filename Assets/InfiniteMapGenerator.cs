using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// �� ���ڰ� ������ �ǹ�
public enum Biome : int {
    Snow = 0, Cave = 1, Ocean = 2, Desert = 3, Forest = 4, Swamp = 5, Lava = 6, Grassland = 7, MAX          
}

public class ChunkLoader : MonoBehaviour {
    
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

    [Space]
    [Header("�� ����")]
    [SerializeField] private float mapScale = 0.01f; // ������ : �ڿ������� ������ �� ����
    [SerializeField] private int chunkSize = 16; // ûũ ũ��
    [SerializeField] private int worldSizeInChunks = 10; // ���� ���� ���� ũ��

    [SerializeField] private int octaves = 3; // ��Ÿ�� : ������ ������ ���⵵
    [SerializeField] private int pointNum = 2; // ���̿� ũ��� �ݺ�� (Ŭ���� ���̿� ũ�Ⱑ �۾���)
    private float seed; // �õ� ��

   // ûũ ĳ���� ���� ��ųʸ� (�ε�� ûũ ����)
   private Dictionary<Vector3Int, bool> loadedChunks = new Dictionary<Vector3Int, bool>();
   // Ÿ�� ĳ���� ���� ��ųʸ� (Ÿ�� ����� ����)
   private Dictionary<Vector3Int, TileBase> tileCache = new Dictionary<Vector3Int, TileBase>();

   void Start()
   {
       seed = Random.Range(0f, 10000f); // �õ� �� �ʱ�ȭ
       GenerateFullMap(); // ��ü ���� �� ���� ����
       StartCoroutine(UpdateChunks()); // �÷��̾� �̵��� ���� ûũ ������Ʈ
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
           0
       );
   }

   // �÷��̾� �̵��� ���� ûũ�� ������Ʈ�ϴ� �Լ�
   private IEnumerator UpdateChunks()
   {
       while (true)
       {
           Vector3Int currentPlayerChunk = GetPlayerChunkPosition();
           LoadSurroundingChunks(currentPlayerChunk); // ���ο� �ֺ� ûũ �ε�
           UnloadDistantChunks(currentPlayerChunk); // �־��� ûũ ��ε�

           yield return new WaitForSeconds(0.5f); // 0.5�ʸ��� üũ
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
                   LoadChunk(chunkPos); // �ֺ� ûũ�� �ε���� �ʾ����� �ε�
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
           if (Vector3Int.Distance(chunk, centerChunk) > 2) // ���� �Ÿ� �̻� ������ ûũ�� ��ε�
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

   // Ư�� ûũ�� �ε��ϴ� �Լ� (Ÿ�� ĳ�� ����)
   private void LoadChunk(Vector3Int chunkPos) {
       if (!loadedChunks.ContainsKey(chunkPos)) {         // �̹� �ε�� ûũ�� �ƴϸ� ó�� ����

           float[,] noiseArr = GenerateNoise(chunkPos);   // ������ �迭 ���� (���� ���� ����)
           Vector2[] randomPoints = GenerateRandomPos(pointNum);   // ���� ����Ʈ ���� (���̿� ������)
           Vector2[] biomePoints = GenerateRandomPos((int)Biome.MAX);   // ���̿� ����Ʈ ����

           Biome[,] biomeArr = GenerateBiome(randomPoints, biomePoints);   // ���̿� �迭 ����

           for (int x=0;x<chunkSize;x++) {               // �� Ÿ�� ��ġ�� ���� ó�� ���� 
               for (int y=0;y<chunkSize;y++) {
                   Vector3Int tilePos=new Vector3Int(chunkPos.x*chunkSize+x,chunkPos.y*chunkSize+y,0);
                   
                   if(!tileCache.ContainsKey(tilePos)) {   // �ش� ��ġ�� Ÿ���� ������ ���� ����
                       TileBase tile=GetTileByHight(noiseArr[x,y],biomeArr[x,y]);   // ���̿� ���̿ȿ� ���� ������ Ÿ�� ����
                       tileMap.SetTile(tilePos,tile);      // Ÿ�ϸʿ� Ÿ�� ����
                       tileCache[tilePos]=tile;            // ĳ�ÿ� �����Ͽ� ����� ����
                   }
               }
           }
       }
   }

   // Ư�� ûũ�� ��ε��ϴ� �Լ� (Ÿ�ϸʿ��� ����)
   private void UnloadChunk(Vector3Int chunkPos)
   {
       for (int x=0;x<chunkSize;x++)
       {
           for (int y=0;y<chunkSize;y++)
           {
               Vector3Int tilePos=new Vector3Int(chunkPos.x*chunkSize+x,chunkPos.y*chunkSize+y,0);

               if(tileCache.ContainsKey(tilePos))
               {
                   tileMap.SetTile(tilePos,null); // Ÿ�ϸʿ��� ����
                   tileCache.Remove(tilePos); 
               }
           }
       }
   }

   // ������ �迭�� �����ϴ� �Լ� (ûũ����)
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

   // ���̿� �迭�� �����ϴ� �Լ�
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

   // ���� ��ġ �迭�� �����ϴ� �Լ�
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

   // ���� ����� ���̿� �ε����� ����ϴ� �Լ�
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

   // Ÿ�ϸʿ� Ÿ���� �����ϴ� �Լ�
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

   // ���̿� ���� Ÿ���� �����ϴ� �Լ�
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