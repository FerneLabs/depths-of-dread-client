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
    private TMP_Text usernameText;
    private TMP_Text playerPositionText;
    private TMP_Text playerCoinsText;
    private TMP_Text gameIDText;
    private TMP_Text gameFloorText;
    private GameObject playerEntity;
    private GameObject gameEntity;

    void Start()
    {
        // playerPositionText = GameObject.FindGameObjectWithTag("PlayerPositionText").GetComponent<TMP_Text>();
        // playerCoinsText = GameObject.FindGameObjectWithTag("PlayerCoinsText").GetComponent<TMP_Text>();
        // gameIDText = GameObject.FindGameObjectWithTag("GameIDText").GetComponent<TMP_Text>();
        // gameFloorText = GameObject.FindGameObjectWithTag("GameFloorText").GetComponent<TMP_Text>();

        worldManager.synchronizationMaster.OnEntitySpawned.AddListener(HandleSpawn);
        worldManager.synchronizationMaster.OnModelUpdated.AddListener(HandleUpdate);
    }

    public async void SimulateControllerConnection(string username) {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = new Account(
            provider, 
            new SigningKey(dojoWorkerData.masterPrivateKey), 
            new FieldElement(dojoWorkerData.masterAddress)
        );
    
        await CreatePlayer(username);
    }

    public void SimulateControllerDisconnection() {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = null;
        playerEntity = null;

        UIManager.instance.SetActive(new string[] {"ProfileButton", "LogoutButton"}, false);
        UIManager.instance.SetActive(new string[] {"ConnectButton"}, true);
    }

    public async Task<bool> CreatePlayer(string username) {
        BigInteger encodedUsername = ASCIIToBigInt(username);
        await actions.create_player(account, new FieldElement(encodedUsername));
        return true;
    }

    public async void CreateGame() {
        await actions.create_game(account);
    }

    public async void EndGame() {
        await actions.end_game(account);
    }

    public async void Move(int direction) {
        Direction dir = (Direction)Direction.FromIndex(typeof(Direction), direction);
        await actions.move(account, dir);
    }

    void HandleSpawn(GameObject spawnedEntity)
    {
        var playerState = playerEntity == null ? null : playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var playerKey = account == null ? null : GetPoseidonHash(account.Address);
        var gameKey = playerState == null ? null : GetPoseidonHash(new FieldElement(playerState.game_id));
        
        if (spawnedEntity == null) { return; }
        
        if (spawnedEntity.name == playerKey)
        {
            playerEntity = spawnedEntity;
            OnPlayerDataUpdate();
            OnPlayerStateUpdate();
            OnPlayerPowerUpsUpdate();

            Debug.Log($"Initialized playerEntity {playerEntity}");
        }
        
        if (spawnedEntity.name == gameKey) 
        {
            gameEntity = spawnedEntity;
            OnGameDataUpdate();
            OnGameFloorUpdate();
            OnGameCoinsUpdate();
        }  
    }

    void HandleUpdate(ModelInstance updatedModel)
    {
        switch (updatedModel.GetType().Name) {
            // Player Entity Handlers
            case "depths_of_dread_PlayerData":
                InitEntity("player");
                OnPlayerDataUpdate();
                break;
            case "depths_of_dread_PlayerState":
                //InitIfNot("player");
                OnPlayerStateUpdate();
                break;
            case "depths_of_dread_PlayerPowerUps":
                //InitIfNot("player");
                OnPlayerPowerUpsUpdate();
                break;

            // Game Entity Handlers
            case "depths_of_dread_GameData":
                //InitIfNot("game");
                OnGameDataUpdate();
                break;
            case "depths_of_dread_GameFloor":
                //InitIfNot("game");
                OnGameFloorUpdate();
                break;
            case "depths_of_dread_GameCoins":
                //InitIfNot("game");
                OnGameCoinsUpdate();
                break;
            case "depths_of_dread_GameObstacles":
                //InitIfNot("game");
                OnGameObstaclesUpdate();
                break;

            default:
                Debug.LogWarning($"Received unknown model type: {updatedModel.GetType().Name}");
                break;
        }
    }

    void InitEntity(string entitySelector) 
    {
        var playerState = playerEntity == null ? null : playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var playerKey = account == null ? null : GetPoseidonHash(account.Address);
        var gameKey = playerState == null ? null : GetPoseidonHash(new FieldElement(playerState.game_id));
        var pEntity = GameObject.Find(playerKey);
        var gEntity = GameObject.Find(gameKey);
        
        // Only Init if the local entity has not been initialized 
        // and if the entity matches the current player hashed key
        if (entitySelector == "player" && playerEntity == null)
        {
            if (pEntity != null)
            {
                playerEntity = pEntity;
                OnPlayerDataUpdate();
                OnPlayerStateUpdate();
                OnPlayerPowerUpsUpdate();

                Debug.Log($"Initialized playerEntity {playerEntity}");
            }
        }

        if (entitySelector == "game" && gameEntity == null)
        {
            if (gEntity != null) 
            {
                gameEntity = gEntity;
                OnGameDataUpdate();
                OnGameFloorUpdate();
                OnGameCoinsUpdate();
            } 
        }
    }

    void OnPlayerDataUpdate() {
        if (playerEntity == null) { Debug.Log("Player entity is null"); return; }

        var playerData = playerEntity.GetComponent<depths_of_dread_PlayerData>();
        if (playerData == null) { return; }

        string usernameHex = playerData.username.Hex();

        UIManager.instance.SetActive(new string[] {"ProfileButton", "LogoutButton"}, true);
        UIManager.instance.SetActive(new string[] {"ConnectButton"}, false);
        UIManager.instance.SetText("UsernameText", HexToASCII(usernameHex));

        Debug.Log($"Updated {playerEntity} data");
    }

    void OnPlayerStateUpdate() {
        var playerState = playerEntity.GetComponent<depths_of_dread_PlayerState>();
        if (playerState == null) { return; }

        UIManager.instance.SetText("GameIDText", $"Game ID: {playerState.game_id}");
        UIManager.instance.SetText("GameFloorText", $"Floor: {playerState.current_floor}");
        UIManager.instance.SetText("PlayerCoinsText", $"CoinsD: {playerState.coins}");

        Debug.Log($"Updated {playerEntity} state");
    }

    void OnPlayerPowerUpsUpdate() {
        Debug.Log($"Updated {playerEntity} powerups");
    }

    void OnGameDataUpdate() {
        Debug.Log($"Updated {gameEntity} data");
    }

    void OnGameFloorUpdate() {
        Debug.Log($"Updated {gameEntity} floor");
    }

    void OnGameCoinsUpdate() {
        Debug.Log($"Updated {gameEntity} coins");
    }

    void OnGameObstaclesUpdate() {
        Debug.Log($"Updated {gameEntity} obstacles");
    }
}
