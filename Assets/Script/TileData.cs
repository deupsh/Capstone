using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public Vector3Int position; // Ÿ���� ��ġ
    public string tileType;     // Ÿ���� Ÿ��(�̸�)
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>(); // Ÿ�� ������ ����Ʈ
}