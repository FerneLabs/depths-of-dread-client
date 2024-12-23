using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using bottlenoselabs.C2CS.Runtime;
using Dojo;
using Dojo.Starknet;
using Dojo.Torii;
using TMPro;
using UnityEngine;
using static EncodingService;

public class DojoWorker : MonoBehaviour
{
    [SerializeField] WorldManager worldManager;
    [SerializeField] WorldManagerData dojoConfig;
    [SerializeField] DojoWorkerData dojoWorkerData;
    public Actions actions;
    public JsonRpcClient provider;
    public Account account;
    public GameObject playerEntity;
    public GameObject gameEntity;
    private int localCurrentFloor = 1; // Temporary workaround until we can use events.

    void Start()
    {
        worldManager.synchronizationMaster.OnEntitySpawned.AddListener(HandleSpawn);
        worldManager.synchronizationMaster.OnModelUpdated.AddListener(HandleUpdate);
        worldManager.synchronizationMaster.OnEventMessage.AddListener(HandleEvent);
    }

    async void HandleSpawn(GameObject spownedEntity)
    {
        if (account == null) { return; }
        await SyncLocalEntities();
        InitLocalCurrentFloor(); // Temporary workaround until we can use events.
    }

    // Temporary workaround until we can use events.
    void InitLocalCurrentFloor()
    {
        var playerState = playerEntity != null ? playerEntity.GetComponent<depths_of_dread_PlayerState>() : null;
        localCurrentFloor = playerState != null ? playerState.current_floor : 1;
    }

    async void HandleUpdate(ModelInstance updatedModel)
    {
        // Player Entity Handlers
        await SyncLocalEntities();
        if (playerEntity != null)
        {
            switch (updatedModel.GetType().Name)
            {
                case "depths_of_dread_PlayerData":
                    OnPlayerDataUpdate();
                    break;
                case "depths_of_dread_PlayerState":
                    OnPlayerStateUpdate();
                    break;
                case "depths_of_dread_PlayerPowerUps":
                    OnPlayerPowerUpsUpdate();
                    break;
                default:
                    break;
            }
        }

        // When running SyncLocalEntities() for the first time, PlayerState is not yet defined, making game entity null.
        // We need to run entity sync again to initialize game entity with latest PlayerState.game_id value.

        // Game Entity Handlers
        await SyncLocalEntities();
        if (gameEntity != null)
        {
            switch (updatedModel.GetType().Name)
            {
                case "depths_of_dread_GameData":
                    OnGameDataUpdate();
                    break;
                case "depths_of_dread_GameFloor":
                    OnGameFloorUpdate();
                    break;
                case "depths_of_dread_GameCoins":
                    OnGameCoinsUpdate();
                    break;
                case "depths_of_dread_GameObstacles":
                    OnGameObstaclesUpdate();
                    break;
                default:
                    break;
            }
        }
    }

    private void HandleEvent(ModelInstance eventMessage)
    {
        Debug.Log($"Got event: {eventMessage}");

        // TODO handle events when they work properly in the SDK
        // switch (eventMessage.GetType().Name)
        // {
        //     case "depths_of_dread_Moved":
        //         var playerState = playerEntity.GetComponent<depths_of_dread_PlayerState>();
        //         var direction = eventMessage.GetComponent<depths_of_dread_Moved>().direction;
        //         Debug.Log($"Received movement {direction}");
        //         UIManager.instance.HandleMovement(playerState.position, direction);
        //         break;
        //     default:
        //         break;
        // }
    }

