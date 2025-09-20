using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;

public class LobbyManagerV2 : NetworkBehaviour
{
    public static LobbyManagerV2 Instance { get; private set; }

    private int MaxPlayers = 2;
    public Lobby currentLobby;
    private bool isGameStarting = false;

    private async void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in with PlayerID: " + AuthenticationService.Instance.PlayerId);
        }
        else
        {
            Debug.Log("Already signed in with PlayerID: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (IsHost && NetworkManager.Singleton.ConnectedClients.Count == MaxPlayers)
        {
            Debug.Log("All players have connected to Netcode. Triggering game start.");
            NotifyGameReady();
        }
    }

    public async void CreateLobby(string lobbyName)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, LobbyUIManagerV2.PlayerName) }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);

            SetupTransportAsHost(allocation);
            NetworkManager.Singleton.StartHost();

            Debug.Log($"Lobby created: {currentLobby.Name}");

            foreach (var p in currentLobby.Players)
            {
                string pname = (p.Data != null && p.Data.ContainsKey("PlayerName"))
                    ? p.Data["PlayerName"].Value
                    : p.Id;
                Debug.Log($"[LobbyManagerV2] Created Lobby Player: id={p.Id}, name={pname}");
            }

            LobbyEvents.OnLobbyJoined.Invoke(currentLobby);
        }
        catch (LobbyServiceException e) { Debug.LogError($"CreateLobby failed: {e.Message}"); }
    }

    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, LobbyUIManagerV2.PlayerName) }
                    }
                }
            });

            string joinCode = currentLobby.Data["JoinCode"].Value;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            SetupTransportAsClient(allocation);
            NetworkManager.Singleton.StartClient();

            Debug.Log($"Joined lobby: {currentLobby.Name}");

            foreach (var p in currentLobby.Players)
            {
                string pname = (p.Data != null && p.Data.ContainsKey("PlayerName"))
                    ? p.Data["PlayerName"].Value
                    : p.Id;
                Debug.Log($"[LobbyManagerV2] Joined Lobby Player: id={p.Id}, name={pname}");
            }

            LobbyEvents.OnLobbyJoined.Invoke(currentLobby);
        }
        catch (LobbyServiceException e) { Debug.LogError($"JoinLobby failed: {e.Message}"); }
    }

    public void NotifyGameReady()
    {
        if (isGameStarting || !IsHost) return;
        isGameStarting = true;
        Debug.Log("Host is proceeding to start the game and send RPC.");
        LobbyEvents.OnGameShouldStart.Invoke();
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        if (IsHost) return;
        LobbyEvents.OnGameShouldStart.Invoke();
    }

    public async void QueryLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            LobbyEvents.OnLobbiesQueried.Invoke(response);
        }
        catch (LobbyServiceException e) { Debug.LogError($"QueryLobbies failed: {e.Message}"); }
    }

    private void SetupTransportAsHost(Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );
    }

    private void SetupTransportAsClient(JoinAllocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );
    }
}
