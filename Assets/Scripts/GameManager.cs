using Nakama;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public NakamaConnection NakamaConnection;
    private UnityMainThreadDispatcher mainThread;

    private GameData gameData;

    public List<GameObject> circles;
    public List<GameObject> crosses;

    public RectTransform arrowHolder;

    public GameObject matchmakingPanel;
    public GameObject gameTitle;
    public GameObject border;
    public GameObject spinner;
    public TextMeshProUGUI player1Name;
    public TextMeshProUGUI player2Name;

    public TMP_InputField inputField;

    private float spinTime = 5.0f;

    private async void Start()
    {
        gameData = new GameData();

        var mainThread = UnityMainThreadDispatcher.Instance();
        await NakamaConnection.Connect();

        inputField.characterLimit = 12;
        inputField.onValueChanged.AddListener(SavePlayerName);

        NakamaConnection.Socket.ReceivedMatchmakerMatched += m => mainThread.Enqueue(() => OnReceivedMatchmakerMatched(m));

        gameData.playerShape = 0;
        gameData.playerTurn = true;
    }

    private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matchmakingTicket)
    {
        var match = await NakamaConnection.Socket.JoinMatchAsync(matchmakingTicket);

        Debug.Log("Our session ID : " + match.Self.SessionId);

        foreach (var user in match.Presences)
        {
            Debug.Log("Connected User session ID : " + user.SessionId);
        }

        ActivateTurnmaking();
    }

    public void SavePlayerName(string input)
    {
        player1Name.text = input;
    }

    public void ActivateTurnmaking()
    {
        try
        {
            gameTitle.SetActive(false);
            matchmakingPanel.SetActive(false);

            border.SetActive(true);
            spinner.SetActive(true);

            DisplayNames();
            StartSpin();
        }

        catch (Exception e)
        {
            Debug.LogError("Error in ActivateTurnmaking: " + e);
        }
    }

    public void DisplayNames()
    {
        player1Name.gameObject.SetActive(true);
        player2Name.gameObject.SetActive(true);    
    }

    public void MakeMove(int index)
    {
        if (gameData.playerTurn)
        {
            if (gameData.playerShape == 0)
            {
                gameData.gridState[index] = 0;
                circles[index].gameObject.SetActive(true);
                gameData.playerShape = 1;
            }

            else if (gameData.playerShape == 1)
            {
                gameData.gridState[index] = 1;
                crosses[index].gameObject.SetActive(true);
                gameData.playerShape = 0;
            }
        }
    }

    private void StartSpin()
    {
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        float elapsed = 0f;

        float possibleAngles = UnityEngine.Random.Range(0f, 360f);
        int extraSpins = UnityEngine.Random.Range(3, 6);
        float finalAngle = possibleAngles + extraSpins * 360f;

        float startAngle = arrowHolder.eulerAngles.z;

        while (elapsed < spinTime)
        {
            float currentAngle = Mathf.Lerp(startAngle, finalAngle, elapsed / spinTime);
            arrowHolder.rotation = Quaternion.Euler(0, 0, currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        arrowHolder.rotation = Quaternion.Euler(0, 0, finalAngle);

        float z = arrowHolder.eulerAngles.z;

        if (z > 120f && z < 300f)
        {
            Debug.Log("Player 1 starts!");
        }
        else
        {
            Debug.Log("Player 2 starts!");
        }

        yield return new WaitForSeconds(1.3f);

        border.SetActive(false);
        spinner.SetActive(false);
    }
}
