using Nakama;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine.XR;
using static UnityEngine.Rendering.DebugUI;
using Unity.VisualScripting;

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
    public GameObject gameBoard;

    public TextMeshProUGUI player1Name;
    public TextMeshProUGUI player2Name;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI winnerText;

    public TMP_InputField inputField;

    private float spinTime = 5.0f;
    private float clientFinalAngle;
    private float HostFinalAngle;
    private bool  isHost;
    private bool gameIsFinished = false;
    private int turnsPlayed = 0;

    private async void Start()
    {
        gameData = new GameData();

        var mainThread = UnityMainThreadDispatcher.Instance();
        await NakamaConnection.Connect();

        inputField.characterLimit = 12;
        inputField.onValueChanged.AddListener(SavePlayerName);

        NakamaConnection.Socket.ReceivedMatchmakerMatched += m => mainThread.Enqueue(() => OnReceivedMatchmakerMatched(m));
        NakamaConnection.Socket.ReceivedMatchState += m => mainThread.Enqueue(() => OnReceivedMatchState(m));

        gameData.playerShape = 0;
        gameData.playerTurn = true;
    }

    private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matchmakingTicket)
    {
        var match = await NakamaConnection.Socket.JoinMatchAsync(matchmakingTicket);
        NakamaConnection.matchId = match.Id;

        Debug.Log("Our session ID : " + match.Self.SessionId);

        foreach (var user in match.Presences)
        {
            Debug.Log("Connected User session ID : " + user.SessionId);
        }

        DecideHost(match);

        await SendName(player1Name.text);
        ActivateTurnmaking();
    }

    private void DecideHost(IMatch match)
    {
        string myId = match.Self.SessionId;
        string lowestId = myId;

        foreach (var user in match.Presences)
        {
            if (string.Compare(user.SessionId, lowestId, StringComparison.Ordinal) < 0)
            {
                lowestId = user.SessionId;
            }
        }

        isHost = (myId == lowestId);
        Debug.Log(isHost ? "I am the host!" : "I am a client!");
    }

    private async Task SendName(string player1Name)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(player1Name);
        await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 1, bytes, null);
    }

    private void TurnAnnouncer()
    {
        turnText.gameObject.SetActive(true);
    }

    private void OnReceivedMatchState(IMatchState matchState)
    {
        switch (matchState.OpCode)
        {
            case 1:

                player2Name.text = System.Text.Encoding.UTF8.GetString(matchState.State);
                break;

            case 2:

                clientFinalAngle = BitConverter.ToSingle(matchState.State, 0);
                StartSpin(clientFinalAngle);
                Debug.Log(clientFinalAngle);
                break;

            case 3:

                int receivedIndex = BitConverter.ToInt32(matchState.State, 0);
                Debug.Log("Received inex : " + receivedIndex);

                if (gameData.playerShape == 1)
                {
                    gameData.gridState[receivedIndex] = 0;
                    circles[receivedIndex].gameObject.SetActive(true);

                    gameData.playerTurn = true;
                    TurnAnnouncer();
                }
                else if (gameData.playerShape == 0)
                {
                    gameData.gridState[receivedIndex] = 1;
                    crosses[receivedIndex].gameObject.SetActive(true);

                    gameData.playerTurn = true;
                    TurnAnnouncer();
                }
                AnnounceWinner();
                break;
        }
    }

    public void SavePlayerName(string input)
    {
        player1Name.text = input;
    }

    public async void ActivateTurnmaking()
    {
        gameTitle.SetActive(false);
        matchmakingPanel.SetActive(false);

        border.SetActive(true);
        spinner.SetActive(true);

        DisplayNames();

        if (isHost)
        {
            float possibleAngles = UnityEngine.Random.Range(0f, 360f);
            int extraSpins = UnityEngine.Random.Range(3, 6);
            clientFinalAngle = possibleAngles + extraSpins * 360f;

            Debug.Log("client number has been generated which is : " + clientFinalAngle);
            Debug.Log(clientFinalAngle);

            var bytes = BitConverter.GetBytes(clientFinalAngle);
            await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 2, bytes, null);

            HostFinalAngle = clientFinalAngle + 180;
            StartSpin(HostFinalAngle);
        }
    } 

    public void DisplayNames()
    {
        player1Name.gameObject.SetActive(true);
        player2Name.gameObject.SetActive(true);    
    }

    private void AnnounceWinner()
    {
        if (turnsPlayed == 8)
        {
            gameIsFinished = true;
            Debug.Log("game is finished!");

            turnText.gameObject.SetActive(false);
        }
        if (
               (gameData.gridState[0] == 0 && gameData.gridState[1] == 0 && gameData.gridState[2] == 0) ||
               (gameData.gridState[3] == 0 && gameData.gridState[4] == 0 && gameData.gridState[5] == 0) ||
               (gameData.gridState[6] == 0 && gameData.gridState[7] == 0 && gameData.gridState[8] == 0) ||
               (gameData.gridState[0] == 0 && gameData.gridState[3] == 0 && gameData.gridState[6] == 0) ||
               (gameData.gridState[1] == 0 && gameData.gridState[4] == 0 && gameData.gridState[7] == 0) ||
               (gameData.gridState[2] == 0 && gameData.gridState[5] == 0 && gameData.gridState[8] == 0) ||
               (gameData.gridState[0] == 0 && gameData.gridState[4] == 0 && gameData.gridState[8] == 0) ||
               (gameData.gridState[2] == 0 && gameData.gridState[4] == 0 && gameData.gridState[6] == 0) 
           )
        {
            if (gameData.playerShape == 0)
            {
                gameIsFinished = true;
                Debug.Log("game is finished!");
                turnText.gameObject.SetActive(false);
                winnerText.gameObject.SetActive(true);
            }
                        
        }

        if (
               (gameData.gridState[0] == 1 && gameData.gridState[1] == 1 && gameData.gridState[2] == 1) ||
               (gameData.gridState[3] == 1 && gameData.gridState[4] == 1 && gameData.gridState[5] == 1) ||
               (gameData.gridState[6] == 1 && gameData.gridState[7] == 1 && gameData.gridState[8] == 1) ||
               (gameData.gridState[0] == 1 && gameData.gridState[3] == 1 && gameData.gridState[6] == 1) ||
               (gameData.gridState[1] == 1 && gameData.gridState[4] == 1 && gameData.gridState[7] == 1) ||
               (gameData.gridState[2] == 1 && gameData.gridState[5] == 1 && gameData.gridState[8] == 1) ||
               (gameData.gridState[0] == 1 && gameData.gridState[4] == 1 && gameData.gridState[8] == 1) ||
               (gameData.gridState[2] == 1 && gameData.gridState[4] == 1 && gameData.gridState[6] == 1)
           )
        {
            if (gameData.playerShape == 1)
            {
                gameIsFinished = true;
                Debug.Log("game is finished!");
                turnText.gameObject.SetActive(false);
                winnerText.gameObject.SetActive(true);
            }

        }
    }

    public async void MakeMove(int index)
    {
        if (!gameIsFinished && gameData.playerTurn)
        {
            if (gameData.playerShape == 0 && gameData.gridState[index] == -1)
            {
                gameData.gridState[index] = 0;
                circles[index].gameObject.SetActive(true);
                gameData.playerTurn = false;

                byte[] data = BitConverter.GetBytes(index);
                await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 3, data, null);
                turnText.gameObject.SetActive(false);
            }

            else if (gameData.playerShape == 1 && gameData.gridState[index] == -1)
            {
                gameData.gridState[index] = 1;
                crosses[index].gameObject.SetActive(true);
                gameData.playerTurn = false;

                byte[] data = BitConverter.GetBytes(index);
                await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 3, data, null);
                turnText.gameObject.SetActive(false);
            }

            turnsPlayed++;
            AnnounceWinner();
        }
    }

    Coroutine c;
    private void StartSpin(float finalAngle)
    {
        if(c != null)
        {
            StopCoroutine(c);
            c = null;
        }

        c = StartCoroutine(SpinRoutine(finalAngle));
    }

    private IEnumerator SpinRoutine(float finalAngle)
    {
        float elapsed = 0f;
        float startAngle = arrowHolder.eulerAngles.z;

        while (elapsed < spinTime)
        {
            float currentAngle = Mathf.Lerp(startAngle, finalAngle , elapsed / spinTime);
            arrowHolder.rotation = Quaternion.Euler(0, 0, currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        arrowHolder.rotation = Quaternion.Euler(0, 0, finalAngle);

        float z = arrowHolder.eulerAngles.z;

        if (z > 120f && z < 300f)
        {
            Debug.Log("Player 1 starts!");

            gameData.playerShape = 0;
            gameData.playerTurn = true;

            TurnAnnouncer();
        }
        else
        {
            Debug.Log("Player 2 starts!");

            gameData.playerShape = 1;
            gameData.playerTurn = false;
        }

        yield return new WaitForSeconds(1.3f);

        border.SetActive(false);
        spinner.SetActive(false);
        gameBoard.SetActive(true);
    }

    [System.Serializable]
    public class IntArrayWrapper
    {
        public int values;
    }
}
