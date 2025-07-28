using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


// EVENTS
    public event EventHandler<OnClickrsOnGridPositionEventArgs> OnClickrsOnGridPosition;
    public class OnClickrsOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChange;
    public event EventHandler OnPlaceObject;


// REFERENCES
    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }
    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }

    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }
    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlauerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypesArray;
    private List<Line> lineList;
    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance");
        }
        Instance = this;

        playerTypesArray = new PlayerType[3, 3];

        lineList = new List<Line>
        {
            //Horizontal
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),},
                centerGridPosition = new Vector2Int(1,0),
                orientation = Orientation.Horizontal
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(2,1),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Horizontal
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2),new Vector2Int(1,2),new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(1,2),
                orientation = Orientation.Horizontal
            },
            // Vertical
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2),},
                centerGridPosition = new Vector2Int(0,1),
                orientation = Orientation.Vertical
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(1,0),new Vector2Int(1,1),new Vector2Int(1,2),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Vertical
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(2,0),new Vector2Int(2,1),new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(2,1),
                orientation = Orientation.Vertical
            },
            // Diagonal
            
            new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0),new Vector2Int(1,1),new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA
            },
              new Line {
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2),new Vector2Int(1,1),new Vector2Int(2,0),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB
            },
        };
    }


//
    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn : " + NetworkManager.Singleton.LocalClientId);
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
        currentPlayablePlauerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerChanged?.Invoke(this, EventArgs.Empty);

        };
        playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChange?.Invoke(this, EventArgs.Empty);
        };
        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChange?.Invoke(this, EventArgs.Empty);
        };
    }

//
    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            //start game
            currentPlayablePlauerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }


//
    [Rpc(SendTo.Server)]
    public void ClickedOnGrtidPositionRPC(int x, int y, PlayerType playerType)
    {
        Debug.Log("Clicked on grid position  " + x + ", " + y);
        if (playerType != currentPlayablePlauerType.Value)
        {
            return;
        }

        if (playerTypesArray[x, y] != PlayerType.None)
        {
            return;
        }
        playerTypesArray[x, y] = playerType;
        TriggertOnPlaceObjectRpc();

        OnClickrsOnGridPosition?.Invoke(this, new OnClickrsOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        });
        switch (currentPlayablePlauerType.Value)
        {
            default:

            case PlayerType.Cross:
                currentPlayablePlauerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlauerType.Value = PlayerType.Cross;
                break;
        }
        TestWinner();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggertOnPlaceObjectRpc()
    {
        OnPlaceObject?.Invoke(this, EventArgs.Empty);
    }

//
    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
                playerTypesArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
                playerTypesArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
                playerTypesArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
        );
    }
    private bool TestWinnerLine(PlayerType aplayerType, PlayerType bplayerType, PlayerType cplayerType)
    {
        return
            aplayerType != PlayerType.None &&
            aplayerType == bplayerType &&
            bplayerType == cplayerType;
    }
    private void TestWinner()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];

            if (TestWinnerLine(line))
            {   //win
                Debug.Log("Win!!!!!");
                currentPlayablePlauerType.Value = PlayerType.None;
                PlayerType winPlayerType = playerTypesArray[line.centerGridPosition.x, line.centerGridPosition.y];

                switch (winPlayerType)
                {
                    default:
                    case PlayerType.Cross:
                        playerCrossScore.Value++;
                        break;
                    case PlayerType.Circle:
                        playerCircleScore.Value++;
                        break;
                }
                TriggerOnGameWinRpc(i, winPlayerType);
                break;
            }
        }
        bool hasTie = true;
        for (int i = 0; i < playerTypesArray.GetLength(0); i++)
        {
            for (int j = 0; j < playerTypesArray.GetLength(1); j++)
            {
                if (playerTypesArray[i, j] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }
        }
        if (hasTie)
        {
            TriggerOnGameTiedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType,
        });
    }
    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int i = 0; i < playerTypesArray.GetLength(0); i++)
        {
            for (int j = 0; j < playerTypesArray.GetLength(1); j++)
            {
                playerTypesArray[i, j] = PlayerType.None;
            }
        }
        currentPlayablePlauerType.Value = PlayerType.Cross;
        TriggerRematchRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }
    public PlayerType GetcurrentPlayablePlayerType()
    {
        return currentPlayablePlauerType.Value;
    }
    public void GetScores(out int playerCrossScore,out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}
