using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 2f;
    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform winLinePrefab;

    private List<GameObject> visualGameObjectList;

    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }
    private void Start()
    {
        GameManager.Instance.OnClickrsOnGridPosition += GameManager_OnClickrsOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }
        foreach (GameObject visualGameObject in visualGameObjectList)
        {
            Destroy(visualGameObject);
        }
        visualGameObjectList.Clear();
    }
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }
        float eularZ = 0f;
        switch (e.line.orientation)
        {
            default:
            case GameManager.Orientation.Horizontal: eularZ = 90f; break;
            case GameManager.Orientation.Vertical: eularZ = 0f; break;
            case GameManager.Orientation.DiagonalA: eularZ = -45f; break;
            case GameManager.Orientation.DiagonalB: eularZ = 45f; break;
        }
        Transform lineCompleteTransform =
            Instantiate(winLinePrefab, GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
                 Quaternion.Euler(0, 0, eularZ));
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);
        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }
    private void GameManager_OnClickrsOnGridPosition(object sender, GameManager.OnClickrsOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e.x, e.y, e.playerType);
        Debug.Log("GameManager_OnClickrsOnGridPosition");
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    {
        Debug.Log("SpawnObjectRpc");
        Transform prefab;
        switch (playerType)
        {
            default:
            case GameManager.PlayerType.Cross:
                prefab = crossPrefab;
                break;
            case GameManager.PlayerType.Circle:
                prefab = circlePrefab;
                break;

        }
        Transform spawnedCrossTransform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
        spawnedCrossTransform.GetComponent<NetworkObject>().Spawn(true);
        visualGameObjectList.Add(spawnedCrossTransform.gameObject);
    }

    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
