using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using Alchemy.Inspector;
#endif
using UnityEditor;
using UnityEngine;

public class MatrixMapControl : MonoBehaviour
{
    [SerializeField] private MatrixMapData     _matrixMapData;
    [SerializeField] private List<WormControl> _wormsInMap;
    [SerializeField] private List<HoleControl> _holesInMap;

    private CellControl _cellTargetControl;
    private bool       _isMoveToTarget;
    #region ===== UNITY METHODS =====

    private void Awake()
    {
        GameEventManager.OnUpdateCellState += ChangeCellState;
        InitMatrixMap();
    }

    private void OnDestroy()
    {
        GameEventManager.OnUpdateCellState -= ChangeCellState;
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if (_cellTargetControl != null && _isMoveToTarget)
        {
            MoveToTarget(_cellTargetControl);
            _isMoveToTarget = false;
        }
        #endif
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        _holesInMap = new List<HoleControl>(GetComponentsInChildren<HoleControl>());
    }
#endif

    #endregion

    #region MATRIX MAP FUNCTIONS


    #region ===== INITIAL FUNCTIONS =====
    private void InitMatrixMap()
    {
        _matrixMapData.MatrixCellsState = new int[_matrixMapData.MatrixSize.x, _matrixMapData.MatrixSize.y];
        for (int j = 0; j < _matrixMapData.MatrixSize.y; j++)
        {
            for (int i = 0; i < _matrixMapData.MatrixSize.x; i++)
            {
                var cell = _matrixMapData
                           .MapCells[j * _matrixMapData.MatrixSize.x + i];
                if (cell == null)
                {
                    Debug.Log("<Color=red> Matrix Map Control: Cell is null at pos: " + i + "," + j + "</Color>");
                    continue;
                }
                if(cell.GetCellData().State == MapCellState.Blocked)
                    _matrixMapData.MatrixCellsState[i,j] = 1;
                else
                    _matrixMapData.MatrixCellsState[i,j] = 0;
            }
        }
        GetCellPosOfWorms();
    }

    private void GetCellPosOfWorms()
    {
        foreach (var worm in _wormsInMap)
        {
            if(worm.GetWormData().WormPath.Count == 0) continue;
            foreach (var pos in worm.GetWormData().WormPath)
            {
                _matrixMapData.MatrixCellsState[pos.x, pos.y] = 1;
            }
        }
    }
    #endregion

#if UNITY_EDITOR
    [Button]
#endif
    public void MoveToTarget(CellControl target)
    {
        var wormPos = _wormsInMap[0].GetWormHeadCellPos();
        var targetPos = target.GetCellData().CellPos;
        var pathResult      = ShortestPathFinder.FindShortestPath(_matrixMapData.MatrixCellsState, wormPos, targetPos);
        ChangeColorCell(pathResult);
        if (pathResult.State == PathState.PathFailed)
            return;
        var pathCell  = GetPathCells(pathResult.Path);
        _wormsInMap[0]
           .MoveAlongPath(pathCell);
    }
    
    private void ChangeCellState(Vector2Int cellPos, MapCellState newState)
    {
        var cell = _matrixMapData.MapCells[cellPos.y * _matrixMapData.MatrixSize.x + cellPos.x];
        _matrixMapData.MatrixCellsState[cellPos.x, cellPos.y] = newState == MapCellState.Blocked ? 1 : 0;
        cell.ChangeStateCell(newState);
    }
    
    public void FindHoleAndGetPath(WormControl worm, string colorName)
    {
        var wormPos = worm.GetWormHeadCellPos();
        var targetCells = GetCellsHasSameColor(colorName);
        if (targetCells == null || targetCells.Count == 0) return;
        List<PathResult> pathResults = new List<PathResult>();
        foreach (var targetCell in targetCells)
        {
            var targetPos = targetCell.GetCellData().CellPos;
            var pathResult      = ShortestPathFinder.FindShortestPath(_matrixMapData.MatrixCellsState, wormPos, targetPos);
            pathResults.Add(pathResult);
        }
        //Chọn đường đi ngắn nhất
        int pathCount = int.MaxValue;
        PathResult bestPathResult = new ()
                                    {
                                        Path = null,
                                        State = PathState.PathFailed
                                    };
        foreach (var pathResult in pathResults)
        {
            if (pathResult.Path.Count < pathCount)
            {
                bestPathResult = pathResult;
            }
        }
        ChangeColorCell(bestPathResult);
        if (bestPathResult.State == PathState.PathFailed)
            return;
        var pathCells = GetPathCells(bestPathResult.Path);
        worm.MoveAlongPath(pathCells);
    }
    
