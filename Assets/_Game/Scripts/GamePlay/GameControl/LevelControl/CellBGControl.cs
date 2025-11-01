using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellBGControl : MonoBehaviour
{
    [SerializeField] private SpriteRenderer cellBgSpriteRenderer;


#if UNITY_EDITOR
    private void OnValidate()
    {
        cellBgSpriteRenderer ??= GetComponent<SpriteRenderer>();
    }
#endif

    public void SetSpriteRenderer(Sprite sprite)
    {
        if (cellBgSpriteRenderer)
        {
            cellBgSpriteRenderer.sprite = sprite;
        }
    }
}
