using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using Unity.Netcode;

public class LobbyUIManagerV2 : MonoBehaviour
{
    [Header("Name Entry Panel")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startButton;

    [Header("Lobby Browser Panel")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyListItemPrefab;

    [Header("Create Lobby Panel")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Button confirmCreateLobbyButton;

    [Header("Joined Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyNameLabel;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private Button leaveLobbyButton;

    public static string PlayerName = "Player";

    /* ---------- polling support ---------- */
    private Coroutine pollCoroutine;
    private const float LobbyPollInterval = 1.1f;

    private void Start()
    {
        /* ---- button wiring ---- */
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonPressed);

        createLobbyButton.onClick.AddListener(() => UIFlowManager.Instance.ShowCreateLobby());
        refreshButton.onClick.AddListener(() => LobbyManagerV2.Instance.QueryLobbies());

        // When confirming create lobby, ensure PlayerName is taken from input first
        confirmCreateLobbyButton.onClick.RemoveAllListeners();
        confirmCreateLobbyButton.onClick.AddListener(() =>
        {
            SetPlayerNameFromInput();
            string name = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "MyLobby" : lobbyNameInput.text.Trim();
            LobbyEvents.OnLobbyCreateRequested.Invoke(name);
        });

        leaveLobbyButton.onClick.AddListener(() => LobbyEvents.OnLeaveLobbyRequested.Invoke());

        /* ---- event subscriptions ---- */
        LobbyEvents.OnLobbyCreateRequested.AddListener(LobbyManagerV2.Instance.CreateLobby);
        LobbyEvents.OnLobbyJoinRequested.AddListener(LobbyManagerV2.Instance.JoinLobby);
        LobbyEvents.OnLobbiesQueried.AddListener(RefreshLobbyList);
        LobbyEvents.OnLobbyJoined.AddListener(OnLobbyJoined);
        LobbyEvents.OnGameShouldStart.AddListener(StopPolling);

        /* ---- initial flow ---- */
        UIFlowManager.Instance.ShowNameEntry();
    }

    private void OnStartButtonPressed()
    {
        SetPlayerNameFromInput();
        LobbyEvents.OnStartButtonClicked.Invoke();
    }

    private void SetPlayerNameFromInput()
    {
        if (playerNameInput != null)
        {
            PlayerName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Player" : playerNameInput.text.Trim();
            Debug.Log($"[LobbyUIManagerV2] PlayerName set to '{PlayerName}'");
        }
    }

    private void OnLobbyJoined(Lobby lobby)
    {
        RefreshJoinedLobbyUI(lobby);
        UIFlowManager.Instance.ShowJoinedLobby();

        /* start polling while we are inside a lobby */
        if (pollCoroutine == null)
            pollCoroutine = StartCoroutine(PollLobby());
    }

    private void RefreshLobbyList(QueryResponse response)
    {
        foreach (Transform child in lobbyListContainer) Destroy(child.gameObject);

        foreach (Lobby lobby in response.Results)
        {
            if (lobby.Players.Count >= lobby.MaxPlayers) continue;

            GameObject item = Instantiate(lobbyListItemPrefab, lobbyListContainer);
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();

            TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{lobby.Name} [{lobby.Players.Count}/{lobby.MaxPlayers}]";

            // Make sure we capture the player's current name before joining
            btn.onClick.AddListener(() =>
            {
                SetPlayerNameFromInput();
                LobbyEvents.OnLobbyJoinRequested.Invoke(lobby);
            });
        }
    }

    public void RefreshJoinedLobbyUI(Lobby lobby)
{
    if (lobby == null)
    {
        Debug.LogWarning("RefreshJoinedLobbyUI called with null lobby");
        return;
    }

    // Set lobby name
    lobbyNameLabel.text = string.IsNullOrEmpty(lobby.Name) ? "Unnamed Lobby" : lobby.Name;

    // Clear old list
    foreach (Transform child in playerListContainer)
    {
        Destroy(child.gameObject);
    }

    // Populate players
    foreach (Player p in lobby.Players)
    {
        GameObject playerItem = Instantiate(playerItemPrefab, playerListContainer);
        TextMeshProUGUI nameLabel = playerItem.GetComponentInChildren<TextMeshProUGUI>();

        string displayName = (p.Data != null && p.Data.ContainsKey("PlayerName"))
            ? p.Data["PlayerName"].Value
            : p.Id;

        if (p.Id == lobby.HostId)
        {
            displayName += " [Host]";
        }

        nameLabel.text = displayName;
        Debug.Log("Player in lobby: " + displayName);
    }

    playerCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers}";
}


    private IEnumerator PollLobby()
    {
        var wait = new WaitForSecondsRealtime(LobbyPollInterval);

        while (true)
        {
            yield return wait;

            if (LobbyManagerV2.Instance.currentLobby == null)
                continue;

            var task = LobbyService.Instance.GetLobbyAsync(LobbyManagerV2.Instance.currentLobby.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Debug.LogWarning($"Lobby poll failed: {task.Exception.InnerException?.Message}");
                continue;
            }

            var lobby = task.Result;
            LobbyManagerV2.Instance.currentLobby = lobby;
            RefreshJoinedLobbyUI(lobby);
        }
    }

    private void StopPolling()
    {
        if (pollCoroutine != null)
        {
            Debug.Log("[LobbyUIManager] Game is starting. Stopping lobby poll coroutine.");
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (pollCoroutine != null) StopCoroutine(pollCoroutine);
    }
}
