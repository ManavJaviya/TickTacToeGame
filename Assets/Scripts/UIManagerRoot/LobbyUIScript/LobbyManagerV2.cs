using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbyManagerV2 : MonoBehaviour
{
    public static LobbyManagerV2 Instance;
    private Lobby currentLobby;
    private string joinCode;

    private const int MaxPlayers = 2;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeServices();

        LobbyEvents.OnStartButtonClicked.AddListener(HandleStart);
        LobbyEvents.OnLobbyCreateRequested.AddListener(CreateLobby);
        LobbyEvents.OnLobbyJoinRequested.AddListener(JoinLobby);
        LobbyEvents.OnLeaveLobbyRequested.AddListener(LeaveLobby);
    }

    private async void InitializeServices()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void HandleStart()
    {
        LobbyUIManagerV2.PlayerName = string.IsNullOrWhiteSpace(LobbyUIManagerV2.PlayerName)
            ? "Player"
            : LobbyUIManagerV2.PlayerName.Trim();

        QueryLobbies();
    }

    public async void QueryLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            LobbyEvents.OnLobbiesQueried.Invoke(response);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Query failed: {e.Message}");
        }
    }

    public async void CreateLobby(string lobbyName)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, LobbyUIManagerV2.PlayerName) }
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

            LobbyEvents.OnLobbyJoined.Invoke(currentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"CreateLobby failed: {e.Message}");
        }
    }

    public async void JoinLobby(Lobby lobby)
{
    Debug.Log("Joining lobby: " + lobby.Name);
    try
    {
        Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
        Debug.Log("Successfully joined: " + joinedLobby.Name);
        // Proceed to Relay allocation or scene transition
    }
    catch (LobbyServiceException e)
    {
        Debug.LogError("Join failed: " + e.Message);
        // Show toast or popup to user
    }
}


    public void LeaveLobby()
    {
        if (currentLobby == null) return;

        try
        {
            LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, GetPlayerId());
            currentLobby = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }

            LobbyEvents.OnLobbyLeft.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"LeaveLobby failed: {e.Message}");
        }
    }

    public void KickPlayer(string playerId)
    {
        if (currentLobby == null) return;

        try
        {
            LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            Debug.Log($"Kicked player: {playerId}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Kick failed: {e.Message}");
        }
    }

    private void SetupTransportAsHost(Allocation allocation)
    {
        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);
    }

    private void SetupTransportAsClient(JoinAllocation allocation)
    {
        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData);
    }

    public bool IsHost()
    {
        return currentLobby != null && currentLobby.HostId == GetPlayerId();
    }

    public string GetPlayerId()
    {
        return AuthenticationService.Instance.PlayerId;
    }

    public Lobby GetCurrentLobby() => currentLobby;
}
