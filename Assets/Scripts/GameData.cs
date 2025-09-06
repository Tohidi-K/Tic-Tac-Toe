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

    public int[,] winningCombinations = new int[,]
    {
        {0, 1, 2}, // Row 1
        {3, 4, 5}, // Row 2
        {6, 7, 8}, // Row 3
        {0, 3, 6}, // Col 1
        {1, 4, 7}, // Col 2
        {2, 5, 8}, // Col 3
        {0, 4, 8}, // Diagonal 1
        {2, 4, 6}  // Diagonal 2
    };

    public int[] stopAngles = new int[]
    {
        10, 30, 50, 70, 90, 110, 130, 150, 170, 190, 210, 230, 250, 270, 290, 310, 330, 350
    };
}
