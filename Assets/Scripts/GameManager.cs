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
using Satori;

public class GameManager : MonoBehaviour
{
    public NakamaConnection NakamaConnection;
    private UnityMainThreadDispatcher mainThread;

    private GameData gameData;

    public List<GameObject> circles;
    public List<GameObject> crosses;

    //[SerializeField] private LineRenderer winLine;
    //[SerializeField] private Transform[] cellPositions;

    public RectTransform arrowHolder;

    public GameObject matchmakingPanel;
    public GameObject gameTitle;
    public GameObject border;
    public GameObject spinner;
    public GameObject gameBoard;

    public GameObject player1Timer;
    public GameObject player2Timer;

    public Image fillImage1;
    public Image fillImage2;

    public TextMeshProUGUI player1Name;
    public TextMeshProUGUI player2Name;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI winnerText;

    public TMP_InputField  inputField;

    public  float timerDuration = 10f;
    private float spinTime = 5.0f;
    private float clientFinalAngle;
    private float HostFinalAngle;
    private bool  isHost;
    private bool  gameIsFinished = false;
    private int   turnsPlayed = 0;

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
            //receive other player's name
            case 1:

                player2Name.text = System.Text.Encoding.UTF8.GetString(matchState.State);
                break;
            
            //receive spin data from host
            case 2:

                clientFinalAngle = BitConverter.ToSingle(matchState.State, 0);
                StartSpin(clientFinalAngle);
                Debug.Log(clientFinalAngle);
                break;
            
            //receive other player move
            case 3:

                int receivedIndex = BitConverter.ToInt32(matchState.State, 0);
                Debug.Log("Received inex : " + receivedIndex);

                if (gameData.playerShape == 1)
                {
                    gameData.gridState[receivedIndex] = 0;
                    circles[receivedIndex].gameObject.SetActive(true);

                    //gameData.playerTurn = true;
                    //TurnAnnouncer();
                }
                else if (gameData.playerShape == 0)
                {
                    gameData.gridState[receivedIndex] = 1;
                    crosses[receivedIndex].gameObject.SetActive(true);

                    //gameData.playerTurn = true;
                    //TurnAnnouncer();
                }

                gameData.playerTurn = true;
                TurnAnnouncer();
                StartTimer(gameData.playerShape);

                break;

            //receive endgame notice
            case 4:
                gameIsFinished = true;
                turnText.gameObject.SetActive(false);
                Debug.Log("game is finished!");

                player1Timer.SetActive(false);
                player2Timer.SetActive(false);

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

    private int GetWinningCombo(int playerShape)
    {
        for (int i = 0; i < gameData.winningCombinations.GetLength(0); i++)
        {
            int a = gameData.winningCombinations[i, 0];
            int b = gameData.winningCombinations[i, 1];
            int c = gameData.winningCombinations[i, 2];

            if (gameData.gridState[a] == playerShape &&
                gameData.gridState[b] == playerShape &&
                gameData.gridState[c] == playerShape)
            {
                return i;
            }
        }
        return -1;
    }

    /*private void ShowWinningLine(int comboIndex)
    {
        int a = gameData.winningCombinations[comboIndex, 0];
        int c = gameData.winningCombinations[comboIndex, 2];

        winLine.positionCount = 2;
        winLine.SetPosition(0, cellPositions[a].position);
        winLine.SetPosition(1, cellPositions[c].position);
        winLine.enabled = true;
    }*/

    private async void AnnounceWinner()
    {
        if (turnsPlayed == 8)
        {
            gameIsFinished = true;
            turnText.gameObject.SetActive(false);
            Debug.Log("game is finished!");

            player1Timer.SetActive(false);
            player2Timer.SetActive(false);
        }

        int winningCombo = GetWinningCombo(gameData.playerShape);
        if (winningCombo != -1)
        {
            gameIsFinished = true;
            turnText.gameObject.SetActive(false);
            winnerText.gameObject.SetActive(true);
            //ShowWinningLine(winningCombo);
            Debug.Log("game is finished!");

            await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 4, "", null);

            player1Timer.SetActive(false);
            player2Timer.SetActive(false);
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
                //gameData.playerTurn = false;

                //byte[] data = BitConverter.GetBytes(index);
                //await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 3, data, null);
                //turnText.gameObject.SetActive(false);
            }

            else if (gameData.playerShape == 1 && gameData.gridState[index] == -1)
            {
                gameData.gridState[index] = 1;
                crosses[index].gameObject.SetActive(true);
                //gameData.playerTurn = false;

                //byte[] data = BitConverter.GetBytes(index);
                //await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 3, data, null);
                //turnText.gameObject.SetActive(false);
            }

            gameData.playerTurn = false;
            byte[] data = BitConverter.GetBytes(index);
            await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 3, data, null);
            turnText.gameObject.SetActive(false);

            turnsPlayed++;
            AnnounceWinner();
            StartTimer(gameData.playerShape);
        }
    }

    Coroutine d;
    private void StartTimer(int playerShape)
    {
        if (d != null)
        {
            StopCoroutine(d);
            d = null;
        }

        if (!gameIsFinished)
        {
            d = StartCoroutine(TimerRoutine(playerShape));
        }
    }

    private IEnumerator TimerRoutine(int playershape)
    {
        float elapsed = 0f;

        if (gameData.playerTurn)
        {
            if (playershape == 0)
            {
                player1Timer.SetActive(true);
            }
            else if (playershape == 1)
            {
                player2Timer.SetActive(true);
            }
        }
        else if (!gameData.playerTurn)
        {
            if (playershape == 0)
            {
                player2Timer.SetActive(true);
            }
            else if (playershape == 1)
            {
                player1Timer.SetActive(true);
            }
        }
        
        while (elapsed < timerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (elapsed / timerDuration));

            if (gameData.playerTurn)
            {
                if (playershape == 0)
                {
                    fillImage1.fillAmount = t;
                }
                else if (playershape == 1)
                {
                    fillImage2.fillAmount = t;
                }
            }
            else if (!gameData.playerTurn)
            {
                if (playershape == 0)
                {
                    fillImage2.fillAmount = t;
                }
                else if (playershape == 1)
                {
                    fillImage1.fillAmount = t;
                }
            }

            yield return null;
        }

        if (gameData.playerTurn)
        {
            if (playershape == 0 && fillImage1 != null)
            {
                fillImage1.fillAmount = 0f;
                player1Timer.SetActive(false);
                fillImage1.fillAmount = 1f;
            }
            else if (playershape == 1 && fillImage2 != null)
            {
                fillImage2.fillAmount = 0f;
                player2Timer.SetActive(false);
                fillImage2.fillAmount = 1f;
            }
        }
        else if (!gameData.playerTurn)
        {
            if (playershape == 0 && fillImage2 != null)
            {
                fillImage2.fillAmount = 0f;
                player2Timer.SetActive(false);
                fillImage2.fillAmount = 1f;
            }
            else if (playershape == 1 && fillImage1 != null)
            {
                fillImage1.fillAmount = 0f;
                player1Timer.SetActive(false);
                fillImage1.fillAmount = 1f;
            }
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
