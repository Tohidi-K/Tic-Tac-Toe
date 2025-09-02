using Nakama;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
    private bool  isHost;

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

    private void OnReceivedMatchState(IMatchState matchState)
    {
        switch (matchState.OpCode)
        {
            case 1:
                player2Name.text = System.Text.Encoding.UTF8.GetString(matchState.State);
                break;

            case 2:
                float finalAngle = BitConverter.ToSingle(matchState.State, 0);
                StartSpin(finalAngle);
                Debug.Log(finalAngle);
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
            float finalAngle = possibleAngles + extraSpins * 360f;

            Debug.Log("a number has been generated which is : " + finalAngle);
            Debug.Log(finalAngle);

            var bytes = BitConverter.GetBytes(finalAngle);
            await NakamaConnection.Socket.SendMatchStateAsync(NakamaConnection.matchId, 2, bytes, null);

            //StartSpin(finalAngle);
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

    private void StartSpin(float finalAngle)
    {
        StartCoroutine(SpinRoutine(finalAngle));
    }

    private IEnumerator SpinRoutine(float finalAngle)
    {
        float elapsed = 0f;
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
