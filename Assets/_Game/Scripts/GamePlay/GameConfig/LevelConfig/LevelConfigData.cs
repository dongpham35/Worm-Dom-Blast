using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#region ===== LEVEL DATA =====
[Serializable]
public class LevelData
{
    public MatrixCubeTargetData MatrixCubeTarget;
}
#endregion



#region ===== Maxtrix Cube DATA =====
[Serializable]
public class MatrixCubeTargetData
{
    public List<ColumnData> Columns;
}

[Serializable]
public class ColumnData
{
    public List<string> ColorCubes;
    public int TotalRows;
}
#endregion

#region ===== Maxtrix Map DATA =====
[Serializable]
public class MatrixMapData
{
    public Vector2Int           MatrixSize;
    public int [,]              MatrixCellsState;
    public List<CellControl>    MapCells;
}

[Serializable]
public class MapCellData
{
    public Vector2Int   CellPos;
    public MapCellState State;
}

[Serializable]
public enum MapCellState
{
    Empty,
    Blocked,
}
#endregion


#region ===== Hole Data =====

[Serializable]
public class HoleData
{
    public string     HoleColor;
    public HoleType   Type = HoleType.OneUse;
    public Vector2Int HoleCellPos;
}

[Serializable]
public enum HoleType
{
    ReUse,
    OneUse,
}

#endregion