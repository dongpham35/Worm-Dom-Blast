using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelControl : MonoSingleton<LevelControl>
{
    [Header("Scale Level Global")]
    public float ScaleLevelGlobal = 1f;
    public MatrixMapControl MatrixMapCtrl;
    
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        MatrixMapCtrl ??= GetComponentInChildren<MatrixMapControl>();
    }
#endif
    
    public void SetCellTargetControl(CellControl cellCtrl)
    {
        MatrixMapCtrl.SetCellTargetControl(cellCtrl);
    }
    
    public void FindHoleAndGetPath(WormControl wormCtrl, string wormColor)
    {
        MatrixMapCtrl.FindHoleAndGetPath(wormCtrl, wormColor);
    }
}
