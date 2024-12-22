using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using dojo_bindings;
using UnityEngine;

public class WorldSimulator : MonoBehaviour
{
    public static WorldSimulator instance;
    [SerializeField] private DojoWorker dojoWorker;

    public depths_of_dread_PlayerState playerState;
    public depths_of_dread_GameData gameData;
    public depths_of_dread_GameFloor gameFloor;
    public depths_of_dread_GameCoins gameCoins;
    public depths_of_dread_GameObstacles gameObstacles;
    public bool floorCleared = false;
    private readonly Queue<Func<Task>> _taskQueue = new();
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
        depths_of_dread_GameData gameData,
        depths_of_dread_GameFloor gameFloor,
        depths_of_dread_GameCoins gameCoins,
        depths_of_dread_GameObstacles gameObstacles)
    {
        if (playerState == null || gameFloor == null || gameCoins == null || gameObstacles == null)
        {
            Debug.LogError("Received null data");
            return false;
        }
        if (GameObject.Find("SimulatedData")) { Destroy(GameObject.Find("SimulatedData")); }

        var entity = new GameObject("SimulatedData");
        AddModel(entity, playerState);
        AddModel(entity, gameData);
        AddModel(entity, gameFloor);
        AddModel(entity, gameCoins);
        AddModel(entity, gameObstacles);
        entity.transform.parent = transform;

        this.playerState = entity.GetComponent<depths_of_dread_PlayerState>();
        this.gameData = entity.GetComponent<depths_of_dread_GameData>();
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

    async Task SendMove(Direction direction)
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

        // Do the move
        playerState.position.MoveTo(direction);
        // Check for an obstacle
        foreach (Obstacle obstacle in gameObstacles.instances)
        {
            if (Vec2.AreEqual(obstacle.position, playerState.position))
            {
                // Handle gameover
            }
        }

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

    async Task<bool> ConfirmWorldState()
    {
        UIManager.instance.SetText("GS-Modal-VerificationText", "Waiting for the server response...");
        UIManager.instance.ShowModal("GS-Modal-Verification");

        Debug.Log("Waiting for updated world state...");
        while (_isProcessing || !floorCleared)
        {
            await Task.Yield();
        }
        Debug.Log("World updated");

        // reset flag
        floorCleared = false;

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
            UIManager.instance.SetText("GS-Modal-VerificationText", $"State mismatch...\n{worldPlayerState.current_floor} / {playerState.current_floor}");
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

    void EnqueueTask(Func<Task> task)
    {
        _taskQueue.Enqueue(task);

        // Optionally, you can auto-start processing here
        if (!_isProcessing)
        {
            _ = ProcessQueue();
        }
    }

    async Task ProcessQueue()
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
