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
    private Account account;
    public GameObject playerEntity;
    public GameObject gameEntity;
    private int localCurrentFloor = 1; // Temporary workaround until we can use events.

    void Start()
    {
        worldManager.synchronizationMaster.OnEntitySpawned.AddListener(HandleSpawn);
        // worldManager.synchronizationMaster.OnModelUpdated.AddListener(HandleUpdate);
        // worldManager.synchronizationMaster.OnEventMessage.AddListener(HandleEvent);
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
        var playerKey = account == null ? null : GetPoseidonHash(account.Address);
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

        var playerState = playerEntity == null ? null : playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var gameKey = playerState == null ? null : GetPoseidonHash(new FieldElement(playerState.game_id));
        await Task.Yield();

        var gEntity = GameObject.Find(gameKey);

        if (gEntity != null && gEntity != gameEntity)
        {
            gameEntity = gEntity;
            OnGameDataUpdate();
            OnGameFloorUpdate();
            OnGameCoinsUpdate();

            Debug.Log($"Synced gameEntity {gameEntity}");
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

        if (gameEntity != null)
        {
            ScreenManager.instance.SetActiveScreen("GameOverlay");
            UIManager.instance.HandleNewFloor();
        }
        else
        {
            Debug.LogWarning("Game entity is null");
        }
    }

    public async void EndGame()
    {
        await actions.end_game(account);
        UIManager.instance.HandleExitGame();
    }

    public async void Move(int direction)
    {
        Direction dir = (Direction)Direction.FromIndex(typeof(Direction), direction);
        await actions.move(account, dir);
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

        // Redirect to Game screen if player has an ongoing game
        if (playerState.game_id != 0 && ScreenManager.instance.currentScreen != "GameOverlay")
        {
            ScreenManager.instance.SetActiveScreen("GameOverlay");
            UIManager.instance.HandleNewFloor();
            UIManager.instance.HandleStateUpdate(playerData, playerState);
            return;
        }

        // Gameover is triggered
        if (playerState.game_id == 0 && ScreenManager.instance.currentScreen == "GameOverlay")
        {
            UIManager.instance.HandleGameover();
            return;
        }

        // Update UI only if we are in Game screen
        if (ScreenManager.instance.currentScreen != "GameOverlay")
        {
            return;
        }

        // Floor is cleared
        // Temporary workaround until we can use events
        if (localCurrentFloor < playerState.current_floor && playerState.current_floor > 1)
        {
            localCurrentFloor = playerState.current_floor;
            UIManager.instance.HandleFloorCleared(playerState);
        }

        UIManager.instance.HandleStateUpdate(playerData, playerState);
        Debug.Log($"Updated player state");
    }

    void OnPlayerPowerUpsUpdate()
    {
        Debug.Log($"Updated player powerups");
    }

    void OnGameDataUpdate()
    {

        Debug.Log($"Updated game data");
    }

    void OnGameFloorUpdate()
    {
        var gameFloor = gameEntity.GetComponent<depths_of_dread_GameFloor>();

        if (gameFloor.size.x == 0) {
            UIManager.instance.HandleError("gamefloor size is zero");
        }
        // if (gameFloor == null) { Debug.Log("Game floor is null"); return; }
        // if (gameFloor.game_id != playerState.game_id) { 
        //     Debug.LogWarning("Game floor ID does not match with playerState ID. Force syncing entity state.");
        //     SyncLocalEntities(); 
        //     Debug.Log($"Entity mismatch corrected? {gameFloor.game_id == playerState.game_id}");
        // }

        // Debug.Log($"Going to render floor for ID {gameFloor.game_id}, size {gameFloor.size.x + 1}x{gameFloor.size.y + 1}");
        // UIManager.instance.HandleNewFloor();
        // Debug.Log($"Updated game floor");
    }

    void OnGameCoinsUpdate()
    {
        var gameCoins = gameEntity.GetComponent<depths_of_dread_GameCoins>();
        UIManager.instance.RenderCoins(gameCoins.coins);
        Debug.Log($"Updated game coins");
    }

    void OnGameObstaclesUpdate()
    {
        Debug.Log($"Updated game obstacles");
    }
}
