using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using UnityEngine;

public class WorldSimulator : MonoBehaviour
{
    public static WorldSimulator instance;
    [SerializeField] private DojoWorker dojoWorker;

    public depths_of_dread_PlayerState playerState;
    public depths_of_dread_PlayerPowerUps playerPowerUps;
    public depths_of_dread_GameFloor gameFloor;
    public depths_of_dread_GameCoins gameCoins;
    public depths_of_dread_GameObstacles gameObstacles;
    public bool floorEndEvent = false;
    private readonly Queue<Func<UniTask>> _taskQueue = new();
    private bool _isProcessing = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public bool InitializeInstance(
        depths_of_dread_PlayerState playerState,
        depths_of_dread_PlayerPowerUps playerPowerUps,
        depths_of_dread_GameFloor gameFloor,
        depths_of_dread_GameCoins gameCoins,
        depths_of_dread_GameObstacles gameObstacles)
    {
        if (
            playerState == null
            || playerPowerUps == null
            || gameFloor == null
            || gameCoins == null
            || gameObstacles == null
        )
        {
            Debug.LogError($"Received null data");
            Debug.LogError($"playerState: {playerState}");
            Debug.LogError($"playerPowerUps: {playerPowerUps}");
            Debug.LogError($"gameFloor: {gameFloor}");
            Debug.LogError($"gameCoins: {gameCoins}");
            Debug.LogError($"gameObstacles: {gameObstacles}");
            return false;
        }

        if (GameObject.Find("SimulatedData")) { Destroy(GameObject.Find("SimulatedData")); }

        var entity = Instantiate(new GameObject("SimulatedData"), transform);
        AddModel(entity, playerState);
        AddModel(entity, playerPowerUps);
        AddModel(entity, gameFloor);
        AddModel(entity, gameCoins);
        AddModel(entity, gameObstacles);

        this.playerState = entity.GetComponent<depths_of_dread_PlayerState>();
        this.playerPowerUps = entity.GetComponent<depths_of_dread_PlayerPowerUps>();
        this.gameFloor = entity.GetComponent<depths_of_dread_GameFloor>();
        this.gameCoins = entity.GetComponent<depths_of_dread_GameCoins>();
        this.gameObstacles = entity.GetComponent<depths_of_dread_GameObstacles>();

        ScreenManager.instance.SetActiveScreen("GameOverlay");

        UIManager.instance.HandleNewFloor();
        UIManager.instance.HandleStateUpdate();
        UIManager.instance.RenderCoins(gameCoins.coins);
        return true;
    }

    private void AddModel(GameObject entity, ModelInstance modelInstance)
    {
        var component = (ModelInstance)entity.AddComponent(modelInstance.GetType());
        component.Initialize(modelInstance.Model);
    }

    public void ClearGameEntity()
    {
        gameFloor = null;
        gameCoins = null;
        gameObstacles = null;
    }

    public bool IsInitialized()
    {
        return playerState != null
            && gameFloor != null
            && gameCoins != null
            && gameObstacles != null;
    }

    public bool CanMove(Direction direction)
    {
        Vector3 moveTarget = playerState.position.MoveCheck(direction);
        return Vec2.IsInBounds(moveTarget, gameFloor.size);
    }

    async UniTask SendMove(Direction direction)
    {
        Actions actions = dojoWorker.actions;
        Account account = dojoWorker.account;
        JsonRpcClient provider = dojoWorker.provider;

        var txnHash = await actions.move(account, direction);
        await provider.WaitForTransaction(txnHash);
    }

