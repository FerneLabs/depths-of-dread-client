using System;
using TMPro;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static EncodingService;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] GameObject[] uiElements;
    [SerializeField] GameObject grid;
    [SerializeField] Tilemap tilemap;
    [SerializeField] Tile groundTile;
    [SerializeField] GameObject character;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void SetActive(GameObject[] gameObjects, bool enable)
    {
        foreach (var gameObject in gameObjects)
        {
            foreach (var element in uiElements)
            {
                if (element == gameObject) element.SetActive(enable);
            }
        }
    }

    public void SetActive(string[] tags, bool enable)
    {
        foreach (var tag in tags)
        {
            foreach (var element in uiElements)
            {
                if (element.CompareTag(tag)) element.SetActive(enable);
            }
        }
    }

    public void SetText(string tag, string text)
    {
        foreach (var element in uiElements)
        {
            if (element.CompareTag(tag)) element.GetComponent<TextMeshProUGUI>().text = text;
        }
    }

    public void HandleConnection(string username)
    {
        SetActive(new string[] { "MS-ProfileButton", "MS-LogoutButton", "LB-ProfileButton", "LB-LogoutButton" }, true);
        SetActive(new string[] { "MS-ConnectButton", "LB-ConnectButton" }, false);
        SetText($"MS-UsernameText", username);
        SetText($"LB-UsernameText", username);
    }

    public void HandleDisconnection()
    {
        SetActive(new string[] { "MS-ProfileButton", "MS-LogoutButton", "LB-ProfileButton", "LB-LogoutButton" }, false);
        SetActive(new string[] { "MS-ConnectButton", "LB-ConnectButton" }, true);
        SetText($"MS-UsernameText", "connect");
        SetText($"LB-UsernameText", "connect");
    }

    public void RenderGameGrid(depths_of_dread_GameFloor gameFloor)
    {
        Vector2 gridInitialPosition3x3 = new(-3, -3);
        Vector3 gridInitialScale3x3 = new(2, 2, 1);

        Vector2 gridInitialPosition4x4 = new(-3, -2.5f);
        Vector3 gridInitialScale4x4 = new(1.5f, 1.5f, 1);

        Vector2 gridInitialPosition5x5 = new(-2.85f, -2.25f);
        Vector3 gridInitialScale5x5 = new(1.15f, 1.15f, 1);

        if (gameFloor.size.x == 2)
        {
            grid.transform.position = gridInitialPosition3x3;
            grid.transform.localScale = gridInitialScale3x3;
        }

        if (gameFloor.size.x == 3)
        {
            grid.transform.position = gridInitialPosition4x4;
            grid.transform.localScale = gridInitialScale4x4;
        }

        if (gameFloor.size.x == 4)
        {
            grid.transform.position = gridInitialPosition5x5;
            grid.transform.localScale = gridInitialScale5x5;
        }

        for (int x = 0; x < gameFloor.size.x + 1; x++)
        {
            for (int y = 0; y < gameFloor.size.y + 1; y++)
            {
                Vector3Int coordinate = new(x, y, 0);
                tilemap.SetTile(coordinate, groundTile);
            }
        }
        Debug.Log($"Rendered game grid {gameFloor.size.x + 1}x{gameFloor.size.y + 1}");
    }

    public void HandleStateUpdate(depths_of_dread_PlayerData playerData, depths_of_dread_PlayerState playerState)
    {
        SetText("GS-UsernameText", HexToASCII(playerData.username.Hex()));
        SetText("GS-GameFloorText", $"Floor: {playerState.current_floor}");
        SetText("GS-CoinsText", $"Coins: {playerState.coins}");

        
        Vector3 targetPosition = new(playerState.position.x, playerState.position.y, 0);
        character.GetComponent<MovementScript>().Move(targetPosition);
    }

    public void HandleGameover()
    {
        tilemap.ClearAllTiles();
        grid.transform.localScale = new Vector3(1, 1, 1);
        grid.transform.position = new Vector3(0, 0, 0);
        ScreenManager.instance.SetActiveScreen("MainScreen");
    }

    public void HandleExitGame()
    {
        tilemap.ClearAllTiles();
        grid.transform.localScale = new Vector3(1, 1, 1);
        grid.transform.position = new Vector3(0, 0, 0);
        ScreenManager.instance.SetActiveScreen("MainScreen");
    }
}
