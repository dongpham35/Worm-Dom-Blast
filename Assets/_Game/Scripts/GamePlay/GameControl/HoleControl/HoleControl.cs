using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleControl : MonoBehaviour
{
    [SerializeField] private SpriteRenderer holeSpriteRenderer;
    [SerializeField] private HoleData      holeData;

    private void Awake()
    {
        if(!string.IsNullOrEmpty(holeData.HoleColor))
            SetColor(ColorPaleteData.Instance.ColorPalete[holeData.HoleColor]);        
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        holeSpriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    }
    #endif
    public void SetColor(Color color)
    {
        if(holeSpriteRenderer == null) return;
        holeSpriteRenderer.color = color;
    }

    public string GetHoleColor()
    {
        return holeData.HoleColor;
    }
    
    public Vector2Int GetHoleCellPos()
    {
        return holeData.HoleCellPos;
    }
}
