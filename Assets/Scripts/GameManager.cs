using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameData        gameData;

    public List<GameObject> circles;
    public List<GameObject> crosses;

    void Start()
    {
        gameData.playerShape = 2;
        gameData.playerTurn = true;
    }

    public void MakeMove(int index)
    {
        if (gameData.playerTurn)
        {
            if (gameData.playerShape == 2)
            {
                gameData.gridState[index] = 2;
                circles[index].gameObject.SetActive(true);
                gameData.playerShape = 3;
            }

            else if (gameData.playerShape == 3)
            {
                gameData.gridState[index] = 3;
                crosses[index].gameObject.SetActive(true);
                gameData.playerShape = 2;
            }
        }
    }

     
}
