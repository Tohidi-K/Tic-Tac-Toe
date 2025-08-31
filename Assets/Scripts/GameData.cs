using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public string playerName;
    public bool playerTurn;
    public int playerShape; // 2 for circle, 3 for cross 

    public int[] gridState = new int[]
    {
        1, 1, 1,
        1, 1, 1,
        1, 1, 1,
    };
}
