using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    [Serializable]
    public class PlayerDataInfo_Array { 
        public PlayerDataInfo[] _playerDataInfoArray;
    }

    [Serializable]
    public class PlayerDataInfo 
    {
        public int id = 1;
        public string name = "PEDRO";
        public int rounds_won = 0;
    }

    [Serializable]
    public class PlayerPreferences 
    {
        public int playerId;
        public string characterId; // "Wolf" ou "Bunny"
        public string cosmeticId;  // ID do Chapéu
        public string[] unlockedHats; // List of unlocked hat IDs
    }

    public static PlayerDataInfo_Array CreateClassFromJson(string json)
    {
        if (json.StartsWith("[")) json = "{\"_playerDataInfoArray\":" + json + "}";
        return JsonUtility.FromJson<PlayerDataInfo_Array>(json);
    }   

    public static string CreateJsonFromClass(PlayerDataInfo playerDataInfo)
    {
        return JsonUtility.ToJson(playerDataInfo);
    }

    public static string CreateJsonFromPrefs(PlayerPreferences prefs)
    {
        return JsonUtility.ToJson(prefs);
    }
}
