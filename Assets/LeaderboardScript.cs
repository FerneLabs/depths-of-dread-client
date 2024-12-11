using System;
using Dojo;
using TMPro;
using UnityEngine;
using static EncodingService;

public class LeaderboardScript : MonoBehaviour
{
    [SerializeField] WorldManager worldManager;
    [SerializeField] GameObject leaderboardItemPrefab;
    [SerializeField] GameObject leaderboardItemDefault;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var leaderboardEntries = worldManager.Entities<depths_of_dread_GameData>();
        var playerEntries = worldManager.Entities<depths_of_dread_PlayerData>();

        depths_of_dread_GameData[] gameDatas = new depths_of_dread_GameData[leaderboardEntries.Length];
        depths_of_dread_PlayerData[] playerDatas = new depths_of_dread_PlayerData[playerEntries.Length];

        for (int i = 0; i < gameDatas.Length; i++)
        {
            gameDatas[i] = leaderboardEntries[i].GetComponent<depths_of_dread_GameData>();
        }
        for (int i = 0; i < playerDatas.Length; i++)
        {
            playerDatas[i] = playerEntries[i].GetComponent<depths_of_dread_PlayerData>();
        }

        // Sort entries from highest to lowest score and smaller to larger runtime
        Array.Sort(gameDatas, (a, b) =>
        {
            if (b.total_score == a.total_score)
            {
                var aTime = a.end_time - a.start_time;
                var bTime = b.end_time - b.start_time;
                return (int)(aTime - bTime);
            }
            return b.total_score - a.total_score;
        });

        // Shorten to only 20 entries
        gameDatas = gameDatas.Length > 20 ? gameDatas[..20] : gameDatas;  

        if (gameDatas.Length > 0) 
        {
            leaderboardItemDefault.SetActive(false);
        }
        
        for (int i = 0; i < gameDatas.Length; i++)
        {
            var instance = Instantiate(leaderboardItemPrefab, transform);

            TMP_Text[] textComponents = instance.GetComponentsInChildren<TMP_Text>();
            foreach (TMP_Text textComponent in textComponents)
            {
                switch (textComponent.gameObject.tag)
                {
                    case "LB-Item-PositionText":
                        textComponent.text = $"#{i + 1}";
                        break;
                    case "LB-Item-UsernameText":
                        // Show player address as fallback in case username is not found
                        var username = GetAddressUsername(gameDatas[i].player.Hex(), playerDatas);
                        textComponent.text = username ?? gameDatas[i].player.Hex();
                        break;
                    case "LB-Item-ScoreText":
                        textComponent.text = $"{gameDatas[i].total_score}";
                        break;
                    case "LB-Item-TimeText":
                        var time = gameDatas[i].end_time - gameDatas[i].start_time;
                        textComponent.text = SecondsToTime((int)time);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private string GetAddressUsername(string playerAddress, depths_of_dread_PlayerData[] playerDatas)
    {
        string matchingPlayer = null;
        foreach (var playerData in playerDatas)
        {
            if (playerAddress == playerData.player.Hex())
            {
                matchingPlayer = HexToASCII(playerData.username.Hex());
            }
        }

        return matchingPlayer;
    }

    string SecondsToTime(int seconds)
    {
        if (seconds == 0) return string.Empty;

        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;

        if (minutes > 0)
        {
            return $"{minutes}m {Math.Abs(remainingSeconds)}s";
        }
        else
        {
            return $"{Math.Abs(remainingSeconds)}s";
        }
    }
}
