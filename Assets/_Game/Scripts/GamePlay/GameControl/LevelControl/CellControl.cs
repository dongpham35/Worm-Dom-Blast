using System;
using System.Collections;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

public class CellControl : MonoBehaviour
{
    [SerializeField] private float          CellScale = 1f;
    [SerializeField] private MapCellData    CellData;
    [SerializeField] private SpriteRenderer cellSpriteRenderer;
    
    private Transform _cellTrans;
    
    private void Awake()
    {
        _cellTrans                 =  transform;
        Interacable.Instance.OnTap += ClickCell;
    }

    private void OnDestroy()
    {
        Interacable.Instance.OnTap -= ClickCell;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cellSpriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    }
    #endif


    #region ===== CELL MAIN FUNCTIONS =====


    #region ===== CELL SET AND GET =====

    public void SetCellColor(Color cellColor, bool resetColor = false)
    {
        if (resetColor)
        {
            cellSpriteRenderer.color = Color.white;
            return;
        }
        cellSpriteRenderer.color = cellColor;
    }

    public void SetCellData(MapCellData cellData)
    {
        CellData = cellData;
    }
    public Vector3 GetCellPosition()
    {
        return _cellTrans.position;
    }
    
    
    public MapCellData GetCellData()
    {
        return CellData;
    }
    
    #endregion
    
    public void ChangeStateCell(MapCellState newState)
    {
        CellData.State = newState;
    }
    
    #endregion


    #region ===== CELL EDITOR FUNCTIONS =====
    #if UNITY_EDITOR
    public void SetSpriteRenderer(Sprite cellSprite)
    {
        if (cellSpriteRenderer != null)
        {
            cellSpriteRenderer.sprite = cellSprite;
        }
    }
    #endif
    #endregion


    #region ===== TEST FUNCTIONS =====
    
    private void ClickCell(Vector2 tapPosition)
    {
        Vector3 worldTapPos = Camera.main.ScreenToWorldPoint(new Vector3(tapPosition.x, tapPosition.y, Camera.main.transform.position.y));
        Vector3 cellPos     = GetCellPosition();
        float   distance    = Vector3.Distance(new Vector3(worldTapPos.x, 0, worldTapPos.z), new Vector3(cellPos.x, 0, cellPos.z));
        if (distance <= CellScale / 2f)
        {
            LevelControl.Instance.MatrixMapCtrl.SetCellTargetControl(this);
        }
    }

    #endregion
}


[Serializable]
public enum CellPath
{
    none,
    success,
    fail,
}