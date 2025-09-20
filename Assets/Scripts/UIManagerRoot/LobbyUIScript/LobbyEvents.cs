using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

public static class LobbyEvents
{
    // UI Triggers
    public static UnityEvent OnStartButtonClicked = new UnityEvent();
    public static UnityEvent<string> OnLobbyCreateRequested = new UnityEvent<string>();
    public static UnityEvent<Lobby> OnLobbyJoinRequested = new UnityEvent<Lobby>();
    public static UnityEvent OnLeaveLobbyRequested = new UnityEvent();

    // Network Responses
    public static UnityEvent<QueryResponse> OnLobbiesQueried = new UnityEvent<QueryResponse>();
    public static UnityEvent<Lobby> OnLobbyJoined = new UnityEvent<Lobby>();
    public static UnityEvent OnLobbyLeft = new UnityEvent();

    // Game State Triggers
    public static UnityEvent OnLobbyFull = new UnityEvent();
    public static UnityEvent OnGameShouldStart = new UnityEvent();
}
