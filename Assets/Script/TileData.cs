using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public Vector3Int position; // 타일의 위치
    public string tileType;     // 타일의 타입(이름)
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>(); // 타일 데이터 리스트
}