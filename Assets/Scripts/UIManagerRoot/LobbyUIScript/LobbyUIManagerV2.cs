using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
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
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private Button leaveLobbyButton;

    public static string PlayerName = "Player";

    private void Start()
    {
        startButton.onClick.AddListener(() => LobbyEvents.OnStartButtonClicked.Invoke());
        createLobbyButton.onClick.AddListener(() => UIFlowManager.Instance.ShowCreateLobby());
        refreshButton.onClick.AddListener(() => LobbyManagerV2.Instance.QueryLobbies());
        confirmCreateLobbyButton.onClick.AddListener(() => {
            string name = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "MyLobby" : lobbyNameInput.text.Trim();
            LobbyEvents.OnLobbyCreateRequested.Invoke(name);
        });
        leaveLobbyButton.onClick.AddListener(() => LobbyEvents.OnLeaveLobbyRequested.Invoke());

        LobbyEvents.OnLobbiesQueried.AddListener(RefreshLobbyList);
        LobbyEvents.OnLobbyJoined.AddListener(ShowJoinedLobby);

        UIFlowManager.Instance.ShowNameEntry();
    }

    private void RefreshLobbyList(QueryResponse response)
    {
        foreach (Transform child in lobbyListContainer)
            Destroy(child.gameObject);

        foreach (Lobby lobby in response.Results)
        {
            if (lobby.Players.Count >= lobby.MaxPlayers)
                continue;

            GameObject item = Instantiate(lobbyListItemPrefab, lobbyListContainer);
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();
            if (btn != null)
            {

                TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{lobby.Name} [{lobby.Players.Count}/{lobby.MaxPlayers}]";
                
                btn.onClick.AddListener(() => LobbyEvents.OnLobbyJoinRequested.Invoke(lobby));
            }
            else
            {
                Debug.LogWarning("Missing button");
            }
        }
    }

    private void ShowJoinedLobby(Lobby lobby)
    {
        lobbyNameLabel.text = lobby.Name;
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        foreach (Player p in lobby.Players)
        {
            GameObject playerItem = Instantiate(playerItemPrefab, playerListContainer);
            TextMeshProUGUI nameLabel = playerItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameLabel != null)
            {
                nameLabel.text = p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : p.Id;
            }
            else
            {
                Debug.LogWarning("Missing player name tmp");
            }
        }
    }
}
