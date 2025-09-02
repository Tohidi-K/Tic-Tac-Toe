using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System;
using System.Threading.Tasks;

public class NakamaConnection : MonoBehaviour
{
    private string   Scheme    = "http";
    private string   Host      = "localhost";
    private string   ServerKey = "defaultkey";

    private string   ticket;
    public  string   matchId;

    private int      Port      = 7350;

    private IClient  Client;
    private ISession Session;
    public  ISocket  Socket;

    public async Task Connect()
    {
        Client  = new Nakama.Client(Scheme, Host, Port, ServerKey, UnityWebRequestAdapter.Instance);
        Session = await Client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier);
        Socket  = Client.NewSocket();

        await Socket.ConnectAsync(Session, true);

        Debug.Log(Session);
        Debug.Log(Socket);
    }

    public async void FindMatch()
    {
        Debug.Log("Finding Match");

        var matchmakingTicket = await Socket.AddMatchmakerAsync("*", 2, 2);
        ticket = matchmakingTicket.Ticket;
    }
}
