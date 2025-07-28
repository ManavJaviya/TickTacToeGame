using UnityEngine;

public class UIFlowManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject nameEntryPanel;
    [SerializeField] private GameObject lobbyBrowserPanel;
    [SerializeField] private GameObject createLobbyPanel;
    [SerializeField] private GameObject joinedLobbyPanel;

    public static UIFlowManager Instance;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        LobbyEvents.OnStartButtonClicked.AddListener(ShowLobbyBrowser);
        LobbyEvents.OnLobbyCreateRequested.AddListener(_ => ShowJoinedLobby());
        LobbyEvents.OnLobbyJoined.AddListener(_ => ShowJoinedLobby());
        LobbyEvents.OnLobbyLeft.AddListener(ShowLobbyBrowser);
    }

    public void ShowNameEntry() => SwitchToPanel(nameEntryPanel);
    public void ShowLobbyBrowser() => SwitchToPanel(lobbyBrowserPanel);
    public void ShowCreateLobby() => SwitchToPanel(createLobbyPanel);
    public void ShowJoinedLobby() => SwitchToPanel(joinedLobbyPanel);

    private void SwitchToPanel(GameObject panel)
    {
        HideAllPanels();
        panel.SetActive(true);
    }

    public void HideAllPanels()
    {
        nameEntryPanel.SetActive(false);
        lobbyBrowserPanel.SetActive(false);
        createLobbyPanel.SetActive(false);
        joinedLobbyPanel.SetActive(false);
    }
}
