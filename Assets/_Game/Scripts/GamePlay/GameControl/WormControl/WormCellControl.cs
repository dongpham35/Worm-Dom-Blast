using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WormCellControl : MonoBehaviour
{
    public WormCellType CellType;
    [SerializeField] private Transform    wormCellPreviousTrans;
    [SerializeField] private MeshRenderer wormCellMeshRenderer;
    [SerializeField] private SphereCollider wormCellCollider;

    private MaterialPropertyBlock _materialPropertyBlock;
    
    #if UNITY_EDITOR
    public void SetPreviuosTrans(Transform previousTrans)
    {
        wormCellPreviousTrans = previousTrans;
    }

    private void OnValidate()
    {
        wormCellMeshRenderer ??= GetComponentInChildren<MeshRenderer>();
        wormCellCollider    ??= GetComponent<SphereCollider>();
    }
#endif
    

    public void SetWormCellColor(Color color)
    {
        if (wormCellMeshRenderer == null) return;
        _materialPropertyBlock = new MaterialPropertyBlock();
        if(wormCellMeshRenderer != null)
            wormCellMeshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetColor("_Color", color);
        wormCellMeshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }
}
