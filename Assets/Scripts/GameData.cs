using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public string playerName;
    public bool playerTurn;
    public int playerShape; // 0 for circle, 1 for cross 

    public int[] gridState = new int[]
    {
        -1, -1, -1,
        -1, -1, -1,
        -1, -1, -1,
    };
}