    public async void SimulateMove(Direction direction)
    {
        // Send move to transaction queue
        EnqueueTask(() => SendMove(direction));

        // Update position
        playerState.position.MoveTo(direction);

        // Check for a coin
        foreach (Vec2 coinPosition in gameCoins.coins)
        {
            if (Vec2.AreEqual(coinPosition, playerState.position))
            {
                playerState.coins++;
                // Remove coin from array
                List<Vec2> coinList = new List<Vec2>(gameCoins.coins);
                coinList.Remove(coinPosition);
                gameCoins.coins = coinList.ToArray();

                UIManager.instance.RenderCoins(gameCoins.coins);
            }
        }

        // Update UI
        UIManager.instance.HandleStateUpdate();

        // Check for an obstacle
        foreach (Obstacle obstacle in gameObstacles.instances)
        {
            if (Vec2.AreEqual(obstacle.position, playerState.position))
            {
                foreach (PowerUp powerup in playerPowerUps.powers)
                {
                    if (powerup.power_type.HandlesObstacle(obstacle.obstacle_type))
                    {
                        // TODO: Trigger animation for handled obstacle
                    }
                    else
                    {
                        // TODO: Trigger animation for death

                        // Handle gameover
                        playerState.current_floor = 0;
                        playerState.coins = 0;

                        var isConfirmed = await ConfirmWorldState();
                        if (!isConfirmed) { return; }

                        UIManager.instance.HandleGameover();
                    }
                }
            }
        }

        // Check for new floor
        if (Vec2.AreEqual(playerState.position, gameFloor.end_tile))
        {
            playerState.current_floor++;

            var isConfirmed = await ConfirmWorldState();
            if (!isConfirmed) { return; }

            SyncToWorldState();
            UIManager.instance.HandleFloorCleared(playerState);
        }
    }

    async UniTask<bool> ConfirmWorldState()
    {
        UIManager.instance.DisableJoystick(); // Is enabled again after showing hint modal
        MovementScript playerMovement = GameObject.FindGameObjectWithTag("GS-CharacterSprite").GetComponent<MovementScript>();

        await UniTask.WaitUntil(() => playerMovement.isMoving == false);

        UIManager.instance.SetText("GS-Modal-VerificationText", "Waiting for the server response...");
        UIManager.instance.ShowModal("GS-Modal-Verification");

        Debug.Log("Waiting for updated world state...");
        while (_isProcessing || !floorEndEvent)
        {
            await UniTask.Yield();
        }
        Debug.Log("World updated");

        // reset flag
        floorEndEvent = false;

        var worldPlayerState = dojoWorker.playerEntity.GetComponent<depths_of_dread_PlayerState>();
        bool stateMatch = worldPlayerState.current_floor == playerState.current_floor
            && worldPlayerState.coins == playerState.coins;

        Debug.Log($"State verification: {stateMatch}");
        Debug.Log($"Floor verification: {worldPlayerState.current_floor} / {playerState.current_floor}");
        Debug.Log($"Coin verification: {worldPlayerState.coins} / {playerState.coins}");
        if (stateMatch)
        {
            UIManager.instance.HideModal("GS-Modal-Verification");
            return true;
        }
        else
        {
            string floorMismatchText = $"\n{worldPlayerState.current_floor} / {playerState.current_floor}";
            string coinMismatchText = $"\n{worldPlayerState.coins} / {playerState.coins}";
            UIManager.instance.SetText("GS-Modal-VerificationText", $"State mismatch {floorMismatchText}{coinMismatchText}");
            return false;
        }
    }

    void SyncToWorldState()
    {
        playerState.position = dojoWorker.playerEntity.GetComponent<depths_of_dread_PlayerState>().position;
        playerState.current_floor = dojoWorker.playerEntity.GetComponent<depths_of_dread_PlayerState>().current_floor;
        playerState.coins = dojoWorker.playerEntity.GetComponent<depths_of_dread_PlayerState>().coins;

        gameFloor.size = dojoWorker.gameEntity.GetComponent<depths_of_dread_GameFloor>().size;
        gameFloor.path = dojoWorker.gameEntity.GetComponent<depths_of_dread_GameFloor>().path;
        gameFloor.end_tile = dojoWorker.gameEntity.GetComponent<depths_of_dread_GameFloor>().end_tile;

        gameCoins.coins = dojoWorker.gameEntity.GetComponent<depths_of_dread_GameCoins>().coins;

        gameObstacles.instances = dojoWorker.gameEntity.GetComponent<depths_of_dread_GameObstacles>().instances;
    }

    void EnqueueTask(Func<UniTask> task)
    {
        _taskQueue.Enqueue(task);

        if (!_isProcessing)
        {
            ProcessQueue().Forget();
        }
    }

    async UniTask ProcessQueue()
    {
        _isProcessing = true;

        while (_taskQueue.Count > 0)
        {
            // Dequeue the next task and execute it
            var task = _taskQueue.Dequeue();
            await task();
        }

        _isProcessing = false;
    }
}
