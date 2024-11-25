using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>();
}

[System.Serializable]
public class TileData
{
    public Vector3Int position; // 타일 위치
    public string tileType;     // 타일 타입(이름)
}