using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEventManager
{

    #region ===== GAME PLAY EVENTS =====
    
    public static Action<Vector2Int, MapCellState> OnUpdateCellState;

    #endregion
}