    public async Task SyncLocalEntities()
    {
        var playerKey = account != null ? GetPoseidonHash(account.Address) : null;
        await Task.Yield();

        var pEntity = GameObject.Find(playerKey);

        // and if the entity matches the current player hashed key
        if (pEntity != null && pEntity != playerEntity)
        {
            playerEntity = pEntity;
            OnPlayerDataUpdate();
            OnPlayerStateUpdate();
            OnPlayerPowerUpsUpdate();

            Debug.Log($"Synced playerEntity {playerEntity}");
        }
        await Task.Yield();

        var playerState = playerEntity != null ? playerEntity.GetComponent<depths_of_dread_PlayerState>() : null;
        var gameKey = playerState != null ? GetPoseidonHash(new FieldElement(playerState.game_id)) : null;
        await Task.Yield();

        var gEntity = GameObject.Find(gameKey);
        var gameData = gEntity != null ? gEntity.GetComponent<depths_of_dread_GameData>() : null;

        if (gEntity != null && gEntity != gameEntity && gameData.game_id != 0)
        {
            gameEntity = gEntity;
            OnGameDataUpdate();
            OnGameFloorUpdate();
            OnGameCoinsUpdate();

            Debug.Log($"Synced gameEntity {gameEntity}");

            if (!WorldSimulator.instance.IsInitialized())
            {
                WorldSimulator.instance.InitializeInstance(
                    playerEntity.GetComponent<depths_of_dread_PlayerState>(),
                    playerEntity.GetComponent<depths_of_dread_PlayerPowerUps>(),
                    gameEntity.GetComponent<depths_of_dread_GameFloor>(),
                    gameEntity.GetComponent<depths_of_dread_GameCoins>(),
                    gameEntity.GetComponent<depths_of_dread_GameObstacles>()
                );
            }

        }
    }

    public async void SimulateControllerConnection(string username)
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = new Account(
            provider,
            new SigningKey(dojoWorkerData.masterPrivateKey),
            new FieldElement(dojoWorkerData.masterAddress)
        );

        var txnHash = await CreatePlayer(username);
        await provider.WaitForTransaction(txnHash);

        InitLocalCurrentFloor(); // Temporary workaround until we can use events.
    }

    public void SimulateControllerDisconnection()
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = null;
        playerEntity = null;

        UIManager.instance.HandleDisconnection();
    }

    public async Task<FieldElement> CreatePlayer(string username)
    {
        BigInteger encodedUsername = ASCIIToBigInt(username);
        return await actions.create_player(account, new FieldElement(encodedUsername));
    }

    public async void CreateGame()
    {
        if (account == null)
        {
            SimulateControllerConnection("test_username");
            return;
        }

        var txnHash = await actions.create_game(account);
        await provider.WaitForTransaction(txnHash);
    }

    public async void EndGame()
    {
        await actions.end_game(account);
        UIManager.instance.HandleExitGame();
    }

    public void Move(int direction)
    {
        Direction dir = (Direction)Direction.FromIndex(typeof(Direction), direction);
        if (!WorldSimulator.instance.CanMove(dir)) { return; }
        WorldSimulator.instance.SimulateMove(dir);
    }

    void OnPlayerDataUpdate()
    {
        if (playerEntity == null) { Debug.Log("Player entity is null"); return; }

        var playerData = playerEntity.GetComponent<depths_of_dread_PlayerData>();
        if (playerData == null) { return; }

        string usernameHex = playerData.username.Hex();
        UIManager.instance.HandleConnection(HexToASCII(usernameHex));

        Debug.Log($"Updated player data");
    }

    void OnPlayerStateUpdate()
    {
        var playerState = playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var playerData = playerEntity.GetComponent<depths_of_dread_PlayerData>();
        if (playerState == null || playerData == null) { return; }

        // Gameover is triggered
        // Temporary workaround until we can use events
        if (playerState.game_id == 0 && ScreenManager.instance.currentScreen == "GameOverlay")
        {
            Debug.Log("received gameover, sending flag to simulator");
            WorldSimulator.instance.floorEndEvent = true;
        }

        // Floor is cleared
        // Temporary workaround until we can use events
        if (localCurrentFloor < playerState.current_floor && playerState.current_floor > 1)
        {
            Debug.Log("received new floor, sending flag to simulator");
            localCurrentFloor = playerState.current_floor;
            WorldSimulator.instance.floorEndEvent = true;
        }
    }

    void OnPlayerPowerUpsUpdate()
    {
        // Debug.Log($"Updated player powerups");
    }

    void OnGameDataUpdate()
    {
        // Debug.Log($"Updated game data");
    }

    void OnGameFloorUpdate()
    {
        var gameFloor = gameEntity.GetComponent<depths_of_dread_GameFloor>();

        if (gameFloor.size.x == 0) {
            UIManager.instance.HandleError("gamefloor size is zero");
        }
    }

    void OnGameCoinsUpdate()
    {
        // Debug.Log($"Updated game coins");
    }

    void OnGameObstaclesUpdate()
    {
        // Debug.Log($"Updated game obstacles");
    }
}
