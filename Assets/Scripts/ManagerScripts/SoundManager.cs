using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Transform clickSound;
    [SerializeField] private Transform winSound;
    [SerializeField] private Transform loseSound;
    [SerializeField] private Transform CrossWinParticalSys;
    [SerializeField] private Transform CircleWinParticalSys;


    private void Start()
    {
        GameManager.Instance.OnPlaceObject += GameManager_OnPlaceObject;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;

    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (GameManager.Instance.GetLocalPlayerType() == e.winPlayerType)
        {
            Transform sfsxTransform = Instantiate(winSound);
            Destroy(sfsxTransform.gameObject, 5f);
        }
        else
        {
            Transform sfsxTransform = Instantiate(loseSound);
            Destroy(sfsxTransform.gameObject, 5f);
        }

        if (e.winPlayerType == GameManager.PlayerType.Cross)
        {
            Debug.Log("Winner is Cross!!!!");
            Transform sfsxTransform = Instantiate(CrossWinParticalSys);
        }
        else
        {
            Debug.Log("Winner is Circle!!!!");
            Transform sfsxTransform = Instantiate(CircleWinParticalSys);
        }
    }
    private void GameManager_OnPlaceObject(object sender, System.EventArgs e)
    {
        Transform sfsxTransform = Instantiate(clickSound);
        Destroy(sfsxTransform.gameObject, 5f);
    }
}
