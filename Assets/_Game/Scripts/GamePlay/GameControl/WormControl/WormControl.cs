using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WormControl : MonoBehaviour
{
    [SerializeField] private WormConfigData WormData;
    [SerializeField] private List<WormCellControl> WormCells;

    private void Awake()
    {
        if(!string.IsNullOrEmpty(WormData.WormColor))
            SetColorWormCells(ColorPaleteData.Instance.ColorPalete[WormData.WormColor]);
    }

#if UNITY_EDITOR
    [SerializeField] private GameObject headCell;
    [SerializeField] private GameObject bodyCell;
    [SerializeField] private GameObject tailCell;
    private void OnValidate()
    {
        WormCells = new List<WormCellControl>(GetComponentsInChildren<WormCellControl>());
        for(int i = 0; i < WormCells.Count; i++)
        {
            if (WormCells[i].CellType == WormCellType.Head)
                WormCells[i].SetPreviuosTrans(transform);
            else WormCells[i].SetPreviuosTrans(WormCells[i - 1].transform);
        }
    }
#endif


    #region ===== WORM FUNCTIONS =====


    #region ===== GETTER || SETTER FUNCTIONS =====

    public Vector2Int GetWormHeadCellPos()
    {
        return WormData.WormPath[^1];
    }
    
    public WormConfigData GetWormData()
    {
        return WormData;
    }

    public void ChoseWorm()
    {
        LevelControl.Instance.FindHoleAndGetPath(this, WormData.WormColor);
    }
    #endregion

    public void SetColorWormCells(Color color)
    {
        foreach (var wormCell in WormCells)
        {
            wormCell.SetWormCellColor(color);
        }
    }


    #region ===== MOVE ALONG PATH =====
    private List<CellControl> _currentPathCells;

    public void MoveAlongPath(List<CellControl> pathCells)
    {
        MoveWormCellToTarget(pathCells).Forget();
    }
    
    private async UniTaskVoid MoveWormCellToTarget(List<CellControl> pathCells)
    {
        _currentPathCells = pathCells;
        await MoveInPathState();
        await MoveToHoleState();
        ResetColorCell();
    }

    private async UniTask MoveInPathState()
    {
        var targetPosistions = new List<Vector3>(WormCells.Count);
        for(int i = 0; i < WormCells.Count; i++)
        {
            targetPosistions.Add(i == 0
                    ? _currentPathCells[0].transform.position
                    : WormCells[i - 1].transform.position
                );
        }
        
        var lastCellPos = _currentPathCells[^1].transform.position;
        var cellTargetIndex = 0;
        while(Vector3.Distance(lastCellPos, WormCells[0].transform.position) > 0.1f)
        {
            for(int i = 0; i < WormCells.Count  ; i++)
            {
                var wormCell = WormCells[i];
                if(!UnityNull.IsAlive(wormCell)) continue;
                var targetPos = targetPosistions[i];
                var dir = GetDirection2Demension(wormCell.transform.position, targetPos);
                wormCell.transform.position += dir * (1f * Time.deltaTime);
                if (IsNearTarget_2Demension(wormCell.transform.position, targetPos, 0.01f))
                {
                    if (i == 0)
                    {
                        cellTargetIndex ++;
                        if (cellTargetIndex < _currentPathCells.Count)
                        {
                            targetPosistions[i] = _currentPathCells[cellTargetIndex].transform.position;
                        }
                    }
                    else
                    {
                        targetPosistions[i] = WormCells[i - 1].transform.position;
                    }
                }
            }
            await UniTask.NextFrame();
        }
    }

    private async  UniTask MoveToHoleState()
    {
        var isSuccess = false;
        
        List<Vector3> targetPositions = new List<Vector3>();
        targetPositions.Add(_currentPathCells[^1].transform.position);
        targetPositions.Add(_currentPathCells[^1].transform.position + Vector3.down * 10f);
        
        while (!isSuccess)
        {
            for(int i = 0; i < WormCells.Count  ; i++)
            {
                var wormCell = WormCells[i];
                if(!UnityNull.IsAlive(wormCell)) continue;
               if(IsNearTarget_2Demension(wormCell.transform.position, targetPositions[0]))
               {
                   var dir = (targetPositions[1] - wormCell.transform.position).normalized;
                   wormCell.transform.position += dir * (1f * Time.deltaTime);
               }else
               {
                   var dir = (targetPositions[0] - wormCell.transform.position).normalized;
                   wormCell.transform.position += dir * (1f * Time.deltaTime);
               }
            }
            await UniTask.NextFrame();
            if(WormCells[^1].transform.position.y < -0.5f)
                isSuccess = true;
        }
    }
    #endregion
    #endregion
    

    #region ===== HELPER FUNCTIONS =====
    private void UpdateCellPath(Vector2Int newCellPos)
    {
        for (int index = 0; index < WormData.WormPath.Count-1; index++)
        {
            GameEventManager.OnUpdateCellState?.Invoke(WormData.WormPath[index], MapCellState.Empty);
            WormData.WormPath[index] = WormData.WormPath[index + 1];
        }
        GameEventManager.OnUpdateCellState?.Invoke(WormData.WormPath[^1], MapCellState.Empty);
        WormData.WormPath[^1] = newCellPos;

        foreach (Vector2Int wormCell in WormData.WormPath)
        {
            GameEventManager.OnUpdateCellState?.Invoke(wormCell, MapCellState.Blocked);
        }
    }
    private void ResetColorCell()
    {
        foreach (var cell in _currentPathCells)
        {
            cell.SetCellColor(Color.white, true);
        }
    }
    
    private bool IsNearTarget_2Demension(Vector3 targetPos, Vector3 currentPos, float threshold = 0.1f)
    {
        Vector2 targetPos2D  = new Vector2(targetPos.x, targetPos.z);
        Vector2 currentPos2D = new Vector2(currentPos.x, currentPos.z);
        float   distance     = Vector2.Distance(targetPos2D, currentPos2D);
        return distance <= threshold;
    }
    
    private Vector3 GetDirection2Demension(Vector3 fromPos, Vector3 toPos)
    {
        var dir = toPos - fromPos;
        if(Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
        {
            return new Vector3(Mathf.Sign(dir.x), 0, 0);
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(dir.z));
        }
    }
    #endregion
}
