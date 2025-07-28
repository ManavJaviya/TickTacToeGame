using TMPro;





using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultTextMesh;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;
    [SerializeField] private Color tieColor;
    [SerializeField] private Button rematchButto; 
    

    private void Awake()
    {
        rematchButto.onClick.AddListener(() =>
        {
            GameManager.Instance.RematchRpc();
        });
    }                   
    private void Start()
    {
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;
        Hide();
    }

    private void GameManager_OnGameTied(object sender, System.EventArgs e)
    {
        resultTextMesh.text = "Tie!";
        resultTextMesh.color = tieColor;
        Show();
    }
    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        Hide();
    }
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            resultTextMesh.text = "You Win!";
            resultTextMesh.color = winColor;
        }
        else
        {

            resultTextMesh.text = "You Lose!";
            resultTextMesh.color = loseColor;
        }
        Show();
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }







}
