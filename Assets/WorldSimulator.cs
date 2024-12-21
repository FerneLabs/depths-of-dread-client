using System;
using UnityEditor.PackageManager;
using UnityEngine;

public class WorldSimulator : MonoBehaviour
{
    public static WorldSimulator instance;

    public depths_of_dread_PlayerState playerState;
    public depths_of_dread_GameFloor gameFloor;
    public depths_of_dread_GameCoins gameCoins;
    public depths_of_dread_GameObstacles gameObstacles;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public bool InitializeInstance(
        depths_of_dread_PlayerState playerState,
        depths_of_dread_GameFloor gameFloor,
        depths_of_dread_GameCoins gameCoins,
        depths_of_dread_GameObstacles gameObstacles)
    {
        if (playerState == null || gameFloor == null || gameCoins == null || gameObstacles == null)
        {
            Debug.LogError("Received null data");
            return false;
        }
        
        try {
            this.playerState = playerState;
            this.gameFloor = gameFloor;
            this.gameCoins = gameCoins;
            this.gameObstacles = gameObstacles;
            return true;
        } catch (Exception err){
            Debug.LogError($"Failed to initialize world simulator {err.Message}");
            return false;
        }
    }

    public bool IsInitialized()
    {
        return playerState != null
            && gameFloor != null
            && gameCoins != null
            && gameObstacles != null;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
