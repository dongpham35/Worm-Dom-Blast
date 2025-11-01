using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#region ===== WORM CONFIG DATA =====
[Serializable]
public class WormConfigData
{
    public string           WormColor;
    public int              BulletCount;
    public List<Vector2Int> WormPath;
}

[Serializable]
public enum WormCellType
{
    Head,
    Body,
}
#endregion