    public void SetCellTargetControl(CellControl cellControl)
    {
        if (_cellTargetControl != null)
        {
            if(_cellTargetControl == cellControl)
                return;
        }
        _cellTargetControl = cellControl;
        _isMoveToTarget    = true;
    }

    #region ===== HELPER FUNCTIONS =====
    private List<CellControl> GetPathCells(List<Vector2Int> path)
    {
        List<CellControl> pathCells = new List<CellControl>();
        foreach (var pos in path)
        {
            var cell = _matrixMapData.MapCells[pos.y * _matrixMapData.MatrixSize.x + pos.x];
            pathCells.Add(cell);
        }
        return pathCells;
    }

    private List<CellControl> GetCellsHasSameColor(string colorName)
    {
        if (string.IsNullOrEmpty(colorName)) return null;
        var reultCells = new List<CellControl>();
        foreach (var hole in _holesInMap)
        {
            if (hole
               .GetHoleColor()
               .Equals(colorName))
            {
                var cellPos = hole.GetHoleCellPos();
                var cell    = _matrixMapData.MapCells[cellPos.y * _matrixMapData.MatrixSize.x + cellPos.x];  
                reultCells.Add(cell);
            }
        }
        return reultCells;
    }

    private void ChangeColorCell(PathResult pathResult)
    {
        Color cellColor = pathResult.State == PathState.PathSuccess ? ColorPaleteData.Instance.ColorPalete["PathSuccess"] : ColorPaleteData.Instance.ColorPalete["PathFaild"];
        foreach (var pos in pathResult.Path)
        {
            var cell = _matrixMapData.MapCells[pos.y * _matrixMapData.MatrixSize.x + pos.x];
            cell.SetCellColor(cellColor);
        }
        if(pathResult.State == PathState.PathFailed)
            ChangeCellColorToDefault(pathResult.Path).Forget();
    }
    
    private async UniTaskVoid ChangeCellColorToDefault(List<Vector2Int> pathCells)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        foreach (var cellPos in pathCells)
        {
            var cell = _matrixMapData.MapCells[cellPos.y * _matrixMapData.MatrixSize.x + cellPos.x];
            cell.SetCellColor(Color.white, true);
        }
    }
    #endregion
    #endregion
    

    #region ===== MATRIX MAP EDITOR =====

#if UNITY_EDITOR

    [Button]
    public void InitMatrixMapData(Vector2 mapSize, Vector2Int matrixSize)
    {
        _matrixMapData = new MatrixMapData
                         {
                             MatrixSize = matrixSize,
                             MapCells   = new List<CellControl>(),
                         };
        InstanceCellAndCellBG(mapSize);
    }

    [Button]
    public void DestroyMatrixMapData()
    {
        _matrixMapData.MapCells.Clear();
        var children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        foreach (var child in children)
        {
            DestroyImmediate(child);
        }
    }


    #region ===== HELPER FUNCTIONS =====

    private void InstanceCellAndCellBG(Vector2 mapSize)
    {
        GameObject cell       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Scripts/GamePlay/Test/Cell.prefab");
        GameObject cellBG     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Scripts/GamePlay/Test/CellBG.prefab");
        var        matrixSize = _matrixMapData.MatrixSize;
        var        cellWidth  = mapSize.x / matrixSize.x;
        var        cellHeight = mapSize.y / matrixSize.y;
        for (int row = 0; row < matrixSize.y; row++)
        {
            for (int col = 0; col < matrixSize.x; col++)
            {
                var cellBGObj = Instantiate(cellBG, transform);
                cellBGObj.transform.localPosition = new Vector3(
                        -mapSize.x / 2 + cellWidth  / 2 + col * cellWidth,
                        0f,
                        -mapSize.y / 2 + cellHeight / 2 + row * cellHeight
                    );
                cellBGObj.transform.localScale    = new Vector3(cellWidth, cellHeight, 1f);
                var cellObj  = Instantiate(cell, cellBGObj.transform);
                cellObj.transform.localPosition = Vector3.zero + Vector3.forward * -0.005f;
                cellObj.transform.localScale    = Vector3.one * 0.99f;
                var cellData = new MapCellData
                               {
                                   CellPos = new Vector2Int(col, row),
                                   State  = MapCellState.Empty
                               };
                cellObj
                   .GetComponent<CellControl>()
                   .SetCellData(cellData);
                _matrixMapData.MapCells.Add(cellObj.GetComponent<CellControl>());
            }
        }
    }

    #endregion
#endif

    #endregion
}
